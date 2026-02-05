using SocketIO.Test.UDP;

namespace SocketIO.Tests.UDP
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Arranca el server en background
            var serverTask = Task.Run(() => UdpServer.Run(new[] { "-s", "-p", "9001" }));

            // Dale tiempo a bind/listen
            await Task.Delay(300);

            // Corre el cliente (OJO: aquí era UdpClientApp, no UdpServer)
            await UdpClientApp.Run(new[] { "-c", "-p", "9001" });

            // Espera un poquito para ver logs del server
            await Task.Delay(300);

            // Termina el proceso (smoke test)
            // En un test real, aquí ya validarías salida.
            Console.WriteLine("(OK) Test UDP completado.");
        }
    }
}
