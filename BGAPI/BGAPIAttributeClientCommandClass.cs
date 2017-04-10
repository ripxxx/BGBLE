/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace BGBLE.BGAPI
{
    public enum BGAPIAttributeValueType : byte {
        /// <summary>Value was read.</summary>
        Read = 0,
        /// <summary>Value was notified.</summary>
        Notify = 1,
        /// <summary>Value was indicated.</summary>
        Indicate = 2,
        /// <summary>Value was read by type.</summary>
        ReadByType = 3,
        /// <summary>Value was part of a long attribute.</summary>
        ReadBlob = 4,
        /// <summary>Value was indicated and the remote device is waiting for a confirmation.</summary>
        IndicateRSPReq = 5
    }

    public class BGAPIAttributeClientCommandClassAttributeValueEventArgs : EventArgs
    {
        // <summary>Attribute value (data).</summary>
        public byte[] AttributeData { get; set; }

        /// <summary>Attribute data length.</summary>
        public byte AttributeDataLength { get; set; }

        /// <summary>Attribute handle.</summary>
        public ushort AttributeHandle { get; set; }

        /// <summary>Attribute type.</summary>
        public BGAPIAttributeValueType AttributeValueType { get; set; }

        /// <summary>Connection handle.</summary>
        public byte ConnectionHandle { get; set; }
    }
    public delegate void BGAPIAttributeClientCommandClassAttributeValueEventHandler(object sender, BGAPIAttributeClientCommandClassAttributeValueEventArgs e);

    public class BGAPIAttributeClientCommandClassFindInformationEventArgs : EventArgs
    {
        /// <summary>Attribute handle.</summary>
        public ushort AttributeHandle { get; set; }

        /// <summary>Attribute type (UUID).</summary>
        public string AttributeUUID { get; set; }

        /// <summary>Connection handle.</summary>
        public byte ConnectionHandle { get; set; }
    }
    public delegate void BGAPIAttributeClientCommandClassFindInformationEventHandler(object sender, BGAPIAttributeClientCommandClassFindInformationEventArgs e);

    public class BGAPIAttributeClientCommandClassGroupFoundEventArgs : EventArgs
    {
        /// <summary>Connection handle.</summary>
        public byte ConnectionHandle { get; set; }

        /// <summary>Ending handle. Note: "end" is a reserved word and in BGScript so "end" cannot be used as such.</summary>
        public ushort EndAttributeHandle { get; set; }

        /// <summary>UUID of a service. Length is 0 if no services are found.</summary>
        public string GroupUUID { get; set; }

        /// <summary>Starting handle.</summary>
        public ushort StartAttributeHandle { get; set; }
    }
    public delegate void BGAPIAttributeClientCommandClassGroupFoundEventHandler(object sender, BGAPIAttributeClientCommandClassGroupFoundEventArgs e);

    public class BGAPIAttributeClientCommandClassIndicateEventArgs : EventArgs
    {
        /// <summary>Attribute handle.</summary>
        public ushort AttributeHandle { get; set; }

        /// <summary>Connection handle.</summary>
        public byte ConnectionHandle { get; set; }
    }
    public delegate void BGAPIAttributeClientCommandClassIndicateEventHandler(object sender, BGAPIAttributeClientCommandClassIndicateEventArgs e);

    public class BGAPIAttributeClientCommandClassProcedureCompleteEventArgs : EventArgs
    {
        /// <summary>Attribute handle. Attribute handle at which the event ended.</summary>
        public ushort AttributeHandle { get; set; }

        /// <summary>Connection handle.</summary>
        public byte ConnectionHandle { get; set; }

        /// <summary>0: The operation was successful. Otherwise: attribute protocol error code returned by remote device</summary>
        public ushort Result { get; set; }
    }
    public delegate void BGAPIAttributeClientCommandClassProcedureCompleteEventHandler(object sender, BGAPIAttributeClientCommandClassProcedureCompleteEventArgs e);

    public class BGAPIAttributeClientCommandClassReadMultipleEventArgs : EventArgs
    {
        /// <summary>This array contains the concatenated data from the multiple attributes that have been read, up to 22 bytes.</summary>
        public byte[] AttributesData { get; set; }

        /// <summary>Connection handle.</summary>
        public byte ConnectionHandle { get; set; }
    }
    public delegate void BGAPIAttributeClientCommandClassReadMultipleEventHandler(object sender, BGAPIAttributeClientCommandClassReadMultipleEventArgs e);

    /// <summary>The Attribute Client class implements the Bluetooth Smart Attribute Protocol (ATT) and provides access to the
    /// ATT protocol methods.The Attribute Client class can be used to discover services and characteristics from the
    /// ATT server, read and write values and manage indications and notifications.</summary>
    /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
    class BGAPIAttributeClientCommandClass : BGAPICommandClass
    {
        private Dictionary<byte, List<BGAPIAttributeClientCommandClassAttributeValueEventArgs>> _eventsArgs;
        private Dictionary<byte, Thread> _eventsThreads;

        public const byte CLASS_ID = BGAPIDefinition.CCID_ATTRIBUTE_CLIENT;

        public event BGAPIAttributeClientCommandClassAttributeValueEventHandler AttributeValue;
        public event BGAPIAttributeClientCommandClassFindInformationEventHandler InformationFound;
        public event BGAPIAttributeClientCommandClassGroupFoundEventHandler GroupFound;
        public event BGAPIAttributeClientCommandClassIndicateEventHandler Indicated;
        public event BGAPIAttributeClientCommandClassProcedureCompleteEventHandler ProcedureCompleted;
        public event BGAPIAttributeClientCommandClassReadMultipleEventHandler ReadMultiple;

        public BGAPIAttributeClientCommandClass(BGAPIConnection connection) : base(connection)
        {
            _eventsArgs = new Dictionary<byte, List<BGAPIAttributeClientCommandClassAttributeValueEventArgs>>();
            _eventsThreads = new Dictionary<byte, Thread>();

            _connection.RegisterEventHandlerForCommandClass(CLASS_ID, (BGAPIConnectionEventData eventData) =>
            {
                switch (eventData.header.commandId)
                {
                    case BGAPIDefinition.ATTRIBUTE_CLIENT_EVENT_FIND_INFORMATION_FOUND:
                        if (eventData.header.payloadLength >= 4)
                        {
                            BGAPIAttributeClientCommandClassFindInformationEventArgs eventArgs = new BGAPIAttributeClientCommandClassFindInformationEventArgs();
                            eventArgs.ConnectionHandle = eventData.payload[0];
                            eventArgs.AttributeHandle = BitConverter.ToUInt16(eventData.payload.Skip(1).Take(2).ToArray(), 0);
                            byte _count = eventData.payload[3];
                            eventArgs.AttributeUUID = BitConverter.ToString(eventData.payload.Skip(4).Take(_count).Reverse().ToArray(), 0).Replace("-", "");
                            InformationFound?.Invoke(this, eventArgs);
                        }
                        break;
                    case BGAPIDefinition.ATTRIBUTE_CLIENT_EVENT_GROUP_FOUND:
                        if (eventData.header.payloadLength >= 5)
                        {
                            BGAPIAttributeClientCommandClassGroupFoundEventArgs eventArgs = new BGAPIAttributeClientCommandClassGroupFoundEventArgs();
                            eventArgs.ConnectionHandle = eventData.payload[0];
                            eventArgs.StartAttributeHandle = BitConverter.ToUInt16(eventData.payload.Skip(1).Take(2).ToArray(), 0);
                            eventArgs.EndAttributeHandle = BitConverter.ToUInt16(eventData.payload.Skip(3).Take(2).ToArray(), 0);
                            if (eventData.header.payloadLength > 5)
                            {
                                byte _count = eventData.payload[5];
                                eventArgs.GroupUUID = BitConverter.ToString(eventData.payload.Skip(6).Take(_count).Reverse().ToArray()).Replace("-", "");
                            }
                            GroupFound?.Invoke(this, eventArgs);
                        }
                        break;
                    case BGAPIDefinition.ATTRIBUTE_CLIENT_EVENT_INDICATED:
                        if (eventData.header.payloadLength == 3)
                        {
                            BGAPIAttributeClientCommandClassIndicateEventArgs eventArgs = new BGAPIAttributeClientCommandClassIndicateEventArgs();
                            eventArgs.ConnectionHandle = eventData.payload[0];
                            eventArgs.AttributeHandle = BitConverter.ToUInt16(eventData.payload.Skip(1).Take(2).ToArray(), 0);
                            Indicated?.Invoke(this, eventArgs);
                        }
                        break;
                    case BGAPIDefinition.ATTRIBUTE_CLIENT_EVENT_PROCEDURE_COMPLETED:
                        if (eventData.header.payloadLength == 5)
                        {
                            BGAPIAttributeClientCommandClassProcedureCompleteEventArgs eventArgs = new BGAPIAttributeClientCommandClassProcedureCompleteEventArgs();
                            eventArgs.ConnectionHandle = eventData.payload[0];
                            eventArgs.Result = BitConverter.ToUInt16(eventData.payload.Skip(1).Take(2).ToArray(), 0);
                            eventArgs.AttributeHandle = BitConverter.ToUInt16(eventData.payload.Skip(3).Take(2).ToArray(), 0);
                            ProcedureCompleted?.Invoke(this, eventArgs);
                        }
                        break;
                    case BGAPIDefinition.ATTRIBUTE_CLIENT_EVENT_READ_MULTIPLE_RESPONSE:
                        if (eventData.header.payloadLength >= 3)
                        {
                            BGAPIAttributeClientCommandClassReadMultipleEventArgs eventArgs = new BGAPIAttributeClientCommandClassReadMultipleEventArgs();
                            eventArgs.ConnectionHandle = eventData.payload[0];
                            byte _count = eventData.payload[1];
                            eventArgs.AttributesData = eventData.payload.Skip(2).Take(_count).ToArray();
                            ReadMultiple?.Invoke(this, eventArgs);
                        }
                        break;
                    default:
                        break;
                }
            });
            _connection.RegisterEventHandlerForEvent(CLASS_ID, BGAPIDefinition.ATTRIBUTE_CLIENT_EVENT_ATTRIBUTE_VALUE, (BGAPIConnectionEventData eventData) => {
                if (eventData.header.payloadLength >= 5)
                {
                    BGAPIAttributeValueType attributeValueType = (BGAPIAttributeValueType)eventData.payload[3];

                    if (!_eventsArgs.ContainsKey((byte)attributeValueType))
                    {
                        _eventsArgs[(byte)attributeValueType] = new List<BGAPIAttributeClientCommandClassAttributeValueEventArgs>();
                    }

                    if (!_eventsThreads.ContainsKey((byte)attributeValueType))
                    {
                        Thread eventThread = new Thread(() => {
                            byte t_threadId = (byte)attributeValueType;

                            while (_connection.IsOpen)
                            {
                                if (_eventsArgs[t_threadId].Count > 0)
                                {
                                    BGAPIAttributeClientCommandClassAttributeValueEventArgs t_eventArgs = _eventsArgs[t_threadId].First();
                                    if (t_eventArgs != null) {
                                        AttributeValue?.Invoke(this, t_eventArgs);
                                    }

                                    _eventsArgs[t_threadId].RemoveAt(0);
                                }
                                Thread.Sleep(2);
                            }
                        });
                        _eventsThreads[(byte)attributeValueType] = eventThread;
                        eventThread.Name = "EventThread_AttributeValue_" + attributeValueType;
                        eventThread.Start();
                    }

                    BGAPIAttributeClientCommandClassAttributeValueEventArgs eventArgs = new BGAPIAttributeClientCommandClassAttributeValueEventArgs();
                    eventArgs.ConnectionHandle = eventData.payload[0];
                    eventArgs.AttributeHandle = BitConverter.ToUInt16(eventData.payload.Skip(1).Take(2).ToArray(), 0);
                    eventArgs.AttributeValueType = attributeValueType;
                    byte _count = eventData.payload[4];
                    eventArgs.AttributeDataLength = _count;
                    eventArgs.AttributeData = eventData.payload.Skip(5).Take(_count).ToArray();

                    _eventsArgs[(byte)attributeValueType].Add(eventArgs);


                    /*BGAPIAttributeClientCommandClassAttributeValueEventArgs eventArgs = new BGAPIAttributeClientCommandClassAttributeValueEventArgs();
                    eventArgs.ConnectionHandle = eventData.payload[0];
                    eventArgs.AttributeHandle = BitConverter.ToUInt16(eventData.payload.Skip(1).Take(2).ToArray(), 0);
                    eventArgs.AttributeValueType = attributeValueType;
                    byte _count = eventData.payload[4];
                    eventArgs.AttributeDataLength = _count;
                    eventArgs.AttributeData = eventData.payload.Skip(5).Take(_count).ToArray();
                    AttributeValue?.Invoke(this, eventArgs);*/
                }
            });
        }

        /// <summary>This command can be used to execute or cancel a previously queued prepare_write command on a remote device.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="commit">Commit: TRUE - commit, FALSE - cancel</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <event name="Procedure Completed">This event is produced at the GATT client when an attribute protocol event is completed a and new operation can be issued.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
        public ushort ExecuteWrite(byte connectionHandle, bool commit)//commit: true - commit, false - cancel
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.ATTRIBUTE_COMMAND_EXECUTE_WRITE, new byte[] { connectionHandle, (byte)((commit) ? 0x01 : 0x00) }, 0x02);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command can be used to find specific attributes on a remote device based on their 16-bit UUID value and value.The search can be limited by a starting and ending handle values.
        /// The command returns the handles of all attributes matching the type (UUID) and value.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributeUUID">2 octet UUID to find</param>
        /// <param name="startHandle">First requested handle number</param>
        /// <param name="endHandle">Last requested handle number</param>
        /// <param name="data">Attribute value to find</param>
        /// <param name="count">Data length &lt;=20</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <event name="Group Foundd">This event is produced when an attribute group (a service) is found. Typically this event is produced after Read by Group Type command.</event>
        /// <event name="Procedure Completed">This event is produced at the GATT client when an attribute protocol event is completed a and new operation can be issued.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
        public ushort FindByTypeValue(byte connectionHandle, ushort attributeUUID, ushort startHandle, ushort endHandle, byte[] data, byte count)
        {
            byte payloadLength = (byte)((count > 20) ? 20 : count); //Data Length
            payloadLength += 1;                                     //Byte for data length - count
            payloadLength += 7;                                     //sizeof(connectionHandle) + sizeof(attributeHandle) + sizeof(offset)

            byte[] payload = new byte[payloadLength];

            payload[0] = connectionHandle;

            byte[] _startHandle = BitConverter.GetBytes(startHandle).ToArray();
            Array.Copy(_startHandle, 0, payload, 1, 2);

            byte[] _endHandle = BitConverter.GetBytes(endHandle).ToArray();
            Array.Copy(_endHandle, 0, payload, 3, 2);

            byte[] _attributeUUID = BitConverter.GetBytes(attributeUUID).ToArray();
            Array.Copy(_attributeUUID, 0, payload, 5, 2);

            payload[7] = count;
            Array.Copy(data, 0, payload, 8, count);

            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.ATTRIBUTE_COMMAND_FIND_BY_TYPE_VALUE, payload, payloadLength);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command is used to discover attribute handles and their types (UUIDs) in a given handle range.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="startHandle">First requested handle number</param>
        /// <param name="endHandle">Last requested handle number</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <event name="Find Information Found">This event is generated when characteristics type mappings are found. This happens yypically after Find Information command has been issued to discover all attributes of a service.</event>
        /// <event name="Procedure Completed">This event is produced at the GATT client when an attribute protocol event is completed a and new operation can be issued.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
        public ushort FindInformation(byte connectionHandle, ushort startHandle, ushort endHandle)
        {                                   //sizeof(connectionHandle) + sizeof(attributeHandle) + sizeof(offset)
            byte[] payload = new byte[5];

            payload[0] = connectionHandle;

            byte[] _startHandle = BitConverter.GetBytes(startHandle).ToArray();
            Array.Copy(_startHandle, 0, payload, 1, 2);

            byte[] _endHandle = BitConverter.GetBytes(endHandle).ToArray();
            Array.Copy(_endHandle, 0, payload, 3, 2);

            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.ATTRIBUTE_COMMAND_FIND_INFORMATION, payload, 5);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command can be used to send a acknowledge a received indication from a remote device. This function
        /// allows the application to manually confirm the indicated values instead of the Bluetooth smart stack
        /// automatically doing it.The benefit of this is extra reliability since the application can for example store the
        /// received value on the flash memory before confirming the indication to the remote device.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
        public ushort IndicateConfirm(byte connectionHandle)
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.ATTRIBUTE_COMMAND_INDICATE_CONFIRM, new byte[] { connectionHandle }, 0x01);
            if (response.length == 2)
            {
                return BitConverter.ToUInt16(response.data.Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command will send a prepare write request to a remote device for queued writes. Queued writes can for
        /// example be used to write large attribute values by transmitting the data in chunks using prepare write
        /// command. Once the data has been transmitted with multiple prepare write commands the write must then be executed or
        /// canceled with Execute Write command, which if acknowledged by the remote device triggers a Procedure Completed event.
        /// Use it se to write more than 20 bytes, but 18 bytes per time, then call ExecuteWrite.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="data">Data to write</param>
        /// <param name="offset">Offset for writing</param>
        /// <param name="count">Data length &lt;=20</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <event name="Procedure Completed">This event is produced at the GATT client when an attribute protocol event is completed a and new operation can be issued.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
        public ushort PrepareWriteAttributeByHandle(byte connectionHandle, ushort attributeHandle, byte[] data, ushort offset, byte count)
        {
            byte payloadLength = (byte)((count > 18) ? 18 : count); //Data Length
            payloadLength += 1;                                     //Byte for data length - count
            payloadLength += 5;                                     //sizeof(connectionHandle) + sizeof(attributeHandle) + sizeof(offset)

            byte[] payload = new byte[payloadLength];

            payload[0] = connectionHandle;

            byte[] _attributeHandle = BitConverter.GetBytes(attributeHandle).ToArray();
            Array.Copy(_attributeHandle, 0, payload, 1, 2);

            byte[] _offset = BitConverter.GetBytes(offset).ToArray();
            Array.Copy(_offset, 0, payload, 3, 2);

            payload[5] = count;
            Array.Copy(data, 0, payload, 6, count);

            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.ATTRIBUTE_COMMAND_PREPARE_WRITE, payload, payloadLength);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command reads the value of each attribute of a given type and in a given handle range. The command is typically used for primary(UUID: 0x2800) and secondary(UUID: 0x2801) service discovery.
        /// Discovered services are reported by Group Found event. Finally when the procedure is completed a Procedure Completed event is generated.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="groupUUID">Group UUID to find</param>
        /// <param name="startHandle">First requested handle number</param>
        /// <param name="endHandle">Last requested handle number</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <event name="Group Foundd">This event is produced when an attribute group (a service) is found. Typically this event is produced after Read by Group Type command.</event>
        /// <event name="Procedure Completed">This event is produced at the GATT client when an attribute protocol event is completed a and new operation can be issued.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
        public ushort ReadByGroupType(byte connectionHandle, string groupUUID, ushort startHandle, ushort endHandle)
        {
            int _groupUUIDLength = (groupUUID.Length / 2);
            byte[] _groupUUID = new byte[_groupUUIDLength];
            for (int i = 0; i < _groupUUIDLength; i++)
            {
                int j = (i * 2);
                int k = ((_groupUUIDLength - 1) - i);
                _groupUUID[k] = Convert.ToByte(groupUUID.Substring(j, 2), 16);
            }

            byte payloadLength = (byte)_groupUUIDLength;            //Group Type UUID Length
            payloadLength += 1;                                     //Byte for data length - count
            payloadLength += 5;                                     //sizeof(connectionHandle) + sizeof(attributeHandle) + sizeof(offset)

            byte[] payload = new byte[payloadLength];

            payload[0] = connectionHandle;

            byte[] _startHandle = BitConverter.GetBytes(startHandle).ToArray();
            Array.Copy(_startHandle, 0, payload, 1, 2);

            byte[] _endHandle = BitConverter.GetBytes(endHandle).ToArray();
            Array.Copy(_endHandle, 0, payload, 3, 2);

            payload[5] = (byte)_groupUUID.Length;
            Array.Copy(_groupUUID, 0, payload, 6, _groupUUID.Length);

            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.ATTRIBUTE_COMMAND_READ_BY_GROUP_TYPE, payload, payloadLength);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command reads a remote attribute's value with the given handle. Read by handle can be used to read
        /// attributes up to 22 bytes long. For longer attributes Read Long command must be used.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <event name="Attribute Value">This event is produced at the GATT client side when an attribute value is passed from the GATT server to the GATT client.This event is for example produced after a successful Read by Handle operation or when an attribute is indicated or notified by the remote device.</event>
        /// <event name="Procedure Completed">This event is produced at the GATT client when an attribute protocol event is completed a and new operation can be issued.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
        public ushort ReadAttributeByHandle(byte connectionHandle, ushort attributeHandle)
        {
            byte[] payload = new byte[3];

            payload[0] = connectionHandle;

            byte[] _attributeHandle = BitConverter.GetBytes(attributeHandle).ToArray();
            Array.Copy(_attributeHandle, 0, payload, 1, 2);

            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.ATTRIBUTE_COMMAND_READ_BY_HANDLE, payload, 3);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command can be used to read long attribute values, which are longer than 22 bytes and cannot be read
        /// with a simple Read by Handle command.
        /// The command starts a procedure, where the client first sends a normal read command to the server and if the
        /// returned attribute value length is equal to MTU, the client will send further read long read requests until rest of
        /// the attribute is read.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <event name="Attribute Value">This event is produced at the GATT client side when an attribute value is passed from the GATT server to the GATT client.This event is for example produced after a successful Read by Handle operation or when an attribute is indicated or notified by the remote device.</event>
        /// <event name="Procedure Completed">This event is produced at the GATT client when an attribute protocol event is completed a and new operation can be issued.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
        public ushort ReadAttributeByHandleLong(byte connectionHandle, ushort attributeHandle)
        {
            byte[] payload = new byte[3];

            payload[0] = connectionHandle;

            byte[] _attributeHandle = BitConverter.GetBytes(attributeHandle).ToArray();
            Array.Copy(_attributeHandle, 0, payload, 1, 2);

            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.ATTRIBUTE_COMMAND_READ_LONG, payload, 3);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command can be used to read multiple attributes from a server.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributesHandles">Attribute handles - List of attribute handles to read from the remote device</param>
        /// <param name="attributesHandles">Attribute handles lengths</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <event name="Read Multiple Response">This event is a response to a Read Multiple request.</event>
        /// <event name="Procedure Completed">This event is produced at the GATT client when an attribute protocol event is completed a and new operation can be issued.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
        public ushort ReadAttributeByHandles(byte connectionHandle, ushort[] attributesHandles, byte count)
        {
            byte _count = (byte)(count * sizeof(ushort));
            byte payloadLength = (byte)((_count > 19) ? 19 : _count);   //Data Length
            payloadLength += 1;                                         //Byte for data length - count
            payloadLength += 1;                                         //sizeof(connectionHandle)

            byte[] payload = new byte[payloadLength];

            payload[0] = connectionHandle;

            byte[] _attributesHandles = new byte[_count];
            Buffer.BlockCopy(attributesHandles, 0, _attributesHandles, 0, (_count / 2));
            payload[1] = _count;
            Array.Copy(_attributesHandles, 0, payload, 2, _count);

            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.ATTRIBUTE_COMMAND_READ_MULTIPLE, payload, payloadLength);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command reads the value of each attribute of a given type and in a given handle range. The command is typically used for primary(UUID: 0x2800) and secondary(UUID: 0x2801) service discovery.
        /// Discovered services are reported by Group Found event. Finally when the procedure is completed a Procedure Completed event is generated.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributeUUID">Attribute type (UUID)</param>
        /// <param name="startHandle">First requested handle number</param>
        /// <param name="endHandle">Last requested handle number</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <event name="Attribute Value">This event is produced at the GATT client side when an attribute value is passed from the GATT server to the GATT client.This event is for example produced after a successful Read by Handle operation or when an attribute is indicated or notified by the remote device.</event>
        /// <event name="Procedure Completed">This event is produced at the GATT client when an attribute protocol event is completed a and new operation can be issued.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
        public ushort ReadAttributeByType(byte connectionHandle, string attributeUUID, ushort startHandle, ushort endHandle)
        {
            int _attributeUUIDLength = (attributeUUID.Length / 2);
            byte[] _attributeUUID = new byte[_attributeUUIDLength];
            for (int i = 0; i < _attributeUUIDLength; i++)
            {
                int j = (i * 2);
                int k = ((_attributeUUIDLength - 1) - i);
                _attributeUUID[k] = Convert.ToByte(attributeUUID.Substring(j, 2), 16);
            }

            byte payloadLength = (byte)_attributeUUIDLength;        //Attribute Type UUID Length
            payloadLength += 1;                                     //Byte for data length - count
            payloadLength += 5;                                     //sizeof(connectionHandle) + sizeof(attributeHandle) + sizeof(offset)

            byte[] payload = new byte[payloadLength];

            payload[0] = connectionHandle;

            byte[] _startHandle = BitConverter.GetBytes(startHandle).ToArray();
            Array.Copy(_startHandle, 0, payload, 1, 2);

            byte[] _endHandle = BitConverter.GetBytes(endHandle).ToArray();
            Array.Copy(_endHandle, 0, payload, 3, 2);

            payload[5] = (byte)_attributeUUID.Length;
            Array.Copy(_attributeUUID, 0, payload, 6, _attributeUUID.Length);

            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.ATTRIBUTE_COMMAND_READ_BY_TYPE_VALUE, payload, payloadLength);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>Writes the value of a remote devices attribute. The handle and the new value of the attribute are gives as parameters.
        /// Write command will not be acknowledged by the remote device.
        /// The maximum data payload for Write Command is 20 bytes</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="data">Data to write</param>
        /// <param name="count">Data length &lt;=20</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
        public ushort WriteAttributeByHandle(byte connectionHandle, ushort attributeHandle, byte[] data, byte count)
        {
            byte payloadLength = (byte)((count > 20) ? 20 : count); //Data Length
            payloadLength += 1;                                     //Byte for data length - count
            payloadLength += 3;                                     //sizeof(connectionHandle) + sizeof(attributeHandle)

            byte[] payload = new byte[payloadLength];

            payload[0] = connectionHandle;

            byte[] _attributeHandle = BitConverter.GetBytes(attributeHandle).ToArray();
            Array.Copy(_attributeHandle, 0, payload, 1, 2);

            payload[3] = count;
            Array.Copy(data, 0, payload, 4, count);

            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.ATTRIBUTE_COMMAND_WRITE, payload, payloadLength);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }

        /// <summary>This command can be used to write an attributes value on a remote device. In order to write the value of an
        /// attribute a Bluetooth connection must exists and you need to know the handle of the attribute you want to write.
        /// A successful attribute write will be acknowledged by the remote device and this will generate an event
        /// attclient_procedure_completed.The acknowledgement should happen within a 30 second window or otherwise
        /// the Bluetooth connection will be dropped.
        /// The data payload for the Attribute Write command can be up to 20 bytes.</summary>
        /// <param name="connectionHandle">Connection handle</param>
        /// <param name="attributeHandle">Attribute handle</param>
        /// <param name="data">Data to write</param>
        /// <param name="count">Data length &lt;=20</param>
        /// <returns>Returns error code - 0x00 means connection procedure successfully started.</returns>
        /// <event name="Procedure Completed">This event is produced at the GATT client when an attribute protocol event is completed a and new operation can be issued.</event>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.1 Attribute Client)</seealso>
        public ushort WriteAttributeByHandleWithAcknowledgment(byte connectionHandle, ushort attributeHandle, byte[] data, byte count)
        {

            byte payloadLength = (byte)((count > 20) ? 20 : count); //Data Length
            payloadLength += 1;                                     //Byte for data length - count
            payloadLength += 3;                                     //sizeof(connectionHandle) + sizeof(attributeHandle)

            byte[] payload = new byte[payloadLength];

            payload[0] = connectionHandle;

            byte[] _attributeHandle = BitConverter.GetBytes(attributeHandle).ToArray();
            Array.Copy(_attributeHandle, 0, payload, 1, 2);

            payload[3] = count;
            Array.Copy(data, 0, payload, 4, count);

            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.ATTRIBUTE_COMMAND_ATTRIBUTE_WRITE, payload, payloadLength);
            if (response.length == 3)
            {
                return BitConverter.ToUInt16(response.data.Skip(1).Take(2).ToArray(), 0);
            }
            return 0xFFFE;
        }
    }
}
