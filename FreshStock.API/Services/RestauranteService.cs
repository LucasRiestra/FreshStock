using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class RestauranteService : IRestauranteService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;

        public RestauranteService(MongoDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<RestauranteResponseDTO>> GetAllAsync()
        {
            var restaurantes = await _context.Restaurantes
                .Find(r => r.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<RestauranteResponseDTO>>(restaurantes);
            return response;
        }

        public async Task<RestauranteResponseDTO?> GetByIdAsync(int id)
        {
            var restaurante = await _context.Restaurantes
                .Find(r => r.Id == id)
                .FirstOrDefaultAsync();

            if (restaurante == null)
                return null;

            var response = _mapper.Map<RestauranteResponseDTO>(restaurante);
            return response;
        }

        public async Task<RestauranteResponseDTO> CreateAsync(CreateRestauranteDTO dto)
        {
            var restaurante = _mapper.Map<Restaurante>(dto);
            restaurante.Id = await _context.GetNextSequenceAsync("restaurantes");
            restaurante.Activo = true;

            await _context.Restaurantes.InsertOneAsync(restaurante);

            var response = _mapper.Map<RestauranteResponseDTO>(restaurante);
            return response;
        }

        public async Task<RestauranteResponseDTO?> UpdateAsync(UpdateRestauranteDTO dto)
        {
            var restaurante = await _context.Restaurantes
                .Find(r => r.Id == dto.Id)
                .FirstOrDefaultAsync();

            if (restaurante == null)
                return null;

            _mapper.Map(dto, restaurante);

            await _context.Restaurantes.ReplaceOneAsync(r => r.Id == dto.Id, restaurante);

            var response = _mapper.Map<RestauranteResponseDTO>(restaurante);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var restaurante = await _context.Restaurantes
                .Find(r => r.Id == id)
                .FirstOrDefaultAsync();

            if (restaurante == null)
                return false;

            // Eliminación en cascada - eliminar todos los registros relacionados
            await _context.StockLocal.DeleteManyAsync(s => s.RestauranteId == id);
            await _context.MovimientosInventario.DeleteManyAsync(m => m.RestauranteId == id);
            await _context.StockIdealRestaurantes.DeleteManyAsync(si => si.RestauranteId == id);
            await _context.AlertasStock.DeleteManyAsync(a => a.RestauranteId == id);
            await _context.RestauranteProveedores.DeleteManyAsync(rp => rp.RestauranteId == id);
            await _context.RestauranteCategorias.DeleteManyAsync(rc => rc.RestauranteId == id);
            await _context.UsuarioRestaurantes.DeleteManyAsync(ur => ur.RestauranteId == id);

            // Eliminar inventarios y sus detalles
            var inventarios = await _context.Inventarios
                .Find(i => i.RestauranteId == id)
                .ToListAsync();

            foreach (var inventario in inventarios)
            {
                await _context.InventarioDetalles.DeleteManyAsync(d => d.InventarioId == inventario.Id);
            }
            await _context.Inventarios.DeleteManyAsync(i => i.RestauranteId == id);

            // Eliminar el restaurante (hard delete)
            await _context.Restaurantes.DeleteOneAsync(r => r.Id == id);

            return true;
        }
    }
}
