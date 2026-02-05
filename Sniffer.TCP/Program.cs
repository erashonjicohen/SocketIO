using SocketIO.Net.Diagnostics;
using SocketIO.Net.Protocol;
using SocketIO.Net.Runtime;
using SocketIO.Net.Transport.Sockets;
using System.Net;


var listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, 9000));
await listener.StartAsync();

var conn = await listener.AcceptAsync();

var opt = new DumpOptions
{
    WriteToConsole = true,
    FilePath = "tcp_frames.log",
    MaxBytesPerMessage = 4096,
    BytesPerLine = 16
};

await using var sink = new DumpSink(opt);
var dumper = new FrameAwareDumper(sink, opt);

var peer = new Peer(conn, new LengthPrefixedCodec());

await peer.ReceiveLoopAsync(async frame =>
{
    Console.WriteLine("App recibió frame de " + frame.Length + " bytes");
    // aquí ya tu app parsea protocolo binario, json, etc.
    await Task.CompletedTask;
}, dumper);
