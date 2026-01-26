using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Enums;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class UsuarioRestauranteService : IUsuarioRestauranteService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;

        public UsuarioRestauranteService(MongoDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UsuarioRestauranteResponseDTO>> GetAllAsync()
        {
            var asignaciones = await _context.UsuarioRestaurantes
                .Find(ur => ur.Activo)
                .ToListAsync();

            var response = new List<UsuarioRestauranteResponseDTO>();
            foreach (var asignacion in asignaciones)
            {
                var dto = _mapper.Map<UsuarioRestauranteResponseDTO>(asignacion);
                await EnrichWithNamesAsync(dto);
                response.Add(dto);
            }

            return response;
        }

        public async Task<UsuarioRestauranteResponseDTO?> GetByIdAsync(int id)
        {
            var asignacion = await _context.UsuarioRestaurantes
                .Find(ur => ur.Id == id)
                .FirstOrDefaultAsync();

            if (asignacion == null)
                return null;

            var response = _mapper.Map<UsuarioRestauranteResponseDTO>(asignacion);
            await EnrichWithNamesAsync(response);
            return response;
        }

        public async Task<IEnumerable<UsuarioRestauranteResponseDTO>> GetByUsuarioIdAsync(int usuarioId)
        {
            var asignaciones = await _context.UsuarioRestaurantes
                .Find(ur => ur.UsuarioId == usuarioId && ur.Activo)
                .ToListAsync();

            var response = new List<UsuarioRestauranteResponseDTO>();
            foreach (var asignacion in asignaciones)
            {
                var dto = _mapper.Map<UsuarioRestauranteResponseDTO>(asignacion);
                await EnrichWithNamesAsync(dto);
                response.Add(dto);
            }

            return response;
        }

        public async Task<IEnumerable<UsuarioRestauranteResponseDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            var asignaciones = await _context.UsuarioRestaurantes
                .Find(ur => ur.RestauranteId == restauranteId && ur.Activo)
                .ToListAsync();

            var response = new List<UsuarioRestauranteResponseDTO>();
            foreach (var asignacion in asignaciones)
            {
                var dto = _mapper.Map<UsuarioRestauranteResponseDTO>(asignacion);
                await EnrichWithNamesAsync(dto);
                response.Add(dto);
            }

            return response;
        }

        public async Task<UsuarioRestauranteResponseDTO?> GetByUsuarioAndRestauranteAsync(int usuarioId, int restauranteId)
        {
            var asignacion = await _context.UsuarioRestaurantes
                .Find(ur => ur.UsuarioId == usuarioId && ur.RestauranteId == restauranteId && ur.Activo)
                .FirstOrDefaultAsync();

            if (asignacion == null)
                return null;

            var response = _mapper.Map<UsuarioRestauranteResponseDTO>(asignacion);
            await EnrichWithNamesAsync(response);
            return response;
        }

        public async Task<RolUsuario?> GetRolUsuarioEnRestauranteAsync(int usuarioId, int restauranteId)
        {
            var asignacion = await _context.UsuarioRestaurantes
                .Find(ur => ur.UsuarioId == usuarioId && ur.RestauranteId == restauranteId && ur.Activo)
                .FirstOrDefaultAsync();

            return asignacion?.Rol;
        }

        public async Task<IEnumerable<int>> GetRestaurantesIdsByUsuarioAsync(int usuarioId)
        {
            var asignaciones = await _context.UsuarioRestaurantes
                .Find(ur => ur.UsuarioId == usuarioId && ur.Activo)
                .ToListAsync();

            return asignaciones.Select(ur => ur.RestauranteId);
        }

        public async Task<bool> EsAdminEnAlgunRestauranteAsync(int usuarioId)
        {
            var asignacion = await _context.UsuarioRestaurantes
                .Find(ur => ur.UsuarioId == usuarioId && ur.Rol == RolUsuario.Admin && ur.Activo)
                .FirstOrDefaultAsync();

            return asignacion != null;
        }

        public async Task<UsuarioRestauranteResponseDTO> CreateAsync(CreateUsuarioRestauranteDTO dto)
        {
            // Verificar que no exista ya una asignación activa
            var existente = await _context.UsuarioRestaurantes
                .Find(ur => ur.UsuarioId == dto.UsuarioId && ur.RestauranteId == dto.RestauranteId && ur.Activo)
                .FirstOrDefaultAsync();

            if (existente != null)
                throw new InvalidOperationException("El usuario ya está asignado a este restaurante");

            var asignacion = _mapper.Map<UsuarioRestaurante>(dto);
            asignacion.Id = await _context.GetNextSequenceAsync("usuarioRestaurantes");
            asignacion.Activo = true;

            await _context.UsuarioRestaurantes.InsertOneAsync(asignacion);

            var response = _mapper.Map<UsuarioRestauranteResponseDTO>(asignacion);
            await EnrichWithNamesAsync(response);
            return response;
        }

        public async Task<UsuarioRestauranteResponseDTO?> UpdateAsync(UpdateUsuarioRestauranteDTO dto)
        {
            var asignacion = await _context.UsuarioRestaurantes
                .Find(ur => ur.Id == dto.Id)
                .FirstOrDefaultAsync();

            if (asignacion == null)
                return null;

            asignacion.Rol = dto.Rol;
            asignacion.Activo = dto.Activo;

            await _context.UsuarioRestaurantes.ReplaceOneAsync(ur => ur.Id == dto.Id, asignacion);

            var response = _mapper.Map<UsuarioRestauranteResponseDTO>(asignacion);
            await EnrichWithNamesAsync(response);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var asignacion = await _context.UsuarioRestaurantes
                .Find(ur => ur.Id == id)
                .FirstOrDefaultAsync();

            if (asignacion == null)
                return false;

            // Soft delete
            var update = Builders<UsuarioRestaurante>.Update.Set(ur => ur.Activo, false);
            await _context.UsuarioRestaurantes.UpdateOneAsync(ur => ur.Id == id, update);

            return true;
        }

        private async Task EnrichWithNamesAsync(UsuarioRestauranteResponseDTO dto)
        {
            var usuario = await _context.Usuarios
                .Find(u => u.Id == dto.UsuarioId)
                .FirstOrDefaultAsync();
            dto.NombreUsuario = usuario?.Nombre;

            var restaurante = await _context.Restaurantes
                .Find(r => r.Id == dto.RestauranteId)
                .FirstOrDefaultAsync();
            dto.NombreRestaurante = restaurante?.Nombre;
        }
    }
}
