using SocketIO.Net.Transport.Sockets;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketIO.Test.UDP
{
    public class UdpClientApp
    {
        public static async Task Run(string[] args)
        {
            int port = 9001;

            bool isClient =
                args.Length > 0 &&
                (args[0].Equals("client", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("-c", StringComparison.OrdinalIgnoreCase));

            if (!isClient)
            {
                Console.WriteLine("(WARN)  Para iniciar el cliente, ejecute con 'client' o '-c'");
                Console.WriteLine("   Opcional: -p 9001 | port 9001 | --port 9001");
                return;
            }

            // parse port: -p 9002 | port 9002 | --port 9002
            for (int i = 1; i < args.Length; i++)
            {
                var a = args[i];

                bool isPortFlag =
                    a.Equals("-p", StringComparison.OrdinalIgnoreCase) ||
                    a.Equals("port", StringComparison.OrdinalIgnoreCase) ||
                    a.Equals("--port", StringComparison.OrdinalIgnoreCase);

                if (!isPortFlag) continue;

                if (i + 1 >= args.Length)
                {
                    Console.WriteLine("(WARN)  Falta el valor del puerto después de " + a);
                    return;
                }

                if (!int.TryParse(args[i + 1], out port) || port < 1 || port > 65535)
                {
                    Console.WriteLine("(WARN)  Puerto inválido: " + args[i + 1]);
                    return;
                }

                break;
            }

            var server = new IPEndPoint(IPAddress.Loopback, port);

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // ✅ fija el peer (para que ReceiveAsync tenga sentido)
            socket.Connect(server);

            var conn = new UdpConnection(socket, server);

            Console.WriteLine($"(Cliente) Enviando PING al {server} ...");

            await conn.SendAsync(Encoding.UTF8.GetBytes("PING"));

            var buffer = new byte[1024];

            // opcional: timeout simple
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                int received = await conn.ReceiveAsync(buffer, cts.Token);
                Console.WriteLine("(Cliente) Respuesta: " + Encoding.UTF8.GetString(buffer, 0, received));
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("(Cliente) TRIGGER: finalizado conexion.");
            }
            finally
            {
                cts.Dispose();
                await conn.CloseAsync();
            }


            
        }
    }
}
