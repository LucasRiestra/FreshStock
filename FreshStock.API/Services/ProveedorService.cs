using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class ProveedorService : IProveedorService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;

        public ProveedorService(MongoDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProveedorResponseDTO>> GetAllAsync()
        {
            var proveedores = await _context.Proveedores
                .Find(p => p.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<ProveedorResponseDTO>>(proveedores);
            return response;
        }

        public async Task<ProveedorResponseDTO?> GetByIdAsync(int id)
        {
            var proveedor = await _context.Proveedores
                .Find(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (proveedor == null)
                return null;

            var response = _mapper.Map<ProveedorResponseDTO>(proveedor);
            return response;
        }

        public async Task<ProveedorResponseDTO> CreateAsync(CreateProveedorDTO dto)
        {
            var proveedor = _mapper.Map<Proveedor>(dto);
            proveedor.Id = await _context.GetNextSequenceAsync("proveedores");
            proveedor.Activo = true;

            await _context.Proveedores.InsertOneAsync(proveedor);

            var response = _mapper.Map<ProveedorResponseDTO>(proveedor);
            return response;
        }

        public async Task<ProveedorResponseDTO?> UpdateAsync(UpdateProveedorDTO dto)
        {
            var proveedor = await _context.Proveedores
                .Find(p => p.Id == dto.Id)
                .FirstOrDefaultAsync();

            if (proveedor == null)
                return null;

            _mapper.Map(dto, proveedor);
            await _context.Proveedores.ReplaceOneAsync(p => p.Id == dto.Id, proveedor);

            var response = _mapper.Map<ProveedorResponseDTO>(proveedor);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var proveedor = await _context.Proveedores
                .Find(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (proveedor == null)
                return false;

            // Soft delete
            var update = Builders<Proveedor>.Update.Set(p => p.Activo, false);
            await _context.Proveedores.UpdateOneAsync(p => p.Id == id, update);

            return true;
        }
    }
}
