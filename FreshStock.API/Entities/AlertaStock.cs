using FreshStock.API.Enums;

namespace FreshStock.API.Entities
{
    public class AlertaStock : BaseEntity
    {
        public int ProductoId { get; set; }
        public int RestauranteId { get; set; }
        public TipoAlerta TipoAlerta { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public decimal? StockActual { get; set; }
        public decimal? StockMinimo { get; set; }
        public DateTime? FechaCaducidad { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public bool Leida { get; set; } = false;
        public DateTime? FechaLectura { get; set; }
        public int? UsuarioLecturaId { get; set; }
    }
}
