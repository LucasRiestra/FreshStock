using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // POST - Crear stock ideal individual
    public class CreateStockIdealRestauranteDTO
    {
        [Required]
        public int ProductoId { get; set; }

        [Required]
        public int RestauranteId { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal StockIdeal { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal StockMinimo { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal StockMaximo { get; set; }
    }

    // POST - Crear múltiples stock ideal a la vez
    public class CreateStockIdealBulkDTO
    {
        [Required]
        public int RestauranteId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe proporcionar al menos un item")]
        public List<StockIdealItemDTO> Items { get; set; }
    }

    public class StockIdealItemDTO
    {
        [Required]
        public int ProductoId { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal StockIdeal { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal StockMinimo { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal StockMaximo { get; set; }
    }

    // PUT - Actualizar stock ideal
    public class UpdateStockIdealRestauranteDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal StockIdeal { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal StockMinimo { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal StockMaximo { get; set; }

        [Required]
        public bool Activo { get; set; }
    }

    // GET - Respuesta
    public class StockIdealRestauranteResponseDTO
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public int RestauranteId { get; set; }
        public decimal StockIdeal { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal StockMaximo { get; set; }
        public bool Activo { get; set; }

        // Datos adicionales para facilitar uso en frontend
        public string? NombreProducto { get; set; }
        public string? NombreRestaurante { get; set; }
    }
}
