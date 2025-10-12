using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // POST - Crear nuevo movimiento de inventario (el costo se obtiene del Producto)
    public class CreateMovimientoInventarioDTO
    {
        [Required]
        [MaxLength(20)]
        public string Tipo { get; set; } // "Entrada" o "Salida"

        [Required]
        public int ProductoId { get; set; }

        [Required]
        public int RestauranteId { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal Cantidad { get; set; }

        [Required]
        [MaxLength(50)]
        public string Lote { get; set; }

        [Required]
        [MaxLength(100)]
        public string Motivo { get; set; } // "Compra", "Venta", "Ajuste", "Transferencia", etc.

        [Required]
        public int UsuarioId { get; set; }

        public int? RestauranteDestinoId { get; set; } // Para transferencias entre restaurantes
    }

    // POST - Registrar merma (simplificado, el costo se obtiene del Producto)
    public class CreateMermaDTO
    {
        [Required]
        public int ProductoId { get; set; }

        [Required]
        public int RestauranteId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Lote { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal Cantidad { get; set; }

        [Required]
        [MaxLength(50)]
        public string TipoMerma { get; set; } // "Caducidad", "Daño", "Robo", "Error"

        [Required]
        public int UsuarioId { get; set; }
    }

    // GET - Respuesta
    public class MovimientoInventarioResponseDTO
    {
        public int Id { get; set; }
        public string Tipo { get; set; }
        public int ProductoId { get; set; }
        public int RestauranteId { get; set; }
        public decimal Cantidad { get; set; }
        public string Lote { get; set; }
        public string Motivo { get; set; }
        public decimal? CostoUnitario { get; set; }
        public int UsuarioId { get; set; }
        public DateTime Fecha { get; set; }
        public int? RestauranteDestinoId { get; set; }
    }
}
