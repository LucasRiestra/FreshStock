using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IUsuarioService
    {
        Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync();
        Task<UsuarioResponseDTO?> GetByIdAsync(int id);
        Task<IEnumerable<UsuarioResponseDTO>> GetByRestauranteIdAsync(int restauranteId);
        Task<UsuarioResponseDTO> CreateAsync(CreateUsuarioDTO dto);
        Task<UsuarioResponseDTO?> UpdateAsync(UpdateUsuarioDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
