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
    public enum BGAPIDeviceState : byte
    {
        /// <summary>Device is alive.</summary>
        Alive = 0,
        /// <summary>Lost for the first checking period.</summary>
        TemporaryLost = 1,
        /// <summary>Lost for the second checking period(Unavailable).</summary>
        Unavailable = 2,
        /// <summary>Totally lost.</summary>
        TotallyLost = 3,
    }

    public struct BGBLECharacteristicData
    {
        public ulong count;
        public byte[] data;

        public BGBLECharacteristicData(byte[] _data, ulong _count)
        {
            count = _count;
            data = _data;
        }
    }
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

    public delegate void BGBLEDeviceRSSIUpdatedEventHandler(object sender, BGBLEDeviceInfoReceivedEventArgs e);

    /// <summary>This class implements BLE device which using BG API.</summary>
    public class BGBLEDevice
    {
        private BGBLECentral _central;
        private BGBLEDeviceCommandCompletition _commandComletition = new BGBLEDeviceCommandCompletition();
        private byte _connectionHandle;
        private BGAPIBLEDeviceConnectionStatus _connectionStatus = new BGAPIBLEDeviceConnectionStatus(false, false, false, false);
        private ulong _discoveryPacketsReceived = 0;
        private BGAPIBLEDeviceInfo _info;
        private bool _isTimeoutReached = false;
        private DateTime _lastUpdateDateTime;
        private BGAPIDeviceState _state;
        private System.Timers.Timer _timer;

        private Dictionary<ushort, string> _descriptorsByAttributeHandle = new Dictionary<ushort, string>();
        private Dictionary<ushort, BGBLEService> _servicesByCharacteristicHandle = new Dictionary<ushort, BGBLEService>();
        private Dictionary<ushort, BGBLEService> _servicesByHandle = new Dictionary<ushort, BGBLEService>();
        private Dictionary<string, BGBLEService> _servicesByUUID = null;

        /// <summary>Fires when descriptors discovery procedure completed.</summary>
        public event BGBLEDeviceDescriptorsFoundEventHandler DescriptorsFound;
        /// <summary>Fires when device was disconnected.</summary>
        public event BGBLEDeviceDisconnectedEventHandler DeviceDisconnected;
        /// <summary>Fires when device RSSI updated.</summary>
        public event BGBLEDeviceInfoReceivedEventHandler RSSIUpdated;
        /// <summary>Fires when device information was updated.</summary>
        public event BGBLEDeviceInfoReceivedEventHandler Updated;

        public BGBLEDevice(BGBLECentral central, BGAPIBLEDeviceInfo info)
        {
            _central = central;
            _info = info;
            _lastUpdateDateTime = DateTime.Now;
            _state = BGAPIDeviceState.Alive;

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

        /// <summary>Available services.</summary>
        public Dictionary<string, BGBLEService> Services
        {
            get {
                if (_servicesByUUID == null)
                {
                    ushort _result = WaitForCompletition(() => {
                        return _central.FindServices(_connectionHandle);
                    });
                }
                return _servicesByUUID;
            }
        }

        /// <summary>Device state.</summary>
        public BGAPIDeviceState State
        {
            get { return _state; }
        }
        //PROPRTIES

        // EVENT HANDLERS
        /// <summary>Event handler for Elapsed event of System.Timers.Timer.</summary>
        /// <param name="sender">Instace of System.Timers.Timer class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void TimeoutReached(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            _isTimeoutReached = true;
            _state = BGAPIDeviceState.Unavailable;
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
            while (!_commandComletition.isDone)
            {
                if (_isTimeoutReached)
                {
                    _isTimeoutReached = false;
                    _central.Hello();
                    throw new BGAPIException(0xFF94, new TimeoutException());
                }
            }
            _commandComletition.isDone = false;
            if (finish != null)
            {
                return finish.Invoke(_commandComletition.attributeHandle, _commandComletition.result);
            }
            return _commandComletition.result;
        }

        /// <summary>Process attribute indicated event.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        public void AttributeIndicated(ushort attributeHandle)
        {
            //Not Implemented
        }

        /// <summary>Process attribute value event.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="attributeValueType">Attribute value type: Read, Notify, Indicate, etc.</param>
        /// <param name="data">Payload</param>
        /// <param name="count">Payload length</param>
        public void AttributeValue(ushort attributeHandle, BGAPIAttributeValueType attributeValueType, byte[] data, byte count)
        {
            BGBLECharacteristic characteristic;
            switch (attributeValueType)
            {
                case BGAPIAttributeValueType.Indicate:
                    if (_servicesByCharacteristicHandle.ContainsKey(attributeHandle))
                    {
                        characteristic = _servicesByCharacteristicHandle[attributeHandle].FindCharacteristicByHandle(attributeHandle);
                        characteristic?.AttributeIndicated(data, count);
                    }
                    break;
                case BGAPIAttributeValueType.IndicateRSPReq:
                    //Not implemented
                    break;
                case BGAPIAttributeValueType.Notify:
                    if (_servicesByCharacteristicHandle.ContainsKey(attributeHandle))
                    {
                        characteristic = _servicesByCharacteristicHandle[attributeHandle].FindCharacteristicByHandle(attributeHandle);
                        characteristic?.AttributeNotified(data, count);
                    }
                    break;
                case BGAPIAttributeValueType.Read:
                    if (_servicesByCharacteristicHandle.ContainsKey(attributeHandle))
                    {
                        characteristic = _servicesByCharacteristicHandle[attributeHandle].FindCharacteristicByHandle(attributeHandle);
                        characteristic?.ValueRead(data, count);
                        ProcedureCompleted(attributeHandle, 0x0000);
                    }
                    break;
                case BGAPIAttributeValueType.ReadBlob:
                    if (_servicesByCharacteristicHandle.ContainsKey(attributeHandle))
                    {
                        characteristic = _servicesByCharacteristicHandle[attributeHandle].FindCharacteristicByHandle(attributeHandle);
                        characteristic?.ValueReadBlob(data, count);
                    }
                    break;
                case BGAPIAttributeValueType.ReadByType:
                    BGBLEService service = FindServiceByAttributeHandle(attributeHandle);
                    if (service != null)
                    {
                        //_servicesByCharacteristicHandle[attributeHandle] = service;//_servicesByHandle[service.Handle];
                        var _characteristic = service.CharacteristicFound(attributeHandle, data, count);
                        _servicesByCharacteristicHandle[_characteristic.ValueAttributeHandle] = service;
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>Connects the device, and set connection handle.</summary>
        /// <returns>TRUE if succesfully connected.</returns>
        public bool Connect()
        {
            _connectionHandle = _central.Connect(Address, AddressType);
            _timer.Start();
            while (!_connectionStatus.isCompleted && _timer.Enabled) ;
            if (_isTimeoutReached)
            {
                _isTimeoutReached = false;
                _central.Hello();
                throw new BGAPIException(0xFF94, new TimeoutException());
            }
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
            return (_result == 0);
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
            ushort error = _central.Disconnect(_connectionHandle);
            if (error == 0)
            {
                _timer.Start();
                while (_connectionStatus.isConnected) ;
                return true;
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

            _descriptorsByAttributeHandle.Clear();

            _servicesByCharacteristicHandle.Clear();
            _servicesByHandle.Clear();
            if(_servicesByUUID != null) {
                _servicesByUUID.Clear();
                _servicesByUUID = null;
            }

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

        /// <summary>Starts find characteristics procedure on connected device.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="startHandle">Handle to start from, start of service handles range</param>
        /// <param name="endHandle">>Handle to end at, end of service handles range</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort FindCharacteristics(ushort startHandle, ushort endHandle)
        {
            ushort _result = WaitForCompletition(() => {
                return _central.FindCharacteristics(_connectionHandle, startHandle, endHandle);
            });
            return _result;
        }

        /// <summary>Searches for descriptors which follows specified characteristic handle.</summary>
        /// <param name="handle">Characteristic handle</param>
        /// <returns>Dictionary with found descriptors or null if nothing found.</returns>
        public Dictionary<ushort, string> FindDescriptorsByCharacteristicHandle(ushort handle)
        {
            if ((_descriptorsByAttributeHandle.Count > 0) && _descriptorsByAttributeHandle.ContainsKey(handle))
            {
                var descriptorsByCharacteristicHandle = _descriptorsByAttributeHandle.SkipWhile(entry => entry.Key != handle).TakeWhile(entry => ((entry.Value != "2803") || (entry.Key == handle))).ToDictionary(entry => entry.Key, entry => entry.Value);
                return descriptorsByCharacteristicHandle;
            }
            return null;
        }

        /// <summary>Searches for service with attribute handle in range.</summary>
        /// <param name="attributeHandle">Service handle, characteristic handle or descriptor handle</param>
        /// <returns>service or null if nothing found.</returns>
        public BGBLEService FindServiceByAttributeHandle(ushort attributeHandle)
        {
            if((_servicesByCharacteristicHandle.Count > 0) && _servicesByCharacteristicHandle.ContainsKey(attributeHandle))
            {
                return _servicesByCharacteristicHandle[attributeHandle];
            }
            else if ((Services.Count > 0) && (_servicesByHandle.Count > 0))
            {
                foreach (KeyValuePair<ushort, BGBLEService> entry in _servicesByHandle)
                {
                    if (entry.Value.IsAttributeInServiceRange(attributeHandle))
                    {
                        _servicesByCharacteristicHandle[attributeHandle] = entry.Value;
                        return entry.Value;
                    }
                }
            }
            return null;
        }

        /// <summary>Searches for service by handle.</summary>
        /// <param name="attributeHandle">Service handle</param>
        /// <returns>service or null if nothing found.</returns>
        public BGBLEService FindServiceByHandle(ushort handle)
        {
            if ((Services.Count > 0) && (_servicesByHandle.ContainsKey(handle)))
            {
                return _servicesByHandle[handle];
            }
            return null;
        }

        /// <summary>Searches for service by UUID.</summary>
        /// <param name="uuid">Service UUID</param>
        /// <returns>service or null if nothing found.</returns>
        public BGBLEService FindServiceByUUID(string uuid)
        {
            if ((Services.Count > 0) && (_servicesByUUID.ContainsKey(uuid)))
            {
                return _servicesByUUID[uuid];
            }
            return null;
        }

        /// <summary>Gets RSSI of the device.</summary>
        /// <returns>RSSI value.</returns>
        public sbyte GetRSSI()
        {
            if (_connectionStatus.isConnected) {
                sbyte rssi = _central.GetRSSIOfConnection(_connectionHandle);
                if (rssi != _info.rssi)
                {
                    _info.rssi = rssi;
                    BGBLEDeviceInfoReceivedEventArgs eventArgs = new BGBLEDeviceInfoReceivedEventArgs();
                    eventArgs.Device = this;
                    eventArgs.RSSI = rssi;
                    RSSIUpdated?.Invoke(this, eventArgs);
                }
            }
            return _info.rssi;
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

        /// <summary>Reads attribute long value.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <returns>Characteristic data structure.</returns>
        public BGBLECharacteristicData ReadAttributeLongValue(ushort attributeHandle)
        {
            BGBLECharacteristicData data = new BGBLECharacteristicData(null, 0);
            ushort _result = WaitForCompletition(() => {
                return _central.ReadAttributeLongValue(_connectionHandle, attributeHandle);
            }, (ushort _attributeHandle, ushort result) => {
                if (_servicesByCharacteristicHandle.ContainsKey(_attributeHandle))
                {
                    BGBLECharacteristic characteristic = _servicesByCharacteristicHandle[_attributeHandle].FindCharacteristicByHandle(_attributeHandle);
                    if (characteristic != null)
                    {
                        data = characteristic.ValueReadCompleted();
                    }
                }
                return result;
            });
            return data;
        }

        /// <summary>Reads attribute value.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <returns>Characteristic data structure.</returns>
        public BGBLECharacteristicData ReadAttributeValue(ushort attributeHandle)
        {
            BGBLECharacteristicData data = new BGBLECharacteristicData(null, 0);
            ushort _result = WaitForCompletition(() => {
                return _central.ReadAttributeValue(_connectionHandle, attributeHandle);
            }, (ushort _attributeHandle, ushort result) => {
                if (_servicesByCharacteristicHandle.ContainsKey(_attributeHandle))
                {
                    BGBLECharacteristic characteristic = _servicesByCharacteristicHandle[_attributeHandle].FindCharacteristicByHandle(_attributeHandle);
                    if (characteristic != null)
                    {
                        data = characteristic.ValueReadCompleted();
                    }
                }
                return result;
            });
            return data;
        }

        /// <summary>Reads characteristic description.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <returns>Characteristic description string.</returns>
        public string ReadCharacteristicDescription(ushort attributeHandle)
        {
            string description = "";
            var descriptorsByCharacteristicHandle = FindDescriptorsByCharacteristicHandle(attributeHandle);
            if ((descriptorsByCharacteristicHandle != null) && (descriptorsByCharacteristicHandle.Count > 0))
            {
                var handles = descriptorsByCharacteristicHandle.Where(entry => entry.Value == "2901").ToArray();
                if (handles.Length > 0)
                {
                    ushort _handle = handles.First().Key;
                    BGBLEService service = FindServiceByAttributeHandle(_handle);
                    if (service != null)
                    {
                        service.RegisterCharacteristicHandleAlias(attributeHandle, _handle);
                        var _result = WaitForCompletition(() => {
                            return _central.ReadAttributeValue(_connectionHandle, _handle);
                        }, (ushort _attributeHandle, ushort result) => {
                            if (_servicesByCharacteristicHandle.ContainsKey(_attributeHandle))
                            {
                                BGBLECharacteristic characteristic = _servicesByCharacteristicHandle[_attributeHandle].FindCharacteristicByHandle(_attributeHandle);
                                if (characteristic != null)
                                {
                                    var data = characteristic.ValueReadCompleted();
                                    description = Encoding.UTF8.GetString(data.data.Take((int)data.count).ToArray());
                                }
                            }
                            return result;
                        });
                    }

                }
            }
            return description;
        }

        /// <summary>Adds service to list.</summary>
        /// <param name="uuid">Service UUID handle</param>
        /// <param name="startAttributeHandle">First handle in service handles range</param>
        /// <param name="endAttributeHandle">Last handle in service handles range</param>
        /// <returns>Service object.</returns>
        public BGBLEService ServiceFound(string uuid, ushort startAttributeHandle, ushort endAttributeHandle)
        {
            if (_servicesByUUID == null)
            {
                _servicesByUUID = new Dictionary<string, BGBLEService>();
            }

            BGBLEService service = new BGBLEService(this, uuid, startAttributeHandle, endAttributeHandle);

            _servicesByHandle[startAttributeHandle] = service;
            _servicesByUUID[uuid] = service;

            return service;
        }

        /// <summary>Subscribes for attribute notifications.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="unsubscribe">Set to TRUE to unsubscribe</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort SubscribeForAttributeNotification(ushort attributeHandle, bool unsubscribe = false)
        {
            var descriptorsByCharacteristicHandle = FindDescriptorsByCharacteristicHandle(attributeHandle);
            if ((descriptorsByCharacteristicHandle != null) && (descriptorsByCharacteristicHandle.Count > 0))
            {
                List<string> uuids = new List<string>() { "2902" };
                var service = FindServiceByAttributeHandle(attributeHandle);
                if (service != null) {
                    var characteristic = service.FindCharacteristicByHandle(attributeHandle);
                    if (characteristic != null)
                    {
                        uuids.Add(characteristic.UUID);
                    }
                }

                foreach (var uuid in uuids)
                {
                    var handles = descriptorsByCharacteristicHandle.Where(entry => entry.Value == uuid).ToArray();
                    if (handles.Length > 0)
                    {
                        ushort _handle = handles.First().Key;
                        if (uuid == "2902") {
                            return _central.WriteAttributeValueWithoutAcknowledgment(_connectionHandle, _handle, new byte[] { (byte)((unsubscribe) ? 0x00 : 0x01), 0x00 }, 2);
                        }
                        else
                        {
                            var result = WaitForCompletition(() => {
                                return _central.ReadAttributeValue(_connectionHandle, _handle);
                            });

                            return result;
                        }
                    }
                }
            }
            return 0xFF97;
        }

        /// <summary>Updates device information.</summary>
        /// <param name="info">Strucuture with device information from advertisment or scan packet</param>
        public void Update(BGAPIBLEDeviceInfo info, bool silent = false)
        {
            _lastUpdateDateTime = DateTime.Now;
            _state = BGAPIDeviceState.Alive;

            BGBLEDeviceInfoReceivedEventArgs eventArgs = new BGBLEDeviceInfoReceivedEventArgs();
            eventArgs.Device = this;

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
            if (_info.rssi != info.rssi)
            {
                _info.rssi = info.rssi;
                
                eventArgs.RSSI = info.rssi;
                RSSIUpdated?.Invoke(this, eventArgs);
            }
            else
            {
                eventArgs.RSSI = info.rssi;
            }
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

            if (!silent) {
                Updated?.Invoke(this, eventArgs);
            }
        }

        /// <summary>Updates device state based on last update time.</summary>
        /// <param name="checkingInterver">Interval used by central to update state</param>
        public void UpdateState(double checkingInterver)
        {
            if (_state != BGAPIDeviceState.TotallyLost)
            {
                var now = DateTime.Now;
                var timeDif = now.Subtract(_lastUpdateDateTime);
                if (timeDif.TotalMilliseconds > checkingInterver)
                {
                    if (_state == BGAPIDeviceState.Alive)
                    {
                        _state = BGAPIDeviceState.TemporaryLost;
                    }
                    else if (_state == BGAPIDeviceState.TemporaryLost)
                    {
                        _state = BGAPIDeviceState.Unavailable;
                    }
                    else
                    {
                        _state = BGAPIDeviceState.TotallyLost;
                        DescriptorsFound = null;
                        DeviceDisconnected = null;
                        RSSIUpdated = null;
                        Updated = null;
                    }
                }
            }
        }

        // <summary>Writes data to attribute.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="data">Data to write</param>
        /// <param name="count">Data length</param>
        /// <param name="doNotWaiteCompletition">Do not wait for procedure completed event</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort WriteAttributeValue(ushort attributeHandle, byte[] data, ushort count, bool doNotWaiteCompletition = false)
        {
            ushort result = 0x0000;
            if (count > 20) {
                for (ushort i = 0; i < count; i += 18)
                {
                    byte _count = (byte)(((count - i) > 18)? 18: (count - i));
                    result = WaitForCompletition(() => {
                        return _central.WriteAttributeValuePrepare(_connectionHandle, attributeHandle, data, i, _count);
                    });
                    if (result != 0)
                    {
                        break;
                    }
                }
                result = WaitForCompletition(() => {
                    return _central.WritePreparedAttributeValue(_connectionHandle, (result == 0));
                });
            }
            else
            {
                if(doNotWaiteCompletition)
                {
                    return _central.WriteAttributeValueWithAcknowledgment(_connectionHandle, attributeHandle, data, (byte)count);
                }
                result = WaitForCompletition(() => {
                    return _central.WriteAttributeValueWithAcknowledgment(_connectionHandle, attributeHandle, data, (byte)count);
                });
            }
            return result;
        }

        // <summary>Starts attribute value write without acknowledgment procedure.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="data">Data to write</param>
        /// <param name="count">Data lemgth</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort WriteAttributeValueWithoutAcknowledgment(ushort attributeHandle, byte[] data, byte count)
        {
            return _central.WriteAttributeValueWithoutAcknowledgment(_connectionHandle, attributeHandle, data, count);
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
