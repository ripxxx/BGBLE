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
using System.Runtime.InteropServices;

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

    public class BGAPIDeviceChangeEventArgs : EventArgs
    {
        public string PortName { get; set; }
    }
    public delegate void BGAPIDeviceChangeEventHandler(object sender, BGAPIDeviceChangeEventArgs e);

    /// <summary>This class serves connection with BLED112 device. To get instance of the class call class method Connection().</summary>
    public class BGAPIConnection
    {
        private static Dictionary<string, BGAPIConnection> _instances;

        private static ManagementEventWatcher deviceArrivalWatcher;
        private static ManagementEventWatcher deviceRemovalWatcher;

        private Dictionary<ushort, BGAPIEventReceivedHandler> _eventHandlers;
        private volatile Dictionary<ushort, List<BGAPIConnectionEventData>> _eventsData;
        private Dictionary<ushort, Thread> _eventsThreads;
        private volatile bool _isTimeoutReached = false;
        private bool _isWatingResponse = false;
        private bool _isWatingRestore = false;
        private BGAPIConnectionResponseData _responseData;
        private SerialPort _serialPort;
        private System.Timers.Timer _timer;

        /// <summary>Fires when device was attached to PC.</summary>
        public event BGAPIDeviceChangeEventHandler DeviceInserted;
        /// <summary>Fires when device was removed from PC.</summary>
        public event BGAPIDeviceChangeEventHandler DeviceRemoved;

        /// <summary>Fires when device was attached to PC.</summary>
        public static event BGAPIDeviceChangeEventHandler DeviceAvailable;

        [DllImport("winmm.dll")]
        internal static extern uint timeBeginPeriod(uint period);

        [DllImport("winmm.dll")]
        internal static extern uint timeEndPeriod(uint period);

        private BGAPIConnection(SerialPort serialPort)
        {
            _eventHandlers = new Dictionary<ushort, BGAPIEventReceivedHandler>();
            _eventsData = new Dictionary<ushort, List<BGAPIConnectionEventData>>();
            _eventsThreads = new Dictionary<ushort, Thread>();
            

            _responseData = new BGAPIConnectionResponseData();
            _responseData.isReady = false;

            _serialPort = serialPort;

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
            Close();
        }

        //PROPRTIES
        /// <summary>Is connection open.</summary>
        public bool IsOpen
        {
            get { return _serialPort.IsOpen; }
        }
        //PROPRTIES

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
            _isTimeoutReached = true;
        }
        // EVENT HANDLERS

        private void RaseDeviceInserted(string newPortName = null)
        {
            if (_isWatingRestore)
            {
                if (newPortName != null)
                {
                    if (IsOpen)
                    {
                        Close();
                        _serialPort.PortName = newPortName;
                    }
                }
                _isWatingRestore = false;
                Open();

                BGAPIDeviceChangeEventArgs eventArgs = new BGAPIDeviceChangeEventArgs();
                eventArgs.PortName = _serialPort.PortName;
                DeviceInserted?.Invoke(this, eventArgs);
            }
        }

        private void RaseDeviceRemoved()
        {
            if (!_isWatingRestore)
            {
                _isWatingRestore = true;
                Close();

                BGAPIDeviceChangeEventArgs eventArgs = new BGAPIDeviceChangeEventArgs();
                eventArgs.PortName = _serialPort.PortName;
                DeviceRemoved?.Invoke(this, eventArgs);
            }
        }

        /// <summary>Seraches for serial ports of devices with VID&PID = VID_2458&PID_0001 in WMI database.</summary>
        /// <returns>Returns list with serial ports names: COM1, COM2, ...</returns>
        public static List<string> FindPorts()
        {
            string query = "SELECT DeviceID, PNPDeviceID FROM Win32_SerialPort WHERE PNPDeviceID LIKE '%VID_2458&PID_0001%'";
            List<string> ports = new List<string>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", query);

            try
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var port = obj["DeviceID"].ToString().Trim();
                    ports.Add(port);
                }
            }
            catch (Exception ex) {
#if DEBUG
                Console.WriteLine("Searching available ports error: " + ex.Message);
#endif
            }

            return ports;
        }

        /// <summary>Registers watchers to receive device inserted an device removed events.</summary>
        private static void RegisterDeviceChangeWatchers()
        {
            //Device Arrival = 2
            var deviceArrivalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
            //Device Removal = 3
            var deviceRemovalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");

            deviceArrivalWatcher = new ManagementEventWatcher(deviceArrivalQuery);
            deviceRemovalWatcher = new ManagementEventWatcher(deviceRemovalQuery);

            deviceArrivalWatcher.EventArrived += ((sender, eventArgs) => {
                var ports = FindPorts();
                if (ports.Count == 0)
                {
                    return;
                }

                if (_instances.Count == 0)
                {
                    BGAPIDeviceChangeEventArgs _eventArgs = new BGAPIDeviceChangeEventArgs();
                    _eventArgs.PortName = ports.First();
                    DeviceAvailable?.Invoke(null, _eventArgs);
                    return;
                }

                var freePorts = new List<string>();

                foreach (var portName in ports)
                {
                    if (_instances.ContainsKey(portName))
                    {
                        _instances[portName].RaseDeviceInserted();
                    }
                    else
                    {
                        freePorts.Add(portName);
                    }
                }

                while (freePorts.Count > 0)
                {
                    var portName = freePorts.First();
                    freePorts.RemoveAt(0);
                    foreach (KeyValuePair<string, BGAPIConnection> entry in _instances)
                    {
                        if (!ports.Contains(entry.Key))
                        {
                            _instances[portName] = _instances[entry.Key];
                            _instances.Remove(entry.Key);
                            entry.Value.RaseDeviceInserted(portName);
                        }
                    }
                }
            });

            deviceRemovalWatcher.EventArrived += ((sender, eventArgs) => {
                var ports = FindPorts();

                foreach (KeyValuePair<string, BGAPIConnection> entry in _instances)
                {
                    if (!ports.Contains(entry.Key))
                    {
                        entry.Value.RaseDeviceRemoved();
                    }
                }
            });

            // Start listening for events
            deviceArrivalWatcher.Start();
            deviceRemovalWatcher.Start();
        }

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
                    //RESPONSE
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
                                ushort threadId = (ushort)((header.commandClassId << 8) + header.commandId);
                                if (!_eventHandlers.ContainsKey(threadId))
                                {
                                    threadId = (ushort)((header.commandClassId << 8) + 0xFF);
                                    if (!_eventHandlers.ContainsKey(threadId))
                                    {
                                        continue;
                                    }
                                }
                                
                                if (!_eventsData.ContainsKey(threadId))
                                {
                                    _eventsData[threadId] = new List<BGAPIConnectionEventData>();
                                }

                                if (!_eventsThreads.ContainsKey(threadId) || (_eventsThreads[threadId].ThreadState == ThreadState.Stopped))
                                {
                                    var threadName = "EventThread_" + threadId.ToString("X");
                                    if (_eventsThreads.ContainsKey(threadId))
                                    {
#if DEBUG
                                        Console.WriteLine("RESTARTING THREAD: " + threadName);
#endif
                                    }
                                    else
                                    {
#if DEBUG
                                        Console.WriteLine("STARTING THREAD: " + threadName);
#endif
                                    }
                                    Thread eventThread = new Thread(() => {
                                        byte t_commandClassId = header.commandClassId;
                                        ushort t_threadId = threadId;
                                        string t_threadName = threadName;

                                        while (_serialPort.IsOpen)
                                        {
                                            if (_eventsData[t_threadId].Count > 0)
                                            {
#if DEBUG
                                                var _event = BGAPIDefinition.FindEventById(t_threadId);
                                                BGBLEDebug.Tick("EVENT", 100, _event.ToString());
#endif
                                                BGAPIConnectionEventData t_eventData = _eventsData[t_threadId].First();
                                                if ((t_eventData.payload != null) && _eventHandlers.ContainsKey(t_threadId))
                                                {
                                                    _eventHandlers[t_threadId].Invoke(t_eventData);
                                                }

                                                _eventsData[t_threadId].RemoveAt(0);
                                            }
                                            timeBeginPeriod(1);
                                            Thread.Sleep(2);
                                            timeEndPeriod(1);
                                        }
                                    });
                                    _eventsThreads[threadId] = eventThread;
                                    eventThread.Name = threadName;
                                    eventThread.Start();
                                }
                                _eventsData[threadId].Add(new BGAPIConnectionEventData(threadId, header, data.Take(payloadLength).ToArray()));
                            }
                            else if (header.isCommandOrResponse)
                            {
                                _responseData.header = header;
                                _responseData.payload = data.Take(payloadLength).ToArray();
#if DEBUG
                                var _command = BGAPIDefinition.FindCommandById(header.commandClassId, header.commandId);
                                var _payload = BitConverter.ToString(_responseData.payload, 0, payloadLength).Replace("-", " ");
                                BGBLEDebug.Log("RESPONSE", _command.ToString() + "  [" + _payload + "]");
#endif
                                _responseData.isReady = true;
                            }
                        }
                    }
                }
            } while (serialPort.BytesToRead > 0);
        }

        /// <summary>Closes the serial port.</summary>
        public void Close()
        {
            if ((_serialPort != null) && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        /// <summary>Opens the serial port.</summary>
        public void Open()
        {
            if ((_serialPort != null) && !_serialPort.IsOpen)
            {
                _serialPort.Open();
            }
        }

        /// <summary>Registering event handler for command class.</summary>
        /// <param name="commandClassId">Id of BG API command class</param>
        /// <param name="eventHandler">Event Handler which will be called when event packet(of command class) received</param>
        public void RegisterEventHandlerForCommandClass(byte commandClassId, BGAPIEventReceivedHandler eventHandler)
        {
            RegisterEventHandlerForEvent(commandClassId, 0xFF, eventHandler);
        }

        /// <summary>Registering event handler for event.</summary>
        /// <param name="commandClassId">Id of BG API command class</param>
        /// <param name="eventId">Id of BG API event in corresponding command class</param>
        /// <param name="eventHandler">Event Handler which will be called when event packet(of command class) received</param>
        public void RegisterEventHandlerForEvent(byte commandClassId, byte eventId, BGAPIEventReceivedHandler eventHandler)
        {
            ushort id = (ushort)((commandClassId << 8) + eventId);
            _eventHandlers[id] = eventHandler;
        }

        /// <summary>Sends data packet to BLED112 device and waits for response.</summary>
        /// <param name="commandClassId">ID of commands class from BG API</param>
        /// <param name="commandId">ID of command from BG API</param>
        /// <param name="payload">Packet data</param>
        /// <param name="payloadLength">Quantity of bytes to send</param>
        /// <returns>Returns structure of type BGAPIPacketPayload which contains packet header information and packet data.</returns>
        public BGAPIPacketPayload SendCommand(byte commandClassId, byte commandId, byte[] payload, ushort payloadLength)
        {
            if (_isWatingRestore)
            {
                throw new BGAPIException(0xFF04);
            }
            else if (!IsOpen)
            {
                throw new BGAPIException(0xFF05);
            }
            else if (_isWatingResponse)
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
#if DEBUG
            var _command = BGAPIDefinition.FindCommandById(commandClassId, commandId);
            var _requestData = BitConverter.ToString(requestData, 0, requestDataLength).Replace("-", " ");
            BGBLEDebug.Log("REQUEST", _command.ToString() + "  [" + _requestData + "]");
#endif
            _timer.Start();
            _serialPort.Write(requestData, 0, requestDataLength);
            _isWatingResponse = true;
            while (_isWatingResponse && !_isWatingRestore)
            {
                if (_isTimeoutReached)
                {
                    _isTimeoutReached = false;
                    throw new BGAPIException(0xFF02, new TimeoutException());
                }

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
            if ((_instances == null) || (_instances.Count == 0)) {
                if (_instances == null) {
                    _instances = new Dictionary<string, BGAPIConnection>();

                    RegisterDeviceChangeWatchers();
                }

                SerialPort _serialPort = serialPort;
                if (_serialPort == null)
                {
                    var portName = FindPort();
                    if (portName != "")
                    {
                        _serialPort = new SerialPort(portName, 115200);
                    }
                    else
                    {
                        throw new BGAPIException(0xFF01);
                    }
                }

                _instances[_serialPort.PortName] = new BGAPIConnection(_serialPort);

                return _instances[_serialPort.PortName];
            }
            else
            {
                SerialPort _serialPort = serialPort;
                if (_serialPort == null)
                {
                    return _instances.First().Value;
                }
                else
                {
                    var portName = _serialPort.PortName;
                    if (!_instances.ContainsKey(portName))
                    {
                        _instances[portName] = new BGAPIConnection(serialPort);
                    }
                    return _instances[portName];
                }
            }

            
        }

        /// <summary>Seraches for free serial port device with VID&PID = VID_2458&PID_0001 in WMI database.</summary>
        /// <returns>Returns string with serial port name: COM1, COM2, ...</returns>
        public static string FindPort()
        {
            var ports = FindPorts();
            if (ports.Count > 0)
            {
                foreach (var portName in ports)
                {
                    if (!_instances.ContainsKey(portName))
                    {
                        return portName;
                    }
                }
            }
            return "";
        }
    }
}
