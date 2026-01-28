using FreshStock.API.Enums;

namespace FreshStock.API.Entities
{
    public class Inventario : BaseEntity
    {
        public int RestauranteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
        public DateTime? FechaFin { get; set; }
        public EstadoInventario Estado { get; set; } = EstadoInventario.EnProgreso;
        public int UsuarioId { get; set; }
        public string? Notas { get; set; }
    }
}
