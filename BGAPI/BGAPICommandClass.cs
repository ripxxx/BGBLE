/**
 * Created by Aleksandr Berdnikov.
 * Copyright 2017 Onix-Systems.
*/

using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace BGBLE.BGAPI
{
    /// <summary>Base class for all commands classes.</summary>
    class BGAPICommandClass
    {
        protected BGAPIConnection _connection;

        public BGAPICommandClass(BGAPIConnection connection)
        {
            _connection = connection;
        }

        /// <summary>Converts bytes array to structure(members only primmitive types) of type T.</summary>
        /// <param name="bytes">Array of bytes with size = sizeof(T)</param>
        /// <returns>Returns structure of type T.</returns>
        protected T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return stuff;
        }
    }
}
