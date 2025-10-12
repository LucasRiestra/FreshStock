namespace FreshStock.API.Entities
{
    public class MovimientoInventario : BaseEntity
    {
        public string Tipo { get; set; } // "Entrada" o "Salida"
        public int ProductoId { get; set; }
        public int RestauranteId { get; set; }
        public decimal Cantidad { get; set; }
        public string Lote { get; set; }
        public string Motivo { get; set; } // "Compra", "Venta", "Ajuste", etc.
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public int? RestauranteDestinoId { get; set; } // Para transferencias entre restaurantes
        public decimal? CostoUnitario { get; set; }
        public int UsuarioId { get; set; }
    }
}
