/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Diagnostics;

namespace BGBLE
{
    class BGBLEDebugItemInfo
    {
        public ulong count;
        public DateTime startTimestamp { get; }
        public string threadName { get; }
        public DateTime timestamp { get; private set; }
        
        public BGBLEDebugItemInfo(DateTime _timestamp)
        {
            count = 0;
            threadName = Dispatcher.CurrentDispatcher.Thread.Name;
            startTimestamp = timestamp = _timestamp;
        }

        public void Inc(ulong value = 1)
        {
            count += value;
        }

        public void Reset()
        {
            count = 0;
            timestamp = DateTime.Now;
        }
    }

    public class BGBLEDebug
    {
        private static ulong _instancesCreated = 0;
        private static Dictionary<string, BGBLEDebugItemInfo> _items = new Dictionary<string, BGBLEDebugItemInfo>();

        public static void Log(string name, string message)
        {
#if DEBUG
            var _message = name + "[" + Dispatcher.CurrentDispatcher.Thread.Name + "]: " + message;
            Console.WriteLine(_message);
#endif
        }

        public static void Start(string name)
        {
#if DEBUG
            if (!_items.ContainsKey(name))
            {
                _items[name] = new BGBLEDebugItemInfo(DateTime.Now);
            }
            _items[name].Reset();
#endif
        }

        public static void Stop(string name, string message = null)
        {
#if DEBUG
            var timestamp = DateTime.Now;
            string threadName;
            long ticks = 0;
            if (_items.ContainsKey(name))
            {
                ticks = timestamp.Ticks - _items[name].timestamp.Ticks;
                threadName = _items[name].threadName;
                _items[name].Inc();
            }
            else
            {
                threadName = Dispatcher.CurrentDispatcher.Thread.Name;
            }
            var _message = name + "[" + threadName + ":" + ((float)ticks / 10000).ToString() + "ms]" + ((message == null) ? "" : ": " + message);
            Console.WriteLine(_message);
#endif
        }

        public static void Tick(string name, ulong treshhold, string message = null)
        {
#if DEBUG
            if (!_items.ContainsKey(name))
            {
                _items[name] = new BGBLEDebugItemInfo(DateTime.Now);
            }
            _items[name].Inc();

            if ((_items[name].count % treshhold) == 0)
            {
                var timestamp = DateTime.Now;
                var ticks = (timestamp.Ticks - _items[name].timestamp.Ticks);
                var count = _items[name].count;
                _items[name].Reset();

                var ms = ((float)ticks / 10000);
                var ips = (ulong)(treshhold * (1000 / ms));

                var threadName = _items[name].threadName;
                var _message = name + "[" + threadName + ":" + count + ":"+ ips.ToString() + "ips:" + ms.ToString() + "ms]" + ((message == null) ? "" : ": " + message);
                Console.WriteLine(_message);
            }    
#endif
        }
    }
}
