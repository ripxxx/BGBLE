/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BGBLE.BGAPI;

namespace BGBLE
{
    struct BGBLEDeviceCommandCompletition
    {
        public ushort attributeHandle;
        public bool isDone;
        public ushort result;
    }

    delegate ushort BGBLEDeviceCommandWaitForCompletitionStartDelegate();
    delegate ushort BGBLEDeviceCommandWaitForCompletitionFinishDelegate(ushort attributeHandle, ushort result);

    public class BGBLEDeviceDisconnectedEventArgs : EventArgs
    {
        public ushort ReasonCode;
    }
    public delegate void BGBLEDeviceDisconnectedEventHandler(object sender, BGBLEDeviceDisconnectedEventArgs e);

    public class BGBLEDeviceDescriptorsFoundEventArgs : EventArgs
    {
        public Dictionary<ushort, string> Descriptors;
    }
    public delegate void BGBLEDeviceDescriptorsFoundEventHandler(object sender, BGBLEDeviceDescriptorsFoundEventArgs e);

    /// <summary>This class implements BLE device which using BG API.</summary>
    public class BGBLEDevice
    {
        private BGBLECentral _central;
        private BGBLEDeviceCommandCompletition _commandComletition = new BGBLEDeviceCommandCompletition();
        private byte _connectionHandle;
        private BGAPIBLEDeviceConnectionStatus _connectionStatus = new BGAPIBLEDeviceConnectionStatus(false, false, false, false);
        private ulong _discoveryPacketsReceived = 0;
        private BGAPIBLEDeviceInfo _info;
        private System.Timers.Timer _timer;

        private Dictionary<ushort, string> _descriptorsByAttributeHandle;

        /// <summary>Fires when device information was updated.</summary>
        public event BGBLEDeviceInfoReceivedEventHandler Updated;
        /// <summary>Fires when device was disconnected.</summary>
        public event BGBLEDeviceDisconnectedEventHandler DeviceDisconnected;
        /// <summary>Fires when descriptors discovery procedure completed.</summary>
        public event BGBLEDeviceDescriptorsFoundEventHandler DescriptorsFound;

        public BGBLEDevice(BGBLECentral central, BGAPIBLEDeviceInfo info)
        {
            _central = central;
            _info = info;

            _descriptorsByAttributeHandle = new Dictionary<ushort, string>();

            _timer = new System.Timers.Timer(5000);
            _timer.Elapsed += TimeoutReached;
        }

        //PROPRTIES
        /// <summary>MAC addres of device.</summary>
        public string Address
        {
            get { return _info.address; }
            private set { _info.address = value; }
        }

        /// <summary>Device MAC addres type. Can be: Public, Random.</summary>
        public BGAPIBluetoothAddressType AddressType
        {
            get { return _info.addressType; }
        }

        /// <summary>List of services which UUIDs were found in advertising packets.</summary>
        public List<string> AdvertisedServices
        {
            get { return _info.services; }
        }

        /// <summary>Bond handle if there is known bond for this device, 0xff otherwise.</summary>
        public byte BondingHandle
        {
            get { return _info.bond; }
            private set { _info.bond = value; }
        }

        /// <summary>Name of the device.</summary>
        public string Name
        {
            get { return _info.name; }
            private set { _info.name = value; }
        }

        /// <summary>RSSI value (dBm), range: -103 to -38.</summary>
        public sbyte RSSI
        {
            get { return _info.rssi; }
            private set { _info.rssi = value; }
        }
        //PROPRTIES

        // EVENT HANDLERS
        /// <summary>Event handler for Elapsed event of System.Timers.Timer.</summary>
        /// <param name="sender">Instace of System.Timers.Timer class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void TimeoutReached(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            throw new BGAPIException(0xFF94, new TimeoutException());
        }
        // EVENT HANDLERS

        /// <summary>Runs delegate and waits for procedure completed received.</summary>
        /// <param name="start">Delegate to start</param>
        private ushort WaitForCompletition(BGBLEDeviceCommandWaitForCompletitionStartDelegate start, BGBLEDeviceCommandWaitForCompletitionFinishDelegate finish = null)
        {
            _commandComletition.isDone = false;
            ushort result = start.Invoke();
            if (result != 0)
            {
                return result;
            }
            while (!_commandComletition.isDone);
            _commandComletition.isDone = false;
            if (finish != null)
            {
                return finish.Invoke(_commandComletition.attributeHandle, _commandComletition.result);
            }
            return _commandComletition.result;
        }

        /// <summary>Connects the device, and set connection handle.</summary>
        /// <returns>TRUE if succesfully connected.</returns>
        public bool Connect()
        {
            try
            {
                _connectionHandle = _central.Connect(Address, AddressType);
                _timer.Start();
                while (!_connectionStatus.isCompleted && _timer.Enabled) ;
                _timer.Stop();
                //STARTING FIND DESCRIPTORS PROCESS
                _timer.Start();
                ushort _result = WaitForCompletition(() => {
                    return _central.FindDescriptors(_connectionHandle);
                }, (ushort attributeHandle, ushort result) => {
                    _timer.Stop();
                    BGBLEDeviceDescriptorsFoundEventArgs eventArgs = new BGBLEDeviceDescriptorsFoundEventArgs();
                    eventArgs.Descriptors = _descriptorsByAttributeHandle;
                    DescriptorsFound?.Invoke(this, eventArgs);
                    return result;
                });
                return true;
            }
            catch (BGAPIException ex)
            {
                return false;
            }
        }

        /// <summary>Sets connection status.</summary>
        /// <param name="isConnected">This status flag tells the connection exists to a remote device</param>
        /// <param name="isConnectioProcedureCompleted">Connection completed flag, which is used to tell a new connection has been created</param>
        /// <param name="bondingHandle">Bond handle if there is known bond for this device, 0xff otherwise</param>
        public void Connected(bool isConnected, bool isConnectioProcedureCompleted, byte bondingHandle)
        {
            BondingHandle = bondingHandle;
            if (isConnected)
            {
                _connectionStatus.isConnected = true;
            }
            if (isConnectioProcedureCompleted)
            {
                _connectionStatus.isCompleted = true;
            }
        }

        /// <summary>Sets encryption flag of connection status.</summary>
        /// <param name="bondingHandle">Bond handle if there is known bond for this device, 0xff otherwise</param>
        public void ConnectionBecameEncrypted(byte bondingHandle)
        {
            _connectionStatus.isEncrypted = true;
            BondingHandle = bondingHandle;
        }

        /// <summary>Updates connection parameters information.</summary>
        /// <param name="connectionInterval">Current connection interval (units of 1.25ms)</param>
        /// <param name="latency">Slave latency which tells how many connection intervals the slave may skip.</param>
        /// <param name="timeout">Current supervision timeout (units of 10ms).</param>
        /// <param name="bondingHandle">Bond handle if there is known bond for this device, 0xff otherwise.</param>
        public void ConnectionParametersChanged(ushort connectionInterval, ushort latency, ushort timeout, byte bondingHandle)
        {
            BondingHandle = bondingHandle;
        }

        /// <summary>Disconnect the device.</summary>
        /// <returns>TRUE if succesfully disconnected.</returns>
        public bool Disconnect()
        {
            try
            {
                ushort error = _central.Disconnect(_connectionHandle);
                if (error == 0)
                {
                    _timer.Start();
                    while (_connectionStatus.isConnected) ;
                    return true;
                }
            }
            catch (BGAPIException ex)
            {
                return false;
            }
            return false;
        }

        /// <summary>Reset connection status.</summary>
        /// <param name="reasonCode">Code, which repesents reason of disconnect</param>
        public void Disconnected(ushort reasonCode)
        {
            _timer.Stop();
            _connectionStatus.isParametersChanged = false;
            _connectionStatus.isEncrypted = false;
            _connectionStatus.isCompleted = false;
            _connectionStatus.isConnected = false;

            BGBLEDeviceDisconnectedEventArgs eventArgs = new BGBLEDeviceDisconnectedEventArgs();
            eventArgs.ReasonCode = reasonCode;
            DeviceDisconnected?.Invoke(this, eventArgs);
        }

        /// <summary>Adds descriptor to list.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="uuid">Attribute UUID: 2800, 2803, 2901, 2902, etc.</param>
        public void DescriptorFound(ushort attributeHandle, string uuid)
        {
            _descriptorsByAttributeHandle[attributeHandle] = uuid;
        }

        /// <summary>Adds descriptor to list.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="uuid">Attribute UUID: 2800, 2803, 2901, 2902, etc.</param>
        public void ProcedureCompleted(ushort attributeHandle, ushort result)
        {
            _commandComletition.attributeHandle = attributeHandle;
            _commandComletition.isDone = true;
            _commandComletition.result = result;
        }

        /// <summary>Updates device information.</summary>
        /// <param name="info">Strucuture with device information from advertisment or scan packet</param>
        public void Update(BGAPIBLEDeviceInfo info)
        {
            _discoveryPacketsReceived += 1;
            _info.bond = info.bond;
            _info.connectableAdvertisementPacket += info.connectableAdvertisementPacket;
            _info.discoverableAdvertisementPacket += info.discoverableAdvertisementPacket;
            _info.nonConnectableAdvertisementPacket += info.nonConnectableAdvertisementPacket;
            _info.scanResponsePacket += info.scanResponsePacket;
            if ((info.flags != null) && (info.flags.Length > 0))
            {
                _info.flags = info.flags;
            }
            if ((info.manufacturerSpecificData != null) && (info.manufacturerSpecificData.Length > 0))
            {
                _info.manufacturerSpecificData = info.manufacturerSpecificData;
            }
            _info.maxConnectionInterval = ((info.maxConnectionInterval > 0) ? info.maxConnectionInterval : _info.maxConnectionInterval);
            _info.minConnectionInterval = ((info.minConnectionInterval > 0) ? info.minConnectionInterval : _info.minConnectionInterval);
            _info.name = ((info.name != "") ? info.name : _info.name);
            _info.rssi = info.rssi;
            _info.txPower = ((info.txPower > 0) ? info.txPower : _info.txPower);
            if ((info.services != null) && (info.services.Count > 0))
            {
                if ((_info.services != null) && (_info.services.Count > 0))
                {
                    _info.services = info.services.Concat(_info.services).Distinct().ToList();
                }
                else
                {
                    _info.services = info.services;
                }
            }

            BGBLEDeviceInfoReceivedEventArgs eventArgs = new BGBLEDeviceInfoReceivedEventArgs();
            eventArgs.Device = this;
            eventArgs.RSSI = info.rssi;
            Updated?.Invoke(this, eventArgs);
        }

        // OVERRIDED METHODS
        /// <summary>Returns string with BLE device details.</summary>
        public override string ToString()
        {
            string result = base.ToString() + "\n" + _info.name + "<" + _info.address + ">\nRSSI: " + _info.rssi + "dBm";

            result += "\nDiscovery Packets Received: " + _discoveryPacketsReceived;
            result += "\nConnectable Packets Received: " + _info.connectableAdvertisementPacket;
            result += "\nDiscoverable Packets Received: " + _info.discoverableAdvertisementPacket;
            result += "\nNonConnectable Packets Received: " + _info.nonConnectableAdvertisementPacket;
            result += "\nScan Packets Received: " + _info.scanResponsePacket;
            result += "\nTX Power: " + _info.txPower;
            result += "\nServices: " + _info.services.Count;
            if (_info.services.Count > 0)
            {
                for (int i = 0; i < _info.services.Count; i++)
                {
                    result += "\n\t0x" + _info.services[i];
                }
            }

            return result;
        }
    }
}
