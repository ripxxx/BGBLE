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
    public class BGBLEService
    {
        private BGBLEDevice _device;
        private ushort _endAttributeHandle;
        private ushort _startAttributeHandle;
        private string _uuid;

        /// <summary>This class implements service of BGBLEDevice.</summary>
        public BGBLEService(BGBLEDevice device, string uuid, ushort startAttributeHandle, ushort endAttributeHandle)
        {
            _device = device;
            _endAttributeHandle = endAttributeHandle;
            _startAttributeHandle = startAttributeHandle;
            _uuid = uuid;
        }

        //PROPRTIES
        /// <summary>UUID(Type) of service.</summary>
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
    }
}
