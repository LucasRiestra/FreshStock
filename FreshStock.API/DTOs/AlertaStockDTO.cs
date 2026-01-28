using FreshStock.API.Enums;

namespace FreshStock.API.DTOs
{
    // GET - Respuesta de alerta
    public class AlertaStockResponseDTO
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public int RestauranteId { get; set; }
        public TipoAlerta TipoAlerta { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public decimal? StockActual { get; set; }
        public decimal? StockMinimo { get; set; }
        public DateTime? FechaCaducidad { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Leida { get; set; }
        public DateTime? FechaLectura { get; set; }
        public int? UsuarioLecturaId { get; set; }

        // Datos adicionales
        public string? NombreProducto { get; set; }
        public string? NombreRestaurante { get; set; }
        public string? UnidadMedida { get; set; }
    }

    // GET - Resumen de alertas
    public class ResumenAlertasDTO
    {
        public int TotalAlertas { get; set; }
        public int AlertasNoLeidas { get; set; }
        public int AlertasCriticas { get; set; }
        public int AlertasStockBajo { get; set; }
        public int AlertasProximoCaducar { get; set; }
        public int AlertasSinStock { get; set; }
    }

    // POST - Marcar alertas como leídas
    public class MarcarAlertasLeidasDTO
    {
        public List<int> AlertaIds { get; set; } = new();
    }

    // GET - Resultado de generación de alertas
    public class GeneracionAlertasResultDTO
    {
        public int AlertasGeneradas { get; set; }
        public int AlertasStockBajo { get; set; }
        public int AlertasStockCritico { get; set; }
        public int AlertasProximoCaducar { get; set; }
        public int AlertasSinStock { get; set; }
    }
}
