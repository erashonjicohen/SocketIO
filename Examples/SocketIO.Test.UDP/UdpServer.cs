using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketIO.Test.UDP
{
    public class UdpServer
    {
        public static async Task Run(string[] args)
        {
            int port = 9001;

            // --- modo ---
            bool isServer =
                args.Length > 0 &&
                (args[0].Equals("server", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("-s", StringComparison.OrdinalIgnoreCase));

            if (!isServer)
            {
                Console.WriteLine("(WARN)  Para iniciar el servidor, ejecute con 'server' o '-s'");
                Console.WriteLine("   Opcional: -p 9001 | port 9001 | --port 9001");
                return;
            }

            // --- parse port ---
            // soporta: -p 9002 | port 9002 | --port 9002
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

            Console.WriteLine($"(connect) UDP Server escuchando en {port}");

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));

            var buffer = new byte[2048];

            while (true)
            {
                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

                var result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, remote);

                var message = Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes);
                Console.WriteLine($"(Server) {result.RemoteEndPoint}: {message}");

                // responder
                var response = Encoding.UTF8.GetBytes("PONG");
                await socket.SendToAsync(response, SocketFlags.None, result.RemoteEndPoint);
            }
        }
    }
}
