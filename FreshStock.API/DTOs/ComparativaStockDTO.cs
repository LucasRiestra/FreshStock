using FreshStock.API.Enums;

namespace FreshStock.API.DTOs
{
    // GET - Vista comparativa completa
    public class ComparativaStockDTO
    {
        public int RestauranteId { get; set; }
        public string NombreRestaurante { get; set; } = string.Empty;
        public int? UltimoInventarioId { get; set; }
        public DateTime? FechaUltimoInventario { get; set; }
        public List<ComparativaProductoDTO> Productos { get; set; } = new();
        public ResumenComparativaDTO Resumen { get; set; } = new();
    }

    // GET - Comparativa por producto
    public class ComparativaProductoDTO
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public int ProveedorId { get; set; }
        public string NombreProveedor { get; set; } = string.Empty;
        public int CategoriaId { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;

        // Stock configurado
        public decimal StockIdeal { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal StockMaximo { get; set; }

        // Stock real (del último inventario)
        public decimal StockReal { get; set; }

        // Indicadores
        public decimal DiferenciaIdeal { get; set; }  // StockIdeal - StockReal
        public decimal DiferenciaAbsoluta { get; set; }  // Valor absoluto
        public EstadoStock Estado { get; set; }
        public decimal PorcentajeOcupacion { get; set; }  // (StockReal / StockIdeal) * 100
    }

    // GET - Resumen de la comparativa
    public class ResumenComparativaDTO
    {
        public int TotalProductos { get; set; }
        public int ProductosCriticos { get; set; }
        public int ProductosBajos { get; set; }
        public int ProductosNormales { get; set; }
        public int ProductosExceso { get; set; }
        public int ProductosSinInventariar { get; set; }
        public decimal ValorTotalIdeal { get; set; }
        public decimal ValorTotalReal { get; set; }
        public decimal DiferenciaValor { get; set; }
    }

    // GET - Historial de comparativas
    public class HistorialComparativaDTO
    {
        public int InventarioId { get; set; }
        public string NombreInventario { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public int ProductosContados { get; set; }
        public int ProductosCriticos { get; set; }
        public int ProductosBajos { get; set; }
        public decimal PorcentajeCumplimiento { get; set; }
    }
}
