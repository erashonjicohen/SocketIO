using Sniffer.UDP;
using SocketIO.Net.Diagnostics;
using System.Threading;

namespace SocketIO.Examples;


class Program
{
    static async Task Main(string[] args)
    {
        const int port = 9001;

        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine("🕵️ Iniciando UDP RAW sniffer...");
        Console.WriteLine("Presiona Ctrl+C para salir\n");

        // Arranca sniffer en background
        var snifferTask = Task.Run(() =>
            UdpSnifferExample.RunAsync(port, cts.Token)
        );

        // Dale tiempo a bindear
        await Task.Delay(500);

        Console.WriteLine("📤 Enviando datagramas UDP...\n");
        await UdpSenderExample.RunAsync(port);

        await snifferTask;
    }
}




