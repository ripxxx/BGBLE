/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Collections.Generic;

namespace BGBLE.BGAPI
{
    /// <summary>Command class details structure for debug purpose.</summary>
    public interface IBGAPICommandClassInfo
    {
        /// <summary>Command class description.</summary>
        string description { get; }
        /// <summary>Command class id.</summary>
        ushort id { get; }
        /// <summary>Command class name.</summary>
        string name { get; }


    }

    /// <summary>Command details structure for debug purpose.</summary>
    public interface IBGAPICommandInfo
    {
        /// <summary>Command class info.</summary>
        IBGAPICommandClassInfo commandClass { get; }
        /// <summary>Command description.</summary>
        string description { get; }
        /// <summary>Command id.</summary>
        ushort id { get; }
        /// <summary>Command name.</summary>
        string name { get; }
    }

    /// <summary>Error details structure.</summary>
    public interface IBGAPIErrorInfo
    {
        /// <summary>Error description.</summary>
        string description { get; }
        /// <summary>Error group.</summary>
        string group { get; }
        /// <summary>Error code.</summary>
        ushort id { get; }
        /// <summary>Error name.</summary>
        string name { get; }
    }

    /// <summary>BG API Exception.</summary>
    public class BGAPIException : Exception
    {
        private ushort _errorCode;
        private string _errorGroup;

        public BGAPIException(ushort errorCode) : base(BGAPIDefinition.FindErrorByCode(errorCode).description)
        {
            _errorCode = errorCode;
            _errorGroup = BGAPIDefinition.FindErrorByCode(errorCode).group;
        }

        public BGAPIException(ushort errorCode, Exception innerException) : base(BGAPIDefinition.FindErrorByCode(errorCode).description, innerException)
        {
            _errorCode = errorCode;
            _errorGroup = BGAPIDefinition.FindErrorByCode(errorCode).group;
        }

        public BGAPIException(ushort errorCode, string message) : base(message)
        {
            _errorCode = errorCode;
            _errorGroup = BGAPIDefinition.FindErrorByCode(errorCode).group;
        }

        public BGAPIException(ushort errorCode, string message, Exception innerException) : base(message, innerException)
        {
            _errorCode = errorCode;
            _errorGroup = BGAPIDefinition.FindErrorByCode(errorCode).group;
        }

        //PROPRTIES
        /// <summary>Error code.</summary>
        public int ErrorCode
        {
            get { return _errorCode; }
        }

        /// <summary>Error group.</summary>
        public string ErrorGroup
        {
            get { return _errorGroup; }
        }
        //PROPRTIES
    }

    /// <summary>Command class details structure for debug purpose.</summary>
    struct BGAPICommandClassInfo : IBGAPICommandClassInfo
    {
        /// <summary>Command class description.</summary>
        public string description { get; }
        /// <summary>Command class id.</summary>
        public ushort id { get; }
        /// <summary>Command class name.</summary>
        public string name { get; }

        public BGAPICommandClassInfo(ushort _id, string _name, string _description)
        {
            description = _description;
            id = _id;
            name = _name;
        }

        public override string ToString()
        {
            string result = name + "(";

            result += id.ToString("X");

            return result + ")";
        }
    }

    /// <summary>Command details structure for debug purpose.</summary>
    struct BGAPICommandInfo : IBGAPICommandInfo
    {
        /// <summary>Command class info.</summary>
        public IBGAPICommandClassInfo commandClass { get; }
        /// <summary>Command description.</summary>
        public string description { get; }
        /// <summary>Command id.</summary>
        public ushort id { get; }
        /// <summary>Command name.</summary>
        public string name { get; }

        public BGAPICommandInfo(ushort _id, string _name, string _description, IBGAPICommandClassInfo _commandClass)
        {
            commandClass = _commandClass;
            description = _description;
            id = _id;
            name = _name;
        }

        public override string ToString()
        {
            string result = name + "(";

            result += id.ToString("X");
            result += ", " + commandClass.ToString();

            return result + ")";
        }
    }

    /// <summary>Error details structure.</summary>
    public struct BGAPIErrorInfo : IBGAPIErrorInfo
    {
        /// <summary>Error description.</summary>
        public string description { get; }
        /// <summary>Error group.</summary>
        public string group { get; }
        /// <summary>Error code.</summary>
        public ushort id { get; }
        /// <summary>Error name.</summary>
        public string name { get; }

        public BGAPIErrorInfo(ushort _id, string _name, string _group, string _description)
        {
            description = _description;
            group = _group;
            id = _id;
            name = _name;
        }

        public override string ToString()
        {
            string result = name + "(";

            result += id.ToString("X");
            result += ", " + group;
            result += ", " + description;

            return result + ")";
        }
    }

    /// <summary>This class API constants and describes the error codes the API commands may produce.</summary>
    /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(4 API definition, 5.11 Error Codes)</seealso>
    public class BGAPIDefinition
    {
        /// <summary>Commands classes.</summary>
        private static Lazy<Dictionary<byte, IBGAPICommandClassInfo>> _commandsClasses = new Lazy<Dictionary<byte, IBGAPICommandClassInfo>>(() => GetCommandsClasses());
        /// <summary>Available commands.</summary>
        private static Lazy<Dictionary<ushort, IBGAPICommandInfo>> _commands = new Lazy<Dictionary<ushort, IBGAPICommandInfo>>(() => GetCommands());
        /// <summary>BGAPI and BGBLE software stack errors.</summary>
        private static Lazy<Dictionary<ushort, IBGAPIErrorInfo>> _errors = new Lazy<Dictionary<ushort, IBGAPIErrorInfo>>(() => GetErrors());
        /// <summary>Available events.</summary>
        private static Lazy<Dictionary<ushort, IBGAPICommandInfo>> _events = new Lazy<Dictionary<ushort, IBGAPICommandInfo>>(() => GetEvents());
        //GENERAL
        /// <summary>Maximum payload length.</summary>
        public const ushort COMMAND_PAYLOAD_MAX_LENGTH = 2047;
        //Message Types
        /// <summary>BGAPI message type is command or response(command from host to the stack or response from stack to the host).</summary>
        public const byte MT_COMMAND_RESPONSE = 0x00;
        /// <summary>BGAPI message type is event(event from stack to the host).</summary>
        public const byte MT_EVENT = 0x80;
        //Technology Types
        /// <summary>Technology type is Bluetooth.</summary>
        public const byte TT_BLUETOOTH_SMART = 0x00;
        /// <summary>Technology type is WIFI.</summary>
        public const byte TT_WIFI = 0x08;
        //Command Class IDs
        /// <summary>Provides access to system functions.</summary>
        public const byte CCID_SYSTEM = 0x00;
        /// <summary>Provides access the persistence store (parameters).</summary>
        public const byte CCID_PERSISTENT_STORE = 0x01;
        /// <summary>Provides access to local GATT database.</summary>
        public const byte CCID_ATTRIBUTE_DATABASE = 0x02;
        /// <summary>Provides access to connection management functions.</summary>
        public const byte CCID_CONNECTION = 0x03;
        /// <summary>Functions to access remote devices GATT database.</summary>
        public const byte CCID_ATTRIBUTE_CLIENT = 0x04;
        /// <summary>Bluetooth low energy security functions.</summary>
        public const byte CCID_SECURITY_MANAGER = 0x05;
        /// <summary>GAP functions.</summary>
        public const byte CCID_GAP = 0x06;
        /// <summary>Provides access to hardware such as timers and ADC.</summary>
        public const byte CCID_HARDWARE = 0x07;

        //ATTRIBUTE CLIENT
        //ATTRIBUTE CLIENT - COMMANDS
        public const byte ATTRIBUTE_COMMAND_FIND_BY_TYPE_VALUE = 0x00;          //+
        public const byte ATTRIBUTE_COMMAND_READ_BY_GROUP_TYPE = 0x01;          //+
        public const byte ATTRIBUTE_COMMAND_READ_BY_TYPE_VALUE = 0x02;          //+
        public const byte ATTRIBUTE_COMMAND_FIND_INFORMATION = 0x03;            //+
        public const byte ATTRIBUTE_COMMAND_READ_BY_HANDLE = 0x04;              //+
        public const byte ATTRIBUTE_COMMAND_ATTRIBUTE_WRITE = 0x05;             //+
        public const byte ATTRIBUTE_COMMAND_WRITE = 0x06;                       //+
        public const byte ATTRIBUTE_COMMAND_INDICATE_CONFIRM = 0x07;            //+
        public const byte ATTRIBUTE_COMMAND_READ_LONG = 0x08;                   //+
        public const byte ATTRIBUTE_COMMAND_PREPARE_WRITE = 0x09;               //+
        public const byte ATTRIBUTE_COMMAND_EXECUTE_WRITE = 0x0A;               //+
        public const byte ATTRIBUTE_COMMAND_READ_MULTIPLE = 0x0B;               //+
        //ATTRIBUTE CLIENT - Events
        public const byte ATTRIBUTE_CLIENT_EVENT_INDICATED = 0x00;              //+
        public const byte ATTRIBUTE_CLIENT_EVENT_PROCEDURE_COMPLETED = 0x01;    //+
        public const byte ATTRIBUTE_CLIENT_EVENT_GROUP_FOUND = 0x02;            //+
        public const byte ATTRIBUTE_CLIENT_EVENT_FIND_INFORMATION_FOUND = 0x04; //+
        public const byte ATTRIBUTE_CLIENT_EVENT_ATTRIBUTE_VALUE = 0x05;        //+
        public const byte ATTRIBUTE_CLIENT_EVENT_READ_MULTIPLE_RESPONSE = 0x06; //+

        //CONNECTION
        //CONNECTION - COMMANDS
        public const byte CONNECTION_COMMAND_DISCONNECT = 0x00;                 //+
        public const byte CONNECTION_COMMAND_GET_RSSI = 0x01;                   //+
        public const byte CONNECTION_COMMAND_UPDATE = 0x02;                     //+
        public const byte CONNECTION_COMMAND_VERSION_UPDATE = 0x03;             //+
        public const byte CONNECTION_COMMAND_GET_STATUS = 0x07;                 //+
        //CONNECTION - Events
        public const byte CONNECTION_EVENT_STATUS = 0x00;                       //+
        public const byte CONNECTION_EVENT_VERSION_IND = 0x01;                  //+
        public const byte CONNECTION_EVENT_FEATURE_IND = 0x02;                  //+
        public const byte CONNECTION_EVENT_DISCONNECTED = 0x04;                 //+

        //GAP
        //GAP - COMMANDS
        public const byte GAP_COMMAND_SET_PRIVACY_FLAGS = 0x00;
        public const byte GAP_COMMAND_SET_MODE = 0x01;
        public const byte GAP_COMMAND_DISCOVER = 0x02;                          //+
        public const byte GAP_COMMAND_CONNECT_DIRECT = 0x03;                    //+
        public const byte GAP_COMMAND_END_PROCEDURE = 0x04;                     //+
        public const byte GAP_COMMAND_CONNECT_SELECTIVE = 0x05;
        public const byte GAP_COMMAND_SET_FILTERING = 0x06;
        public const byte GAP_COMMAND_SET_SCAN_PARAMETERS = 0x07;               //+
        public const byte GAP_COMMAND_SET_ADV_PARAMETERS = 0x08;
        public const byte GAP_COMMAND_SET_ADV_DATA = 0x09;
        public const byte GAP_COMMAND_SET_DIRECTED_CONNECTABLE_MODE = 0x0A;
        //GAP - Events
        public const byte GAP_EVENT_SCAN = 0x00;                                //+

        //SYSTEM
        //SYSTEM - COMMANDS
        public const byte SYSTEM_COMMAND_RESET = 0x00;                          //+
        public const byte SYSTEM_COMMAND_HELLO = 0x01;                          //+
        public const byte SYSTEM_COMMAND_GET_ADDRESS = 0x02;                    //+
        public const byte SYSTEM_COMMAND_GET_COUNTERS = 0x05;
        public const byte SYSTEM_COMMAND_GET_CONNECTIONS = 0x06;
        public const byte SYSTEM_COMMAND_GET_INFO = 0x08;                       //+
        public const byte SYSTEM_COMMAND_ENDPOINT_TX = 0x09;
        public const byte SYSTEM_COMMAND_WHITELIST_APPEND = 0x0A;
        public const byte SYSTEM_COMMAND_WHITELIST_REMOVE = 0x0B;
        public const byte SYSTEM_COMMAND_WHITELIST_CLEAR = 0x0C;
        public const byte SYSTEM_COMMAND_ENDPOINT_RX = 0x0D;
        public const byte SYSTEM_COMMAND_ENDPOINT_SET_WATERMARKS = 0x0E;
        public const byte SYSTEM_COMMAND_AES_SET_KEY = 0x0F;
        public const byte SYSTEM_COMMAND_AES_ENCRYPT = 0x10;
        public const byte SYSTEM_COMMAND_AES_DECRYPT = 0x11;
        //SYSTEM - Events
        public const byte SYSTEM_EVENT_BOOT = 0x00;
        public const byte SYSTEM_EVENT_ENDPOINT_WATERMARK_RX = 0x02;
        public const byte SYSTEM_EVENT_ENDPOINT_WATERMARK_TX = 0x03;
        public const byte SYSTEM_EVENT_SCRIPT_FAILURE = 0x04;
        public const byte SYSTEM_EVENT_NO_LICENSE_KEY = 0x05;
        public const byte SYSTEM_EVENT_PROTOCOL_ERROR = 0x06;

        private static Dictionary<byte, IBGAPICommandClassInfo> GetCommandsClasses()
        {
            return new Dictionary<byte, IBGAPICommandClassInfo>() {
                { CCID_SYSTEM, new BGAPICommandClassInfo(CCID_SYSTEM, "CCID_SYSTEM", "System - Provides access to system functions") },
                { CCID_PERSISTENT_STORE, new BGAPICommandClassInfo(CCID_PERSISTENT_STORE, "CCID_PERSISTENT_STORE", "Persistent Store - Provides access the persistence store (parameters)") },
                { CCID_ATTRIBUTE_DATABASE, new BGAPICommandClassInfo(CCID_ATTRIBUTE_DATABASE, "CCID_ATTRIBUTE_DATABASE", "Attribute database - Provides access to local GATT database") },
                { CCID_CONNECTION, new BGAPICommandClassInfo(CCID_CONNECTION, "CCID_CONNECTION", "Connection - Provides access to connection management functions") },
                { CCID_ATTRIBUTE_CLIENT, new BGAPICommandClassInfo(CCID_ATTRIBUTE_CLIENT, "CCID_ATTRIBUTE_CLIENT", "Attribute client - Functions to access remote devices GATT database") },
                { CCID_SECURITY_MANAGER, new BGAPICommandClassInfo(CCID_SECURITY_MANAGER, "CCID_SECURITY_MANAGER", "Security Manager - Bluetooth low energy security functions") },
                { CCID_GAP, new BGAPICommandClassInfo(CCID_GAP, "CCID_GAP", "Generic Access Profile - GAP functions") },
                { CCID_HARDWARE, new BGAPICommandClassInfo(CCID_HARDWARE, "CCID_HARDWARE", "Hardware - Provides access to hardware such as timers and ADC") },
            };
        }

        private static Dictionary<ushort, IBGAPICommandInfo> GetCommands()
        {
            Dictionary<ushort, IBGAPICommandInfo> commands = new Dictionary<ushort, IBGAPICommandInfo>();

            if (_commandsClasses.Value.ContainsKey(CCID_SYSTEM))
            {
                var eventId = (ushort)(CCID_SYSTEM << 8);
                //15
                commands[(ushort)(eventId + SYSTEM_COMMAND_RESET)] = new BGAPICommandInfo(SYSTEM_COMMAND_RESET, "SYSTEM_COMMAND_RESET", "Reset", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_HELLO)] = new BGAPICommandInfo(SYSTEM_COMMAND_HELLO, "SYSTEM_COMMAND_HELLO", "Hello", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_GET_ADDRESS)] = new BGAPICommandInfo(SYSTEM_COMMAND_GET_ADDRESS, "SYSTEM_COMMAND_GET_ADDRESS", "Address Get", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_GET_COUNTERS)] = new BGAPICommandInfo(SYSTEM_COMMAND_GET_COUNTERS, "SYSTEM_COMMAND_GET_COUNTERS", "Get Counters", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_GET_CONNECTIONS)] = new BGAPICommandInfo(SYSTEM_COMMAND_GET_CONNECTIONS, "SYSTEM_COMMAND_GET_CONNECTIONS", "Get Connections", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_GET_INFO)] = new BGAPICommandInfo(SYSTEM_COMMAND_GET_INFO, "SYSTEM_COMMAND_GET_INFO", "Get Info", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_ENDPOINT_TX)] = new BGAPICommandInfo(SYSTEM_COMMAND_ENDPOINT_TX, "SYSTEM_COMMAND_ENDPOINT_TX", "Endpoint TX", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_WHITELIST_APPEND)] = new BGAPICommandInfo(SYSTEM_COMMAND_WHITELIST_APPEND, "SYSTEM_COMMAND_WHITELIST_APPEND", "Whitelist Append", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_WHITELIST_REMOVE)] = new BGAPICommandInfo(SYSTEM_COMMAND_WHITELIST_REMOVE, "SYSTEM_COMMAND_WHITELIST_REMOVE", "Whitelist Remove", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_WHITELIST_CLEAR)] = new BGAPICommandInfo(SYSTEM_COMMAND_WHITELIST_CLEAR, "SYSTEM_COMMAND_WHITELIST_CLEAR", "Whitelist Clear", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_ENDPOINT_RX)] = new BGAPICommandInfo(SYSTEM_COMMAND_ENDPOINT_RX, "SYSTEM_COMMAND_ENDPOINT_RX", "Endpoint RX", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_ENDPOINT_SET_WATERMARKS)] = new BGAPICommandInfo(SYSTEM_COMMAND_ENDPOINT_SET_WATERMARKS, "SYSTEM_COMMAND_ENDPOINT_SET_WATERMARKS", "Endpoint Set Watermarks", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_AES_SET_KEY)] = new BGAPICommandInfo(SYSTEM_COMMAND_AES_SET_KEY, "SYSTEM_COMMAND_AES_SET_KEY", "AES Setkey", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_AES_ENCRYPT)] = new BGAPICommandInfo(SYSTEM_COMMAND_AES_ENCRYPT, "SYSTEM_COMMAND_AES_ENCRYPT", "AES Encrypt", _commandsClasses.Value[CCID_SYSTEM]);
                commands[(ushort)(eventId + SYSTEM_COMMAND_AES_DECRYPT)] = new BGAPICommandInfo(SYSTEM_COMMAND_AES_DECRYPT, "SYSTEM_COMMAND_AES_DECRYPT", "AES Decrypt", _commandsClasses.Value[CCID_SYSTEM]);
            }

            if (_commandsClasses.Value.ContainsKey(CCID_PERSISTENT_STORE))
            {
                //9
            }

            if (_commandsClasses.Value.ContainsKey(CCID_ATTRIBUTE_DATABASE))
            {
                //6
            }

            if (_commandsClasses.Value.ContainsKey(CCID_CONNECTION))
            {
                var eventId = (ushort)(CCID_CONNECTION << 8);
                //5
                commands[(ushort)(eventId + CONNECTION_COMMAND_DISCONNECT)] = new BGAPICommandInfo(CONNECTION_COMMAND_DISCONNECT, "CONNECTION_COMMAND_DISCONNECT", "Disconnect", _commandsClasses.Value[CCID_CONNECTION]);
                commands[(ushort)(eventId + CONNECTION_COMMAND_GET_RSSI)] = new BGAPICommandInfo(CONNECTION_COMMAND_GET_RSSI, "CONNECTION_COMMAND_GET_RSSI", "Get Rssi", _commandsClasses.Value[CCID_CONNECTION]);
                commands[(ushort)(eventId + CONNECTION_COMMAND_UPDATE)] = new BGAPICommandInfo(CONNECTION_COMMAND_UPDATE, "CONNECTION_COMMAND_UPDATE", "Get Status", _commandsClasses.Value[CCID_CONNECTION]);
                commands[(ushort)(eventId + CONNECTION_COMMAND_VERSION_UPDATE)] = new BGAPICommandInfo(CONNECTION_COMMAND_VERSION_UPDATE, "CONNECTION_COMMAND_VERSION_UPDATE", "Update", _commandsClasses.Value[CCID_CONNECTION]);
                commands[(ushort)(eventId + CONNECTION_COMMAND_GET_STATUS)] = new BGAPICommandInfo(CONNECTION_COMMAND_GET_STATUS, "CONNECTION_COMMAND_GET_STATUS", "Version Update", _commandsClasses.Value[CCID_CONNECTION]);
            }

            if (_commandsClasses.Value.ContainsKey(CCID_ATTRIBUTE_CLIENT))
            {
                var eventId = (ushort)(CCID_ATTRIBUTE_CLIENT << 8);
                //12
                commands[(ushort)(eventId + ATTRIBUTE_COMMAND_FIND_BY_TYPE_VALUE)] = new BGAPICommandInfo(ATTRIBUTE_COMMAND_FIND_BY_TYPE_VALUE, "ATTRIBUTE_CLIENT_COMMAND_FIND_BY_TYPE_VALUE", "Find By Type Value", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                commands[(ushort)(eventId + ATTRIBUTE_COMMAND_READ_BY_GROUP_TYPE)] = new BGAPICommandInfo(ATTRIBUTE_COMMAND_READ_BY_GROUP_TYPE, "ATTRIBUTE_CLIENT_COMMAND_READ_BY_GROUP_TYPE", "Read By Group Type", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                commands[(ushort)(eventId + ATTRIBUTE_COMMAND_READ_BY_TYPE_VALUE)] = new BGAPICommandInfo(ATTRIBUTE_COMMAND_READ_BY_TYPE_VALUE, "ATTRIBUTE_CLIENT_COMMAND_READ_BY_TYPE_VALUE", "Read By Type", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                commands[(ushort)(eventId + ATTRIBUTE_COMMAND_FIND_INFORMATION)] = new BGAPICommandInfo(ATTRIBUTE_COMMAND_FIND_INFORMATION, "ATTRIBUTE_CLIENT_COMMAND_FIND_INFORMATION", "Find Information", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                commands[(ushort)(eventId + ATTRIBUTE_COMMAND_READ_BY_HANDLE)] = new BGAPICommandInfo(ATTRIBUTE_COMMAND_READ_BY_HANDLE, "ATTRIBUTE_CLIENT_COMMAND_READ_BY_HANDLE", "Read By Handle", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                commands[(ushort)(eventId + ATTRIBUTE_COMMAND_ATTRIBUTE_WRITE)] = new BGAPICommandInfo(ATTRIBUTE_COMMAND_ATTRIBUTE_WRITE, "ATTRIBUTE_CLIENT_COMMAND_ATTRIBUTE_WRITE", "Attribute Write", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                commands[(ushort)(eventId + ATTRIBUTE_COMMAND_WRITE)] = new BGAPICommandInfo(ATTRIBUTE_COMMAND_WRITE, "ATTRIBUTE_CLIENT_COMMAND_WRITE", "Write Command", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                commands[(ushort)(eventId + ATTRIBUTE_COMMAND_INDICATE_CONFIRM)] = new BGAPICommandInfo(ATTRIBUTE_COMMAND_INDICATE_CONFIRM, "ATTRIBUTE_CLIENT_COMMAND_INDICATE_CONFIRM", "Indicate Confirm", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                commands[(ushort)(eventId + ATTRIBUTE_COMMAND_READ_LONG)] = new BGAPICommandInfo(ATTRIBUTE_COMMAND_READ_LONG, "ATTRIBUTE_CLIENT_COMMAND_READ_LONG", "Read Long", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                commands[(ushort)(eventId + ATTRIBUTE_COMMAND_PREPARE_WRITE)] = new BGAPICommandInfo(ATTRIBUTE_COMMAND_PREPARE_WRITE, "ATTRIBUTE_CLIENT_COMMAND_PREPARE_WRITE", "Prepare Write", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                commands[(ushort)(eventId + ATTRIBUTE_COMMAND_EXECUTE_WRITE)] = new BGAPICommandInfo(ATTRIBUTE_COMMAND_EXECUTE_WRITE, "ATTRIBUTE_CLIENT_COMMAND_EXECUTE_WRITE", "Execute Write", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                commands[(ushort)(eventId + ATTRIBUTE_COMMAND_READ_MULTIPLE)] = new BGAPICommandInfo(ATTRIBUTE_COMMAND_READ_MULTIPLE, "ATTRIBUTE_CLIENT_COMMAND_READ_MULTIPLE", "Read Multiple", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
            }

            if (_commandsClasses.Value.ContainsKey(CCID_SECURITY_MANAGER))
            {
                //8
            }

            if (_commandsClasses.Value.ContainsKey(CCID_GAP))
            {
                var eventId = (ushort)(CCID_GAP << 8);
                //11
                commands[(ushort)(eventId + GAP_COMMAND_SET_PRIVACY_FLAGS)] = new BGAPICommandInfo(GAP_COMMAND_SET_PRIVACY_FLAGS, "GAP_COMMAND_SET_PRIVACY_FLAGS", "Set Privacy Flags", _commandsClasses.Value[CCID_GAP]);
                commands[(ushort)(eventId + GAP_COMMAND_SET_MODE)] = new BGAPICommandInfo(GAP_COMMAND_SET_MODE, "GAP_COMMAND_SET_MODE", "Set Mode", _commandsClasses.Value[CCID_GAP]);
                commands[(ushort)(eventId + GAP_COMMAND_DISCOVER)] = new BGAPICommandInfo(GAP_COMMAND_DISCOVER, "GAP_COMMAND_DISCOVER", "Discover", _commandsClasses.Value[CCID_GAP]);
                commands[(ushort)(eventId + GAP_COMMAND_CONNECT_DIRECT)] = new BGAPICommandInfo(GAP_COMMAND_CONNECT_DIRECT, "GAP_COMMAND_CONNECT_DIRECT", "Connect Direct", _commandsClasses.Value[CCID_GAP]);
                commands[(ushort)(eventId + GAP_COMMAND_END_PROCEDURE)] = new BGAPICommandInfo(GAP_COMMAND_END_PROCEDURE, "GAP_COMMAND_END_PROCEDURE", "End Procedure", _commandsClasses.Value[CCID_GAP]);
                commands[(ushort)(eventId + GAP_COMMAND_CONNECT_SELECTIVE)] = new BGAPICommandInfo(GAP_COMMAND_CONNECT_SELECTIVE, "GAP_COMMAND_CONNECT_SELECTIVE", "Connect Selective", _commandsClasses.Value[CCID_GAP]);
                commands[(ushort)(eventId + GAP_COMMAND_SET_FILTERING)] = new BGAPICommandInfo(GAP_COMMAND_SET_FILTERING, "GAP_COMMAND_SET_FILTERING", "Set Filtering", _commandsClasses.Value[CCID_GAP]);
                commands[(ushort)(eventId + GAP_COMMAND_SET_SCAN_PARAMETERS)] = new BGAPICommandInfo(GAP_COMMAND_SET_SCAN_PARAMETERS, "GAP_COMMAND_SET_SCAN_PARAMETERS", "Set Scan Parameters", _commandsClasses.Value[CCID_GAP]);
                commands[(ushort)(eventId + GAP_COMMAND_SET_ADV_PARAMETERS)] = new BGAPICommandInfo(GAP_COMMAND_SET_ADV_PARAMETERS, "GAP_COMMAND_SET_ADV_PARAMETERS", "Set Advertising Parameters", _commandsClasses.Value[CCID_GAP]);
                commands[(ushort)(eventId + GAP_COMMAND_SET_ADV_DATA)] = new BGAPICommandInfo(GAP_COMMAND_SET_ADV_DATA, "GAP_COMMAND_SET_ADV_DATA", "Set Advertising Data", _commandsClasses.Value[CCID_GAP]);
                commands[(ushort)(eventId + GAP_COMMAND_SET_DIRECTED_CONNECTABLE_MODE)] = new BGAPICommandInfo(GAP_COMMAND_SET_DIRECTED_CONNECTABLE_MODE, "GAP_COMMAND_SET_DIRECTED_CONNECTABLE_MODE", "Set Directed Connectable Mode", _commandsClasses.Value[CCID_GAP]);
            }

            if (_commandsClasses.Value.ContainsKey(CCID_HARDWARE))
            {
                //21
            }

            return commands;
        }

        private static Dictionary<ushort, IBGAPIErrorInfo> GetErrors()
        {
            return new Dictionary<ushort, IBGAPIErrorInfo>() {
                { 0x0000, new BGAPIErrorInfo(0x0000, "Command Successfully Executed", "General", "Command Successfully Executed") },
                //BGAPI Errors - Errors related to BGAPI protocol
                { 0x0180, new BGAPIErrorInfo(0x0180, "Invalid Parameter", "API", "Command contained invalid parameter") },
                { 0x0181, new BGAPIErrorInfo(0x0181, "Device in Wrong State", "API", "Device is in wrong state to receive command") },
                { 0x0182, new BGAPIErrorInfo(0x0182, "Out Of Memory", "API", "Device has run out of memory") },
                { 0x0183, new BGAPIErrorInfo(0x0183, "Feature Not Implemented", "API", "Feature is not implemented") },
                { 0x0184, new BGAPIErrorInfo(0x0184, "Command Not Recognized", "API", "Command was not recognized") },
                { 0x0185, new BGAPIErrorInfo(0x0185, "Timeout", "API", "Command or Procedure failed due to timeout") },
                { 0x0186, new BGAPIErrorInfo(0x0186, "Not Connected", "API", "Connection handle passed is to command is not a valid handle") },
                { 0x0187, new BGAPIErrorInfo(0x0187, "Flow", "API", "Command would cause either underflow or overflow error") },
                { 0x0188, new BGAPIErrorInfo(0x0188, "User Attribute", "API", "User attribute was accessed through API which is not supported") },
                { 0x0189, new BGAPIErrorInfo(0x0189, "Invalid License Key", "API", "No valid license key found") },
                { 0x018A, new BGAPIErrorInfo(0x018A, "Command Too Long", "API", "Command maximum length exceeded") },
                { 0x018B, new BGAPIErrorInfo(0x018B, "Out of Bonds", "API", "Bonding procedure can't be started because device has no space left for bond") },
                //Bluetooth Errors
                { 0x0205, new BGAPIErrorInfo(0x0205, "Authentication Failure", "Bluetooth", "Pairing or authentication failed due to incorrect results in the pairing or authentication procedure. This could be due to an incorrect PIN or Link Key") },
                { 0x0206, new BGAPIErrorInfo(0x0206, "Pin or Key Missing", "Bluetooth", "Pairing failed because of missing PIN, or authentication failed because of missing Key") },
                { 0x0207, new BGAPIErrorInfo(0x0207, "Memory Capacity Exceeded", "Bluetooth", "Controller is out of memory") },
                { 0x0208, new BGAPIErrorInfo(0x0208, "Connection Timeout", "Bluetooth", "Link supervision timeout has expired") },
                { 0x0209, new BGAPIErrorInfo(0x0209, "Connection Limit Exceeded", "Bluetooth", "Controller is at limit of connections it can support") },
                { 0x020C, new BGAPIErrorInfo(0x020C, "Command Disallowed", "Bluetooth", "Command requested cannot be executed because the Controller is in a state where it cannot process this command at this time") },
                { 0x0212, new BGAPIErrorInfo(0x0212, "Invalid Command Parameters", "Bluetooth", "Command contained invalid parameters") },
                { 0x0213, new BGAPIErrorInfo(0x0213, "Remote User Terminated Connection", "Bluetooth", "User on the remote device terminated the connection") },
                { 0x0216, new BGAPIErrorInfo(0x0216, "Connection Terminated by Local Host", "Bluetooth", "Local device terminated the connection") },
                { 0x0222, new BGAPIErrorInfo(0x0222, "LL Response Timeout", "Bluetooth", "Connection terminated due to link-layer procedure timeout") },
                { 0x0228, new BGAPIErrorInfo(0x0228, "LL Instant Passed", "Bluetooth", "Received link-layer control packet where instant was in the past") },
                { 0x023A, new BGAPIErrorInfo(0x023A, "Controller Busy", "Bluetooth", "Operation was rejected because the controller is busy and unable to process the request") },
                { 0x023B, new BGAPIErrorInfo(0x023B, "Unacceptable Connection Interval", "Bluetooth", "The Unacceptable Connection Interval error code indicates that the remote device terminated the connection because of an unacceptable connection interval") },
                { 0x023C, new BGAPIErrorInfo(0x023C, "Directed Advertising Timeout", "Bluetooth", "Directed advertising completed without a connection being created") },
                { 0x023D, new BGAPIErrorInfo(0x023D, "MIC Failure", "Bluetooth", "Connection was terminated because the Message Integrity Check (MIC) failed on a received packet") },
                { 0x023E, new BGAPIErrorInfo(0x023E, "Connection Failed to be Established", "Bluetooth", "LL initiated a connection but the connection has failed to be established. Controller did not receive any packets from remote end") },
                //Security Manager Protocol Errors - Errors from Security Manager Protocol
                { 0x0301, new BGAPIErrorInfo(0x0301, "Passkey Entry Failed", "Security", "The user input of passkey failed, for example, the user cancelled the operation") },
                { 0x0302, new BGAPIErrorInfo(0x0302, "OOB Data is not available", "Security", "Out of Band data is not available for authentication") },
                { 0x0303, new BGAPIErrorInfo(0x0303, "Authentication Requirements", "Security", "The pairing procedure cannot be performed as authentication requirements cannot be met due to IO capabilities of one or both devices") },
                { 0x0304, new BGAPIErrorInfo(0x0304, "Confirm Value Failed", "Security", "The confirm value does not match the calculated compare value") },
                { 0x0305, new BGAPIErrorInfo(0x0305, "Pairing Not Supported", "Security", "Pairing is not supported by the device") },
                { 0x0306, new BGAPIErrorInfo(0x0306, "Encryption Key Size", "Security", "The resultant encryption key size is insufficient for the security requirements of this device") },
                { 0x0307, new BGAPIErrorInfo(0x0307, "Command Not Supported", "Security", "The SMP command received is not supported on this device") },
                { 0x0308, new BGAPIErrorInfo(0x0308, "Unspecified Reason", "Security", "Pairing failed due to an unspecified reason") },
                { 0x0309, new BGAPIErrorInfo(0x0309, "Repeated Attempts", "Security", "Pairing or authentication procedure is disallowed because too little time has elapsed since last pairing request or security request") },
                { 0x030A, new BGAPIErrorInfo(0x030A, "Invalid Parameters", "Security", "The Invalid Parameters error code indicates: the command length is invalid or a parameter is outside of the specified range") },
                //Attribute Protocol Errors - Errors from Attribute Protocol
                { 0x0401, new BGAPIErrorInfo(0x0401, "Invalid Handle", "AttributeProtocol", "The attribute handle given was not valid on this server") },
                { 0x0402, new BGAPIErrorInfo(0x0402, "Read Not Permitted", "AttributeProtocol", "The attribute cannot be read") },
                { 0x0403, new BGAPIErrorInfo(0x0403, "Write Not Permitted", "AttributeProtocol", "The attribute cannot be written") },
                { 0x0404, new BGAPIErrorInfo(0x0404, "Invalid PDU", "AttributeProtocol", "The attribute PDU was invalid") },
                { 0x0405, new BGAPIErrorInfo(0x0405, "Insufficient Authentication", "AttributeProtocol", "The attribute requires authentication before it can be read or written") },
                { 0x0406, new BGAPIErrorInfo(0x0406, "Request Not Supported", "AttributeProtocol", "Attribute Server does not support the request received from the client") },
                { 0x0407, new BGAPIErrorInfo(0x0407, "Invalid Offset", "AttributeProtocol", "Offset specified was past the end of the attribute") },
                { 0x0408, new BGAPIErrorInfo(0x0408, "Insufficient Authorization", "AttributeProtocol", "The attribute requires authorization before it can be read or written") },
                { 0x0409, new BGAPIErrorInfo(0x0409, "Prepare Queue Full", "AttributeProtocol", "Too many prepare writes have been queueud") },
                { 0x040A, new BGAPIErrorInfo(0x040A, "Attribute Not Found", "AttributeProtocol", "No attribute found within the given attribute handle range") },
                { 0x040B, new BGAPIErrorInfo(0x040B, "Attribute Not Long", "AttributeProtocol", "The attribute cannot be read or written using the Read Blob Request") },
                { 0x040C, new BGAPIErrorInfo(0x040C, "Insufficient Encryption Key Size", "AttributeProtocol", "The Encryption Key Size used for encrypting this link is insufficient") },
                { 0x040D, new BGAPIErrorInfo(0x040D, "Invalid Attribute Value Length", "AttributeProtocol", "The attribute value length is invalid for the operation") },
                { 0x040E, new BGAPIErrorInfo(0x040E, "Unlikely Error", "AttributeProtocol", "The attribute request that was requested has encountered an error that was unlikely, and therefore could not be completed as requested") },
                { 0x040F, new BGAPIErrorInfo(0x040F, "Insufficient Encryption", "AttributeProtocol", "The attribute requires encryption before it can be read or written") },
                { 0x0410, new BGAPIErrorInfo(0x0410, "Unsupported Group Type", "AttributeProtocol", "The attribute type is not a supported grouping attribute as defined by a higher layer specification") },
                { 0x0411, new BGAPIErrorInfo(0x0411, "Insufficient Resources", "AttributeProtocol", "Insufficient Resources to complete the request") },
                { 0x0480, new BGAPIErrorInfo(0x0480, "Application Error Codes", "AttributeProtocol", "Application error code defined by a higher layer specification") },
                //Command Execution
                { 0xFE01, new BGAPIErrorInfo(0xFE01, "General Command Execution Error", "CommandExecution", "General Command Execution Error") },
                { 0xFE02, new BGAPIErrorInfo(0xFE02, "Received Response has Wrong Command Class ID", "CommandExecution", "Received Response has Wrong Command Class ID") },
                { 0xFE03, new BGAPIErrorInfo(0xFE03, "Received Response has Wrong Command ID", "CommandExecution", "Received Response has Wrong Command ID") },
                //Connection
                { 0xFF01, new BGAPIErrorInfo(0xFF01, "Bluegiga BLED112 port was not found", "APIConnection", "Bluegiga BLED112 port was not found") },
                { 0xFF02, new BGAPIErrorInfo(0xFF02, "Bluegiga BLED112 command execution timeout", "APIConnection", "Bluegiga BLED112 command execution timeout") },
                { 0xFF03, new BGAPIErrorInfo(0xFF03, "Connection is wating for resposponse", "APIConnection", "Connection is wating for resposponse") },
                { 0xFF04, new BGAPIErrorInfo(0xFF04, "Connection is wating for BLED112 connection to restore", "APIConnection", "Connection is wating for BLED112 connection to restore") },
                { 0xFF05, new BGAPIErrorInfo(0xFF05, "Connection is wating for serial port to open", "APIConnection", "Connection is wating for serial port to open") },
                //BGBLE Software Stack
                { 0xFF91, new BGAPIErrorInfo(0xFF91, "Bluegiga BLED112 connection check failed", "BGBLE", "Bluegiga BLED112 connection check failed") },
                { 0xFF92, new BGAPIErrorInfo(0xFF92, "Bluegiga BLED112 unable connect to device", "BGBLE", "Bluegiga BLED112 unable connect to device") },
                { 0xFF93, new BGAPIErrorInfo(0xFF93, "Bluegiga BLED112 failed to disconnect from device", "BGBLE", "Bluegiga BLED112 failed to disconnect from device") },
                { 0xFF94, new BGAPIErrorInfo(0xFF94, "Bluegiga BLED112 device command execution timeout", "BGBLE", "Bluegiga BLED112 device command execution timeout") },
                { 0xFF95, new BGAPIErrorInfo(0xFF95, "BGBLECharacteristic config to short", "BGBLE", "BGBLECharacteristic config to short") },
                { 0xFF96, new BGAPIErrorInfo(0xFF96, "BGBLECharacteristic operation not supported", "BGBLE", "BGBLECharacteristic operation not supported") },
                { 0xFF97, new BGAPIErrorInfo(0xFF97, "Bluegiga BLED112 device failed to execute command", "BGBLE", "Bluegiga BLED112 device failed to execute command") },
                { 0xFF98, new BGAPIErrorInfo(0xFF98, "Bluegiga BLED112 device usupported software version", "BGBLE", "Bluegiga BLED112 device usupported software version") },
                { 0xFF99, new BGAPIErrorInfo(0xFF99, "Bluegiga BLED112 device firmware upgrade required", "BGBLE", "Bluegiga BLED112 device firmware upgrade required") },
            };
        }

        private static Dictionary<ushort, IBGAPICommandInfo> GetEvents()
        {
            Dictionary<ushort, IBGAPICommandInfo> events = new Dictionary<ushort, IBGAPICommandInfo>();

            if (_commandsClasses.Value.ContainsKey(CCID_SYSTEM))
            {
                var eventId = (ushort)(CCID_SYSTEM << 8);
                //6
                events[(ushort)(eventId + SYSTEM_EVENT_BOOT)] = new BGAPICommandInfo(SYSTEM_EVENT_BOOT, "SYSTEM_EVENT_BOOT", "Boot", _commandsClasses.Value[CCID_SYSTEM]);
                events[(ushort)(eventId + SYSTEM_EVENT_ENDPOINT_WATERMARK_RX)] = new BGAPICommandInfo(SYSTEM_EVENT_ENDPOINT_WATERMARK_RX, "SYSTEM_EVENT_ENDPOINT_WATERMARK_RX", "Endpoint Watermark RX", _commandsClasses.Value[CCID_SYSTEM]);
                events[(ushort)(eventId + SYSTEM_EVENT_ENDPOINT_WATERMARK_TX)] = new BGAPICommandInfo(SYSTEM_EVENT_ENDPOINT_WATERMARK_TX, "SYSTEM_EVENT_ENDPOINT_WATERMARK_TX", "Endpoint Watermark TX", _commandsClasses.Value[CCID_SYSTEM]);
                events[(ushort)(eventId + SYSTEM_EVENT_SCRIPT_FAILURE)] = new BGAPICommandInfo(SYSTEM_EVENT_SCRIPT_FAILURE, "SYSTEM_EVENT_SCRIPT_FAILURE", "Script Failure", _commandsClasses.Value[CCID_SYSTEM]);
                events[(ushort)(eventId + SYSTEM_EVENT_NO_LICENSE_KEY)] = new BGAPICommandInfo(SYSTEM_EVENT_NO_LICENSE_KEY, "SYSTEM_EVENT_NO_LICENSE_KEY", "No License Key", _commandsClasses.Value[CCID_SYSTEM]);
                events[(ushort)(eventId + SYSTEM_EVENT_PROTOCOL_ERROR)] = new BGAPICommandInfo(SYSTEM_EVENT_PROTOCOL_ERROR, "SYSTEM_EVENT_PROTOCOL_ERROR", "Protocol Error", _commandsClasses.Value[CCID_SYSTEM]);
            }

            if (_commandsClasses.Value.ContainsKey(CCID_PERSISTENT_STORE))
            {
                //1
            }

            if (_commandsClasses.Value.ContainsKey(CCID_ATTRIBUTE_DATABASE))
            {
                //3
            }

            if (_commandsClasses.Value.ContainsKey(CCID_CONNECTION))
            {
                var eventId = (ushort)(CCID_CONNECTION << 8);
                //4
                events[(ushort)(eventId + CONNECTION_EVENT_STATUS)] = new BGAPICommandInfo(CONNECTION_EVENT_STATUS, "CONNECTION_EVENT_STATUS", "Status", _commandsClasses.Value[CCID_CONNECTION]);
                events[(ushort)(eventId + CONNECTION_EVENT_VERSION_IND)] = new BGAPICommandInfo(CONNECTION_EVENT_VERSION_IND, "CONNECTION_EVENT_VERSION_IND", "Device Version Indication", _commandsClasses.Value[CCID_CONNECTION]);
                events[(ushort)(eventId + CONNECTION_EVENT_FEATURE_IND)] = new BGAPICommandInfo(CONNECTION_EVENT_FEATURE_IND, "CONNECTION_EVENT_FEATURE_IND", "Device Feature Indication", _commandsClasses.Value[CCID_CONNECTION]);
                events[(ushort)(eventId + CONNECTION_EVENT_DISCONNECTED)] = new BGAPICommandInfo(CONNECTION_EVENT_DISCONNECTED, "CONNECTION_EVENT_DISCONNECTED", "Disconnected", _commandsClasses.Value[CCID_CONNECTION]);
            }

            if (_commandsClasses.Value.ContainsKey(CCID_ATTRIBUTE_CLIENT))
            {
                var eventId = (ushort)(CCID_ATTRIBUTE_CLIENT << 8);
                //6
                events[(ushort)(eventId + ATTRIBUTE_CLIENT_EVENT_INDICATED)] = new BGAPICommandInfo(ATTRIBUTE_CLIENT_EVENT_INDICATED, "ATTRIBUTE_CLIENT_EVENT_INDICATED", "Indicated", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                events[(ushort)(eventId + ATTRIBUTE_CLIENT_EVENT_PROCEDURE_COMPLETED)] = new BGAPICommandInfo(ATTRIBUTE_CLIENT_EVENT_PROCEDURE_COMPLETED, "ATTRIBUTE_CLIENT_EVENT_PROCEDURE_COMPLETED", "Procedure Completed", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                events[(ushort)(eventId + ATTRIBUTE_CLIENT_EVENT_GROUP_FOUND)] = new BGAPICommandInfo(ATTRIBUTE_CLIENT_EVENT_GROUP_FOUND, "ATTRIBUTE_CLIENT_EVENT_GROUP_FOUND", "Group Found", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                events[(ushort)(eventId + ATTRIBUTE_CLIENT_EVENT_FIND_INFORMATION_FOUND)] = new BGAPICommandInfo(ATTRIBUTE_CLIENT_EVENT_FIND_INFORMATION_FOUND, "ATTRIBUTE_CLIENT_EVENT_FIND_INFORMATION_FOUND", "Find Information Found", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                events[(ushort)(eventId + ATTRIBUTE_CLIENT_EVENT_ATTRIBUTE_VALUE)] = new BGAPICommandInfo(ATTRIBUTE_CLIENT_EVENT_ATTRIBUTE_VALUE, "ATTRIBUTE_CLIENT_EVENT_ATTRIBUTE_VALUE", "Attribute Value", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
                events[(ushort)(eventId + ATTRIBUTE_CLIENT_EVENT_READ_MULTIPLE_RESPONSE)] = new BGAPICommandInfo(ATTRIBUTE_CLIENT_EVENT_READ_MULTIPLE_RESPONSE, "ATTRIBUTE_CLIENT_EVENT_READ_MULTIPLE_RESPONSE", "Read Multiple Response", _commandsClasses.Value[CCID_ATTRIBUTE_CLIENT]);
            }

            if (_commandsClasses.Value.ContainsKey(CCID_SECURITY_MANAGER))
            {
                //4
            }

            if (_commandsClasses.Value.ContainsKey(CCID_GAP))
            {
                var eventId = (ushort)(CCID_GAP << 8);
                //1
                events[(ushort)(eventId + GAP_EVENT_SCAN)] = new BGAPICommandInfo(GAP_EVENT_SCAN, "GAP_EVENT_SCAN", "Scan Response", _commandsClasses.Value[CCID_GAP]);
            }

            if (_commandsClasses.Value.ContainsKey(CCID_HARDWARE))
            {
                //4
            }

            return events;
        }

        /// <summary>This method findes command details by command id(command class, command).</summary>
        /// <param name="commandId">Command id</param>
        /// <returns>Returns structure with command description.</returns>
        public static IBGAPICommandInfo FindCommandById(ushort commandId)
        {
            if (_commands.Value.ContainsKey(commandId)) {
                return _commands.Value[commandId];
            }
            else
            {
                var _commandClassId = (byte)((commandId >> 8) & 0xFF);
                var _commandId = (byte)(commandId & 0xFF);
                return new BGAPICommandInfo(_commandId, "Unknown", "Unknown command", ((_commandsClasses.Value.ContainsKey(_commandClassId)) ? _commandsClasses.Value[_commandClassId] : new BGAPICommandClassInfo(0xFF, "Unknown", "Unknown command class")));
            }
        }

        /// <summary>This method findes command details by command class id and one byte command id.</summary>
        /// <param name="commandClassId">Command class id</param>
        /// <param name="commandId">One byte command id</param>
        /// <returns>Returns structure with command description.</returns>
        public static IBGAPICommandInfo FindCommandById(byte commandClassId, byte commandId)
        {
            var _commandId = (ushort)((commandClassId << 8) + commandId);
            return FindCommandById(_commandId);
        }

        /// <summary>This method findes error details by error code.</summary>
        /// <param name="errorCode">Error code</param>
        /// <returns>Returns structure with error description.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.11 Error Codes)</seealso>
        public static IBGAPIErrorInfo FindErrorByCode(ushort errorCode)
        {
            if (_errors.Value.ContainsKey(errorCode))
            {
                return _errors.Value[errorCode];
            }
            return new BGAPIErrorInfo(0xFFFF, "Unknown Error", "General", "Error code " + errorCode.ToString("X") + " was not found in errors list.");
        }

        /// <summary>This method findes event details by event id (command class, event).</summary>
        /// <param name="eventId">Event id</param>
        /// <returns>Returns structure with event description.</returns>
        public static IBGAPICommandInfo FindEventById(ushort eventId)
        {
            if (_events.Value.ContainsKey(eventId))
            {
                return _events.Value[eventId];
            }
            else
            {
                var _commandClassId = (byte)((eventId >> 8) & 0xFF);
                var _eventId = (byte)(eventId & 0xFF);
                return new BGAPICommandInfo(_eventId, "Unknown", "Unknown event", ((_commandsClasses.Value.ContainsKey(_commandClassId)) ? _commandsClasses.Value[_commandClassId] : new BGAPICommandClassInfo(0xFF, "Unknown", "Unknown command class")));
            }
        }

        /// <summary>This method findes event details by command class id and one byte event id.</summary>
        /// <param name="commandClassId">Command class id</param>
        /// <param name="eventId">One byte event id</param>
        /// <returns>Returns structure with event description.</returns>
        public static IBGAPICommandInfo FindEventById(byte commandClassId, byte eventId)
        {
            var _eventId = (ushort)((commandClassId << 8) + eventId);
            return FindEventById(_eventId);
        }
    }
}
