using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FreshStock.API.Services
{
    public class StockLocalService : IStockLocalService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public StockLocalService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StockLocalResponseDTO>> GetAllAsync()
        {
            var stocks = await _context.StockLocal.ToListAsync();

            var response = _mapper.Map<IEnumerable<StockLocalResponseDTO>>(stocks);
            return response;
        }

        public async Task<StockLocalResponseDTO?> GetByIdAsync(int id)
        {
            var stock = await _context.StockLocal
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stock == null)
                return null;

            var response = _mapper.Map<StockLocalResponseDTO>(stock);
            return response;
        }

        public async Task<IEnumerable<StockLocalResponseDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            var stocks = await _context.StockLocal
                .Where(s => s.RestauranteId == restauranteId)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<StockLocalResponseDTO>>(stocks);
            return response;
        }

        public async Task<IEnumerable<StockLocalResponseDTO>> GetByProductoIdAsync(int productoId)
        {
            var stocks = await _context.StockLocal
                .Where(s => s.ProductoId == productoId)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<StockLocalResponseDTO>>(stocks);
            return response;
        }

        public async Task<StockLocalResponseDTO?> GetByLoteAsync(int productoId, int restauranteId, string lote)
        {
            var stock = await _context.StockLocal
                .FirstOrDefaultAsync(s => s.ProductoId == productoId
                    && s.RestauranteId == restauranteId
                    && s.Lote == lote);

            if (stock == null)
                return null;

            var response = _mapper.Map<StockLocalResponseDTO>(stock);
            return response;
        }

        public async Task<StockLocalResponseDTO> CreateAsync(CreateStockLocalDTO dto)
        {
            // Validar si ya existe stock para ese lote
            var stockExistente = await _context.StockLocal
                .FirstOrDefaultAsync(s => s.ProductoId == dto.ProductoId
                    && s.RestauranteId == dto.RestauranteId
                    && s.Lote == dto.Lote);

            if (stockExistente != null)
            {
                throw new InvalidOperationException(
                    $"Ya existe stock para el producto {dto.ProductoId} " +
                    $"en el restaurante {dto.RestauranteId} con lote {dto.Lote}");
            }

            // Obtener el costo del producto
            var producto = await _context.Productos.FindAsync(dto.ProductoId);
            if (producto == null)
            {
                throw new InvalidOperationException($"Producto con ID {dto.ProductoId} no encontrado");
            }

            var stock = _mapper.Map<StockLocal>(dto);
            stock.CostoUnitario = producto.CostoUnitario;
            stock.FechaEntrada = DateTime.UtcNow;

            _context.StockLocal.Add(stock);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<StockLocalResponseDTO>(stock);
            return response;
        }

        public async Task<StockLocalResponseDTO?> UpdateAsync(UpdateStockLocalDTO dto)
        {
            var stock = await _context.StockLocal
                .FirstOrDefaultAsync(s => s.Id == dto.Id);

            if (stock == null)
                return null;

            // Solo se puede actualizar cantidad y fecha de caducidad
            stock.Cantidad = dto.Cantidad;
            stock.FechaCaducidad = dto.FechaCaducidad;

            await _context.SaveChangesAsync();

            var response = _mapper.Map<StockLocalResponseDTO>(stock);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var stock = await _context.StockLocal
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stock == null)
                return false;

            // Hard delete - eliminar registro de stock
            _context.StockLocal.Remove(stock);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
