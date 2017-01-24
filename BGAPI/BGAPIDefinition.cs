/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Collections.Generic;

namespace BGBLE.BGAPI
{
    /// <summary>Error details structure.</summary>
    public struct BGAPIError
    {
        /// <summary>Error description.</summary>
        public string description;

        /// <summary>Error code.</summary>
        public ushort id;

        /// <summary>Error name.</summary>
        public string name;

        public BGAPIError(ushort _id, string _name, string _description)
        {
            description = _description;
            id = _id;
            name = _name;
        }
    }

    /// <summary>This class API constants and describes the error codes the API commands may produce.</summary>
    /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(4 API definition, 5.11 Error Codes)</seealso>
    public class BGAPIDefinition
    {
        //GENERAL
        public const ushort COMMAND_PAYLOAD_MAX_LENGTH = 2047;
        //Message Types
        public const byte MT_COMMAND_RESPONSE = 0x00;
        public const byte MT_EVENT = 0x80;
        //Technology Type
        public const byte TT_BLUETOOTH_SMART = 0x00;
        public const byte TT_WIFI = 0x08;
        //Command Class IDs
        public const byte CCID_SYSTEM = 0x00;
        public const byte CCID_PERSISTENT_STORE = 0x01;
        public const byte CCID_ATTRIBUTE_DATABASE = 0x02;
        public const byte CCID_CONNECTION = 0x03;
        public const byte CCID_ATTRIBUTE_CLIENT = 0x04;
        public const byte CCID_SECURITY_MANAGER = 0x05;
        public const byte CCID_GAP = 0x06;
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

        /// <summary>This method findes error details by error code.</summary>
        /// <param name="errorCode">Error code</param>
        /// <returns>Returns structure with error description.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.11 Error Codes)</seealso>
        public static BGAPIError FindErrorByCode(ushort errorCode)
        {
            Dictionary<ushort, BGAPIError> errors = new Dictionary<ushort, BGAPIError>() {
                { 0x0000, new BGAPIError(0x0000, "Command Successfully Executed", "Command Successfully Executed") },
                //BGAPI Errors - Errors related to BGAPI protocol
                { 0x0180, new BGAPIError(0x0180, "Invalid Parameter", "Command contained invalid parameter") },
                { 0x0181, new BGAPIError(0x0181, "Device in Wrong State", "Device is in wrong state to receive command") },
                { 0x0182, new BGAPIError(0x0182, "Out Of Memory", "Device has run out of memory") },
                { 0x0183, new BGAPIError(0x0183, "Feature Not Implemented", "Feature is not implemented") },
                { 0x0184, new BGAPIError(0x0184, "Command Not Recognized", "Command was not recognized") },
                { 0x0185, new BGAPIError(0x0185, "Timeout", "Command or Procedure failed due to timeout") },
                { 0x0186, new BGAPIError(0x0186, "Not Connected", "Connection handle passed is to command is not a valid handle") },
                { 0x0187, new BGAPIError(0x0187, "Flow", "Command would cause either underflow or overflow error") },
                { 0x0188, new BGAPIError(0x0188, "User Attribute", "User attribute was accessed through API which is not supported") },
                { 0x0189, new BGAPIError(0x0189, "Invalid License Key", "No valid license key found") },
                { 0x018A, new BGAPIError(0x018A, "Command Too Long", "Command maximum length exceeded") },
                { 0x018B, new BGAPIError(0x018B, "Out of Bonds", "Bonding procedure can't be started because device has no space left for bond") },
                //Bluetooth Errors
                { 0x0205, new BGAPIError(0x0205, "Authentication Failure", "Pairing or authentication failed due to incorrect results in the pairing or authentication procedure. This could be due to an incorrect PIN or Link Key") },
                { 0x0206, new BGAPIError(0x0206, "Pin or Key Missing", "Pairing failed because of missing PIN, or authentication failed because of missing Key") },
                { 0x0207, new BGAPIError(0x0207, "Memory Capacity Exceeded", "Controller is out of memory") },
                { 0x0208, new BGAPIError(0x0208, "Connection Timeout", "Link supervision timeout has expired") },
                { 0x0209, new BGAPIError(0x0209, "Connection Limit Exceeded", "Controller is at limit of connections it can support") },
                { 0x020C, new BGAPIError(0x020C, "Command Disallowed", "Command requested cannot be executed because the Controller is in a state where it cannot process this command at this time") },
                { 0x0212, new BGAPIError(0x0212, "Invalid Command Parameters", "Command contained invalid parameters") },
                { 0x0213, new BGAPIError(0x0213, "Remote User Terminated Connection", "User on the remote device terminated the connection") },
                { 0x0216, new BGAPIError(0x0216, "Connection Terminated by Local Host", "Local device terminated the connection") },
                { 0x0222, new BGAPIError(0x0222, "LL Response Timeout", "Connection terminated due to link-layer procedure timeout") },
                { 0x0228, new BGAPIError(0x0228, "LL Instant Passed", "Received link-layer control packet where instant was in the past") },
                { 0x023A, new BGAPIError(0x023A, "Controller Busy", "Operation was rejected because the controller is busy and unable to process the request") },
                { 0x023B, new BGAPIError(0x023B, "Unacceptable Connection Interval", "The Unacceptable Connection Interval error code indicates that the remote device terminated the connection because of an unacceptable connection interval") },
                { 0x023C, new BGAPIError(0x023C, "Directed Advertising Timeout", "Directed advertising completed without a connection being created") },
                { 0x023D, new BGAPIError(0x023D, "MIC Failure", "Connection was terminated because the Message Integrity Check (MIC) failed on a received packet") },
                { 0x023E, new BGAPIError(0x023E, "Connection Failed to be Established", "LL initiated a connection but the connection has failed to be established. Controller did not receive any packets from remote end") },
                //Security Manager Protocol Errors - Errors from Security Manager Protocol
                { 0x0301, new BGAPIError(0x0301, "Passkey Entry Failed", "The user input of passkey failed, for example, the user cancelled the operation") },
                { 0x0302, new BGAPIError(0x0302, "OOB Data is not available", "Out of Band data is not available for authentication") },
                { 0x0303, new BGAPIError(0x0303, "Authentication Requirements", "The pairing procedure cannot be performed as authentication requirements cannot be met due to IO capabilities of one or both devices") },
                { 0x0304, new BGAPIError(0x0304, "Confirm Value Failed", "The confirm value does not match the calculated compare value") },
                { 0x0305, new BGAPIError(0x0305, "Pairing Not Supported", "Pairing is not supported by the device") },
                { 0x0306, new BGAPIError(0x0306, "Encryption Key Size", "The resultant encryption key size is insufficient for the security requirements of this device") },
                { 0x0307, new BGAPIError(0x0307, "Command Not Supported", "The SMP command received is not supported on this device") },
                { 0x0308, new BGAPIError(0x0308, "Unspecified Reason", "Pairing failed due to an unspecified reason") },
                { 0x0309, new BGAPIError(0x0309, "Repeated Attempts", "Pairing or authentication procedure is disallowed because too little time has elapsed since last pairing request or security request") },
                { 0x030A, new BGAPIError(0x030A, "Invalid Parameters", "The Invalid Parameters error code indicates: the command length is invalid or a parameter is outside of the specified range") },
                //Attribute Protocol Errors - Errors from Attribute Protocol
                { 0x0401, new BGAPIError(0x0401, "Invalid Handle", "The attribute handle given was not valid on this server") },
                { 0x0402, new BGAPIError(0x0402, "Read Not Permitted", "The attribute cannot be read") },
                { 0x0403, new BGAPIError(0x0403, "Write Not Permitted", "The attribute cannot be written") },
                { 0x0404, new BGAPIError(0x0404, "Invalid PDU", "The attribute PDU was invalid") },
                { 0x0405, new BGAPIError(0x0405, "Insufficient Authentication", "The attribute requires authentication before it can be read or written") },
                { 0x0406, new BGAPIError(0x0406, "Request Not Supported", "Attribute Server does not support the request received from the client") },
                { 0x0407, new BGAPIError(0x0407, "Invalid Offset", "Offset specified was past the end of the attribute") },
                { 0x0408, new BGAPIError(0x0408, "Insufficient Authorization", "The attribute requires authorization before it can be read or written") },
                { 0x0409, new BGAPIError(0x0409, "Prepare Queue Full", "Too many prepare writes have been queueud") },
                { 0x040A, new BGAPIError(0x040A, "Attribute Not Found", "No attribute found within the given attribute handle range") },
                { 0x040B, new BGAPIError(0x040B, "Attribute Not Long", "The attribute cannot be read or written using the Read Blob Request") },
                { 0x040C, new BGAPIError(0x040C, "Insufficient Encryption Key Size", "The Encryption Key Size used for encrypting this link is insufficient") },
                { 0x040D, new BGAPIError(0x040D, "Invalid Attribute Value Length", "The attribute value length is invalid for the operation") },
                { 0x040E, new BGAPIError(0x040E, "Unlikely Error", "The attribute request that was requested has encountered an error that was unlikely, and therefore could not be completed as requested") },
                { 0x040F, new BGAPIError(0x040F, "Insufficient Encryption", "The attribute requires encryption before it can be read or written") },
                { 0x0410, new BGAPIError(0x0410, "Unsupported Group Type", "The attribute type is not a supported grouping attribute as defined by a higher layer specification") },
                { 0x0411, new BGAPIError(0x0411, "Insufficient Resources", "Insufficient Resources to complete the request") },
                { 0x0480, new BGAPIError(0x0480, "Application Error Codes", "Application error code defined by a higher layer specification") },
            };

            if (errors.ContainsKey(errorCode))
            {
                return errors[errorCode];
            }
            return new BGAPIError(0xFFFF, "Unknown Error", "Error code " + errorCode.ToString("X") + " was not found in errors list.");
        }
    }
}
