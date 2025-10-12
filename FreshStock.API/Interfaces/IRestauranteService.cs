using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IRestauranteService
    {
        Task<IEnumerable<RestauranteResponseDTO>> GetAllAsync();
        Task<RestauranteResponseDTO?> GetByIdAsync(int id);
        Task<RestauranteResponseDTO> CreateAsync(CreateRestauranteDTO dto);
        Task<RestauranteResponseDTO?> UpdateAsync(UpdateRestauranteDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
