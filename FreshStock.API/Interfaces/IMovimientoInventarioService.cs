using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IMovimientoInventarioService
    {
        Task<IEnumerable<MovimientoInventarioResponseDTO>> GetAllAsync();
        Task<MovimientoInventarioResponseDTO?> GetByIdAsync(int id);
        Task<IEnumerable<MovimientoInventarioResponseDTO>> GetByRestauranteIdAsync(int restauranteId);
        Task<IEnumerable<MovimientoInventarioResponseDTO>> GetByRestaurantesIdsAsync(IEnumerable<int> restauranteIds);
        Task<IEnumerable<MovimientoInventarioResponseDTO>> GetByProductoIdAsync(int productoId);
        Task<IEnumerable<MovimientoInventarioResponseDTO>> GetByUsuarioIdAsync(int usuarioId);
        Task<MovimientoInventarioResponseDTO> CreateAsync(CreateMovimientoInventarioDTO dto);
        Task<MovimientoInventarioResponseDTO> RegistrarMermaAsync(CreateMermaDTO dto);
        Task<MovimientoInventarioResponseDTO> RevertirMovimientoAsync(int movimientoId, int usuarioId, string motivo);
    }
}
