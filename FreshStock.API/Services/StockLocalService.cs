using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class StockLocalService : IStockLocalService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;

        public StockLocalService(MongoDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StockLocalResponseDTO>> GetAllAsync()
        {
            var stocks = await _context.StockLocal
                .Find(_ => true)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<StockLocalResponseDTO>>(stocks);
            return response;
        }

        public async Task<StockLocalResponseDTO?> GetByIdAsync(int id)
        {
            var stock = await _context.StockLocal
                .Find(s => s.Id == id)
                .FirstOrDefaultAsync();

            if (stock == null)
                return null;

            var response = _mapper.Map<StockLocalResponseDTO>(stock);
            return response;
        }

        public async Task<IEnumerable<StockLocalResponseDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            var stocks = await _context.StockLocal
                .Find(s => s.RestauranteId == restauranteId)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<StockLocalResponseDTO>>(stocks);
            return response;
        }

        public async Task<IEnumerable<StockLocalResponseDTO>> GetByProductoIdAsync(int productoId)
        {
            var stocks = await _context.StockLocal
                .Find(s => s.ProductoId == productoId)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<StockLocalResponseDTO>>(stocks);
            return response;
        }

        public async Task<StockLocalResponseDTO?> GetByLoteAsync(int productoId, int restauranteId, string lote)
        {
            var stock = await _context.StockLocal
                .Find(s => s.ProductoId == productoId
                    && s.RestauranteId == restauranteId
                    && s.Lote == lote)
                .FirstOrDefaultAsync();

            if (stock == null)
                return null;

            var response = _mapper.Map<StockLocalResponseDTO>(stock);
            return response;
        }

        public async Task<StockLocalResponseDTO> CreateAsync(CreateStockLocalDTO dto)
        {
            // Validar si ya existe stock para ese lote
            var stockExistente = await _context.StockLocal
                .Find(s => s.ProductoId == dto.ProductoId
                    && s.RestauranteId == dto.RestauranteId
                    && s.Lote == dto.Lote)
                .FirstOrDefaultAsync();

            if (stockExistente != null)
            {
                throw new InvalidOperationException(
                    $"Ya existe stock para el producto {dto.ProductoId} " +
                    $"en el restaurante {dto.RestauranteId} con lote {dto.Lote}");
            }

            // Obtener el costo del producto
            var producto = await _context.Productos
                .Find(p => p.Id == dto.ProductoId)
                .FirstOrDefaultAsync();
            if (producto == null)
            {
                throw new InvalidOperationException($"Producto con ID {dto.ProductoId} no encontrado");
            }

            var stock = _mapper.Map<StockLocal>(dto);
            stock.Id = await _context.GetNextSequenceAsync("stockLocal");
            stock.CostoUnitario = producto.CostoUnitario;
            stock.FechaEntrada = DateTime.UtcNow;

            await _context.StockLocal.InsertOneAsync(stock);

            var response = _mapper.Map<StockLocalResponseDTO>(stock);
            return response;
        }

        public async Task<StockLocalResponseDTO?> UpdateAsync(UpdateStockLocalDTO dto)
        {
            var stock = await _context.StockLocal
                .Find(s => s.Id == dto.Id)
                .FirstOrDefaultAsync();

            if (stock == null)
                return null;

            // Solo se puede actualizar cantidad y fecha de caducidad
            var update = Builders<StockLocal>.Update
                .Set(s => s.Cantidad, dto.Cantidad)
                .Set(s => s.FechaCaducidad, dto.FechaCaducidad);
            await _context.StockLocal.UpdateOneAsync(s => s.Id == dto.Id, update);

            stock.Cantidad = dto.Cantidad;
            stock.FechaCaducidad = dto.FechaCaducidad;

            var response = _mapper.Map<StockLocalResponseDTO>(stock);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _context.StockLocal.DeleteOneAsync(s => s.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
