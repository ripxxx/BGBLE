/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Linq;

namespace BGBLE.BGAPI
{
    /// <summary>Hardware and Software versions of BLE dongle.</summary>
    /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.8 System)</seealso>
    public struct BGAPIHardwareInfo
    {
        public ushort majorSoftwareVersion;     //Major software version
        public ushort minorSoftwareVersion;     //Minor software version
        public ushort patchId;                  //Patch ID
        public ushort buildVersion;             //Build version
        public ushort linkLayerVersion;         //Link layer version
        public byte apiProtocolVersion;         //BGAPI protocol version
        public byte hardwareVersion;            //Hardware version

        public override string ToString()
        {
            string result = base.ToString();

            result += "\nMajor Software Version: " + majorSoftwareVersion.ToString("X");
            result += "\nMinor Software Version: " + minorSoftwareVersion.ToString("X");
            result += "\nPatch ID: " + patchId.ToString("X");
            result += "\nBuild Version: " + buildVersion.ToString("X");
            result += "\nLink Layer Version: " + linkLayerVersion.ToString("X");
            result += "\nBGAPI Protocol Version: " + apiProtocolVersion.ToString("X");
            result += "\nHardware Version: " + hardwareVersion.ToString("X");

            return result;
        }
    }
    /// <summary>Counters TX, RX and memory buffers.</summary>
    /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.8 System)</seealso>
    public struct BGAPICounters
    {
        public byte txOk;       //Number of transmitted packets
        public byte txRetry;    //Number of retransmitted packets
        public byte rxOk;       //Number of received packets where CRC was OK
        public byte rxFail;     //Number of received packets with CRC error
        public byte mBuffers;   //Number of available packet buffers

        public override string ToString()
        {
            string result = base.ToString();

            result += "\nNumber of transmitted packets: " + txOk;
            result += "\nNumber of retransmitted packets: " + txRetry;
            result += "\nNumber of received packets where CRC was OK: " + rxOk;
            result += "\nNumber of received packets with CRC error: " + rxFail;
            result += "\nNumber of available packet buffers: " + mBuffers;

            return result;
        }
    }

    /// <summary>The System class provides access to the local device and contains functions for example to query the local Bluetooth address, read firmware version, read radio packet counters etc.</summary>
    /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.8 System)</seealso>
    class BGAPISystemCommandClass : BGAPICommandClass
    {
        public const byte CLASS_ID = BGAPIDefinition.CCID_SYSTEM;

        public BGAPISystemCommandClass(BGAPIConnection connection) : base(connection) {
            /*_connection.RegisterEventHandlerForCommandClass(CLASS_ID, (BGAPIConnectionEventData eventData) => {
                
            });*/
        }

        /// <summary>This command can be used to test if the local device is functional. Similar to a typical "AT" -> "OK" test.</summary>
        /// <returns>Returns true or exception will be throwed.</returns>
        /// <seealso cref="String">Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.8 System)</seealso>
        public bool Hello()
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.SYSTEM_COMMAND_HELLO, new byte[] { }, 0x00);
            return true;
        }

        /// <summary>This command reads the local device's public Bluetooth address.</summary>
        /// <returns>Returns MAC address of the BLE dongle.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.8 System)</seealso>
        public string GetAddress()
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.SYSTEM_COMMAND_GET_ADDRESS, new byte[] { }, 0x00);
            if (response.length == 6)
            {
                return BitConverter.ToString(response.data.Take(response.length).Reverse().ToArray()).Replace('-', ':');
            }
            return null;
        }

        /// <summary>This command reads the number of supported connections from the local device.</summary>
        /// <returns>Returns max number of supported connections.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.8 System)</seealso>
        public byte GetConnections()
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.SYSTEM_COMMAND_GET_CONNECTIONS, new byte[] { }, 0x00);
            if (response.length == 1)
            {
                return response.data[0];
            }
            return 0;
        }

        /// <summary>Read packet counters and resets them, also returns available packet buffers.</summary>
        /// <returns>Returns structure with available packet buffers and packet counters.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.8 System)</seealso>
        public BGAPICounters GetCounters()
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.SYSTEM_COMMAND_GET_INFO, new byte[] { }, 0x00);
            if (response.length == 5)
            {
                return ByteArrayToStructure<BGAPICounters>(response.data.Take(response.length).ToArray());
            }
            return new BGAPICounters();
        }

        /// <summary>This command reads the local devices software and hardware versions.</summary>
        /// <returns>Returns structure with hardware and software info of the BLE dongle.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.8 System)</seealso>
        public BGAPIHardwareInfo GetInfo()
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.SYSTEM_COMMAND_GET_INFO, new byte[] { }, 0x00);
            if (response.length == 12)
            {
                return ByteArrayToStructure<BGAPIHardwareInfo>(response.data.Take(response.length).ToArray());
            }
            return new BGAPIHardwareInfo();
        }

        /// <summary>This command resets the local device immediately. The command does not have a response. Will not work properly with usb connection.</summary>
        /// <param name="bootToDFU">Selects the boot mode - FALSE : boot to main program, TRUE : boot to DFU</param>
        /// <returns>Returns true or exception will be throwed.</returns>
        /// <seealso>Bluetooth_Smart_Software-BLE-1.3-API-RM.pdf(5.8 System)</seealso>
        public bool Reset(bool bootToDFU = false)
        {
            BGAPIPacketPayload response = _connection.SendCommand(CLASS_ID, BGAPIDefinition.SYSTEM_COMMAND_RESET, new byte[] { (byte)((bootToDFU) ? 0x01 : 0x00) }, 0x01);
            return true;
        }
    }
}
