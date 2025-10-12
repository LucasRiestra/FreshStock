using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FreshStock.API.Services
{
    public class RestauranteService : IRestauranteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public RestauranteService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<RestauranteResponseDTO>> GetAllAsync()
        {
            var restaurantes = await _context.Restaurantes
                .Where(r => r.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<RestauranteResponseDTO>>(restaurantes);
            return response;
        }

        public async Task<RestauranteResponseDTO?> GetByIdAsync(int id)
        {
            var restaurante = await _context.Restaurantes
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurante == null)
                return null;

            var response = _mapper.Map<RestauranteResponseDTO>(restaurante);
            return response;
        }

        public async Task<RestauranteResponseDTO> CreateAsync(CreateRestauranteDTO dto)
        {
            var restaurante = _mapper.Map<Restaurante>(dto);
            restaurante.Activo = true;

            _context.Restaurantes.Add(restaurante);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<RestauranteResponseDTO>(restaurante);
            return response;
        }

        public async Task<RestauranteResponseDTO?> UpdateAsync(UpdateRestauranteDTO dto)
        {
            var restaurante = await _context.Restaurantes
                .FirstOrDefaultAsync(r => r.Id == dto.Id);

            if (restaurante == null)
                return null;

            _mapper.Map(dto, restaurante);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<RestauranteResponseDTO>(restaurante);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var restaurante = await _context.Restaurantes
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurante == null)
                return false;

            // Soft delete
            restaurante.Activo = false;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
