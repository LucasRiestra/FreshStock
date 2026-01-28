using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IComparativaStockService
    {
        Task<ComparativaStockDTO> GetComparativaByRestauranteAsync(int restauranteId);
        Task<ComparativaStockDTO> GetComparativaByCategoriaAsync(int restauranteId, int categoriaId);
        Task<IEnumerable<ComparativaProductoDTO>> GetProductosCriticosAsync(int restauranteId);
        Task<IEnumerable<ComparativaProductoDTO>> GetProductosBajosAsync(int restauranteId);
        Task<IEnumerable<HistorialComparativaDTO>> GetHistorialAsync(int restauranteId, DateTime? desde = null);
    }
}
