using SocketIO.Net.Abstractions;
using System.IO.Ports;
using System.Net;


namespace SocketIO.Net.Transport.Serial
{
    public sealed class SerialConnection : IConnection
    {
        private readonly SerialPort _port;
        private string portName;
        private int baud;

        public SerialConnection(
            string portName,
            int baudRate,
            int dataBits,
            Parity parity,
            StopBits stopBits,
            Handshake handshake)
        {
            _port = new SerialPort(portName, baudRate)
            {
                DataBits = dataBits,
                Parity = parity,
                StopBits = stopBits,
                Handshake = handshake,
                ReadTimeout = -1,
                WriteTimeout = -1,
                DtrEnable = true,
                RtsEnable = true
            };

            _port.Open();
        }

        public SerialConnection(string portName, int baud)
        {
            this.portName = portName;
            this.baud = baud;
        }


        //public EndPoint RemoteEndPoint => new DnsEndPoint($"SERIAL:{_port.PortName}", _port.BaudRate);
        public EndPoint RemoteEndPoint => new SerialEndPoint(_port.PortName, _port.BaudRate);


        public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
        {
            await _port.BaseStream.WriteAsync(data, ct);
            await _port.BaseStream.FlushAsync(ct);
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            return await _port.BaseStream.ReadAsync(buffer, ct);
        }

        public ValueTask CloseAsync()
        {
            if (_port.IsOpen) _port.Close();
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            _port.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
