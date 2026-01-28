using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class StockIdealRestauranteService : IStockIdealRestauranteService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;

        public StockIdealRestauranteService(MongoDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StockIdealRestauranteResponseDTO>> GetAllAsync()
        {
            var items = await _context.StockIdealRestaurantes
                .Find(s => s.Activo)
                .ToListAsync();

            return await EnrichWithNamesAsync(items);
        }

        public async Task<StockIdealRestauranteResponseDTO?> GetByIdAsync(int id)
        {
            var item = await _context.StockIdealRestaurantes
                .Find(s => s.Id == id)
                .FirstOrDefaultAsync();

            if (item == null)
                return null;

            var response = _mapper.Map<StockIdealRestauranteResponseDTO>(item);
            await EnrichSingleWithNamesAsync(response);
            return response;
        }

        public async Task<IEnumerable<StockIdealRestauranteResponseDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            var items = await _context.StockIdealRestaurantes
                .Find(s => s.RestauranteId == restauranteId && s.Activo)
                .ToListAsync();

            return await EnrichWithNamesAsync(items);
        }

        public async Task<StockIdealRestauranteResponseDTO?> GetByProductoRestauranteAsync(int productoId, int restauranteId)
        {
            var item = await _context.StockIdealRestaurantes
                .Find(s => s.ProductoId == productoId && s.RestauranteId == restauranteId && s.Activo)
                .FirstOrDefaultAsync();

            if (item == null)
                return null;

            var response = _mapper.Map<StockIdealRestauranteResponseDTO>(item);
            await EnrichSingleWithNamesAsync(response);
            return response;
        }

        public async Task<StockIdealRestauranteResponseDTO> CreateAsync(CreateStockIdealRestauranteDTO dto)
        {
            // Validar que no exista ya una configuración para este producto/restaurante
            var existente = await _context.StockIdealRestaurantes
                .Find(s => s.ProductoId == dto.ProductoId && s.RestauranteId == dto.RestauranteId)
                .FirstOrDefaultAsync();

            if (existente != null)
            {
                throw new InvalidOperationException($"Ya existe una configuración de stock ideal para el producto {dto.ProductoId} en el restaurante {dto.RestauranteId}");
            }

            // Validar que existan producto y restaurante
            var producto = await _context.Productos.Find(p => p.Id == dto.ProductoId).FirstOrDefaultAsync();
            if (producto == null)
            {
                throw new InvalidOperationException($"Producto con ID {dto.ProductoId} no encontrado");
            }

            var restaurante = await _context.Restaurantes.Find(r => r.Id == dto.RestauranteId).FirstOrDefaultAsync();
            if (restaurante == null)
            {
                throw new InvalidOperationException($"Restaurante con ID {dto.RestauranteId} no encontrado");
            }

            // Validar que StockMinimo <= StockIdeal <= StockMaximo
            ValidarStockLimits(dto.StockMinimo, dto.StockIdeal, dto.StockMaximo);

            var entity = _mapper.Map<StockIdealRestaurante>(dto);
            entity.Id = await _context.GetNextSequenceAsync("stockIdealRestaurantes");
            entity.Activo = true;

            await _context.StockIdealRestaurantes.InsertOneAsync(entity);

            var response = _mapper.Map<StockIdealRestauranteResponseDTO>(entity);
            response.NombreProducto = producto.Nombre;
            response.NombreRestaurante = restaurante.Nombre;
            return response;
        }

        public async Task<IEnumerable<StockIdealRestauranteResponseDTO>> CreateBulkAsync(CreateStockIdealBulkDTO dto)
        {
            // Validar que exista el restaurante
            var restaurante = await _context.Restaurantes.Find(r => r.Id == dto.RestauranteId).FirstOrDefaultAsync();
            if (restaurante == null)
            {
                throw new InvalidOperationException($"Restaurante con ID {dto.RestauranteId} no encontrado");
            }

            // Obtener todos los productos para validación
            var productoIds = dto.Items.Select(i => i.ProductoId).ToList();
            var productos = await _context.Productos
                .Find(p => productoIds.Contains(p.Id))
                .ToListAsync();

            var productosDict = productos.ToDictionary(p => p.Id);

            // Verificar que no existan configuraciones previas
            var existentes = await _context.StockIdealRestaurantes
                .Find(s => s.RestauranteId == dto.RestauranteId && productoIds.Contains(s.ProductoId))
                .ToListAsync();

            if (existentes.Any())
            {
                var idsExistentes = string.Join(", ", existentes.Select(e => e.ProductoId));
                throw new InvalidOperationException($"Ya existen configuraciones para los productos: {idsExistentes}");
            }

            var entities = new List<StockIdealRestaurante>();
            foreach (var item in dto.Items)
            {
                if (!productosDict.ContainsKey(item.ProductoId))
                {
                    throw new InvalidOperationException($"Producto con ID {item.ProductoId} no encontrado");
                }

                ValidarStockLimits(item.StockMinimo, item.StockIdeal, item.StockMaximo, item.ProductoId);

                var entity = new StockIdealRestaurante
                {
                    Id = await _context.GetNextSequenceAsync("stockIdealRestaurantes"),
                    ProductoId = item.ProductoId,
                    RestauranteId = dto.RestauranteId,
                    StockIdeal = item.StockIdeal,
                    StockMinimo = item.StockMinimo,
                    StockMaximo = item.StockMaximo,
                    Activo = true
                };
                entities.Add(entity);
            }

            await _context.StockIdealRestaurantes.InsertManyAsync(entities);

            var response = entities.Select(e =>
            {
                var responseDto = _mapper.Map<StockIdealRestauranteResponseDTO>(e);
                responseDto.NombreProducto = productosDict[e.ProductoId].Nombre;
                responseDto.NombreRestaurante = restaurante.Nombre;
                return responseDto;
            }).ToList();

            return response;
        }

        public async Task<StockIdealRestauranteResponseDTO?> UpdateAsync(UpdateStockIdealRestauranteDTO dto)
        {
            var entity = await _context.StockIdealRestaurantes
                .Find(s => s.Id == dto.Id)
                .FirstOrDefaultAsync();

            if (entity == null)
                return null;

            ValidarStockLimits(dto.StockMinimo, dto.StockIdeal, dto.StockMaximo);

            entity.StockIdeal = dto.StockIdeal;
            entity.StockMinimo = dto.StockMinimo;
            entity.StockMaximo = dto.StockMaximo;
            entity.Activo = dto.Activo;

            await _context.StockIdealRestaurantes.ReplaceOneAsync(s => s.Id == dto.Id, entity);

            var response = _mapper.Map<StockIdealRestauranteResponseDTO>(entity);
            await EnrichSingleWithNamesAsync(response);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.StockIdealRestaurantes
                .Find(s => s.Id == id)
                .FirstOrDefaultAsync();

            if (entity == null)
                return false;

            // Soft delete
            var update = Builders<StockIdealRestaurante>.Update.Set(s => s.Activo, false);
            await _context.StockIdealRestaurantes.UpdateOneAsync(s => s.Id == id, update);

            return true;
        }

        // Helpers
        private void ValidarStockLimits(decimal stockMinimo, decimal stockIdeal, decimal stockMaximo, int? productoId = null)
        {
            var prefix = productoId.HasValue ? $"Producto {productoId}: " : "";

            if (stockMinimo > stockIdeal)
            {
                throw new InvalidOperationException($"{prefix}El stock mínimo no puede ser mayor que el stock ideal");
            }
            if (stockIdeal > stockMaximo)
            {
                throw new InvalidOperationException($"{prefix}El stock ideal no puede ser mayor que el stock máximo");
            }
        }

        private async Task<List<StockIdealRestauranteResponseDTO>> EnrichWithNamesAsync(List<StockIdealRestaurante> items)
        {
            if (!items.Any())
                return new List<StockIdealRestauranteResponseDTO>();

            var productoIds = items.Select(i => i.ProductoId).Distinct().ToList();
            var restauranteIds = items.Select(i => i.RestauranteId).Distinct().ToList();

            var productos = await _context.Productos
                .Find(p => productoIds.Contains(p.Id))
                .ToListAsync();
            var productosDict = productos.ToDictionary(p => p.Id, p => p.Nombre);

            var restaurantes = await _context.Restaurantes
                .Find(r => restauranteIds.Contains(r.Id))
                .ToListAsync();
            var restaurantesDict = restaurantes.ToDictionary(r => r.Id, r => r.Nombre);

            return items.Select(item =>
            {
                var dto = _mapper.Map<StockIdealRestauranteResponseDTO>(item);
                dto.NombreProducto = productosDict.GetValueOrDefault(item.ProductoId);
                dto.NombreRestaurante = restaurantesDict.GetValueOrDefault(item.RestauranteId);
                return dto;
            }).ToList();
        }

        private async Task EnrichSingleWithNamesAsync(StockIdealRestauranteResponseDTO dto)
        {
            var producto = await _context.Productos.Find(p => p.Id == dto.ProductoId).FirstOrDefaultAsync();
            var restaurante = await _context.Restaurantes.Find(r => r.Id == dto.RestauranteId).FirstOrDefaultAsync();

            dto.NombreProducto = producto?.Nombre;
            dto.NombreRestaurante = restaurante?.Nombre;
        }
    }
}
