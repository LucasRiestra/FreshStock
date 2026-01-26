using FreshStock.API.DTOs;
using FreshStock.API.Enums;

namespace FreshStock.API.Interfaces
{
    public interface IUsuarioRestauranteService
    {
        Task<IEnumerable<UsuarioRestauranteResponseDTO>> GetAllAsync();
        Task<UsuarioRestauranteResponseDTO?> GetByIdAsync(int id);
        Task<IEnumerable<UsuarioRestauranteResponseDTO>> GetByUsuarioIdAsync(int usuarioId);
        Task<IEnumerable<UsuarioRestauranteResponseDTO>> GetByRestauranteIdAsync(int restauranteId);
        Task<UsuarioRestauranteResponseDTO?> GetByUsuarioAndRestauranteAsync(int usuarioId, int restauranteId);
        Task<RolUsuario?> GetRolUsuarioEnRestauranteAsync(int usuarioId, int restauranteId);
        Task<IEnumerable<int>> GetRestaurantesIdsByUsuarioAsync(int usuarioId);
        Task<bool> EsAdminEnAlgunRestauranteAsync(int usuarioId);
        Task<UsuarioRestauranteResponseDTO> CreateAsync(CreateUsuarioRestauranteDTO dto);
        Task<UsuarioRestauranteResponseDTO?> UpdateAsync(UpdateUsuarioRestauranteDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
