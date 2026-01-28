using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // POST - Registrar conteo de un producto
    public class CreateInventarioDetalleDTO
    {
        [Required]
        public int ProductoId { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal CantidadContada { get; set; }

        public string? Observacion { get; set; }
    }

    // POST - Registrar conteos en bulk
    public class CreateInventarioDetalleBulkDTO
    {
        [Required]
        [MinLength(1, ErrorMessage = "Debe proporcionar al menos un conteo")]
        public List<CreateInventarioDetalleDTO> Conteos { get; set; } = new();
    }

    // PUT - Actualizar conteo existente
    public class UpdateInventarioDetalleDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal CantidadContada { get; set; }

        public string? Observacion { get; set; }
    }

    // GET - Respuesta de detalle
    public class InventarioDetalleResponseDTO
    {
        public int Id { get; set; }
        public int InventarioId { get; set; }
        public int ProductoId { get; set; }
        public int ProveedorId { get; set; }
        public int CategoriaId { get; set; }
        public decimal CantidadContada { get; set; }
        public decimal? CantidadSistema { get; set; }
        public decimal? Diferencia { get; set; }
        public string? Observacion { get; set; }
        public DateTime FechaConteo { get; set; }

        // Datos adicionales
        public string? NombreProducto { get; set; }
        public string? NombreProveedor { get; set; }
        public string? NombreCategoria { get; set; }
        public string? UnidadMedida { get; set; }
    }
}
