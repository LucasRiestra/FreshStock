using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class RestauranteProveedorService : IRestauranteProveedorService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUsuarioRestauranteService _usuarioRestauranteService;

        public RestauranteProveedorService(
            MongoDbContext context,
            IMapper mapper,
            IUsuarioRestauranteService usuarioRestauranteService)
        {
            _context = context;
            _mapper = mapper;
            _usuarioRestauranteService = usuarioRestauranteService;
        }

        public async Task<IEnumerable<RestauranteProveedorResponseDTO>> GetAllAsync()
        {
            var asignaciones = await _context.RestauranteProveedores
                .Find(rp => rp.Activo)
                .ToListAsync();

            var response = new List<RestauranteProveedorResponseDTO>();
            foreach (var asignacion in asignaciones)
            {
                var dto = _mapper.Map<RestauranteProveedorResponseDTO>(asignacion);
                await EnrichWithNamesAsync(dto);
                response.Add(dto);
            }

            return response;
        }

        public async Task<RestauranteProveedorResponseDTO?> GetByIdAsync(int id)
        {
            var asignacion = await _context.RestauranteProveedores
                .Find(rp => rp.Id == id)
                .FirstOrDefaultAsync();

            if (asignacion == null)
                return null;

            var response = _mapper.Map<RestauranteProveedorResponseDTO>(asignacion);
            await EnrichWithNamesAsync(response);
            return response;
        }

        public async Task<IEnumerable<RestauranteProveedorResponseDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            var asignaciones = await _context.RestauranteProveedores
                .Find(rp => rp.RestauranteId == restauranteId && rp.Activo)
                .ToListAsync();

            var response = new List<RestauranteProveedorResponseDTO>();
            foreach (var asignacion in asignaciones)
            {
                var dto = _mapper.Map<RestauranteProveedorResponseDTO>(asignacion);
                await EnrichWithNamesAsync(dto);
                response.Add(dto);
            }

            return response;
        }

        public async Task<IEnumerable<RestauranteProveedorResponseDTO>> GetByProveedorIdAsync(int proveedorId)
        {
            var asignaciones = await _context.RestauranteProveedores
                .Find(rp => rp.ProveedorId == proveedorId && rp.Activo)
                .ToListAsync();

            var response = new List<RestauranteProveedorResponseDTO>();
            foreach (var asignacion in asignaciones)
            {
                var dto = _mapper.Map<RestauranteProveedorResponseDTO>(asignacion);
                await EnrichWithNamesAsync(dto);
                response.Add(dto);
            }

            return response;
        }

        public async Task<IEnumerable<ProveedorResponseDTO>> GetProveedoresByRestauranteIdAsync(int restauranteId)
        {
            var asignaciones = await _context.RestauranteProveedores
                .Find(rp => rp.RestauranteId == restauranteId && rp.Activo)
                .ToListAsync();

            var proveedorIds = asignaciones.Select(rp => rp.ProveedorId).ToList();

            var proveedores = await _context.Proveedores
                .Find(p => proveedorIds.Contains(p.Id) && p.Activo)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProveedorResponseDTO>>(proveedores);
        }

        public async Task<IEnumerable<ProveedorResponseDTO>> GetProveedoresByUsuarioAsync(int usuarioId)
        {
            // Obtener los restaurantes del usuario
            var restauranteIds = await _usuarioRestauranteService.GetRestaurantesIdsByUsuarioAsync(usuarioId);

            // Obtener los proveedores de esos restaurantes
            var asignaciones = await _context.RestauranteProveedores
                .Find(rp => restauranteIds.Contains(rp.RestauranteId) && rp.Activo)
                .ToListAsync();

            var proveedorIds = asignaciones.Select(rp => rp.ProveedorId).Distinct().ToList();

            var proveedores = await _context.Proveedores
                .Find(p => proveedorIds.Contains(p.Id) && p.Activo)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProveedorResponseDTO>>(proveedores);
        }

        public async Task<RestauranteProveedorResponseDTO> CreateAsync(CreateRestauranteProveedorDTO dto)
        {
            // Verificar que no exista ya una asignación activa
            var existente = await _context.RestauranteProveedores
                .Find(rp => rp.RestauranteId == dto.RestauranteId && rp.ProveedorId == dto.ProveedorId && rp.Activo)
                .FirstOrDefaultAsync();

            if (existente != null)
                throw new InvalidOperationException("El proveedor ya está asignado a este restaurante");

            var asignacion = _mapper.Map<RestauranteProveedor>(dto);
            asignacion.Id = await _context.GetNextSequenceAsync("restauranteProveedores");
            asignacion.Activo = true;

            await _context.RestauranteProveedores.InsertOneAsync(asignacion);

            var response = _mapper.Map<RestauranteProveedorResponseDTO>(asignacion);
            await EnrichWithNamesAsync(response);
            return response;
        }

        public async Task<RestauranteProveedorResponseDTO?> UpdateAsync(UpdateRestauranteProveedorDTO dto)
        {
            var asignacion = await _context.RestauranteProveedores
                .Find(rp => rp.Id == dto.Id)
                .FirstOrDefaultAsync();

            if (asignacion == null)
                return null;

            asignacion.Activo = dto.Activo;

            await _context.RestauranteProveedores.ReplaceOneAsync(rp => rp.Id == dto.Id, asignacion);

            var response = _mapper.Map<RestauranteProveedorResponseDTO>(asignacion);
            await EnrichWithNamesAsync(response);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var asignacion = await _context.RestauranteProveedores
                .Find(rp => rp.Id == id)
                .FirstOrDefaultAsync();

            if (asignacion == null)
                return false;

            // Soft delete
            var update = Builders<RestauranteProveedor>.Update.Set(rp => rp.Activo, false);
            await _context.RestauranteProveedores.UpdateOneAsync(rp => rp.Id == id, update);

            return true;
        }

        private async Task EnrichWithNamesAsync(RestauranteProveedorResponseDTO dto)
        {
            var restaurante = await _context.Restaurantes
                .Find(r => r.Id == dto.RestauranteId)
                .FirstOrDefaultAsync();
            dto.NombreRestaurante = restaurante?.Nombre;

            var proveedor = await _context.Proveedores
                .Find(p => p.Id == dto.ProveedorId)
                .FirstOrDefaultAsync();
            dto.NombreProveedor = proveedor?.Nombre;
        }
    }
}
