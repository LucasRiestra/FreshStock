using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface ICategoriaService
    {
        Task<IEnumerable<CategoriaResponseDTO>> GetAllAsync();
        Task<CategoriaResponseDTO?> GetByIdAsync(int id);
        Task<CategoriaResponseDTO> CreateAsync(CreateCategoriaDTO dto);
        Task<CategoriaResponseDTO?> UpdateAsync(UpdateCategoriaDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
