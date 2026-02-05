using SocketIO.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Sniffer.UDP
{
    public static class UdpSenderExample
    {
        public static async Task RunAsync(int port)
        {
            using var socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp
            );

            var target = new IPEndPoint(IPAddress.Loopback, port);

            // Texto
            await socket.SendToAsync(
                Encoding.UTF8.GetBytes("HELLO UDP RAW"),
                SocketFlags.None,
                target
            );

            // Binario arbitrario
            await socket.SendToAsync(
                new byte[] { 0x01, 0xFF, 0x10, 0x20, 0x30, 0x00, 0x7F },
                SocketFlags.None,
                target
            );

            // Payload más grande
            var random = new Random();
            var blob = new byte[64];
            random.NextBytes(blob);

            await socket.SendToAsync(blob, SocketFlags.None, target);
        }
    }

    public static class UdpSnifferExample
    {
        public static async Task RunAsync(int port, CancellationToken ct)
        {
            var options = new DumpOptions
            {
                WriteToConsole = true,
                FilePath = "udp_dump.log",
                MaxBytesPerMessage = 1024,
                BytesPerLine = 16
            };

            await using var sink = new DumpSink(options);

            var sniffer = new UdpSnifferRaw(sink, options);

            await sniffer.RunAsync(port, ct);
        }
    }
}
