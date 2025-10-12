using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FreshStock.API.Services
{
    public class ProveedorService : IProveedorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ProveedorService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProveedorResponseDTO>> GetAllAsync()
        {
            var proveedores = await _context.Proveedores
                .Where(p => p.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<ProveedorResponseDTO>>(proveedores);
            return response;
        }

        public async Task<ProveedorResponseDTO?> GetByIdAsync(int id)
        {
            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proveedor == null)
                return null;

            var response = _mapper.Map<ProveedorResponseDTO>(proveedor);
            return response;
        }

        public async Task<ProveedorResponseDTO> CreateAsync(CreateProveedorDTO dto)
        {
            var proveedor = _mapper.Map<Proveedor>(dto);
            proveedor.Activo = true;

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<ProveedorResponseDTO>(proveedor);
            return response;
        }

        public async Task<ProveedorResponseDTO?> UpdateAsync(UpdateProveedorDTO dto)
        {
            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(p => p.Id == dto.Id);

            if (proveedor == null)
                return null;

            _mapper.Map(dto, proveedor);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<ProveedorResponseDTO>(proveedor);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proveedor == null)
                return false;

            // Soft delete
            proveedor.Activo = false;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
