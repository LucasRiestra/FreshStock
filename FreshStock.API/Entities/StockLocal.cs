using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.Entities
{
    public class StockLocal : BaseEntity
    {
        public int ProductoId { get; set; }
        public int RestauranteId { get; set; }
        public string Lote { get; set; }
        public decimal Cantidad { get; set; }
        public DateTime? FechaCaducidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public DateTime FechaEntrada { get; set; } = DateTime.UtcNow;
    }
}
