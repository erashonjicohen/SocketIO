using SocketIO.Net.Diagnostics;
using SocketIO.Net.Protocol.Codec;
using SocketIO.Net.Runtime;
using SocketIO.Net.Transport.Sockets;
using System.Data;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;




// en prod: IP virtual KiSoft One
IPAddress ip            = IPAddress.Loopback;
int port                = 9801;

bool writeToConsole     = true;
int maxBytesPerMessage  = 8192;
int bytesPerLine        = 16;
string filePath = "./kisoft_9801";


DataSet Settings = new DataSet("Settings");
Settings.ReadXml("Settings.xml");

var KisoftTable      = Settings.Tables["KiSoft"];
var DumpOptionsTable = Settings.Tables["DumpOptions"];
var LogOptionsTable  = Settings.Tables["LogOptions"];

if (Settings.Tables.Count > 0)
{
    IPAddress.TryParse(KisoftTable.AsEnumerable().First().Field<string>("Ip"), out ip);
    int.TryParse(KisoftTable.AsEnumerable().First().Field<string>("Port"), out port);
    bool.TryParse(DumpOptionsTable.AsEnumerable().First().Field<string>("WriteToConsole"), out writeToConsole); 
    int.TryParse(DumpOptionsTable.AsEnumerable().First().Field<string>("MaxBytesPerMessage"), out maxBytesPerMessage);
    int.TryParse(DumpOptionsTable.AsEnumerable().First().Field<string>("BytesPerLine"), out bytesPerLine);
    filePath = LogOptionsTable.AsEnumerable().First().Field<string>("FilePath") ?? "./kisoft_9801";
}

Logger _logger = new Logger(
    new LoggerOptions { 
        Enabled = true, 
        FilePath = filePath, 
        MinimumLevel = LogLevel.Trace, 
        WriteToConsole = true 
    }
);

var opt = new DumpOptions
{
    WriteToConsole = writeToConsole,
    FilePath = filePath,
    MaxBytesPerMessage = maxBytesPerMessage,
    BytesPerLine = bytesPerLine
};


await using var sink = new DumpSink(opt, _logger);

_logger.Info($"Dumper iniciado con opciones: \nWriteToConsole={opt.WriteToConsole}, FilePath='{opt.FilePath}', MaxBytesPerMessage={opt.MaxBytesPerMessage}, BytesPerLine={opt.BytesPerLine}");

// recibe ReadOnlyMemory en DumpFrameAsync
var dumper = new FrameAwareDumper(sink, opt);

var remoteEndPoint = new IPEndPoint(ip, port);

var socket = new Socket(
                remoteEndPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);

var Tcp = await new TcpConnection(socket, _logger).TryConnectAsync(remoteEndPoint);




if(Tcp.IsConnected == false || Tcp.Connection == null)
{
    _logger.Error($"No se pudo conectar a {ip}:{port}");
    return;
}

await using var peer = new Peer(Tcp.Connection, new KiSoftFrameCodec());

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

_logger.Info($"Conectado a {ip}:{port}");

var statusTcs = new TaskCompletionSource<ReadOnlyMemory<byte>>(TaskCreationOptions.RunContinuationsAsynchronously);

var rxTask = peer.ReceiveLoopAsync(frame =>
{
    statusTcs.TrySetResult(frame);
    return Task.CompletedTask;
}, dumper, cts.Token);


// ---- TX: manda un registro de prueba (payload UTF-8)
// ejemplo del doc 
string record = "12N 09 03 000000001 001 T 01 A 03 00012"; 

var payload = System.Text.Encoding.UTF8.GetBytes(record);


_logger.Info($"Enviando payload de prueba: '{record}'");

// Dump TX (opcional)
await dumper.DumpFrameAsync("TX", Tcp.Connection.RemoteEndPoint!.ToString()!, 1, payload);

await peer.SendAsync(payload, cts.Token);

// esperar status <= 10s
using var statusTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    await using var reg = statusTimeout.Token.Register(() => statusTcs.TrySetCanceled());
    var status = await statusTcs.Task;
    _logger.Info($"Status recibido: {System.Text.Encoding.UTF8.GetString(status.Span)}");
}
catch
{
    _logger.Warning("No llegó status en 10s -> cerrar y reconectar");
    await peer.CloseAsync();
}

cts.Cancel();
await rxTask;

