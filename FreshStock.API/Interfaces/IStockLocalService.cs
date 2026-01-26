using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IStockLocalService
    {
        Task<IEnumerable<StockLocalResponseDTO>> GetAllAsync();
        Task<StockLocalResponseDTO?> GetByIdAsync(int id);
        Task<IEnumerable<StockLocalResponseDTO>> GetByRestauranteIdAsync(int restauranteId);
        Task<IEnumerable<StockLocalResponseDTO>> GetByRestaurantesIdsAsync(IEnumerable<int> restauranteIds);
        Task<IEnumerable<StockLocalResponseDTO>> GetByProductoIdAsync(int productoId);
        Task<StockLocalResponseDTO?> GetByLoteAsync(int productoId, int restauranteId, string lote);
        Task<StockLocalResponseDTO> CreateAsync(CreateStockLocalDTO dto);
        Task<StockLocalResponseDTO?> UpdateAsync(UpdateStockLocalDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
