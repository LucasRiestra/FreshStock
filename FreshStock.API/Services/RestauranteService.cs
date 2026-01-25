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

            // Soft delete
            var update = Builders<Restaurante>.Update.Set(r => r.Activo, false);
            await _context.Restaurantes.UpdateOneAsync(r => r.Id == id, update);

            return true;
        }
    }
}
