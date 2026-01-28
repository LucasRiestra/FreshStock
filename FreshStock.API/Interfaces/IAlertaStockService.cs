using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IAlertaStockService
    {
        Task<IEnumerable<AlertaStockResponseDTO>> GetByRestauranteIdAsync(int restauranteId);
        Task<IEnumerable<AlertaStockResponseDTO>> GetNoLeidasByRestauranteIdAsync(int restauranteId);
        Task<ResumenAlertasDTO> GetResumenByRestauranteIdAsync(int restauranteId);
        Task<AlertaStockResponseDTO?> MarcarLeidaAsync(int alertaId, int usuarioId);
        Task<int> MarcarVariasLeidasAsync(MarcarAlertasLeidasDTO dto, int usuarioId);
        Task<GeneracionAlertasResultDTO> GenerarAlertasAsync(int restauranteId);
    }
}
