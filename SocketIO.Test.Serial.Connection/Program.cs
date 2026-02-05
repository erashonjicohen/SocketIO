using SocketIO.Net.Abstractions;
using SocketIO.Net.Transport.Serial;
using System.IO.Ports;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        // Defaults
        string port = "COM5";
        int baud = 115200;
        int dataBits = 8;
        Parity parity = Parity.None;
        StopBits stopBits = StopBits.One;
        Handshake handshake = Handshake.None;

        string txTerminator = "\r\n"; // default CRLF

        // ---------------- ARG PARSER ----------------
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            string? Next() => (i + 1 < args.Length) ? args[++i] : null;

            switch (a.ToLowerInvariant())
            {
                case "--port":
                    port = Next() ?? port;
                    break;

                case "--baud":
                    if (int.TryParse(Next(), out var b)) baud = b;
                    break;

                case "--databits":
                    if (int.TryParse(Next(), out var db)) dataBits = db;
                    break;

                case "--parity":
                    parity = ParseParity(Next());
                    break;

                case "--stopbits":
                    stopBits = ParseStopBits(Next());
                    break;

                case "--handshake":
                    handshake = ParseHandshake(Next());
                    break;

                case "--crlf":
                    txTerminator = "\r\n";
                    break;

                case "--lf":
                    txTerminator = "\n";
                    break;

                case "--raw":
                    txTerminator = "";
                    break;

                case "--help":
                case "-h":
                    PrintHelp();
                    return;
            }
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        await using IConnection conn = new SerialConnection(
            port, baud, dataBits, parity, stopBits, handshake
        );

        Console.WriteLine($"✅ Abierto {port}");
        Console.WriteLine($"   {baud},{dataBits},{parity},{stopBits} handshake={handshake}");
        Console.WriteLine($"   TX terminator: {(txTerminator == "" ? "(raw)" : txTerminator.Replace("\r", "\\r").Replace("\n", "\\n"))}");
        Console.WriteLine("Escribí y Enter para TX. Ctrl+C para salir.\n");

        // ---------------- RX TASK ----------------
        var rxTask = Task.Run(async () =>
        {
            var buf = new byte[4096];

            while (!cts.IsCancellationRequested)
            {
                int n;
                try
                {
                    n = await conn.ReceiveAsync(buf, cts.Token);
                }
                catch (OperationCanceledException) { break; }

                if (n <= 0) continue;

                var hex = BitConverter.ToString(buf, 0, n);
                var ascii = Encoding.ASCII.GetString(buf, 0, n)
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n");

                Console.WriteLine($"📥 RX {n} bytes: {hex}   |{ascii}|");
            }
        });

        // ---------------- TX LOOP ----------------
        while (!cts.IsCancellationRequested)
        {
            var line = Console.ReadLine();
            if (line is null) break;

            var bytes = Encoding.UTF8.GetBytes(line + txTerminator);
            await conn.SendAsync(bytes, cts.Token);

            Console.WriteLine($"📤 TX {bytes.Length} bytes");
        }

        cts.Cancel();
        await rxTask;
        await conn.CloseAsync();
    }

    // ---------------- HELPERS ----------------

    static Parity ParseParity(string? v) => v?.ToLowerInvariant() switch
    {
        "even" => Parity.Even,
        "odd" => Parity.Odd,
        "mark" => Parity.Mark,
        "space" => Parity.Space,
        _ => Parity.None
    };

    static StopBits ParseStopBits(string? v) => v switch
    {
        "2" => StopBits.Two,
        "1.5" => StopBits.OnePointFive,
        _ => StopBits.One
    };

    static Handshake ParseHandshake(string? v) => v?.ToLowerInvariant() switch
    {
        "xonxoff" => Handshake.XOnXOff,
        "rtscts" => Handshake.RequestToSend,
        _ => Handshake.None
    };

    static void PrintHelp()
    {
        Console.WriteLine("""
Serial Terminal (SocketIO)

Uso:
  dotnet run -- --port COM5 --baud 115200 --databits 8 --parity none --stopbits 1 --handshake none

Opciones:
  --port COMx
  --baud N
  --databits 7|8
  --parity none|even|odd|mark|space
  --stopbits 1|1.5|2
  --handshake none|xonxoff|rtscts

TX:
  --crlf   enviar \\r\\n (default)
  --lf     enviar \\n
  --raw    sin terminador
""");
    }



}
