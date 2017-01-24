/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BGBLE.BGAPI
{
    public enum BGAPIDiscoverMode : byte {
        Limited = 0x00,     //Discover only limited discoverable devices, that is, Slaves which have the LE Limited Discoverable Mode bit set in the Flags AD type of their advertisement packets
        Generic = 0x01,     //Discover limited and generic discoverable devices, that is, Slaves which have the LE Limited Discoverable Mode or the LE General Discoverable Mode bit set in the Flags AD type of their advertisement packets
        Observation = 0x02  //Discover all devices regardless of the Flags AD type, so also devices in non-discoverable mode will be reported to host
    }
    public enum BGAPIBluetoothAddressType : byte {
        Public = 0x00,      //Public Address
        Random = 0x01       //Random Address
    }
    public struct BGAPIBLEDeviceInfo
    {
        public BGAPIBluetoothAddressType addressType;
        public string address;
        public byte bond;
        public ulong connectableAdvertisementPacket;
        public ulong discoverableAdvertisementPacket;
        public ulong nonConnectableAdvertisementPacket;
        public ulong scanResponsePacket;
        public byte[] flags;
        public byte[] manufacturerSpecificData;
        public double maxConnectionInterval;
        public double minConnectionInterval;
        public string name;
        public sbyte rssi;
        public byte txPower;
        public List<string> services;
    }
    public struct BGAPIConnectionResult
    {
        public byte connectionHandle;
        public ushort error;

        public BGAPIConnectionResult(byte _connectionHandle, ushort _error)
        {
            connectionHandle = _connectionHandle;
            error = _error;
        }
    }

    public class BGAPIGAPCommandClassScanEventArgs : EventArgs
    {
        public BGAPIBLEDeviceInfo DeviceInfo { get; set; }
    }
    public delegate void BGAPIGAPCommandClassScanEventHandler(object sender, BGAPIGAPCommandClassScanEventArgs e);

    /// <summary>The Generic Access Profile (GAP) class provides methods to control the Bluetooth GAP level functionality of
    /// the local device.The GAP call for example allows remote device discovery, connection establishment and local
    /// devices connection and discovery modes.The GAP class also allows the control of local devices privacy
    /// modes.</summary>
    /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.4 Generic Access Profile)</seealso>
    class BGAPIGAPCommandClass : BGAPICommandClass
    {
        public const byte CLASS_ID = BGAPIDefinition.CCID_GAP;

        public event BGAPIGAPCommandClassScanEventHandler DeviceFound;
        public BGAPIGAPCommandClass(BGAPIConnection connection) : base(connection) {
            _connection.RegisterEventHandlerForCommandClass(CLASS_ID, (BGAPIConnectionEventData eventData) => {
                switch (eventData.header.commandId)
                {
                    case BGAPIDefinition.GAP_EVENT_SCAN:
                        BGAPIBLEDeviceInfo info = new BGAPIBLEDeviceInfo();

                        byte[] data = eventData.payload;
                        //RSSI
                        info.rssi = (sbyte)data[0];
                        //PACKET TYPE
                        info.connectableAdvertisementPacket = 0;
                        info.nonConnectableAdvertisementPacket = 0;
                        info.scanResponsePacket = 0;
                        info.discoverableAdvertisementPacket = 0;
                        switch (data[1])
                        {
                            case 0x2:
                                info.nonConnectableAdvertisementPacket += 1;
                                break;
                            case 0x4:
                                info.scanResponsePacket += 1;
                                break;
                            case 0x6:
                                info.discoverableAdvertisementPacket += 1;
                                break;
                            default:
                                info.connectableAdvertisementPacket += 1;
                                break;
                        }
                        //MAC
                        info.address = BitConverter.ToString(data.Skip(2).Take(6).ToArray().Reverse().ToArray()).Replace('-', ':');
                        //ADDRESS TYPE (PUBLIC< RANDOM)
                        BGAPIBluetoothAddressType addressType = ((data[8] == (byte)BGAPIBluetoothAddressType.Random) ? BGAPIBluetoothAddressType.Random : BGAPIBluetoothAddressType.Public);
                        info.addressType = addressType;
                        //BOND - Bond handle if there is known bond for this device, 0xff otherwise
                        info.bond = data[9];
                        //PARSING SCAN RESPONSE DATA
                        ushort _count = (ushort)(eventData.header.payloadLength - 10);
                        byte[] _data = data.Skip(10).Take(_count).ToArray();
                        info.name = "";
                        info.maxConnectionInterval = 0.0;
                        info.minConnectionInterval = 0.0;
                        info.txPower = 0;
                        info.services = new List<string>();
                        //GAP 
                        if (_count > 3)
                        {
                            for (byte i = 1; i < _data[0];)
                            {
                                byte length = (byte)(_data[i] - 1);
                                byte type = _data[i + 1];
                                switch (type)
                                {
                                    case 0x01://Flags
                                        info.flags = _data.Skip(i + 2).Take(length).ToArray();
                                        break;
                                    case 0x02://Incomplete List of 16-bit Service Class UUIDs
                                        for (byte _i = 0; _i < length; _i += 2)
                                        {
                                            string _service = BitConverter.ToString(_data.Skip(i + _i + 2).Take(2).Reverse().ToArray()).Replace("-", "");
                                            info.services.Add(_service);
                                        }
                                        break;
                                    case 0x03://Complete List of 16-bit Service Class UUIDs
                                    case 0x07://Complete List of 128-bit Service Class UUIDs
                                        string service = BitConverter.ToString(_data.Skip(i + 2).Take(length).Reverse().ToArray()).Replace("-", "");
                                        info.services.Add(service);
                                        break;
                                    case 0x08://Shortened Local Name
                                    case 0x09://Complete Local Name
                                        info.name = Encoding.UTF8.GetString(_data.Skip(i + 2).Take(length).ToArray());
                                        break;
                                    case 0x12://Slave Connection Interval Range
                                        byte[] connectionInterval = _data.Skip(i + 2).Take(length).ToArray();
                                        ushort _minConnectionInterval = BitConverter.ToUInt16(connectionInterval, 0);
                                        ushort _maxConnectionInterval = BitConverter.ToUInt16(connectionInterval, 2);
                                        info.maxConnectionInterval = 1.25 * _maxConnectionInterval;
                                        info.minConnectionInterval = 1.25 * _minConnectionInterval;
                                        break;
                                    case 0x0A://Tx Power Level
                                        info.txPower = _data[i + 2];
                                        break;
                                    case 0xFF://Manufacturer Specific Data
                                        info.manufacturerSpecificData = _data.Skip(i + 2).Take(length).ToArray();
                                        break;
                                    default:
                                        break;
                                }
                                i += (byte)(_data[i] + 1);
                            }
                        }

                        BGAPIGAPCommandClassScanEventArgs eventArgs = new BGAPIGAPCommandClassScanEventArgs();
                        eventArgs.DeviceInfo = info;
                        DeviceFound?.Invoke(this, eventArgs);
                        break;
                    default:
                        break;
                }
            });
        }

        /// <summary>This command will start the GAP direct connection establishment procedure to a dedicated Bluetooth Smart device.
        /// The Bluetooth module will enter a state where it continuously scans for the connectable advertisement packets
        /// from the remote device which matches the Bluetooth address gives as a parameter.Upon receiving the
        /// advertisement packet, the module will send a connection request packet to the target device to imitate a
        /// Bluetooth connection.A successful connection will bi indicated by a Status event.
        /// If the device is configured to support more than one connection, the smallest connection interval which is
        /// divisible by maximum_connections * 2.5ms will be selected. Thus, it is important to provide minimum and
        /// maximum connection intervals so that such a connection interval is available within the range.
        /// The connection establishment procedure can be cancelled with End Procedure command.</summary>
        /// <param name="bluetoothAddress">String with MAC address with format - 00:00:00:00:00:00</param>
        /// <param name="bluetoothAddressType">One of BGAPIBluetoothAddressType: Public, Random</param>
        /// <returns>Returns structure with connection handle and error code - 0x00 means connection procedure successfully started.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.4 Generic Access Profile)</seealso>
        public BGAPIConnectionResult ConnectDirect(string bluetoothAddress, BGAPIBluetoothAddressType bluetoothAddressType)
        {
            ushort payloadLength = 15;
            byte[] data = new byte[payloadLength];
            //MAC address
            string _bluetoothAddress = bluetoothAddress.Replace(":", "");
            byte[] __bluetoothAddress = Enumerable.Range(0, _bluetoothAddress.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(_bluetoothAddress.Substring(x, 2), 16)).Reverse().ToArray();
            Array.Copy(__bluetoothAddress, 0, data, 0, 6);
            data[6] = (byte)bluetoothAddressType;

            //Minimum connection interval (in units of 1.25ms). Range: 6 - 3200. 7.50ms - 4000ms
            ushort connectionIntervalMin = 60;
            byte[] _connectionIntervalMin = BitConverter.GetBytes(connectionIntervalMin).ToArray();
            Array.Copy(_connectionIntervalMin, 0, data, 7, 2);

            //Maximum connection interval (in units of 1.25ms). Range: 6 - 3200. Must be equal or bigger than minimum connection interval.
            ushort connectionIntervalMax = 76;
            byte[] _connectionIntervalMax = BitConverter.GetBytes(connectionIntervalMax).ToArray();
            Array.Copy(_connectionIntervalMax, 0, data, 9, 2);

            //Supervision timeout (in units of 10ms). The supervision timeout defines how long the devices can be out of range before the connection is closed. Range: 10 - 3200. 100ms - 32000ms
            ushort timeout = 100;
            byte[] _timeout = BitConverter.GetBytes(timeout).ToArray();
            Array.Copy(_timeout, 0, data, 11, 2);

            //Slave latency defines how many connection intervals a slave device can skip. Range: 0 - 500. 0 : Slave latency is disabled.
            ushort latency = 0;
            byte[] _latency = BitConverter.GetBytes(latency).ToArray();
            Array.Copy(_latency, 0, data, 13, 2);

            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.GAP_COMMAND_CONNECT_DIRECT, data, payloadLength);
            if (response.length == 3)
            {
                return new BGAPIConnectionResult(response.data[0], BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0));
            }
            return new BGAPIConnectionResult(0x00, 0xFFFE);
        }

        /// <summary>This command starts the GAP discovery procedure to scan for advertising devices i.e. to perform a device discovery.
        /// Scanning parameters can be configured with the Set Scan Parameters command before issuing this command.
        /// To cancel on an ongoing discovery process use the End Procedure command.</summary>
        /// <param name="mode">One of BGAPIDiscoverMode: Limited, Generic, Observation</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.4 Generic Access Profile)</seealso>
        public ushort Discover(BGAPIDiscoverMode mode)
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.GAP_COMMAND_DISCOVER, new byte[]{ (byte)mode }, 0x01);
            if(response.length == 2)
            {
                return BitConverter.ToUInt16(response.data.Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command ends the current GAP discovery procedure and stop the scanning of advertising devices.</summary>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.4 Generic Access Profile)</seealso>
        public ushort EndProcedure()
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.GAP_COMMAND_END_PROCEDURE, new byte[] { }, 0x00);
            if (response.length == 2)
            {
                return BitConverter.ToUInt16(response.data.Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command sets the scan parameters which affect how other Bluetooth Smart devices are discovered.</summary>
        /// <param name="isActiveScanning">Controls active scanning mode - FALSE: Passive scanning is used. No scan request is made, TRUE: Active scanning is used. When an advertisement packet is received the Bluetooth stack will send a scan request packet to the advertiser to try and read the scan response data</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.4 Generic Access Profile)</seealso>
        public ushort SetScanParameters(bool isActiveScanning)
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.GAP_COMMAND_SET_SCAN_PARAMETERS, new byte[] { 0x4B, 0x00, 0x32, 0x00, (byte)((isActiveScanning) ? 0x01 : 0x00) }, 0x05);
            if (response.length == 2)
            {
                return BitConverter.ToUInt16(response.data.Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }
    }
}
