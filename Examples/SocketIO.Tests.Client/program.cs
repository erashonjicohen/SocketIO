using SocketIO.Net.Protocol;
using SocketIO.Net.Runtime;
using SocketIO.Net.Transport.Sockets;
using System.Net;

namespace SocketIO.Tests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();

            // Handle Ctrl+C for graceful shutdown
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var serverTask = TcpServer.Run(new[] { "-s", "-p","9000" });

            // Give server time to start
            await Task.Delay(500);

            var clientTask = TcpClient.Run(new[] { "-c", "-p","9000" });

            // Wait for either to complete
            await Task.WhenAny(serverTask, clientTask);

            cts.Cancel();

            // Give time for cleanup
            await Task.Delay(500);
        }
    }
}