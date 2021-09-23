using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Discreet.Network.Core
{
    public class StateObject
    {
        public Socket workSocket = null;
        public const int BUFFER_SIZE = 1024;
        public byte[] buffer = new byte[BUFFER_SIZE];
        public StringBuilder sb = new StringBuilder();
    }
}
