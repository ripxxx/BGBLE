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

    public class BGBLEDeviceDisconnectedEventArgs : EventArgs
    {
        public ushort ReasonCode;
    }
    public delegate void BGBLEDeviceDisconnectedEventHandler(object sender, BGBLEDeviceDisconnectedEventArgs e);

    /// <summary>This class implements BLE central which using BG API.</summary>
    public class BGBLECentral
    {
        private string _address;
        private BGAPIConnection _connection;
        private Dictionary<string, BGBLEDevice> _devicesByAddress;
        private Dictionary<byte, BGBLEDevice> _devicesByConnectionHandle;
        private byte _maxConnectionsAllowed = 0;
        //COMMANDS CLASSES
        private BGAPIConnectionCommandClass _connectionCommandClass;
        private BGAPIGAPCommandClass _gapCommandClass;
        private BGAPISystemCommandClass _systemCommandClass;

        /// <summary>Fires when device is found.</summary>
        public event BGBLEDeviceInfoReceivedEventHandler DeviceFound;

        public BGBLECentral(SerialPort serialPort = null)
        {
            _connection = BGAPIConnection.SharedConnection(serialPort);

            _devicesByAddress = new Dictionary<string, BGBLEDevice>();
            _devicesByConnectionHandle = new Dictionary<byte, BGBLEDevice>();

            _connectionCommandClass = new BGAPIConnectionCommandClass(_connection);
            _gapCommandClass = new BGAPIGAPCommandClass(_connection);
            _systemCommandClass = new BGAPISystemCommandClass(_connection);

            //CHECKING DEVICE
            try
            {
                _systemCommandClass.Hello();

                //GETTING BLED112 MAC ADDRESS
                _address = _systemCommandClass.GetAddress();

                //GETTING MAX CONNECTIONS
                _maxConnectionsAllowed = _systemCommandClass.GetConnections();

                //STOP DISCOVERY PROCESS, JUST IN CASE)
                _gapCommandClass.EndProcedure();

                //SETTING UP ACTIVE SCANNING MODE
                _gapCommandClass.SetScanParameters(true);
            }
            catch (BGAPIException ex) {
                throw new BGAPIException(0xFF91, ex);
            }

            _connectionCommandClass.StatusChanged += ConnectionCommandClassStatusChanged;
            _connectionCommandClass.Disconnected += ConnectionCommandClassDisconnected;
            _gapCommandClass.DeviceFound += GAPCommandClassDeviceFound;
        }

        // PROPRTIES
        /// <summary>Shared BG API Connection.</summary>
        public BGAPIConnection Connection
        {
            get { return _connection; }
        }
        // PROPRTIES

        // EVENT HANDLERS
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
                DeviceFound?.Invoke(this, eventArgs);
            }
            else
            {
                _devicesByAddress[e.DeviceInfo.address].Update(e.DeviceInfo);
            }
        }
        // EVENT HANDLERS

        /// <summary>Starts connect procedure.</summary>
        /// <param name="address">MAC address of BLE device</param>
        /// <param name="addressType">Device MAC addres type, can be: Public, Random</param>
        /// <returns>Connection handle.</returns>
        public byte Connect(string address, BGAPIBluetoothAddressType addressType)
        {
            if (_devicesByAddress.ContainsKey(address))
            {
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

        /// <summary>Starts device discovery procedure.</summary>
        public void FindDevices()
        {
            _gapCommandClass.Discover(BGAPIDiscoverMode.Observation);
        }

        /// <summary>Stops device discovery procedure.</summary>
        public void StopScanning()
        {
            _gapCommandClass.EndProcedure();
        }

        // OVERRIDED METHODS
        /// <summary>Returns string with BLE central and hardware details.</summary>
        public override string ToString()
        {
            string result = base.ToString();

            result += "\nAddress: " + _address;
            result += "\nMax Connections: " + _maxConnectionsAllowed;

            return result;
        }
    }
}
