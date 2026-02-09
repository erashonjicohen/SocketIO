using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using KiSoftOneService.Models;
using KiSoftOneService.Services;

namespace KiSoftOneService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IKiSoftOneService _kiSoftService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IKiSoftOneService kiSoftService, ILogger<OrderController> logger)
        {
            _kiSoftService = kiSoftService;
            _logger = logger;
        }

        /// <summary>
        /// Crea un nuevo pedido de salida
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderData order)
        {
            try
            {
                if (order == null || string.IsNullOrEmpty(order.OrderNumber))
                    return BadRequest("Datos de pedido inválidos");

                var result = await _kiSoftService.CreateOrderAsync(order);
                
                return Ok(new
                {
                    success = result.IsSuccess,
                    message = result.IsSuccess ? "Pedido creado exitosamente" : $"Error: {result.State}",
                    orderNumber = order.OrderNumber,
                    status = result.State
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creando pedido: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un pedido existente
        /// </summary>
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteOrder([FromQuery] string orderNumber, [FromQuery] string tenant = "DEFAULT")
        {
            try
            {
                if (string.IsNullOrEmpty(orderNumber))
                    return BadRequest("Número de pedido requerido");

                var result = await _kiSoftService.DeleteOrderAsync(tenant, orderNumber, "0000");
                
                return Ok(new
                {
                    success = result.IsSuccess,
                    message = result.IsSuccess ? "Pedido eliminado" : $"Error: {result.State}",
                    orderNumber = orderNumber,
                    status = result.State
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error eliminando pedido: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ArticleController : ControllerBase
    {
        private readonly IKiSoftOneService _kiSoftService;
        private readonly ILogger<ArticleController> _logger;

        public ArticleController(IKiSoftOneService kiSoftService, ILogger<ArticleController> logger)
        {
            _kiSoftService = kiSoftService;
            _logger = logger;
        }

        /// <summary>
        /// Crea datos maestros de artículo
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateArticle([FromBody] ArticleMasterData article)
        {
            try
            {
                if (article == null || string.IsNullOrEmpty(article.ArticleNumber))
                    return BadRequest("Datos de artículo inválidos");

                var result = await _kiSoftService.CreateArticleMasterDataAsync(article);
                
                return Ok(new
                {
                    success = result.IsSuccess,
                    message = result.IsSuccess ? "Artículo creado" : $"Error: {result.State}",
                    articleNumber = article.ArticleNumber,
                    status = result.State
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creando artículo: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class StorageController : ControllerBase
    {
        private readonly IKiSoftOneService _kiSoftService;
        private readonly ILogger<StorageController> _logger;

        public StorageController(IKiSoftOneService kiSoftService, ILogger<StorageController> logger)
        {
            _kiSoftService = kiSoftService;
            _logger = logger;
        }

        /// <summary>
        /// Crea una unidad de carga de almacenamiento
        /// </summary>
        [HttpPost("create-unit")]
        public async Task<IActionResult> CreateStorageUnit([FromBody] StorageUnitData storageUnit)
        {
            try
            {
                if (storageUnit == null || string.IsNullOrEmpty(storageUnit.StorageUnitCode))
                    return BadRequest("Datos de unidad de almacenamiento inválidos");

                var result = await _kiSoftService.CreateStorageUnitAsync(storageUnit);
                
                return Ok(new
                {
                    success = result.IsSuccess,
                    message = result.IsSuccess ? "Unidad de almacenamiento creada" : $"Error: {result.State}",
                    unitCode = storageUnit.StorageUnitCode,
                    status = result.State
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creando unidad: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IKiSoftOneService _kiSoftService;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(IKiSoftOneService kiSoftService, ILogger<InventoryController> logger)
        {
            _kiSoftService = kiSoftService;
            _logger = logger;
        }

        /// <summary>
        /// Crea solicitud de inventario
        /// </summary>
        [HttpPost("request")]
        public async Task<IActionResult> CreateInventoryRequest([FromBody] InventoryRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.InventoryRequestNumber))
                    return BadRequest("Datos de solicitud inválidos");

                var result = await _kiSoftService.CreateInventoryRequestAsync(request);
                
                return Ok(new
                {
                    success = result.IsSuccess,
                    message = result.IsSuccess ? "Solicitud de inventario creada" : $"Error: {result.State}",
                    requestNumber = request.InventoryRequestNumber,
                    status = result.State
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creando solicitud: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Bloquea stock
        /// </summary>
        [HttpPost("lock")]
        public async Task<IActionResult> LockStock([FromQuery] string tenant, [FromBody] List<InventoryFilter> filters)
        {
            try
            {
                if (filters == null || filters.Count == 0)
                    return BadRequest("Filtros requeridos");

                var result = await _kiSoftService.LockStockAsync(tenant ?? "DEFAULT", filters, "HOST");
                
                return Ok(new
                {
                    success = result.IsSuccess,
                    message = result.IsSuccess ? "Stock bloqueado" : $"Error: {result.State}",
                    status = result.State
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error bloqueando stock: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Desbloquea stock
        /// </summary>
        [HttpPost("unlock")]
        public async Task<IActionResult> UnlockStock([FromQuery] string tenant, [FromBody] List<InventoryFilter> filters)
        {
            try
            {
                if (filters == null || filters.Count == 0)
                    return BadRequest("Filtros requeridos");

                var result = await _kiSoftService.UnlockStockAsync(tenant ?? "DEFAULT", filters, "HOST");
                
                return Ok(new
                {
                    success = result.IsSuccess,
                    message = result.IsSuccess ? "Stock desbloqueado" : $"Error: {result.State}",
                    status = result.State
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error desbloqueando stock: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly IKiSoftOneService _kiSoftService;
        private readonly ILogger<SystemController> _logger;

        public SystemController(IKiSoftOneService kiSoftService, ILogger<SystemController> logger)
        {
            _kiSoftService = kiSoftService;
            _logger = logger;
        }

        /// <summary>
        /// Inicializa la conexión con KiSoft One
        /// </summary>
        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize([FromQuery] string ipAddress = "192.168.1.100")
        {
            try
            {
                bool success = await _kiSoftService.InitializeAsync(ipAddress);
                
                return Ok(new
                {
                    success = success,
                    message = success ? "Sistema inicializado" : "Falló la inicialización",
                    ipAddress = ipAddress
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error inicializando: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Apaga el servicio
        /// </summary>
        [HttpPost("shutdown")]
        public async Task<IActionResult> Shutdown()
        {
            try
            {
                await _kiSoftService.Shutdown();
                return Ok(new { message = "Sistema apagado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error apagando: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
