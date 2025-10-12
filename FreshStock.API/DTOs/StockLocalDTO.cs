using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // POST - Crear nueva entrada de stock (el costo se obtiene del Producto)
    public class CreateStockLocalDTO
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

        public DateTime? FechaCaducidad { get; set; }
    }

    // PUT - Actualizar stock (solo cantidad y fecha caducidad)
    public class UpdateStockLocalDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal Cantidad { get; set; }

        public DateTime? FechaCaducidad { get; set; }
    }

    // GET - Respuesta
    public class StockLocalResponseDTO
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public int RestauranteId { get; set; }
        public string Lote { get; set; }
        public decimal Cantidad { get; set; }
        public DateTime? FechaCaducidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public DateTime FechaEntrada { get; set; }
    }
}
