/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

using BGBLE.BGAPI;

namespace BGBLE
{
    public class BGBLEDeviceInfoReceivedEventArgs : EventArgs
    {
        public BGBLEDevice Device { get; set; }

        public sbyte RSSI { get; set; }
    }
    public delegate void BGBLEDeviceInfoReceivedEventHandler(object sender, BGBLEDeviceInfoReceivedEventArgs e);

    public class BGBLECentralAdapterConnectionStateChangeEventArgs : EventArgs
    {
       
    }
    public delegate void BGBLECentralAdapterConnectionStateChangEventHandler(object sender, BGBLECentralAdapterConnectionStateChangeEventArgs e);

    /// <summary>This class implements BLE central which using BG API.</summary>
    public class BGBLECentral
    {
        private const string REQUIRED_ADAPTER_SOFTWARE_VERSION = "1.3.2.122";
        private const double STATE_UPDATE_INTERVAL = 2000;

        private string _address;
        private BGAPIConnection _connection;
        private Dictionary<string, BGBLEDevice> _devicesByAddress;
        private Dictionary<byte, BGBLEDevice> _devicesByConnectionHandle;
        private BGAPIHardwareInfo _hardwareInfo = new BGAPIHardwareInfo();
        private byte _maxConnectionsAllowed = 0;
        //COMMANDS CLASSES
        private BGAPIAttributeClientCommandClass _attributeClientCommandClass;
        private BGAPIConnectionCommandClass _connectionCommandClass;
        private BGAPIGAPCommandClass _gapCommandClass;
        private BGAPISystemCommandClass _systemCommandClass;

        private System.Timers.Timer _timer;

        /// <summary>Fires when device is found.</summary>
        public event BGBLEDeviceInfoReceivedEventHandler DeviceFound;
        /// <summary>Fires when device was lost.</summary>
        public event BGBLEDeviceInfoReceivedEventHandler DeviceLost;
        public event BGBLECentralAdapterConnectionStateChangEventHandler ConnectionLost;
        public event BGBLECentralAdapterConnectionStateChangEventHandler ConnectionRestored;

        public BGBLECentral(SerialPort serialPort = null, BGBLECentralAdapterConnectionStateChangEventHandler connectionLostEventHandler = null, BGBLECentralAdapterConnectionStateChangEventHandler connectionRestoredEventHandler = null)
        {
            ConnectionLost = connectionLostEventHandler;
            ConnectionRestored = connectionRestoredEventHandler;

            BGAPIConnection.DeviceAvailable += ((object sender, BGAPIDeviceChangeEventArgs e) => {
                SerialPort _serialPort = new SerialPort(e.PortName, 115200);

                _connection = BGAPIConnection.SharedConnection(_serialPort);

                Initialize();

                BGBLECentralAdapterConnectionStateChangeEventArgs eventArgs = new BGBLECentralAdapterConnectionStateChangeEventArgs();
                ConnectionRestored?.Invoke(this, eventArgs);
            });

            _connection = BGAPIConnection.SharedConnection(serialPort);

            Initialize();
        }

        public BGBLECentral(BGBLECentralAdapterConnectionStateChangEventHandler connectionLostEventHandler = null, BGBLECentralAdapterConnectionStateChangEventHandler connectionRestoredEventHandler = null) : this(null, connectionLostEventHandler, connectionRestoredEventHandler)
        {
        }

        public BGBLECentral() : this(null, null, null)
        {

        }

        ~BGBLECentral()
        {
            Close();
        }

        // PROPRTIES
        /// <summary>BLED112 adapter firmware version.</summary>
        public string AdapterSoftwareVersion
        {
            get { return _hardwareInfo.majorSoftwareVersion + "." + _hardwareInfo.minorSoftwareVersion + "." + _hardwareInfo.patchId + "." + _hardwareInfo.buildVersion; }
        }
        /// <summary>Shared BG API Connection.</summary>
        public BGAPIConnection Connection
        {
            get { return _connection; }
        }
        // PROPRTIES

        // EVENT HANDLERS
        /// <summary>Event handler for attribute value event of attribute client command class.</summary>
        /// <param name="sender">Instace of BGBLECentral class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void AttributeClientCommandClassAttributeValue(object sender, BGAPIAttributeClientCommandClassAttributeValueEventArgs e)
        {
            if (_devicesByConnectionHandle.ContainsKey(e.ConnectionHandle))
            {
                _devicesByConnectionHandle[e.ConnectionHandle].AttributeValue(e.AttributeHandle, e.AttributeValueType, e.AttributeData, e.AttributeDataLength);
            }
        }

        /// <summary>Event handler for group found event of attribute client command class.</summary>
        /// <param name="sender">Instace of BGBLECentral class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void AttributeClientCommandClassGroupFound(object sender, BGAPIAttributeClientCommandClassGroupFoundEventArgs e)
        {
            if (_devicesByConnectionHandle.ContainsKey(e.ConnectionHandle))
            {
                _devicesByConnectionHandle[e.ConnectionHandle].ServiceFound(e.GroupUUID, e.StartAttributeHandle, e.EndAttributeHandle);
            }
        }

        /// <summary>Event handler for indicated event of attribute client command class.</summary>
        /// <param name="sender">Instace of BGBLECentral class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void AttributeClientCommandClassIndicated(object sender, BGAPIAttributeClientCommandClassIndicateEventArgs e)
        {
            if (_devicesByConnectionHandle.ContainsKey(e.ConnectionHandle))
            {
                _devicesByConnectionHandle[e.ConnectionHandle].AttributeIndicated(e.AttributeHandle);
            }
        }

        /// <summary>Event handler for information found event of attribute client command class.</summary>
        /// <param name="sender">Instace of BGBLECentral class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void AttributeClientCommandClassInformationFound(object sender, BGAPIAttributeClientCommandClassFindInformationEventArgs e)
        {
            if (_devicesByConnectionHandle.ContainsKey(e.ConnectionHandle))
            {
                _devicesByConnectionHandle[e.ConnectionHandle].DescriptorFound(e.AttributeHandle, e.AttributeUUID);
            }
        }

        /// <summary>Event handler for procedure completed event of attribute client command class.</summary>
        /// <param name="sender">Instace of BGBLECentral class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void AttributeClientCommandClassProcedureCompleted(object sender, BGAPIAttributeClientCommandClassProcedureCompleteEventArgs e)
        {
            if (_devicesByConnectionHandle.ContainsKey(e.ConnectionHandle))
            {
                _devicesByConnectionHandle[e.ConnectionHandle].ProcedureCompleted(e.AttributeHandle, e.Result);
            }
        }

        /// <summary>Event handler for connection status event of connection command class.</summary>
        /// <param name="sender">Instace of BGBLECentral class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void ConnectionCommandClassStatusChanged(object sender, BGAPIConnectionCommandClassStatusEventArgs e)
        {
            if(_devicesByConnectionHandle.ContainsKey(e.ConnectionHandle))
            {
                if (e.ConnectionStatus.isCompleted && e.ConnectionStatus.isConnected) {
                    _devicesByConnectionHandle[e.ConnectionHandle].Connected(e.ConnectionStatus.isConnected, e.ConnectionStatus.isCompleted, e.BondingHandle);
                }
                if (e.ConnectionStatus.isEncrypted)
                {
                    _devicesByConnectionHandle[e.ConnectionHandle].ConnectionBecameEncrypted(e.BondingHandle);
                }
                if (e.ConnectionStatus.isParametersChanged)
                {
                    _devicesByConnectionHandle[e.ConnectionHandle].ConnectionParametersChanged(e.ConnectionInterval, e.Latency, e.Timeout, e.BondingHandle);
                }
            }
        }

        /// <summary>Event handler for disconnect event of connection command class.</summary>
        /// <param name="sender">Instace of BGBLECentral class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void ConnectionCommandClassDisconnected(object sender, BGAPIConnectionCommandClassDisconnectEventArgs e)
        {
            if (_devicesByConnectionHandle.ContainsKey(e.ConnectionHandle))
            {
                _devicesByConnectionHandle[e.ConnectionHandle].Disconnected(e.ReasonCode);
                _devicesByConnectionHandle.Remove(e.ConnectionHandle);
            }
        }

        /// <summary>Event handler for DeviceFound event of GAP command class.</summary>
        /// <param name="sender">Instace of BGBLECentral class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void GAPCommandClassDeviceFound(object sender, BGAPIGAPCommandClassScanEventArgs e)
        {
            if (!_devicesByAddress.ContainsKey(e.DeviceInfo.address))
            {
                BGBLEDevice device = new BGBLEDevice(this, e.DeviceInfo);
                _devicesByAddress[e.DeviceInfo.address] = device;
                BGBLEDeviceInfoReceivedEventArgs eventArgs = new BGBLEDeviceInfoReceivedEventArgs();
                eventArgs.Device = device;
                eventArgs.RSSI = e.DeviceInfo.rssi;
#if DEBUG
                BGBLEDebug.Log("\t\tDEVICE_FOUND", e.DeviceInfo.address + ", " + e.DeviceInfo.rssi);
#endif
                DeviceFound?.Invoke(this, eventArgs);
            }
            else
            {
                if (_devicesByAddress[e.DeviceInfo.address].State == BGAPIDeviceState.TotallyLost) {
                    _devicesByAddress[e.DeviceInfo.address].Update(e.DeviceInfo, true);
                    BGBLEDeviceInfoReceivedEventArgs eventArgs = new BGBLEDeviceInfoReceivedEventArgs();
                    eventArgs.Device = _devicesByAddress[e.DeviceInfo.address];
                    eventArgs.RSSI = e.DeviceInfo.rssi;
#if DEBUG
                    BGBLEDebug.Log("\t\tDEVICE_REFOUND", e.DeviceInfo.address + ", " + e.DeviceInfo.rssi);
#endif
                    DeviceFound?.Invoke(this, eventArgs);
                }
                else
                {
                    _devicesByAddress[e.DeviceInfo.address].Update(e.DeviceInfo);
                }
            }
        }

        /// <summary>Event handler for Elapsed event of System.Timers.Timer.</summary>
        /// <param name="sender">Instace of System.Timers.Timer class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void TimeoutReached(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach(KeyValuePair<string, BGBLEDevice> entry in _devicesByAddress)
            {
                var device = entry.Value;
                var deviceLastState = device.State;
                device.UpdateState(STATE_UPDATE_INTERVAL);
                if ((device.State != BGAPIDeviceState.Alive) && (device.State != deviceLastState))
                {
                    BGBLEDeviceInfoReceivedEventArgs eventArgs = new BGBLEDeviceInfoReceivedEventArgs();
                    eventArgs.Device = device;
                    eventArgs.RSSI = -128;
                    DeviceLost?.Invoke(this, eventArgs);
                }
            }
        }
        // EVENT HANDLERS

        private void Initialize()
        {
            _connection.DeviceInserted += ((object sender, BGAPIDeviceChangeEventArgs e) => {
                Open();
                BGBLECentralAdapterConnectionStateChangeEventArgs eventArgs = new BGBLECentralAdapterConnectionStateChangeEventArgs();
                ConnectionRestored?.Invoke(this, eventArgs);
            });

            _connection.DeviceRemoved += ((object sender, BGAPIDeviceChangeEventArgs e) => {
                Close();
                BGBLECentralAdapterConnectionStateChangeEventArgs eventArgs = new BGBLECentralAdapterConnectionStateChangeEventArgs();
                ConnectionLost?.Invoke(this, eventArgs);
            });

            _devicesByAddress = new Dictionary<string, BGBLEDevice>();
            _devicesByConnectionHandle = new Dictionary<byte, BGBLEDevice>();

            _attributeClientCommandClass = new BGAPIAttributeClientCommandClass(_connection);
            _connectionCommandClass = new BGAPIConnectionCommandClass(_connection);
            _gapCommandClass = new BGAPIGAPCommandClass(_connection);
            _systemCommandClass = new BGAPISystemCommandClass(_connection);

            _timer = new System.Timers.Timer(STATE_UPDATE_INTERVAL);
            _timer.Elapsed += TimeoutReached;

            Open();

            _attributeClientCommandClass.AttributeValue += AttributeClientCommandClassAttributeValue;
            _attributeClientCommandClass.GroupFound += AttributeClientCommandClassGroupFound;
            _attributeClientCommandClass.Indicated += AttributeClientCommandClassIndicated;
            _attributeClientCommandClass.InformationFound += AttributeClientCommandClassInformationFound;
            _attributeClientCommandClass.ProcedureCompleted += AttributeClientCommandClassProcedureCompleted;
            //_attributeClientCommandClass.ReadMultiple += AttributeClientCommandClassReadMultiple;

            _connectionCommandClass.StatusChanged += ConnectionCommandClassStatusChanged;
            _connectionCommandClass.Disconnected += ConnectionCommandClassDisconnected;
            _gapCommandClass.DeviceFound += GAPCommandClassDeviceFound;
        }

        /// <summary>Closes connection.</summary>
        public void Close()
        {
            _timer?.Stop();
            _connection?.Close();
        }

        /// <summary>Starts connect procedure.</summary>
        /// <param name="address">MAC address of BLE device</param>
        /// <param name="addressType">Device MAC addres type, can be: Public, Random</param>
        /// <returns>Connection handle.</returns>
        public byte Connect(string address, BGAPIBluetoothAddressType addressType)
        {
            if (_devicesByAddress.ContainsKey(address))
            {
                _timer.Stop();
                try
                {
                    BGAPIConnectionResult result = _gapCommandClass.ConnectDirect(address, addressType);
                    if (result.error == 0)
                    {
                        _devicesByConnectionHandle[result.connectionHandle] = _devicesByAddress[address];
                        return result.connectionHandle;
                    }
                }
                catch (BGAPIException ex)
                {
                    throw new BGAPIException(0xFE92, "Unnable to connect to device with address: " + address, ex);
                }
            }
            throw new BGAPIException(0xFE92, "Unnable to connect to device with address: " + address);
        }

        /// <summary>Starts disconnect procedure.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort Disconnect(byte connectionHandle)
        {
            if (_devicesByConnectionHandle.ContainsKey(connectionHandle))
            {
                return _connectionCommandClass.Disconnect(connectionHandle);
            }
            return 0xFF93;
        }

        /// <summary>Starts read by type 2803 procedure on connected device. Procedure completed event will be generated.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="startHandle">Handle to start from, start of service handles range</param>
        /// <param name="endHandle">>Handle to end at, end of service handles range</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort FindCharacteristics(byte connectionHandle, ushort startHandle, ushort endHandle)
        {
            return _attributeClientCommandClass.ReadAttributeByType(connectionHandle, "2803", startHandle, endHandle);
        }

        /// <summary>Starts descriptors discovery procedure on connected device. Procedure completed event will be generated.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort FindDescriptors(byte connectionHandle)
        {
            return _attributeClientCommandClass.FindInformation(connectionHandle, 0x0001, 0xFFFF);
        }

        /// <summary>Starts find by group type 2800 procedure on connected device. Procedure completed event will be generated.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort FindServices(byte connectionHandle)
        {
            return _attributeClientCommandClass.ReadByGroupType(connectionHandle, "2800", 0x0001, 0xFFFF);
        }

        /// <summary>Starts device discovery procedure.</summary>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort FindDevices()
        {
            _timer.Start();
            return _gapCommandClass.Discover(BGAPIDiscoverMode.Observation);
        }

        /// <summary>Gets RSSI of connected device.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <returns>RSSI value.</returns>
        public sbyte GetRSSIOfConnection(byte connectionHandle)
        {
            return _connectionCommandClass.GetRSSI(connectionHandle);
        }

        /// <summary>Sends hello to BLED112.</summary>
        /// <returns>Return TRUE if response received.</returns>
        public bool Hello()
        {
            return _systemCommandClass.Hello();
        }

        /// <summary>Opens connection.</summary>
        public void Open()
        {
            _connection?.Open();

            while (!_connection.IsOpen);

            //CHECKING DEVICE
            try
            {
                Hello();
                //GETTING BLED112 MAC ADDRESS
                _address = _systemCommandClass.GetAddress();
                //GETTING MAX CONNECTIONS
                _maxConnectionsAllowed = _systemCommandClass.GetConnections();
                //GETTING HARDWARE INFORMATION
                _hardwareInfo = _systemCommandClass.GetInfo();
                //STOP DISCOVERY PROCESS, JUST IN CASE)
                _gapCommandClass.EndProcedure();
                //SETTING UP ACTIVE SCANNING MODE
                _gapCommandClass.SetScanParameters(true);
#if DEBUG
                Console.WriteLine("BLED112 CONECTION OPENED");
#endif
            }
            catch (BGAPIException ex)
            {
                throw new BGAPIException(0xFF91, ex);
            }

            if (_maxConnectionsAllowed != 1)
            {
                throw new BGAPIException(0xFF99);
            }

            if (!AdapterSoftwareVersion.Equals(REQUIRED_ADAPTER_SOFTWARE_VERSION))
            {
                throw new BGAPIException(0xFF98);
            }
        }

        // <summary>Starts attribute value read procedure on connected device.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort ReadAttributeValue(byte connectionHandle, ushort attributeHandle)
        {
            return _attributeClientCommandClass.ReadAttributeByHandle(connectionHandle, attributeHandle);
        }

        // <summary>Starts attribute long value read procedure on connected device. Procedure completed event will be generated.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort ReadAttributeLongValue(byte connectionHandle, ushort attributeHandle)
        {
            return _attributeClientCommandClass.ReadAttributeByHandleLong(connectionHandle, attributeHandle);
        }

        /// <summary>Stops device discovery procedure.</summary>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort StopScanning()
        {
            _timer.Stop();
            return _gapCommandClass.EndProcedure();
        }

        // <summary>Starts attribute value prepare write procedure on connected device. Procedure completed event will be generated.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="data">Data to write</param>
        /// <param name="offset">Data offset</param>
        /// <param name="count">Data lemgth</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort WriteAttributeValuePrepare(byte connectionHandle, ushort attributeHandle, byte[] data, ushort offset, byte count)
        {
            return _attributeClientCommandClass.PrepareWriteAttributeByHandle(connectionHandle, attributeHandle, data, offset, count);
        }

        // <summary>Starts attribute value write with acknowledgment procedure on connected device. Procedure completed event will be generated.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="data">Data to write</param>
        /// <param name="count">Data lemgth</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort WriteAttributeValueWithAcknowledgment(byte connectionHandle, ushort attributeHandle, byte[] data, byte count)
        {
            return _attributeClientCommandClass.WriteAttributeByHandleWithAcknowledgment(connectionHandle, attributeHandle, data, count);
        }

        // <summary>Starts attribute value write without acknowledgment procedure on connected device.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="data">Data to write</param>
        /// <param name="count">Data lemgth</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort WriteAttributeValueWithoutAcknowledgment(byte connectionHandle, ushort attributeHandle, byte[] data, byte count)
        {
            return _attributeClientCommandClass.WriteAttributeByHandle(connectionHandle, attributeHandle, data, count);
        }

        // <summary>Starts execute write procedure(send prepered data) on connected device. Procedure completed event will be generated.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort WritePreparedAttributeValue(byte connectionHandle, bool commit = true)
        {
            return _attributeClientCommandClass.ExecuteWrite(connectionHandle, commit);
        }

        // OVERRIDED METHODS
        /// <summary>Returns string with BLE central and hardware details.</summary>
        public override string ToString()
        {
            string result = base.ToString();

            result += "\nAddress: " + _address;
            result += "\nMax Connections: " + _maxConnectionsAllowed;
            result += "\nAPI Protocol Version: " + _hardwareInfo.apiProtocolVersion;
            result += "\nSoftware Version: " + AdapterSoftwareVersion;
            result += "\nHardware Version: " + _hardwareInfo.hardwareVersion;
            result += "\nLink Layer Version: " + _hardwareInfo.linkLayerVersion;

            return result;
        }
    }
}
