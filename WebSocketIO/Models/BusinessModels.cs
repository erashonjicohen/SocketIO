using System;
using System.Collections.Generic;

namespace KiSoftOneService.Models
{
    /// <summary>
    /// Modelo para datos maestros de artículos (14N)
    /// </summary>
    public class ArticleMasterData
    {
        public string Station { get; set; }           // 001–004, 010, 011, 061, 065, 199
        public string Tenant { get; set; }            // 16 caracteres
        public string ArticleNumber { get; set; }     // 12 caracteres
        public string PackingSize { get; set; }       // 0001–9999
        public int EjectionNumber { get; set; }       // 10–80
        public int MaxQuantity { get; set; }          // 0001–9999
        public int Length { get; set; }               // Longitud [mm]
        public int Width { get; set; }                // Anchura [mm]
        public int Height { get; set; }               // Altura [mm]
        public int Weight { get; set; }               // En 1/10 gramos
        public List<string> ArticleCodes { get; set; } = new List<string>();
        public string ArticleName { get; set; }       // 40 caracteres
        public string GeoCode { get; set; }           // 12 caracteres
        public int MinStock { get; set; }             // Stock mínimo
        public int MaxStock { get; set; }             // Stock máximo
        public List<string> Characteristics { get; set; } = new List<string>();
        public List<int> Properties { get; set; } = new List<int>();
        public string ReplenishmentStation { get; set; }
        public string ReplenishmentGeoCode { get; set; }
    }

    /// <summary>
    /// Modelo para datos de pedido (12N) - Entregas de salida
    /// </summary>
    public class OrderData
    {
        public string Tenant { get; set; }
        public string OrderNumber { get; set; }      // 12 caracteres
        public string SheetNumber { get; set; }      // 0000
        public string OrderType { get; set; }        // 01–99
        public string LoadCarrierType { get; set; } // LARGE, CARTON, etc.
        public string CustomerNumber { get; set; }   // 12 caracteres
        public string TheoreticalRouteNumber { get; set; } // 08 caracteres
        public int OrderPriority { get; set; }       // 000–999
        public List<string> ControlParameters { get; set; } = new List<string>();
        public List<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
    }

    /// <summary>
    /// Línea de pedido
    /// </summary>
    public class OrderLine
    {
        public string LineReference { get; set; }    // Referencia de línea (opcional)
        public string Station { get; set; }          // Número de estación
        public string ArticleNumber { get; set; }    // 12 caracteres
        public string PackingSize { get; set; }      // 0001–9999
        public string StockType { get; set; }        // STANDARD, B6, QQ, etc.
        public string Batch { get; set; }            // Lote
        public string ExpirationDate { get; set; }   // YYYYMMDD
        public string ReservationCode { get; set; }  // Código de reservación
        public int Quantity { get; set; }            // 0001–9999
        public string ProcessingNote { get; set; }   // Nota de procesamiento (opcional)
    }

    /// <summary>
    /// Modelo para respuesta de pedido (32R)
    /// </summary>
    public class OrderResponse
    {
        public string RecordIdentifier { get; set; } = "32R";
        public string Tenant { get; set; }
        public string OrderNumber { get; set; }
        public string SheetNumber { get; set; }
        public string OrderType { get; set; }
        public string NumberOfSheets { get; set; }
        public string StartingPoint { get; set; }    // Estación donde arrancó
        public string LoadCarrierType { get; set; }
        public string LoadCarrierCode { get; set; }  // 08 caracteres
        public string CustomerNumber { get; set; }
        public string DispatchRampNumber { get; set; } // 05 caracteres
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string LastReadingStation { get; set; }
        public List<string> OrderStates { get; set; } = new List<string>();
        public List<ProcessedLine> ProcessedLines { get; set; } = new List<ProcessedLine>();
    }

    /// <summary>
    /// Línea procesada en respuesta de pedido
    /// </summary>
    public class ProcessedLine
    {
        public string LineReference { get; set; }
        public string ArticleNumber { get; set; }
        public string PackingSize { get; set; }
        public string StockType { get; set; }
        public string Batch { get; set; }
        public string ExpirationDate { get; set; }
        public int Quantity { get; set; }
        public string State { get; set; }             // 01–99
        public string WarehouseWorker { get; set; }
        public DateTime ProcessingTime { get; set; }
    }

    /// <summary>
    /// Modelo para datos de stock (1UN)
    /// </summary>
    public class StorageUnitData
    {
        public string StorageUnitCode { get; set; }  // 06 caracteres
        public string Station { get; set; }
        public string GeoCode { get; set; }
        public List<StockLine> StockLines { get; set; } = new List<StockLine>();
    }

    /// <summary>
    /// Línea de stock
    /// </summary>
    public class StockLine
    {
        public string SlotNumber { get; set; }
        public string Tenant { get; set; }
        public string ArticleNumber { get; set; }
        public string PackingSize { get; set; }
        public string StockType { get; set; }
        public string Batch { get; set; }
        public string ExpirationDate { get; set; }   // YYYYMMDD
        public int Quantity { get; set; }
        public string StockQuality { get; set; }     // 1 (nuevo), 2 (devolución)
        public string BlockingState { get; set; }    // 00 o 01
    }

    /// <summary>
    /// Modelo para solicitud de inventario (1IA)
    /// </summary>
    public class InventoryRequest
    {
        public string Tenant { get; set; }
        public string InventoryRequestNumber { get; set; } // 07 caracteres
        public List<InventoryFilter> Filters { get; set; } = new List<InventoryFilter>();
    }

    /// <summary>
    /// Filtro para solicitud de inventario
    /// </summary>
    public class InventoryFilter
    {
        public string Station { get; set; }
        public string GeoCode { get; set; }
        public string ArticleNumber { get; set; }
        public string PackingSize { get; set; }
        public string StockType { get; set; }
        public string Batch { get; set; }
        public string ExpirationDate { get; set; }
        public string ReservationCode { get; set; }
        public string StorageUnitCode { get; set; }
        public string SlotNumber { get; set; }
    }

    /// <summary>
    /// Ajuste de stock (3SC)
    /// </summary>
    public class StockAdjustment
    {
        public string CorrectionNumber { get; set; } // SC00000001–SC99999999
        public string Station { get; set; }
        public string MessageType { get; set; }      // 40, 41, 42, 43, 45
        public string StorageUnitCode { get; set; }
        public string SlotNumber { get; set; }
        public string Tenant { get; set; }
        public string ArticleNumber { get; set; }
        public string PackingSize { get; set; }
        public string StockType { get; set; }
        public string Batch { get; set; }
        public string ExpirationDate { get; set; }
        public string StockQuality { get; set; }
        public string BlockingReason { get; set; }
        public string Reason { get; set; }           // FOUND, LOST, HOST, SUBSYSTEM, etc.
        public DateTime AdjustmentTime { get; set; }
        public string WarehouseWorker { get; set; }
        public int Difference { get; set; }
        public string DifferenceSing { get; set; }   // + o -
    }
}
