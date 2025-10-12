using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FreshStock.API.Services
{
    public class MovimientoInventarioService : IMovimientoInventarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public MovimientoInventarioService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MovimientoInventarioResponseDTO>> GetAllAsync()
        {
            var movimientos = await _context.MovimientosInventario
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<MovimientoInventarioResponseDTO>>(movimientos);
            return response;
        }

        public async Task<MovimientoInventarioResponseDTO?> GetByIdAsync(int id)
        {
            var movimiento = await _context.MovimientosInventario
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movimiento == null)
                return null;

            var response = _mapper.Map<MovimientoInventarioResponseDTO>(movimiento);
            return response;
        }

        public async Task<IEnumerable<MovimientoInventarioResponseDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            var movimientos = await _context.MovimientosInventario
                .Where(m => m.RestauranteId == restauranteId)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<MovimientoInventarioResponseDTO>>(movimientos);
            return response;
        }

        public async Task<IEnumerable<MovimientoInventarioResponseDTO>> GetByProductoIdAsync(int productoId)
        {
            var movimientos = await _context.MovimientosInventario
                .Where(m => m.ProductoId == productoId)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<MovimientoInventarioResponseDTO>>(movimientos);
            return response;
        }

        public async Task<IEnumerable<MovimientoInventarioResponseDTO>> GetByUsuarioIdAsync(int usuarioId)
        {
            var movimientos = await _context.MovimientosInventario
                .Where(m => m.UsuarioId == usuarioId)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<MovimientoInventarioResponseDTO>>(movimientos);
            return response;
        }

        public async Task<MovimientoInventarioResponseDTO> CreateAsync(CreateMovimientoInventarioDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Obtener el costo del producto
                var producto = await _context.Productos.FindAsync(dto.ProductoId);
                if (producto == null)
                {
                    throw new InvalidOperationException($"Producto con ID {dto.ProductoId} no encontrado");
                }

                // Crear el movimiento
                var movimiento = _mapper.Map<MovimientoInventario>(dto);
                movimiento.Fecha = DateTime.UtcNow;
                movimiento.CostoUnitario = producto.CostoUnitario;

                _context.MovimientosInventario.Add(movimiento);
                await _context.SaveChangesAsync();

                // Actualizar stock según el tipo de movimiento
                await ActualizarStockAsync(movimiento);

                await transaction.CommitAsync();

                var response = _mapper.Map<MovimientoInventarioResponseDTO>(movimiento);
                return response;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<MovimientoInventarioResponseDTO> RegistrarMermaAsync(CreateMermaDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Obtener el costo del producto
                var producto = await _context.Productos.FindAsync(dto.ProductoId);
                if (producto == null)
                {
                    throw new InvalidOperationException($"Producto con ID {dto.ProductoId} no encontrado");
                }

                // Validar que exista stock
                var stock = await _context.StockLocal
                    .FirstOrDefaultAsync(s =>
                        s.ProductoId == dto.ProductoId &&
                        s.RestauranteId == dto.RestauranteId &&
                        s.Lote == dto.Lote);

                if (stock == null)
                {
                    throw new InvalidOperationException(
                        $"No hay stock disponible para el producto {dto.ProductoId} " +
                        $"en el restaurante {dto.RestauranteId} con lote {dto.Lote}");
                }

                if (stock.Cantidad < dto.Cantidad)
                {
                    throw new InvalidOperationException(
                        $"Stock insuficiente. Disponible: {stock.Cantidad}, Solicitado: {dto.Cantidad}");
                }

                // Crear movimiento de salida por merma
                var movimiento = new MovimientoInventario
                {
                    Tipo = "Salida",
                    ProductoId = dto.ProductoId,
                    RestauranteId = dto.RestauranteId,
                    Cantidad = dto.Cantidad,
                    Lote = dto.Lote,
                    Motivo = $"Merma - {dto.TipoMerma}",
                    CostoUnitario = producto.CostoUnitario,
                    UsuarioId = dto.UsuarioId,
                    Fecha = DateTime.UtcNow,
                    RestauranteDestinoId = null
                };

                _context.MovimientosInventario.Add(movimiento);
                await _context.SaveChangesAsync();

                // Actualizar stock
                await ActualizarStockAsync(movimiento);

                await transaction.CommitAsync();

                var response = _mapper.Map<MovimientoInventarioResponseDTO>(movimiento);
                return response;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<MovimientoInventarioResponseDTO> RevertirMovimientoAsync(int movimientoId, int usuarioId, string motivo)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Buscar el movimiento original
                var movimientoOriginal = await _context.MovimientosInventario
                    .FirstOrDefaultAsync(m => m.Id == movimientoId);

                if (movimientoOriginal == null)
                {
                    throw new InvalidOperationException($"Movimiento con ID {movimientoId} no encontrado");
                }

                // Crear movimiento de reversión (inverso)
                var movimientoReversion = new MovimientoInventario
                {
                    Tipo = movimientoOriginal.Tipo == "Entrada" ? "Salida" : "Entrada",
                    ProductoId = movimientoOriginal.ProductoId,
                    RestauranteId = movimientoOriginal.Tipo == "Entrada"
                        ? movimientoOriginal.RestauranteId
                        : (movimientoOriginal.RestauranteDestinoId ?? movimientoOriginal.RestauranteId),
                    Cantidad = movimientoOriginal.Cantidad,
                    Lote = movimientoOriginal.Lote,
                    Motivo = $"Reversión: {motivo}",
                    CostoUnitario = movimientoOriginal.CostoUnitario,
                    UsuarioId = usuarioId,
                    Fecha = DateTime.UtcNow,
                    RestauranteDestinoId = movimientoOriginal.Tipo == "Salida" && movimientoOriginal.RestauranteDestinoId.HasValue
                        ? movimientoOriginal.RestauranteId
                        : null
                };

                _context.MovimientosInventario.Add(movimientoReversion);
                await _context.SaveChangesAsync();

                // Actualizar stock con la reversión
                await ActualizarStockAsync(movimientoReversion);

                await transaction.CommitAsync();

                var response = _mapper.Map<MovimientoInventarioResponseDTO>(movimientoReversion);
                return response;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task ActualizarStockAsync(MovimientoInventario movimiento)
        {
            // Buscar stock existente
            var stock = await _context.StockLocal
                .FirstOrDefaultAsync(s =>
                    s.ProductoId == movimiento.ProductoId &&
                    s.RestauranteId == movimiento.RestauranteId &&
                    s.Lote == movimiento.Lote);

            if (movimiento.Tipo == "Entrada")
            {
                if (stock == null)
                {
                    // Crear nuevo registro de stock
                    stock = new StockLocal
                    {
                        ProductoId = movimiento.ProductoId,
                        RestauranteId = movimiento.RestauranteId,
                        Lote = movimiento.Lote,
                        Cantidad = movimiento.Cantidad,
                        CostoUnitario = movimiento.CostoUnitario ?? 0,
                        FechaEntrada = DateTime.UtcNow
                    };
                    _context.StockLocal.Add(stock);
                }
                else
                {
                    // Incrementar stock existente
                    stock.Cantidad += movimiento.Cantidad;
                }
            }
            else if (movimiento.Tipo == "Salida")
            {
                if (stock == null)
                {
                    throw new InvalidOperationException(
                        $"No hay stock disponible para el producto {movimiento.ProductoId} " +
                        $"en el restaurante {movimiento.RestauranteId} con lote {movimiento.Lote}");
                }

                if (stock.Cantidad < movimiento.Cantidad)
                {
                    throw new InvalidOperationException(
                        $"Stock insuficiente. Disponible: {stock.Cantidad}, Solicitado: {movimiento.Cantidad}");
                }

                // Decrementar stock
                stock.Cantidad -= movimiento.Cantidad;

                // Si el stock llega a 0, eliminar el registro
                if (stock.Cantidad == 0)
                {
                    _context.StockLocal.Remove(stock);
                }
            }

            // Si es una transferencia, actualizar stock del restaurante destino
            if (movimiento.RestauranteDestinoId.HasValue && movimiento.Tipo == "Salida")
            {
                var stockDestino = await _context.StockLocal
                    .FirstOrDefaultAsync(s =>
                        s.ProductoId == movimiento.ProductoId &&
                        s.RestauranteId == movimiento.RestauranteDestinoId.Value &&
                        s.Lote == movimiento.Lote);

                if (stockDestino == null)
                {
                    // Crear stock en restaurante destino
                    stockDestino = new StockLocal
                    {
                        ProductoId = movimiento.ProductoId,
                        RestauranteId = movimiento.RestauranteDestinoId.Value,
                        Lote = movimiento.Lote,
                        Cantidad = movimiento.Cantidad,
                        CostoUnitario = movimiento.CostoUnitario ?? 0,
                        FechaEntrada = DateTime.UtcNow
                    };
                    _context.StockLocal.Add(stockDestino);
                }
                else
                {
                    // Incrementar stock en restaurante destino
                    stockDestino.Cantidad += movimiento.Cantidad;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
