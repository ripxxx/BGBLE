/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BGBLE
{
    /// <summary>This class implements service of BGBLEDevice.</summary>
    public class BGBLEService
    {
        private BGBLEDevice _device;
        private ushort _endAttributeHandle;
        private ushort _startAttributeHandle;
        private string _uuid;

        private Dictionary<ushort, BGBLECharacteristic> _characteristicsByHandle = new Dictionary<ushort, BGBLECharacteristic>();
        private Dictionary<string, BGBLECharacteristic> _characteristicsByUUID = null;

        public BGBLEService(BGBLEDevice device, string uuid, ushort startAttributeHandle, ushort endAttributeHandle)
        {
            _device = device;
            _endAttributeHandle = endAttributeHandle;
            _startAttributeHandle = startAttributeHandle;
            _uuid = uuid;
        }

        //PROPRTIES
        /// <summary>Available characteristics.</summary>
        public Dictionary<string, BGBLECharacteristic> Characteristics
        {
            get
            {
                if (_characteristicsByUUID == null)
                {
                    ushort _result = _device.FindCharacteristics(_startAttributeHandle, _endAttributeHandle);
                }
                return _characteristicsByUUID;
            }
        }

        /// <summary>Attribute handle of service.</summary>
        public ushort Handle
        {
            get { return _startAttributeHandle; }
        }

        /// <summary>UUID(Type) of service.</summary>
        public string UUID
        {
            get { return _uuid; }
        }
        //PROPRTIES

        /// <summary>Adds characteristic characteristics list of service.</summary>
        /// <param name="handle">Characteristic handle</param>
        /// <param name="config">Confiration data: Characteristic Properties(byte), Characteristic Value Handle(ushort), UUID(byte[](REVERSED))</param>
        /// <param name="configLength">Confiration data length</param>
        /// <returns>Characteristic object.</returns>
        public BGBLECharacteristic CharacteristicFound(ushort handle, byte[] config, byte configLength)
        {
            if (_characteristicsByUUID == null)
            {
                _characteristicsByUUID = new Dictionary<string, BGBLECharacteristic>();
            }

            BGBLECharacteristic characteristic = new BGBLECharacteristic(this, handle, config, configLength);

            _characteristicsByHandle[handle] = characteristic;
            _characteristicsByHandle[characteristic.ValueAttributeHandle] = characteristic;
            _characteristicsByUUID[characteristic.UUID] = characteristic;

            return characteristic;
        }

        /// <summary>Searches characteristic by handle.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <returns>Characteristic object.</returns>
        public BGBLECharacteristic FindCharacteristicByHandle(ushort attributeHandle)
        {
            if (_characteristicsByHandle.ContainsKey(attributeHandle))
            {
                return _characteristicsByHandle[attributeHandle];
            }
            return null;
        }

        /// <summary>Searches characteristic by UUID.</summary>
        /// <param name="uuid">Attribute UUID</param>
        /// <returns>Characteristic object.</returns>
        public BGBLECharacteristic FindCharacteristicByUUID(string uuid)
        {
            if (_characteristicsByUUID.ContainsKey(uuid))
            {
                return _characteristicsByUUID[uuid];
            }
            return null;
        }

        /// <summary>Checks if attribute handle in service handles range.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <returns>TRUE if in attribute handle in service handles range.</returns>
        public bool IsAttributeInServiceRange(ushort attributeHandle)
        {
            return ((attributeHandle >= _startAttributeHandle) && (attributeHandle <= _endAttributeHandle));
        }

        /// <summary>Reads attribute long value.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <returns>Characteristic data structure.</returns>
        public BGBLECharacteristicData ReadAttributeLongValue(ushort attributeHandle)
        {
            return _device.ReadAttributeLongValue(attributeHandle);
        }

        /// <summary>Reads attribute value.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <returns>Characteristic data structure.</returns>
        public BGBLECharacteristicData ReadAttributeValue(ushort attributeHandle)
        {
            return _device.ReadAttributeValue(attributeHandle);
        }

        /// <summary>Subscribes for attribute notifications.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="unsubscribe">Set to TRUE to unsubscribe</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort SubscribeForAttributeNotification(ushort attributeHandle, bool unsubscribe = false)
        {
            return _device.SubscribeForAttributeNotification(attributeHandle, unsubscribe);
        }

        /// <summary>Writes data to attribute.</summary>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="data">Data to write</param>
        /// <param name="count">Data length</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort WriteAttributeValue(ushort attributeHandle, byte[] data, ushort count)
        {
            return _device.WriteAttributeValue(attributeHandle, data, count);
        }
    }
}
