using KiSoftOneService.Models;
using Microsoft.Extensions.Logging;

namespace KiSoftOneService.Services
{
    /// <summary>
    /// Servicio de negocio para operaciones con KiSoft One
    /// </summary>
    public interface IKiSoftOneService
    {
        Task<bool> InitializeAsync(string ipAddress);
        Task<StatusMessage> CreateOrderAsync(OrderData order);
        Task<StatusMessage> DeleteOrderAsync(string tenant, string orderNumber, string sheetNumber);
        Task<StatusMessage> CreateArticleMasterDataAsync(ArticleMasterData article);
        Task<StatusMessage> CreateStorageUnitAsync(StorageUnitData storageUnit);
        Task<StatusMessage> CreateInventoryRequestAsync(InventoryRequest inventoryRequest);
        Task<StatusMessage> LockStockAsync(string tenant, List<InventoryFilter> filters, string blockingReason);
        Task<StatusMessage> UnlockStockAsync(string tenant, List<InventoryFilter> filters, string blockingReason);
        Task Shutdown();
    }

    public class KiSoftOneService : IKiSoftOneService
    {
        private readonly ITcpCommunicationService _tcpService;
        private readonly ILogger<KiSoftOneService> _logger;
        private string _configuredTenant = "DEFAULT";

        public KiSoftOneService(ITcpCommunicationService tcpService, ILogger<KiSoftOneService> logger)
        {
            _tcpService = tcpService;
            _logger = logger;
        }

        public async Task<bool> InitializeAsync(string ipAddress)
        {
            try
            {
                bool connected = await _tcpService.ConnectAsync(ipAddress, 9801);
                if (!connected)
                {
                    _logger.LogError("No se pudo conectar al servidor KiSoft One");
                    return false;
                }

                _logger.LogInformation("Servicio KiSoft One inicializado");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error inicializando: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Crea un nuevo pedido de salida (12N)
        /// </summary>
        public async Task<StatusMessage> CreateOrderAsync(OrderData order)
        {
            try
            {
                var packet = new DataPacket
                {
                    RecordIdentifier = "12N"
                };

                // Encabezado
                packet.AddField("TENANT_LEN", "16", 2);
                packet.AddField("ORDER_NUM_LEN", "12", 2);
                packet.AddField("SHEET_NUM_LEN", "04", 2);
                packet.AddField("TENANT", order.Tenant ?? _configuredTenant, 16);
                packet.AddField("ORDER_NUMBER", order.OrderNumber, 12);
                packet.AddField("SHEET_NUMBER", order.SheetNumber ?? "0000", 4);

                // Tipo de pedido
                packet.AddField("TYPE_ID", "T", 1);
                packet.AddField("TYPE_LEN", "02", 2);
                packet.AddField("TYPE", order.OrderType, 2);

                // Medio de carga
                packet.AddField("LOAD_ID", "C", 1);
                packet.AddField("LOAD_LEN", "10", 2);
                packet.AddField("LOAD_TYPE", order.LoadCarrierType ?? "LARGE", 10);

                // Cliente
                packet.AddField("CUSTOMER_ID", "E", 1);
                packet.AddField("CUSTOMER_LEN", "12", 2);
                packet.AddField("CUSTOMER", order.CustomerNumber, 12);

                // Ruta teórica
                packet.AddField("ROUTE_ID", "F", 1);
                packet.AddField("ROUTE_LEN", "08", 2);
                packet.AddField("ROUTE", order.TheoreticalRouteNumber, 8);

                // Prioridad
                packet.AddField("PRIORITY_ID", "U", 1);
                packet.AddField("PRIORITY_LEN", "03", 2);
                packet.AddNumericField("PRIORITY", order.OrderPriority, 3);

                // Parámetros de control
                if (order.ControlParameters.Count > 0)
                {
                    packet.AddField("PARAMS_ID", "O", 1);
                    packet.AddField("PARAMS_COUNT", order.ControlParameters.Count.ToString("D2"), 2);
                    packet.AddField("PARAMS_LEN", "04", 2);
                    foreach (var param in order.ControlParameters)
                    {
                        packet.AddField($"PARAM_{param}", param, 4);
                    }
                }

                // Líneas de pedido
                packet.AddField("LINES_ID", "Z", 1);
                packet.AddField("LINES_COUNT", order.OrderLines.Count.ToString("D3"), 3);
                packet.AddField("LINES_REF_LEN", "00", 2);
                packet.AddField("LINES_STATION_LEN", "00", 2);
                packet.AddField("LINES_ARTICLE_LEN", "12", 2);
                packet.AddField("LINES_PACKING_LEN", "00", 2);
                packet.AddField("LINES_STOCK_LEN", "08", 2);
                packet.AddField("LINES_BATCH_LEN", "00", 2);
                packet.AddField("LINES_EXPIRATION_LEN", "00", 2);
                packet.AddField("LINES_RESERVATION_LEN", "00", 2);
                packet.AddField("LINES_QTY_LEN", "04", 2);
                packet.AddField("LINES_NOTE_LEN", "00", 2);

                foreach (var line in order.OrderLines)
                {
                    packet.AddField($"ARTICLE_{line.ArticleNumber}", line.ArticleNumber, 12);
                    packet.AddField($"STOCK_TYPE_{line.ArticleNumber}", line.StockType, 8);
                    packet.AddField($"QTY_{line.ArticleNumber}", line.Quantity.ToString("D4"), 4);
                }

                var statusMessage = await _tcpService.SendDataPacketAsync(packet);
                
                if (statusMessage.IsSuccess)
                {
                    _logger.LogInformation($"Pedido creado: {order.OrderNumber}");
                }
                else
                {
                    _logger.LogWarning($"Error creando pedido {order.OrderNumber}: Código {statusMessage.State}");
                }

                return statusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en CreateOrderAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Elimina un pedido (12D)
        /// </summary>
        public async Task<StatusMessage> DeleteOrderAsync(string tenant, string orderNumber, string sheetNumber)
        {
            try
            {
                var packet = new DataPacket
                {
                    RecordIdentifier = "12D"
                };

                packet.AddField("TENANT_LEN", "16", 2);
                packet.AddField("ORDER_NUM_LEN", "12", 2);
                packet.AddField("SHEET_NUM_LEN", "04", 2);
                packet.AddField("TENANT", tenant ?? _configuredTenant, 16);
                packet.AddField("ORDER_NUMBER", orderNumber, 12);
                packet.AddField("SHEET_NUMBER", sheetNumber, 4);

                var statusMessage = await _tcpService.SendDataPacketAsync(packet);
                
                if (statusMessage.IsSuccess)
                {
                    _logger.LogInformation($"Pedido eliminado: {orderNumber}");
                }

                return statusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en DeleteOrderAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Crea datos maestros de artículo (14N)
        /// </summary>
        public async Task<StatusMessage> CreateArticleMasterDataAsync(ArticleMasterData article)
        {
            try
            {
                var packet = new DataPacket
                {
                    RecordIdentifier = "14N"
                };

                // Longitudes de campos de encabezado
                packet.AddField("STATION_LEN", "03", 2);
                packet.AddField("SHELF_SYSTEM_LEN", "03", 2);
                packet.AddField("SHELF_LEN", "03", 2);
                packet.AddField("SHELF_MODULE_LEN", "03", 2);
                packet.AddField("SHELF_CHANNEL_LEN", "03", 2);
                packet.AddField("LEVEL_LEN", "03", 2);

                packet.AddField("STATION", article.Station, 3);
                packet.AddField("SHELF_SYSTEM", "001", 3);
                packet.AddField("SHELF", "001", 3);
                packet.AddField("SHELF_MODULE", "001", 3);
                packet.AddField("SHELF_CHANNEL", "001", 3);
                packet.AddField("LEVEL", "001", 3);

                // Identificador y datos de artículo
                packet.AddField("ARTICLE_ID", "L", 1);
                packet.AddField("TENANT_LEN", "16", 2);
                packet.AddField("ARTICLE_NUM_LEN", "12", 2);
                packet.AddField("PACKING_SIZE_LEN", "04", 2);
                packet.AddField("RESERVED_LEN", "00", 2);
                
                packet.AddField("TENANT", article.Tenant ?? _configuredTenant, 16);
                packet.AddField("ARTICLE_NUMBER", article.ArticleNumber, 12);
                packet.AddField("PACKING_SIZE", article.PackingSize, 4);

                // Número de eyección
                packet.AddField("EJECTION_ID", "Y", 1);
                packet.AddField("EJECTION_LEN", "02", 2);
                packet.AddNumericField("EJECTION", article.EjectionNumber, 2);

                // Cantidad máxima
                packet.AddField("MAX_QTY_ID", "M", 1);
                packet.AddField("MAX_QTY_LEN", "04", 2);
                packet.AddNumericField("MAX_QUANTITY", article.MaxQuantity, 4);

                // Dimensiones
                packet.AddField("DIMENSIONS_ID", "D", 1);
                packet.AddField("LENGTH_LEN", "04", 2);
                packet.AddField("WIDTH_LEN", "04", 2);
                packet.AddField("HEIGHT_LEN", "04", 2);
                packet.AddField("BAG_WIDTH_LEN", "00", 2);
                packet.AddNumericField("LENGTH", article.Length, 4);
                packet.AddNumericField("WIDTH", article.Width, 4);
                packet.AddNumericField("HEIGHT", article.Height, 4);

                // Peso
                packet.AddField("WEIGHT_ID", "G", 1);
                packet.AddField("WEIGHT_LEN", "06", 2);
                packet.AddNumericField("WEIGHT", article.Weight, 6);

                // Códigos de artículo
                packet.AddField("BARCODES_ID", "B", 1);
                packet.AddField("BARCODES_COUNT", article.ArticleCodes.Count.ToString("D2"), 2);
                packet.AddField("BARCODE_LEN", "20", 2);
                foreach (var code in article.ArticleCodes)
                {
                    packet.AddField($"BARCODE_{code}", code, 20);
                }

                // Nombre de artículo
                packet.AddField("NAME_ID", "K", 1);
                packet.AddField("NAME_LEN", "40", 2);
                packet.AddField("GEOCODE_LEN", "12", 2);
                packet.AddField("ARTICLE_NAME", article.ArticleName, 40);
                packet.AddField("GEOCODE", article.GeoCode, 12);

                // Límites de stock
                packet.AddField("STOCK_LIMITS_ID", "S", 1);
                packet.AddField("MIN_STOCK_LEN", "04", 2);
                packet.AddField("MAX_STOCK_LEN", "04", 2);
                packet.AddNumericField("MIN_STOCK", article.MinStock, 4);
                packet.AddNumericField("MAX_STOCK", article.MaxStock, 4);

                var statusMessage = await _tcpService.SendDataPacketAsync(packet);
                
                if (statusMessage.IsSuccess)
                {
                    _logger.LogInformation($"Artículo maestro creado: {article.ArticleNumber}");
                }

                return statusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en CreateArticleMasterDataAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Crea una unidad de carga de almacenamiento (1UN)
        /// </summary>
        public async Task<StatusMessage> CreateStorageUnitAsync(StorageUnitData storageUnit)
        {
            try
            {
                var packet = new DataPacket
                {
                    RecordIdentifier = "1UN"
                };

                packet.AddField("UNIT_CODE_LEN", "06", 2);
                packet.AddField("STATION_LEN", "03", 2);
                packet.AddField("GEOCODE_LEN", "12", 2);
                packet.AddField("UNIT_CODE", storageUnit.StorageUnitCode, 6);
                packet.AddField("STATION", storageUnit.Station, 3);
                packet.AddField("GEOCODE", storageUnit.GeoCode, 12);

                // Líneas de stock
                packet.AddField("STOCK_LINES_ID", "X", 1);
                packet.AddField("STOCK_LINES_COUNT", storageUnit.StockLines.Count.ToString("D2"), 2);

                foreach (var line in storageUnit.StockLines)
                {
                    packet.AddField("SLOT_ID", "N", 1);
                    packet.AddField("SLOT_LEN", "00", 2);

                    packet.AddField("ARTICLE_ID", "L", 1);
                    packet.AddField("TENANT_LEN", "16", 2);
                    packet.AddField("ARTICLE_NUM_LEN", "12", 2);
                    packet.AddField("PACKING_SIZE_LEN", "04", 2);
                    packet.AddField("STOCK_TYPE_LEN", "08", 2);
                    packet.AddField("TENANT", _configuredTenant, 16);
                    packet.AddField("ARTICLE_NUMBER", line.ArticleNumber, 12);
                    packet.AddField("PACKING_SIZE", line.PackingSize, 4);
                    packet.AddField("STOCK_TYPE", line.StockType, 8);

                    packet.AddField("BATCH_ID", "C", 1);
                    packet.AddField("BATCH_LEN", "20", 2);
                    packet.AddField("BATCH", line.Batch, 20);

                    packet.AddField("EXPIRATION_ID", "E:", 1);
                    packet.AddField("EXPIRATION_LEN", "08", 2);
                    packet.AddField("EXPIRATION_DATE", line.ExpirationDate, 8);

                    packet.AddField("QUANTITY_ID", "S", 1);
                    packet.AddField("QUANTITY_LEN", "04", 2);
                    packet.AddNumericField("QUANTITY", line.Quantity, 4);

                    packet.AddField("QUALITY_ID", "F", 1);
                    packet.AddField("QUALITY_LEN", "01", 2);
                    packet.AddField("QUALITY", line.StockQuality, 1);

                    packet.AddField("BLOCKING_ID", "A", 1);
                    packet.AddField("BLOCKING_LEN", "00", 2);
                    packet.AddField("BLOCKING_STATE", line.BlockingState, 2);

                    packet.AddField("SEPARATOR", "*", 1);
                }

                var statusMessage = await _tcpService.SendDataPacketAsync(packet);
                
                if (statusMessage.IsSuccess)
                {
                    _logger.LogInformation($"Unidad de almacenamiento creada: {storageUnit.StorageUnitCode}");
                }

                return statusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en CreateStorageUnitAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Crea solicitud de inventario (1IA)
        /// </summary>
        public async Task<StatusMessage> CreateInventoryRequestAsync(InventoryRequest inventoryRequest)
        {
            try
            {
                var packet = new DataPacket
                {
                    RecordIdentifier = "1IA"
                };

                packet.AddField("TENANT_LEN", "16", 2);
                packet.AddField("REQUEST_NUM_LEN", "07", 2);
                packet.AddField("RESERVED_LEN", "00", 2);
                packet.AddField("TENANT", inventoryRequest.Tenant ?? _configuredTenant, 16);
                packet.AddField("REQUEST_NUMBER", inventoryRequest.InventoryRequestNumber, 7);

                // Filtros
                packet.AddField("FILTERS_ID", "Y", 1);
                packet.AddField("FILTERS_COUNT", inventoryRequest.Filters.Count.ToString("D3"), 3);
                packet.AddField("STATION_LEN", "00", 2);
                packet.AddField("GEOCODE_LEN", "00", 2);
                packet.AddField("ARTICLE_LEN", "00", 2);
                packet.AddField("PACKING_SIZE_LEN", "00", 2);
                packet.AddField("STOCK_TYPE_LEN", "00", 2);
                packet.AddField("BATCH_LEN", "00", 2);
                packet.AddField("EXPIRATION_LEN", "00", 2);
                packet.AddField("RESERVATION_LEN", "00", 2);
                packet.AddField("UNIT_CODE_LEN", "00", 2);
                packet.AddField("SLOT_LEN", "00", 2);

                var statusMessage = await _tcpService.SendDataPacketAsync(packet);
                
                if (statusMessage.IsSuccess)
                {
                    _logger.LogInformation($"Solicitud de inventario creada: {inventoryRequest.InventoryRequestNumber}");
                }

                return statusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en CreateInventoryRequestAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Bloquea stock (1SL, tipo 41)
        /// </summary>
        public async Task<StatusMessage> LockStockAsync(string tenant, List<InventoryFilter> filters, string blockingReason)
        {
            return await SendStockLockCommandAsync(tenant, filters, "41", blockingReason);
        }

        /// <summary>
        /// Desbloquea stock (1SL, tipo 42)
        /// </summary>
        public async Task<StatusMessage> UnlockStockAsync(string tenant, List<InventoryFilter> filters, string blockingReason)
        {
            return await SendStockLockCommandAsync(tenant, filters, "42", blockingReason);
        }

        private async Task<StatusMessage> SendStockLockCommandAsync(string tenant, List<InventoryFilter> filters, string lockType, string blockingReason)
        {
            try
            {
                var packet = new DataPacket
                {
                    RecordIdentifier = "1SL"
                };

                packet.AddField("TENANT_LEN", "16", 2);
                packet.AddField("TENANT", tenant ?? _configuredTenant, 16);

                packet.AddField("TYPE_ID", "T", 1);
                packet.AddField("TYPE_LEN", "02", 2);
                packet.AddField("TYPE", lockType, 2);

                packet.AddField("REASON_ID", "v", 1);
                packet.AddField("REASON_LEN", "20", 2);
                packet.AddField("REASON", blockingReason ?? "HOST", 20);

                packet.AddField("FILTERS_ID", "Y", 1);
                packet.AddField("FILTERS_COUNT", filters.Count.ToString("D3"), 3);
                packet.AddField("STATION_LEN", "00", 2);
                packet.AddField("GEOCODE_LEN", "00", 2);
                packet.AddField("ARTICLE_LEN", "00", 2);
                packet.AddField("PACKING_SIZE_LEN", "00", 2);
                packet.AddField("STOCK_TYPE_LEN", "00", 2);
                packet.AddField("BATCH_LEN", "00", 2);
                packet.AddField("EXPIRATION_LEN", "00", 2);
                packet.AddField("RESERVATION_LEN", "00", 2);
                packet.AddField("UNIT_CODE_LEN", "00", 2);
                packet.AddField("SLOT_LEN", "00", 2);

                var statusMessage = await _tcpService.SendDataPacketAsync(packet);
                
                string action = lockType == "41" ? "bloqueado" : "desbloqueado";
                if (statusMessage.IsSuccess)
                {
                    _logger.LogInformation($"Stock {action} correctamente");
                }

                return statusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en SendStockLockCommandAsync: {ex.Message}");
                throw;
            }
        }

        public async Task Shutdown()
        {
            await _tcpService.DisconnectAsync();
            _logger.LogInformation("Servicio KiSoft One apagado");
        }
    }
}
