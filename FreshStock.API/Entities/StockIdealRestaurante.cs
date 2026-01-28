namespace FreshStock.API.Entities
{
    public class StockIdealRestaurante : BaseEntity
    {
        public int ProductoId { get; set; }
        public int RestauranteId { get; set; }
        public decimal StockIdeal { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal StockMaximo { get; set; }
        public bool Activo { get; set; } = true;
    }
}
