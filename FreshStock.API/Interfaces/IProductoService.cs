using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IProductoService
    {
        Task<IEnumerable<ProductoResponseDTO>> GetAllAsync();
        Task<ProductoResponseDTO?> GetByIdAsync(int id);
        Task<IEnumerable<ProductoResponseDTO>> GetByCategoriaIdAsync(int categoriaId);
        Task<IEnumerable<ProductoResponseDTO>> GetByProveedorIdAsync(int proveedorId);
        Task<ProductoResponseDTO> CreateAsync(CreateProductoDTO dto);
        Task<IEnumerable<ProductoResponseDTO>> CreateBulkAsync(CreateProductosBulkDTO dto);
        Task<ProductoResponseDTO?> UpdateAsync(UpdateProductoDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
