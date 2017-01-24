/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

using BGBLE.BGAPI;

namespace BGBLE
{
    /// <summary>This class implements BLE central which using BG API.</summary>
    class BGBLECentral
    {
        private BGAPIConnection _connection;
        public BGBLECentral(SerialPort serialPort = null)
        {
            _connection = BGAPIConnection.Connection(serialPort);
        }

    }
}
