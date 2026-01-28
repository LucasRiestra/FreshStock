using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IStockIdealRestauranteService
    {
        Task<IEnumerable<StockIdealRestauranteResponseDTO>> GetAllAsync();
        Task<StockIdealRestauranteResponseDTO?> GetByIdAsync(int id);
        Task<IEnumerable<StockIdealRestauranteResponseDTO>> GetByRestauranteIdAsync(int restauranteId);
        Task<StockIdealRestauranteResponseDTO?> GetByProductoRestauranteAsync(int productoId, int restauranteId);
        Task<StockIdealRestauranteResponseDTO> CreateAsync(CreateStockIdealRestauranteDTO dto);
        Task<IEnumerable<StockIdealRestauranteResponseDTO>> CreateBulkAsync(CreateStockIdealBulkDTO dto);
        Task<StockIdealRestauranteResponseDTO?> UpdateAsync(UpdateStockIdealRestauranteDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
