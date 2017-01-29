/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BGBLE.BGAPI;

namespace BGBLE
{
    public class BGBLECharacteristicValueEventArgs : EventArgs
    {
        public ulong count;
        public byte[] data;
    }
    public delegate void BGBLECharacteristicValueEventHandler(object sender, BGBLECharacteristicValueEventArgs e);

    /// <summary>This class implements characteristic of BGBLEDevice.</summary>
    public class BGBLECharacteristic
    {
        private BGBLECharacteristicData _blobData = new BGBLECharacteristicData();
        private string _description = null;
        private ushort _handle;

        private bool _isAdditionalPropertiesAvailable;
        private bool _isAuthenticatedWrite;
        private bool _isBroadcastSupported;
        private bool _isIndicationSupported;
        private bool _isNotificationSupported;
        private bool _isReadSupported;
        private bool _isWriteSupported;
        private bool _isWriteWithouAcknowledgmentSupported;

        private BGBLEService _service;
        private string _uuid;
        private ushort _valueAttributeHandle;

        /// <summary>Fires when attribute indicated.</summary>
        public event BGBLECharacteristicValueEventHandler Indicated;
        /// <summary>Fires when attribute notification received.</summary>
        public event BGBLECharacteristicValueEventHandler Notyfied;

        public BGBLECharacteristic(BGBLEService service, ushort attributeHandle, byte[] config, byte configLength)
        {
            _service = service;
            _handle = attributeHandle;
            if (configLength < 5)
            {
                throw new BGAPIException(0xFF95, "BGBLECharacteristic config to short, length = " + configLength);
            }
            
            _isBroadcastSupported = ((config[0] & 1) == 1);
            _isReadSupported = ((config[0] & 2) == 2);
            _isWriteWithouAcknowledgmentSupported = ((config[0] & 4) == 4);
            _isWriteSupported = ((config[0] & 8) == 8);
            _isNotificationSupported = ((config[0] & 16) == 16);
            _isIndicationSupported = ((config[0] & 32) == 32);
            _isAuthenticatedWrite = ((config[0] & 64) == 64);
            _isAdditionalPropertiesAvailable = ((config[0] & 128) == 128);

            _valueAttributeHandle = BitConverter.ToUInt16(config, 1);

            _uuid = BitConverter.ToString(config.Reverse().ToArray(), 0, (configLength - 3)).Replace("-", "");
        }

        //PROPRTIES
        /// <summary>Description of characteristic.</summary>
        public string Description
        {
            get
            {
                if (_description == null)
                {
                    _description = _service.ReadCharacteristicDescription(_handle);
                }
                return _description;
            }
        }

        /// <summary>Attribute handle of characteristic.</summary>
        public ushort Handle
        {
            get { return _handle; }
        }

        /// <summary>UUID of characteristic.</summary>
        public string UUID
        {
            get { return _uuid; }
        }

        /// <summary>Related value attribute handle of characteristic.</summary>
        public ushort ValueAttributeHandle
        {
            get { return _valueAttributeHandle; }
        }
        //PROPRTIES

        /// <summary>Event handler for attribute indicated event.</summary>
        /// <param name="sender">Instace of BGBLECentral class which generated the event</param>
        /// <param name="e">EventArgs</param>
        public void AttributeIndicated(byte[] data, byte count)
        {
            BGBLECharacteristicValueEventArgs eventArgs = new BGBLECharacteristicValueEventArgs();
            eventArgs.count = count;
            eventArgs.data = data;
            Indicated?.Invoke(this, eventArgs);
        }

        /// <summary>Event handler for attribute notified event.</summary>
        /// <param name="sender">Instace of BGBLECentral class which generated the event</param>
        /// <param name="e">EventArgs</param>
        public void AttributeNotified(byte[] data, byte count)
        {
            BGBLECharacteristicValueEventArgs eventArgs = new BGBLECharacteristicValueEventArgs();
            eventArgs.count = count;
            eventArgs.data = data;
            Notyfied?.Invoke(this, eventArgs);
        }

        // <summary>Reads attribute value.</summary>
        /// <returns>Characteristic data structure.</returns>
        public BGBLECharacteristicData Read()
        {
            if (_isReadSupported)
            {
                _blobData.count = 0;
                _blobData.data = null;

                return _service.ReadAttributeValue(_valueAttributeHandle);
            }
            throw new BGAPIException(0xFF96, "BGBLECharacteristic read operation not supported");
        }

        // <summary>Reads attribute long value.</summary>
        /// <returns>Characteristic data structure.</returns>
        public BGBLECharacteristicData ReadLong()
        {
            if (_isReadSupported)
            {
                _blobData.count = 0;
                _blobData.data = null;

                return _service.ReadAttributeLongValue(_valueAttributeHandle);
            }
            throw new BGAPIException(0xFF96, "BGBLECharacteristic read long operation not supported");
        }

        /// <summary>Saves data for further use.</summary>
        /// <param name="data">Data to save</param>
        /// <param name="count">Data length</param>
        public void ValueRead(byte[] data, byte count)
        {
            _blobData.count = count;
            _blobData.data = data.Take(count).ToArray();
        }

        /// <summary>Saves data incrementally for further use.</summary>
        /// <param name="data">Data to save</param>
        /// <param name="count">Data length</param>
        public void ValueReadBlob(byte[] data, byte count)
        {
            _blobData.count += count;
            if (_blobData.data == null)
            {
                _blobData.data = data.Take(count).ToArray();
            }
            else
            {
                _blobData.data.Concat(data.Take(count).ToArray());
            }
        }

        /// <summary>Returns previously saved data.</summary>
        /// <returns>Characteristic data structure.</returns>
        public BGBLECharacteristicData ValueReadCompleted()
        {
            return _blobData;
        }

        /// <summary>Subscribes for attribute notifications.</summary>
        /// <param name="unsubscribe">Set to TRUE to unsubscribe</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort Subscribe(bool unsubscribe = false)
        {
            if (_isNotificationSupported)
            {
                return _service.SubscribeForAttributeNotification(_handle, unsubscribe);
            }
            throw new BGAPIException(0xFF96, "BGBLECharacteristic subscribe operation not supported");
        }

        // <summary>Writes data to attribute.</summary>
        /// <param name="data">Data to write</param>
        /// <param name="count">Data length</param>
        /// <param name="doNotWaiteCompletition">Do not wait for procedure completed event</param>
        /// <returns>Error code, 0x0000 if success.</returns>
        public ushort Write(byte[] data, ushort count, bool doNotWaiteCompletition = false)
        {
            if (_isWriteSupported)
            {
                return _service.WriteAttributeValue(_valueAttributeHandle, data, count, doNotWaiteCompletition);
            }
            throw new BGAPIException(0xFF96, "BGBLECharacteristic write operation not supported");
        }

        // OVERRIDED METHODS
        /// <summary>Returns string with BLE device characteristic details.</summary>
        public override string ToString()
        {
            string result = base.ToString() + "\nUUID: " + _uuid + " <" + _handle + ", " + _valueAttributeHandle + ">";

            result += "\nDescription: " + Description;

            result += "\n\tService UUID: " + _service.UUID;
            result += "\n\tBROADCAST: " + ((_isBroadcastSupported)? "Yes": "No");
            result += "\n\tREAD: " + ((_isReadSupported)? "Yes": "No");
            result += "\n\tNOTIFICATION: " + ((_isNotificationSupported)? "Yes": "No");
            result += "\n\tWRITE: " + ((_isWriteSupported)? "Yes": "No");
            result += "\n\tWRITE WITHOUT ACKNOWLEDGMENT: " + ((_isWriteWithouAcknowledgmentSupported) ? "Yes": "No");
            result += "\n\tINDICATION: " + ((_isIndicationSupported)? "Yes": "No");
            result += "\n\tAUTHENTICATED WRITE: " + ((_isAuthenticatedWrite)? "Yes": "No");
            result += "\n\tADDITIONAL PROPERTIES: " + ((_isAdditionalPropertiesAvailable)? "Yes": "No");

            return result;
        }
    }
}
