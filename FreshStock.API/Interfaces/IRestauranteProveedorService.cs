using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IRestauranteProveedorService
    {
        Task<IEnumerable<RestauranteProveedorResponseDTO>> GetAllAsync();
        Task<RestauranteProveedorResponseDTO?> GetByIdAsync(int id);
        Task<IEnumerable<RestauranteProveedorResponseDTO>> GetByRestauranteIdAsync(int restauranteId);
        Task<IEnumerable<RestauranteProveedorResponseDTO>> GetByProveedorIdAsync(int proveedorId);
        Task<IEnumerable<ProveedorResponseDTO>> GetProveedoresByRestauranteIdAsync(int restauranteId);
        Task<IEnumerable<ProveedorResponseDTO>> GetProveedoresByUsuarioAsync(int usuarioId);
        Task<RestauranteProveedorResponseDTO> CreateAsync(CreateRestauranteProveedorDTO dto);
        Task<RestauranteProveedorResponseDTO?> UpdateAsync(UpdateRestauranteProveedorDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
