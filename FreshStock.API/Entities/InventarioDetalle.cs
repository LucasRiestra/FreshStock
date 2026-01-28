namespace FreshStock.API.Entities
{
    public class InventarioDetalle : BaseEntity
    {
        public int InventarioId { get; set; }
        public int ProductoId { get; set; }
        public int ProveedorId { get; set; }
        public int CategoriaId { get; set; }
        public decimal CantidadContada { get; set; }
        public decimal? CantidadSistema { get; set; }
        public decimal? Diferencia { get; set; }
        public string? Observacion { get; set; }
        public DateTime FechaConteo { get; set; } = DateTime.UtcNow;
    }
}
