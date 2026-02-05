using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SocketIO.Net.Transport.Serial
{
    public sealed class SerialEndPoint : EndPoint
    {
        public string PortName { get; }
        public int BaudRate { get; }

        public SerialEndPoint(string portName, int baudRate)
        {
            PortName = portName;
            BaudRate = baudRate;
        }

        public override string ToString()
            => $"SERIAL:{PortName}@{BaudRate}";
    }
}
