using System.Net;
using SocketIO.Net.Protocol;
using SocketIO.Net.Runtime;
using SocketIO.Net.Transport.Sockets;


public class TcpServer
{

    public static async Task Run(string[] args, CancellationToken ct = default)
    {
        int  port  = 9000;
        
        if (args.Length == 0 || !args[0].Equals("-s") && !args[0].Equals("server") ) 
        {
            Console.WriteLine("(WARN)  Para iniciar el servidor, ejecute con el argumento 'server' o '-s'");
            return;
        }

        if (args.Length > 1 && (args[1] == "port" || args[1] == "-p") || args.Length > 1 && args[2] != string.Empty)
        {
            port = int.Parse(args[2]);
        }

        Console.WriteLine("🚀 Servidor iniciado...");

        var listener = new TcpListener(
            new IPEndPoint(IPAddress.Loopback, port)
        );

        await listener.StartAsync();

        while (!ct.IsCancellationRequested)
        {
            var conn = await listener.AcceptAsync();
            var peer = new Peer(conn, new LengthPrefixedCodec());

            _ = Task.Run(() => peer.ReceiveLoopAsync(async (frame) =>
            {
                var text = System.Text.Encoding.UTF8.GetString(frame.Span);
                Console.WriteLine($"📩 Cliente dice: {text}");

                var response = System.Text.Encoding.UTF8.GetBytes("ACK: " + text);
                await peer.SendAsync(response);
            }));
        }
    }
}