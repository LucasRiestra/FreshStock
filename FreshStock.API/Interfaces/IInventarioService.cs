using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IInventarioService
    {
        // Gestión de inventarios
        Task<InventarioResponseDTO> CreateAsync(CreateInventarioDTO dto, int usuarioId);
        Task<InventarioResponseDTO?> GetByIdAsync(int id);
        Task<IEnumerable<InventarioResumenDTO>> GetByRestauranteIdAsync(int restauranteId);
        Task<InventarioResponseDTO?> GetActualByRestauranteIdAsync(int restauranteId);
        Task<InventarioResponseDTO?> FinalizarAsync(int inventarioId, FinalizarInventarioDTO? dto);
        Task<InventarioResponseDTO?> CancelarAsync(int inventarioId);

        // Navegación para conteo
        Task<IEnumerable<CategoriaConteoDTO>> GetCategoriasAsync(int inventarioId);
        Task<IEnumerable<ProveedorConteoDTO>> GetProveedoresByCategoriaAsync(int inventarioId, int categoriaId);
        Task<IEnumerable<ProductoConteoDTO>> GetProductosByProveedorAsync(int inventarioId, int proveedorId);

        // Registro de conteos
        Task<InventarioDetalleResponseDTO> RegistrarConteoAsync(int inventarioId, CreateInventarioDetalleDTO dto);
        Task<IEnumerable<InventarioDetalleResponseDTO>> RegistrarConteosBulkAsync(int inventarioId, CreateInventarioDetalleBulkDTO dto);
        Task<InventarioDetalleResponseDTO?> ActualizarConteoAsync(int inventarioId, UpdateInventarioDetalleDTO dto);

        // Progreso
        Task<ProgresoInventarioDTO> GetProgresoAsync(int inventarioId);

        // Detalles del inventario
        Task<IEnumerable<InventarioDetalleResponseDTO>> GetDetallesAsync(int inventarioId);
    }
}
