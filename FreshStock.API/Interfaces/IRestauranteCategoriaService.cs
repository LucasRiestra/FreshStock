using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IRestauranteCategoriaService
    {
        Task<IEnumerable<RestauranteCategoriaResponseDTO>> GetAllAsync();
        Task<RestauranteCategoriaResponseDTO?> GetByIdAsync(int id);
        Task<IEnumerable<RestauranteCategoriaResponseDTO>> GetByRestauranteIdAsync(int restauranteId);
        Task<IEnumerable<RestauranteCategoriaResponseDTO>> GetByCategoriaIdAsync(int categoriaId);
        Task<IEnumerable<CategoriaResponseDTO>> GetCategoriasByRestauranteIdAsync(int restauranteId);
        Task<IEnumerable<CategoriaResponseDTO>> GetCategoriasByUsuarioAsync(int usuarioId);
        Task<RestauranteCategoriaResponseDTO> CreateAsync(CreateRestauranteCategoriaDTO dto);
        Task<RestauranteCategoriaResponseDTO?> UpdateAsync(UpdateRestauranteCategoriaDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
