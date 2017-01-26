/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Management;
using System.IO.Ports;

namespace BGBLE.BGAPI
{
    /// <summary>Structure with details of packet header.</summary>
    public struct BGAPIPacketHeader
    {
        public byte commandClassId;
        public byte commandId;
        public bool isBluetoothSmart;
        public bool isCommandOrResponse;
        public bool isEvent;
        public bool isWifi;
        public ushort payloadLength;
    }

    /// <summary>Structure with details of packet data.</summary>
    public struct BGAPIPacketPayload
    {
        /// <summary>Data packet.</summary>
        public byte[] data;

        /// <summary>Length of data packet.</summary>
        public ushort length;

        public BGAPIPacketPayload(byte[] _data, ushort _length)
        {
            data = _data;
            length = _length;
        }
    }

    /// <summary>Structure with details of packet response.</summary>
    struct BGAPIConnectionResponseData
    {
        /// <summary>Structure with header details.</summary>
        public BGAPIPacketHeader header;

        /// <summary>Indicates that packet ready to be processed.</summary>
        public bool isReady;

        /// <summary>Packet data.</summary>
        public byte[] payload;
    }

    public struct BGAPIConnectionEventData
    {
        public BGAPIPacketHeader header;
        public byte[] payload;
        public ushort type;

        public BGAPIConnectionEventData(ushort _type, BGAPIPacketHeader _header, byte[] _payload)
        {
            header = _header;
            payload = _payload;
            type = _type;
        }
    }
    public delegate void BGAPIEventReceivedHandler(BGAPIConnectionEventData eventData);

    /// <summary>This class serves connection with BLED112 device. To get instance of the class call class method Connection().</summary>
    public class BGAPIConnection
    {
        private Dictionary<byte, BGAPIEventReceivedHandler> _eventHandlers;
        private List<BGAPIConnectionEventData> _eventsData;
        private Dictionary<ushort, List<ulong>> _eventsDataCount;
        private Dictionary<ushort, Thread> _eventsThreads;
        private bool _isWatingResponse = false;
        private BGAPIConnectionResponseData _responseData;
        private SerialPort _serialPort;
        private System.Timers.Timer _timer;

        private static BGAPIConnection _instance = null;

        private BGAPIConnection(SerialPort serialPort = null)
        {
            _eventHandlers = new Dictionary<byte, BGAPIEventReceivedHandler>();
            _eventsData = new List<BGAPIConnectionEventData>();
            _eventsDataCount = new Dictionary<ushort, List<ulong>>();
            _eventsThreads = new Dictionary<ushort, Thread>();
            

            _responseData = new BGAPIConnectionResponseData();
            _responseData.isReady = false;
            if (serialPort == null)
            {
                string portName = FindPort();
                if(portName != "")
                {
                    _serialPort = new SerialPort(portName, 512000);
                }
                else
                {
                    throw new BGAPIException(0xFF01);
                }
            }
            else {
                _serialPort = serialPort;
            }
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
            _serialPort.DataReceived += DataReceivedOnSerialPort;
            _serialPort.ErrorReceived += ErrorReceivedOnSerialPort;
            _serialPort.ReceivedBytesThreshold = 1;
            _serialPort.Open();

            _timer = new System.Timers.Timer(10000);
            _timer.Elapsed += TimeoutReached;
        }

        ~BGAPIConnection()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        // EVENT HANDLERS
        /// <summary>Event handler for DataReceived event of SerialPort.</summary>
        /// <param name="sender">Instace of SerialPort class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void DataReceivedOnSerialPort(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = sender as SerialPort;
            ReadSerialPortData(serialPort);
        }

        /// <summary>Event handler for ErrorReceived event of SerialPort.</summary>
        /// <param name="sender">Instace of SerialPort class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void ErrorReceivedOnSerialPort(object sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
        {
            Console.WriteLine("!!!Serial Port Error!!!");
        }

        /// <summary>Event handler for Elapsed event of System.Timers.Timer.</summary>
        /// <param name="sender">Instace of System.Timers.Timer class which generated the event</param>
        /// <param name="e">EventArgs</param>
        private void TimeoutReached(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            throw new BGAPIException(0xFF02, new TimeoutException());
        }
        // EVENT HANDLERS

        /// <summary>Parses data packet and extracts header information.</summary>
        /// <param name="data">Packet data</param>
        /// <returns>Returns structure of type BGAPIPacketHeader.</returns>
        private BGAPIPacketHeader ExtractPacketHeader(byte[] data)
        {
            BGAPIPacketHeader result = new BGAPIPacketHeader();

            if ((data[0] & BGAPIDefinition.MT_EVENT) == BGAPIDefinition.MT_EVENT)
            {
                result.isCommandOrResponse = false;
                result.isEvent = true;
            }
            else
            {
                result.isCommandOrResponse = true;
                result.isEvent = false;
            }

            if ((data[0] & BGAPIDefinition.TT_WIFI) == BGAPIDefinition.TT_WIFI)
            {
                result.isBluetoothSmart = false;
                result.isWifi = true;
            }
            else
            {
                result.isBluetoothSmart = true;
                result.isWifi = false;
            }

            ushort length = (ushort)(data[0] & 0x07);
            length <<= 8;
            length += (ushort)data[1];
            result.payloadLength = length;

            result.commandClassId = data[2];
            result.commandId = data[3];

            return result;
        }

        /// <summary>Reads data from serial port. Determines packets types and makes preprocessing of data packets.</summary>
        /// <param name="serialPort">SerialPort object</param>
        private void ReadSerialPortData(SerialPort serialPort)
        {
            byte[] data = new byte[2048];
            int headerSize = 4;
            do
            {
                if (serialPort.BytesToRead >= headerSize)
                {
                    serialPort.Read(data, 0, headerSize);
                    BGAPIPacketHeader header = ExtractPacketHeader(data);
                    //Console.WriteLine("2>>>>>> " + BitConverter.ToString(data, 0, headerSize));
                    if (header.isBluetoothSmart)
                    {
                        ushort payloadLength = header.payloadLength;
                        if (serialPort.BytesToRead >= payloadLength)
                        {
                            if (payloadLength > 0)
                            {
                                serialPort.Read(data, 0, payloadLength);
                                //Console.WriteLine("3>>>>>> " + BitConverter.ToString(data, 0, payloadLength));
                            }
                            if (header.isEvent)
                            {
                                if (_eventHandlers.ContainsKey(header.commandClassId)) {
                                    ushort threadId = (ushort)((header.commandClassId << 8) + header.commandId);
                                    if (!_eventsDataCount.ContainsKey(threadId))
                                    {
                                        _eventsDataCount[threadId] = new List<ulong>();
                                    }
                                    if (!_eventsThreads.ContainsKey(threadId))
                                    {
                                        Thread eventThread = new Thread(() => {
                                            byte t_commandClassId = header.commandClassId;
                                            ushort t_threadId = threadId;
                                            while (true)
                                            {
                                                if ((_eventsDataCount[t_threadId].Count > 0) && (_eventsData.Count > 0))
                                                {
                                                    //Console.WriteLine("Thread " + t_threadId.ToString("x") + " queue length: " + _eventsData.Count);
                                                    BGAPIConnectionEventData t_eventData = _eventsData.First();
                                                    if (t_eventData.type != t_threadId)
                                                    {
                                                        continue;
                                                    }
                                                    _eventsData.RemoveAt(0);
                                                    _eventsDataCount[t_threadId].RemoveAt(0);

                                                    if (_eventHandlers.ContainsKey(t_commandClassId))
                                                    {
                                                        _eventHandlers[header.commandClassId].Invoke(t_eventData);
                                                    }
                                                }
                                                Thread.Sleep(10);
                                            }
                                        });
                                        _eventsThreads[threadId] = eventThread;
                                        eventThread.Name = "EventThread_" + threadId.ToString("X");
                                        eventThread.Start();

                                    }
                                    _eventsData.Add(new BGAPIConnectionEventData(threadId, header, data.Take(payloadLength).ToArray()));
                                    _eventsDataCount[threadId].Add((ulong)(_eventsDataCount[threadId].Count + 1));
                                }
                            }
                            else if (header.isCommandOrResponse)
                            {
                                _responseData.header = header;
                                _responseData.payload = data.Take(payloadLength).ToArray();
                                _responseData.isReady = true;
                            }
                        }
                    }
                }
            } while (serialPort.BytesToRead > 0);
        }

        /// <summary>Registering event handler for command class.</summary>
        /// <param name="commandClassId">Id of BG API command class</param>
        /// <param name="eventHandler">Event Handler which will be called when event packet(of command class) received</param>
        public void RegisterEventHandlerForCommandClass(byte commandClassId, BGAPIEventReceivedHandler eventHandler)
        {
            _eventHandlers[commandClassId] = eventHandler;
        }

        /// <summary>Sends data packet to BLED112 device and waits for response.</summary>
        /// <param name="commandClassId">ID of commands class from BG API</param>
        /// <param name="commandId">ID of command from BG API</param>
        /// <param name="payload">Packet data</param>
        /// <param name="payloadLength">Quantity of bytes to send</param>
        /// <returns>Returns structure of type BGAPIPacketPayload which contains packet header information and packet data.</returns>
        public BGAPIPacketPayload SendCommand(byte commandClassId, byte commandId, byte[] payload, ushort payloadLength)
        {
            if (_isWatingResponse)
            {
                throw new BGAPIException(0xFF03);
            }
            else if (payloadLength > BGAPIDefinition.COMMAND_PAYLOAD_MAX_LENGTH)
            {
                throw new BGAPIException(0xFE01, "Command with id " + commandId.ToString("X") + " of class " + commandClassId.ToString("X") + " has to long payload (>" + BGAPIDefinition.COMMAND_PAYLOAD_MAX_LENGTH + ").");
            }
            ushort requestDataLength = (ushort)(payloadLength + 4);
            byte[] requestData = new byte[requestDataLength];

            byte[] _payloadLength = BitConverter.GetBytes(payloadLength).ToArray();

            requestData[0] = (byte)(BGAPIDefinition.MT_COMMAND_RESPONSE | _payloadLength[1]);
            requestData[1] = _payloadLength[0];
            requestData[2] = commandClassId;
            requestData[3] = commandId;

            Array.Copy(payload, 0, requestData, 4, payloadLength);
            //Console.WriteLine("1>>>>>> " + BitConverter.ToString(requestData, 0, requestDataLength));
            _timer.Start();
            _serialPort.Write(requestData, 0, requestDataLength);
            _isWatingResponse = true;
            while (_isWatingResponse)
            {
                if (_responseData.isReady)
                {
                    _timer.Stop();
                    _responseData.isReady = false;
                    _isWatingResponse = false;
                    if (_responseData.header.commandClassId != commandClassId)
                    {
                        throw new BGAPIException(0xFE02, "Received unexpected command class in response, expected id: " + commandClassId.ToString("X") + ", received:  " + _responseData.header.commandClassId.ToString("X"));
                    }
                    else if(_responseData.header.commandId != commandId)
                    {
                        throw new BGAPIException(0xFE03, "Received unexpected command in response, expected id: " + commandId.ToString("X") + ", received:  " + _responseData.header.commandId.ToString("X"));
                    }
                    BGAPIPacketPayload result = new BGAPIPacketPayload(_responseData.payload, _responseData.header.payloadLength);
                    return result;
                }
            }
            return new BGAPIPacketPayload();
        }

        /// <summary>Returns BGAPIConnection instance. It ensures the existence of only one copy of BGAPIConnection instance.</summary>
        /// <param name="data">Packet data</param>
        /// <returns>Returns object of BGAPIConnection.</returns>
        public static BGAPIConnection SharedConnection(SerialPort serialPort = null)
        {
            if (_instance == null)
            {
                _instance = new BGAPIConnection(serialPort);
            }
            return _instance;
        }

        /// <summary>Seraches for serial port device with VID&PID = VID_2458&PID_0001 in WMI database.</summary>
        /// <returns>Returns string with serial port name: COM1, COM2, ...</returns>
        public static string FindPort()
        {
            string query = "SELECT DeviceID, PNPDeviceID FROM Win32_SerialPort WHERE PNPDeviceID LIKE '%VID_2458&PID_0001%'";
            string result = String.Empty;

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", query);
            try
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    result = obj["DeviceID"].ToString().Trim();
                    return result;
                }
            }
            catch (Exception ex) { }
            return "";
        }
    }
}
