using FreshStock.API.DTOs;

namespace FreshStock.API.Interfaces
{
    public interface IProveedorService
    {
        Task<IEnumerable<ProveedorResponseDTO>> GetAllAsync();
        Task<ProveedorResponseDTO?> GetByIdAsync(int id);
        Task<ProveedorResponseDTO> CreateAsync(CreateProveedorDTO dto);
        Task<ProveedorResponseDTO?> UpdateAsync(UpdateProveedorDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
