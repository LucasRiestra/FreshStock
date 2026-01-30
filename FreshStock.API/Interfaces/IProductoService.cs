using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IProductoService
    {
        Task<IEnumerable<ProductoResponseDTO>> GetAllAsync();
        Task<ProductoResponseDTO?> GetByIdAsync(int id);
        Task<IEnumerable<ProductoResponseDTO>> GetByCategoriaIdAsync(int categoriaId);
        Task<IEnumerable<ProductoResponseDTO>> GetByProveedorIdAsync(int proveedorId);

        // Filtros por restaurante/usuario (solo productos de proveedores y categorías asignados)
        Task<IEnumerable<ProductoResponseDTO>> GetByRestauranteIdAsync(int restauranteId);
        Task<IEnumerable<ProductoResponseDTO>> GetByRestauranteYProveedorAsync(int restauranteId, int proveedorId);
        Task<IEnumerable<ProductoResponseDTO>> GetByRestauranteYCategoriaAsync(int restauranteId, int categoriaId);
        Task<IEnumerable<ProductoResponseDTO>> GetByUsuarioIdAsync(int usuarioId);

        Task<ProductoResponseDTO> CreateAsync(CreateProductoDTO dto);
        Task<IEnumerable<ProductoResponseDTO>> CreateBulkAsync(CreateProductosBulkDTO dto);
        Task<ProductoResponseDTO?> UpdateAsync(UpdateProductoDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
