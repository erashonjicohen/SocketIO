using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KiSoftOneService.Models;
using Microsoft.Extensions.Logging;

namespace KiSoftOneService.Services
{
    /// <summary>
    /// Servicio de comunicación TCP/IP con KiSoft One
    /// </summary>
    public interface ITcpCommunicationService
    {
        Task<bool> ConnectAsync(string ipAddress, int port);
        Task DisconnectAsync();
        Task<StatusMessage> SendDataPacketAsync(DataPacket packet);
        Task<DataPacket> ReceiveDataPacketAsync();
        Task SendHeartbeatAsync();
        bool IsConnected { get; }
    }

    public class TcpCommunicationService : ITcpCommunicationService, IDisposable
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private readonly ILogger<TcpCommunicationService> _logger;
        private CancellationTokenSource _heartbeatCancellation;

        // Configuración de puertos según especificación
        private const int HOST_TO_KISOFT_PORT = 9801;
        private const int KISOFT_TO_HOST_PORT = 9802;
        private const int HEARTBEAT_INTERVAL = 60000; // 60 segundos
        private const int TIMEOUT_RESPONSE = 10000;   // 10 segundos
        private const int TIMEOUT_HEARTBEAT = 120000; // 120 segundos

        public bool IsConnected => _tcpClient?.Connected ?? false;

        public TcpCommunicationService(ILogger<TcpCommunicationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Conecta con el servidor KiSoft One
        /// </summary>
        public async Task<bool> ConnectAsync(string ipAddress, int port)
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ipAddress, port);
                _networkStream = _tcpClient.GetStream();
                _networkStream.ReadTimeout = TIMEOUT_RESPONSE;
                _networkStream.WriteTimeout = TIMEOUT_RESPONSE;

                _logger.LogInformation($"Conectado a {ipAddress}:{port}");
                
                // Iniciar heartbeat
                StartHeartbeat();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al conectar: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Desconecta del servidor
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                _heartbeatCancellation?.Cancel();
                
                if (_networkStream != null)
                {
                    await _networkStream.FlushAsync();
                    _networkStream.Dispose();
                }

                _tcpClient?.Close();
                _tcpClient?.Dispose();

                _logger.LogInformation("Desconectado del servidor");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al desconectar: {ex.Message}");
            }
        }

        /// <summary>
        /// Envía un paquete de datos al KiSoft One
        /// </summary>
        public async Task<StatusMessage> SendDataPacketAsync(DataPacket packet)
        {
            if (!IsConnected)
                throw new InvalidOperationException("No conectado al servidor");

            try
            {
                byte[] data = packet.Serialize();
                await _networkStream.WriteAsync(data, 0, data.Length);
                await _networkStream.FlushAsync();

                _logger.LogInformation($"Paquete enviado - Identificador: {packet.RecordIdentifier}");

                // Esperar mensaje de estado
                var statusMessage = await ReceiveStatusMessageAsync();
                return statusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error enviando paquete: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Recibe un paquete de datos del KiSoft One
        /// </summary>
        public async Task<DataPacket> ReceiveDataPacketAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("No conectado al servidor");

            try
            {
                byte[] buffer = new byte[65536];
                int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                    throw new IOException("Conexión cerrada por el servidor");

                byte[] data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);

                var packet = DataPacket.Deserialize(data);
                _logger.LogInformation($"Paquete recibido - Identificador: {packet.RecordIdentifier}");

                return packet;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error recibiendo paquete: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Recibe un mensaje de estado
        /// </summary>
        private async Task<StatusMessage> ReceiveStatusMessageAsync()
        {
            try
            {
                var packet = await ReceiveDataPacketAsync();
                
                var statusMessage = new StatusMessage
                {
                    RecordIdentifier = packet.RecordIdentifier,
                    State = packet.Fields.ContainsKey("State") 
                        ? packet.Fields["State"].ToString() 
                        : "99"
                };

                return statusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error recibiendo mensaje de estado: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Envía heartbeat periódicamente
        /// </summary>
        public async Task SendHeartbeatAsync()
        {
            try
            {
                var heartbeatPacket = new DataPacket
                {
                    RecordIdentifier = "1HR" // Heartbeat Host -> KiSoft One
                };

                await SendDataPacketAsync(heartbeatPacket);
                _logger.LogDebug("Heartbeat enviado");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error en heartbeat: {ex.Message}");
            }
        }

        /// <summary>
        /// Inicia el envío periódico de heartbeat
        /// </summary>
        private void StartHeartbeat()
        {
            _heartbeatCancellation = new CancellationTokenSource();
            
            _ = Task.Run(async () =>
            {
                while (!_heartbeatCancellation.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(HEARTBEAT_INTERVAL, _heartbeatCancellation.Token);
                        if (IsConnected)
                        {
                            await SendHeartbeatAsync();
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error en hilo de heartbeat: {ex.Message}");
                    }
                }
            }, _heartbeatCancellation.Token);
        }

        public void Dispose()
        {
            _heartbeatCancellation?.Cancel();
            _heartbeatCancellation?.Dispose();
            _networkStream?.Dispose();
            _tcpClient?.Dispose();
        }
    }
}
