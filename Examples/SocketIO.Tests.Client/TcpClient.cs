using SocketIO.Net.Protocol.Codec;
using SocketIO.Net.Runtime;
using SocketIO.Net.Transport.Sockets;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class TcpClient
{
    public static async Task Run(string[] args)
    {
        int port = 9000;

        if (args.Length == 0 || !args[0].Equals("-c") && !args[0].Equals("client"))
        {
            Console.WriteLine("(WARN)  Para iniciar el cliente, ejecute con el argumento 'client' o '-c'");
            return;
        }

        if (args.Length > 1 && (args[1] == "port" || args[1] == "-p") || args.Length > 1 && args[2] != string.Empty)
        {
            port = int.Parse(args[2]);
        }

        var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
        );

        await socket.ConnectAsync(IPAddress.Loopback, port);

        var conn = new TcpConnection(socket);
        var peer = new Peer(conn, new LengthPrefixedCodec());

        Console.WriteLine("🟢 Conectado al servidor");

        _ = Task.Run(() => peer.ReceiveLoopAsync(frame =>
        {
            Console.WriteLine("📥 Servidor responde: " +
                Encoding.UTF8.GetString(frame.Span));
            return Task.CompletedTask;
        }));

        while (true)
        {
            
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) break;
            await peer.SendAsync(Encoding.UTF8.GetBytes(line));
        }

        await conn.CloseAsync();
    }
}
