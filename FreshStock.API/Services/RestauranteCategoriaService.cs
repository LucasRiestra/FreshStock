using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class RestauranteCategoriaService : IRestauranteCategoriaService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUsuarioRestauranteService _usuarioRestauranteService;

        public RestauranteCategoriaService(
            MongoDbContext context,
            IMapper mapper,
            IUsuarioRestauranteService usuarioRestauranteService)
        {
            _context = context;
            _mapper = mapper;
            _usuarioRestauranteService = usuarioRestauranteService;
        }

        public async Task<IEnumerable<RestauranteCategoriaResponseDTO>> GetAllAsync()
        {
            var asignaciones = await _context.RestauranteCategorias
                .Find(rc => rc.Activo)
                .ToListAsync();

            var response = new List<RestauranteCategoriaResponseDTO>();
            foreach (var asignacion in asignaciones)
            {
                var dto = _mapper.Map<RestauranteCategoriaResponseDTO>(asignacion);
                await EnrichWithNamesAsync(dto);
                response.Add(dto);
            }

            return response;
        }

        public async Task<RestauranteCategoriaResponseDTO?> GetByIdAsync(int id)
        {
            var asignacion = await _context.RestauranteCategorias
                .Find(rc => rc.Id == id)
                .FirstOrDefaultAsync();

            if (asignacion == null)
                return null;

            var response = _mapper.Map<RestauranteCategoriaResponseDTO>(asignacion);
            await EnrichWithNamesAsync(response);
            return response;
        }

        public async Task<IEnumerable<RestauranteCategoriaResponseDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            var asignaciones = await _context.RestauranteCategorias
                .Find(rc => rc.RestauranteId == restauranteId && rc.Activo)
                .ToListAsync();

            var response = new List<RestauranteCategoriaResponseDTO>();
            foreach (var asignacion in asignaciones)
            {
                var dto = _mapper.Map<RestauranteCategoriaResponseDTO>(asignacion);
                await EnrichWithNamesAsync(dto);
                response.Add(dto);
            }

            return response;
        }

        public async Task<IEnumerable<RestauranteCategoriaResponseDTO>> GetByCategoriaIdAsync(int categoriaId)
        {
            var asignaciones = await _context.RestauranteCategorias
                .Find(rc => rc.CategoriaId == categoriaId && rc.Activo)
                .ToListAsync();

            var response = new List<RestauranteCategoriaResponseDTO>();
            foreach (var asignacion in asignaciones)
            {
                var dto = _mapper.Map<RestauranteCategoriaResponseDTO>(asignacion);
                await EnrichWithNamesAsync(dto);
                response.Add(dto);
            }

            return response;
        }

        public async Task<IEnumerable<CategoriaResponseDTO>> GetCategoriasByRestauranteIdAsync(int restauranteId)
        {
            var asignaciones = await _context.RestauranteCategorias
                .Find(rc => rc.RestauranteId == restauranteId && rc.Activo)
                .ToListAsync();

            var categoriaIds = asignaciones.Select(rc => rc.CategoriaId).ToList();

            var categorias = await _context.Categorias
                .Find(c => categoriaIds.Contains(c.Id))
                .ToListAsync();

            return _mapper.Map<IEnumerable<CategoriaResponseDTO>>(categorias);
        }

        public async Task<IEnumerable<CategoriaResponseDTO>> GetCategoriasByUsuarioAsync(int usuarioId)
        {
            // Obtener los restaurantes del usuario
            var restauranteIds = await _usuarioRestauranteService.GetRestaurantesIdsByUsuarioAsync(usuarioId);

            // Obtener las categorías de esos restaurantes
            var asignaciones = await _context.RestauranteCategorias
                .Find(rc => restauranteIds.Contains(rc.RestauranteId) && rc.Activo)
                .ToListAsync();

            var categoriaIds = asignaciones.Select(rc => rc.CategoriaId).Distinct().ToList();

            var categorias = await _context.Categorias
                .Find(c => categoriaIds.Contains(c.Id))
                .ToListAsync();

            return _mapper.Map<IEnumerable<CategoriaResponseDTO>>(categorias);
        }

        public async Task<RestauranteCategoriaResponseDTO> CreateAsync(CreateRestauranteCategoriaDTO dto)
        {
            // Verificar que no exista ya una asignación activa
            var existente = await _context.RestauranteCategorias
                .Find(rc => rc.RestauranteId == dto.RestauranteId && rc.CategoriaId == dto.CategoriaId && rc.Activo)
                .FirstOrDefaultAsync();

            if (existente != null)
                throw new InvalidOperationException("La categoría ya está asignada a este restaurante");

            var asignacion = _mapper.Map<RestauranteCategoria>(dto);
            asignacion.Id = await _context.GetNextSequenceAsync("restauranteCategorias");
            asignacion.Activo = true;

            await _context.RestauranteCategorias.InsertOneAsync(asignacion);

            var response = _mapper.Map<RestauranteCategoriaResponseDTO>(asignacion);
            await EnrichWithNamesAsync(response);
            return response;
        }

        public async Task<RestauranteCategoriaResponseDTO?> UpdateAsync(UpdateRestauranteCategoriaDTO dto)
        {
            var asignacion = await _context.RestauranteCategorias
                .Find(rc => rc.Id == dto.Id)
                .FirstOrDefaultAsync();

            if (asignacion == null)
                return null;

            asignacion.Activo = dto.Activo;

            await _context.RestauranteCategorias.ReplaceOneAsync(rc => rc.Id == dto.Id, asignacion);

            var response = _mapper.Map<RestauranteCategoriaResponseDTO>(asignacion);
            await EnrichWithNamesAsync(response);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var asignacion = await _context.RestauranteCategorias
                .Find(rc => rc.Id == id)
                .FirstOrDefaultAsync();

            if (asignacion == null)
                return false;

            // Soft delete
            var update = Builders<RestauranteCategoria>.Update.Set(rc => rc.Activo, false);
            await _context.RestauranteCategorias.UpdateOneAsync(rc => rc.Id == id, update);

            return true;
        }

        private async Task EnrichWithNamesAsync(RestauranteCategoriaResponseDTO dto)
        {
            var restaurante = await _context.Restaurantes
                .Find(r => r.Id == dto.RestauranteId)
                .FirstOrDefaultAsync();
            dto.NombreRestaurante = restaurante?.Nombre;

            var categoria = await _context.Categorias
                .Find(c => c.Id == dto.CategoriaId)
                .FirstOrDefaultAsync();
            dto.NombreCategoria = categoria?.Nombre;
        }
    }
}
