/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Linq;

namespace BGBLE.BGAPI
{
    /// <summary>Connection Status Flags, multiple flags can be set at a time.</summary>
    public struct BGAPIBLEDeviceConnectionStatus
    {
        /// <summary>Connection completed flag, which is used to tell a new connection has been created.</summary>
        public bool isCompleted;

        /// <summary>This status flag tells the connection exists to a remote device.</summary>
        public bool isConnected;

        /// <summary>This flag tells the connection is encrypted.</summary>
        public bool isEncrypted;

        /// <summary>This flag tells that connection parameters have changed and. It is set when connection parameters have changed due to a link layer operation.</summary>
        public bool isParametersChanged;

        public BGAPIBLEDeviceConnectionStatus(bool _isCompleted, bool _isConnected, bool _isEncrypted, bool _isParametersChanged)
        {
            isCompleted = _isCompleted;
            isConnected = _isConnected;
            isEncrypted = _isEncrypted;
            isParametersChanged = _isParametersChanged;
        }
    };

    public class BGAPIConnectionCommandClassDisconnectEventArgs : EventArgs
    {
        /// <summary>Connection handle.</summary>
        public byte ConnectionHandle { get; set; }

        /// <summary>Disconnection reason code - 0 : disconnected by local user.</summary>
        public ushort ReasonCode { get; set; }
    }
    public delegate void BGAPIConnectionCommandClassDisconnectEventHandler(object sender, BGAPIConnectionCommandClassDisconnectEventArgs e);


    public class BGAPIConnectionCommandClassStatusEventArgs : EventArgs
    {
        /// <summary>Remote devices Bluetooth address (MAC).</summary>
        public string Address { get; set; }

        /// <summary>Remote address type - BGAPIBluetoothAddressType: Public, Random.</summary>
        public BGAPIBluetoothAddressType AddressType { get; set; }

        /// <summary>Bonding handle if the device has been bonded with. Otherwise: 0xFF</summary>
        public byte BondingHandle { get; set; }

        /// <summary>Connection handle.</summary>
        public byte ConnectionHandle { get; set; }

        /// <summary>Connection Status from Flags.</summary>
        public BGAPIBLEDeviceConnectionStatus ConnectionStatus { get; set; }

        /// <summary>Connection status flags. The possible connection status flags are described in the table below. The flags field is a bit mask, so multiple
        /// flags can be set at a time.If the bit is 1 the flag is active and if the bit is 0 the flag is inactive.
        /// bit 0 - connection_connected - This status flag tells the connection exists to a remote device.
        /// bit 1 - connection_encrypted - This flag tells the connection is encrypted.
        /// bit 2 - connection_completed - Connection completed flag, which is used to tell a new connection has been created.
        /// bit 3 - connection_parameters_change -  This flag tells that connection parameters have changed and. It is set when connection parameters have changed due to a link layer operation.
        /// </summary>
        public byte Flags { get; set; }

        /// <summary>Slave latency which tells how many connection intervals the slave may skip.</summary>
        public ushort Latency { get; set; }

        /// <summary>Current connection interval (units of 1.25ms).</summary>
        public ushort ConnectionInterval { get; set; }

        /// <summary>Current supervision timeout (units of 10ms).</summary>
        public ushort Timeout { get; set; }
    }
    public delegate void BGAPIConnectionCommandClassStatusEventHandler(object sender, BGAPIConnectionCommandClassStatusEventArgs e);

    public class BGAPIConnectionCommandClassVersionUpdateEventArgs : EventArgs
    {
        /// <summary>Connection handle.</summary>
        public byte ConnectionHandle { get; set; }

        /// <summary>Bluetooth controller specification version.</summary>
        public byte ControllerSpecificationVersion { get; set; }

        /// <summary>Manufacturer of the Bluetooth controller.</summary>
        public ushort ManufacturerId { get; set; }

        /// <summary>Bluetooth controller version.</summary>
        public ushort ControllerVersion { get; set; }
    }
    public delegate void BGAPIConnectionCommandClassVersionUpdateEventHandler(object sender, BGAPIConnectionCommandClassVersionUpdateEventArgs e);

    /// <summary>The Connection class provides methods to manage Bluetooth connections and query their statuses.</summary>
    /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.3 Connection)</seealso>
    class BGAPIConnectionCommandClass : BGAPICommandClass
    {
        public const byte CLASS_ID = BGAPIDefinition.CCID_CONNECTION;

        public event BGAPIConnectionCommandClassDisconnectEventHandler Disconnected;
        public event BGAPIConnectionCommandClassStatusEventHandler StatusChanged;
        public event BGAPIConnectionCommandClassVersionUpdateEventHandler VersionUpdated;

        public BGAPIConnectionCommandClass(BGAPIConnection connection) : base(connection) {
            _connection.RegisterEventHandlerForCommandClass(CLASS_ID, (BGAPIConnectionEventData eventData) => {
                switch (eventData.header.commandId)
                {
                    case BGAPIDefinition.CONNECTION_EVENT_DISCONNECTED:
                        if (eventData.header.payloadLength == 3)
                        {
                            BGAPIConnectionCommandClassDisconnectEventArgs eventArgs = new BGAPIConnectionCommandClassDisconnectEventArgs();
                            eventArgs.ConnectionHandle = eventData.payload[0];
                            eventArgs.ReasonCode = BitConverter.ToUInt16(eventData.payload.Skip(1).Take(2).ToArray(), 0);
                            Disconnected?.Invoke(this, eventArgs);
                        }
                        break;
                    case BGAPIDefinition.CONNECTION_EVENT_FEATURE_IND:
                        if (eventData.header.payloadLength > 2)
                        {
                            //
                        }
                        break;
                    case BGAPIDefinition.CONNECTION_EVENT_STATUS:
                        if (eventData.header.payloadLength > 2)
                        {
                            BGAPIConnectionCommandClassStatusEventArgs eventArgs = new BGAPIConnectionCommandClassStatusEventArgs();
                            eventArgs.ConnectionHandle = eventData.payload[0];
                            eventArgs.Flags = eventData.payload[1];
                            eventArgs.Address = BitConverter.ToString(eventData.payload.Skip(2).Take(6).Reverse().ToArray()).Replace('-', ':');
                            eventArgs.AddressType = (BGAPIBluetoothAddressType)eventData.payload[8];
                            eventArgs.ConnectionInterval = BitConverter.ToUInt16(eventData.payload.Skip(9).Take(2).ToArray(), 0);
                            eventArgs.Timeout = BitConverter.ToUInt16(eventData.payload.Skip(11).Take(2).ToArray(), 0);
                            eventArgs.Latency = BitConverter.ToUInt16(eventData.payload.Skip(13).Take(2).ToArray(), 0);
                            eventArgs.BondingHandle = eventData.payload[15];

                            BGAPIBLEDeviceConnectionStatus connectionStatus = new BGAPIBLEDeviceConnectionStatus();
                            connectionStatus.isConnected = ((eventArgs.Flags & 0x01) == 0x01);
                            connectionStatus.isEncrypted = ((eventArgs.Flags & 0x02) == 0x02);
                            connectionStatus.isCompleted = ((eventArgs.Flags & 0x04) == 0x04);
                            connectionStatus.isParametersChanged = ((eventArgs.Flags & 0x08) == 0x08);
                            eventArgs.ConnectionStatus = connectionStatus;

                            StatusChanged?.Invoke(this, eventArgs);
                        }
                        break;
                    case BGAPIDefinition.CONNECTION_EVENT_VERSION_IND:
                        if (eventData.header.payloadLength == 6)
                        {
                            BGAPIConnectionCommandClassVersionUpdateEventArgs eventArgs = new BGAPIConnectionCommandClassVersionUpdateEventArgs();
                            eventArgs.ConnectionHandle = eventData.payload[0];
                            eventArgs.ControllerSpecificationVersion = eventData.payload[1];
                            eventArgs.ManufacturerId = BitConverter.ToUInt16(eventData.payload.Skip(2).Take(2).ToArray(), 0);
                            eventArgs.ControllerVersion = BitConverter.ToUInt16(eventData.payload.Skip(4).Take(2).ToArray(), 0);

                            VersionUpdated?.Invoke(this, eventArgs);
                        }
                        break;
                    default:
                        break;
                }
            });
        }

        /// <summary>This command disconnects an active Bluetooth connection. When link is disconnected a Disconnected event is produced.</summary>
        /// <param name="connectionHandle">Connection handle to close</param>
        /// <returns>Returns error code - 0x00 means disconnection procedure successfully started.</returns>
        /// <event name="Disconnected">This event is produced when a Bluetooth connection is disconnected.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.3 Connection)</seealso>
        public ushort Disconnect(byte connectionHandle)
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.CONNECTION_COMMAND_DISCONNECT, new byte[] { connectionHandle }, 0x01);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command returns the Receiver Signal Strength Indication (RSSI) related to the connection referred to by
        /// the connection handle parameter.If the connection is not open, then the RSSI value returned in the response
        /// packet will be 0x00, while if the connection is active, then it will be some negative value (2's complement form
        /// between 0x80 and 0xFF and never 0x00). Note that this command also returns an RSSI of 0x7F if you request
        /// RSSI on an invalid/unsupported handle.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <returns>Returns RSSI value of the connection in dBm. Range: -103 to -38</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.3 Connection)</seealso>
        public sbyte GetRSSI(byte connectionHandle)
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.CONNECTION_COMMAND_GET_RSSI, new byte[] { connectionHandle }, 0x01);
            if (response.length == 2)
            {
                return (sbyte)response.data[1];
            }
            return 0x00;
        }

        /// <summary>This command returns the status of the given connection. Status is returned in a Status event.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <returns>Returns TRUE or throws exception</returns>
        /// <event name="Status">This event indicates the connection status and parameters.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.3 Connection)</seealso>
        public bool GetStatus(byte connectionHandle)
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.CONNECTION_COMMAND_GET_STATUS, new byte[] { connectionHandle }, 0x01);
            if (response.length == 1)
            {
                return (connectionHandle == response.data[0]);
            }
            return false;
        }

        /// <summary>This command updates the connection parameters of a given connection. The parameters have the same
        /// meaning and follow the same rules as for the GAP class command : Connect Direct.
        /// If this command is issued at a master device, it will send parameter update request to the Bluetooth link layer.
        /// On the other hand if this command is issued at a slave device, it will send L2CAP connection parameter update
        /// request to the master, which may either accept or reject it.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <returns>Returns error code - 0x00 means disconnection procedure successfully started.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.3 Connection)</seealso>
        public ushort Update(byte connectionHandle)
        {
            ushort payloadLength = 9;
            byte[] data = new byte[payloadLength];

            data[0] = connectionHandle;

            //Minimum connection interval (in units of 1.25ms). Range: 6 - 3200. 7.50ms - 4000ms
            ushort connectionIntervalMin = 1600;
            byte[] _connectionIntervalMin = BitConverter.GetBytes(connectionIntervalMin).ToArray();
            Array.Copy(_connectionIntervalMin, 0, data, 1, 2);

            //Maximum connection interval (in units of 1.25ms). Range: 6 - 3200. Must be equal or bigger than minimum connection interval.
            ushort connectionIntervalMax = 1600;
            byte[] _connectionIntervalMax = BitConverter.GetBytes(connectionIntervalMax).ToArray();
            Array.Copy(_connectionIntervalMax, 0, data, 3, 2);

            //Slave latency defines how many connection intervals a slave device can skip. Range: 0 - 500. 0 : Slave latency is disabled.
            ushort latency = 10;
            byte[] _latency = BitConverter.GetBytes(latency).ToArray();
            Array.Copy(_latency, 0, data, 5, 2);

            //Supervision timeout (in units of 10ms). The supervision timeout defines how long the devices can be out of range before the connection is closed. Range: 10 - 3200. 100ms - 32000ms
            ushort timeout = 1000;
            byte[] _timeout = BitConverter.GetBytes(timeout).ToArray();
            Array.Copy(_timeout, 0, data, 7, 2);

            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.CONNECTION_COMMAND_UPDATE, data, payloadLength);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command requests a version exchange of a given connection.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <returns>Returns error code - 0x00 means disconnection procedure successfully started.</returns>
        /// <event name="Version Ind">This event indicates the remote devices version.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.3 Connection)</seealso>
        public ushort VersionUpdate(byte connectionHandle)
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.CONNECTION_COMMAND_VERSION_UPDATE, new byte[] { connectionHandle }, 0x01);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

    }
}
