using System.ComponentModel.DataAnnotations;
using FreshStock.API.Enums;

namespace FreshStock.API.DTOs
{
    // POST - Crear nuevo inventario
    public class CreateInventarioDTO
    {
        [Required]
        public int RestauranteId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public string? Notas { get; set; }
    }

    // GET - Respuesta de inventario
    public class InventarioResponseDTO
    {
        public int Id { get; set; }
        public int RestauranteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public EstadoInventario Estado { get; set; }
        public int UsuarioId { get; set; }
        public string? Notas { get; set; }

        // Datos adicionales
        public string? NombreRestaurante { get; set; }
        public string? NombreUsuario { get; set; }

        // Progreso del conteo
        public int TotalProductos { get; set; }
        public int ProductosContados { get; set; }
        public decimal PorcentajeProgreso { get; set; }
    }

    // GET - Resumen de inventario para listados
    public class InventarioResumenDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public EstadoInventario Estado { get; set; }
        public int ProductosContados { get; set; }
    }

    // POST - Finalizar inventario
    public class FinalizarInventarioDTO
    {
        public string? Notas { get; set; }
        public bool ActualizarStock { get; set; } = false;
    }

    // GET - Categoría para navegación durante conteo
    public class CategoriaConteoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int TotalProductos { get; set; }
        public int ProductosContados { get; set; }
        public bool Completada { get; set; }
    }

    // GET - Proveedor para navegación durante conteo
    public class ProveedorConteoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int TotalProductos { get; set; }
        public int ProductosContados { get; set; }
        public bool Completado { get; set; }
    }

    // GET - Producto para conteo
    public class ProductoConteoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public int ProveedorId { get; set; }
        public int CategoriaId { get; set; }
        public decimal? CantidadSistema { get; set; }  // Stock actual del sistema
        public decimal? StockIdeal { get; set; }      // Referencia ideal
        public decimal? StockMinimo { get; set; }     // Referencia mínima
        public decimal? CantidadContada { get; set; }  // Si ya fue contado
        public bool YaContado { get; set; }
        public string? Observacion { get; set; }
    }

    // GET - Progreso del inventario
    public class ProgresoInventarioDTO
    {
        public int InventarioId { get; set; }
        public int TotalProductos { get; set; }
        public int ProductosContados { get; set; }
        public decimal PorcentajeProgreso { get; set; }
        public List<CategoriasProgresoDTO> CategoriasPendientes { get; set; } = new();
    }

    public class CategoriasProgresoDTO
    {
        public int CategoriaId { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;
        public int Pendientes { get; set; }
    }
}
