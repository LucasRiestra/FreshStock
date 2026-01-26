using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class MovimientoInventarioService : IMovimientoInventarioService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;

        public MovimientoInventarioService(MongoDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MovimientoInventarioResponseDTO>> GetAllAsync()
        {
            var movimientos = await _context.MovimientosInventario
                .Find(_ => true)
                .SortByDescending(m => m.Fecha)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<MovimientoInventarioResponseDTO>>(movimientos);
            return response;
        }

        public async Task<MovimientoInventarioResponseDTO?> GetByIdAsync(int id)
        {
            var movimiento = await _context.MovimientosInventario
                .Find(m => m.Id == id)
                .FirstOrDefaultAsync();

            if (movimiento == null)
                return null;

            var response = _mapper.Map<MovimientoInventarioResponseDTO>(movimiento);
            return response;
        }

        public async Task<IEnumerable<MovimientoInventarioResponseDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            var movimientos = await _context.MovimientosInventario
                .Find(m => m.RestauranteId == restauranteId)
                .SortByDescending(m => m.Fecha)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<MovimientoInventarioResponseDTO>>(movimientos);
            return response;
        }

        public async Task<IEnumerable<MovimientoInventarioResponseDTO>> GetByRestaurantesIdsAsync(IEnumerable<int> restauranteIds)
        {
            var ids = restauranteIds.ToList();
            var movimientos = await _context.MovimientosInventario
                .Find(m => ids.Contains(m.RestauranteId))
                .SortByDescending(m => m.Fecha)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<MovimientoInventarioResponseDTO>>(movimientos);
            return response;
        }

        public async Task<IEnumerable<MovimientoInventarioResponseDTO>> GetByProductoIdAsync(int productoId)
        {
            var movimientos = await _context.MovimientosInventario
                .Find(m => m.ProductoId == productoId)
                .SortByDescending(m => m.Fecha)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<MovimientoInventarioResponseDTO>>(movimientos);
            return response;
        }

        public async Task<IEnumerable<MovimientoInventarioResponseDTO>> GetByUsuarioIdAsync(int usuarioId)
        {
            var movimientos = await _context.MovimientosInventario
                .Find(m => m.UsuarioId == usuarioId)
                .SortByDescending(m => m.Fecha)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<MovimientoInventarioResponseDTO>>(movimientos);
            return response;
        }

        public async Task<MovimientoInventarioResponseDTO> CreateAsync(CreateMovimientoInventarioDTO dto)
        {
            // Obtener el costo del producto
            var producto = await _context.Productos
                .Find(p => p.Id == dto.ProductoId)
                .FirstOrDefaultAsync();
            if (producto == null)
            {
                throw new InvalidOperationException($"Producto con ID {dto.ProductoId} no encontrado");
            }

            // Crear el movimiento
            var movimiento = _mapper.Map<MovimientoInventario>(dto);
            movimiento.Id = await _context.GetNextSequenceAsync("movimientosInventario");
            movimiento.Fecha = DateTime.UtcNow;
            movimiento.CostoUnitario = producto.CostoUnitario;

            await _context.MovimientosInventario.InsertOneAsync(movimiento);

            // Actualizar stock según el tipo de movimiento
            await ActualizarStockAsync(movimiento);

            var response = _mapper.Map<MovimientoInventarioResponseDTO>(movimiento);
            return response;
        }

        public async Task<MovimientoInventarioResponseDTO> RegistrarMermaAsync(CreateMermaDTO dto)
        {
            // Obtener el costo del producto
            var producto = await _context.Productos
                .Find(p => p.Id == dto.ProductoId)
                .FirstOrDefaultAsync();
            if (producto == null)
            {
                throw new InvalidOperationException($"Producto con ID {dto.ProductoId} no encontrado");
            }

            // Validar que exista stock
            var stock = await _context.StockLocal
                .Find(s =>
                    s.ProductoId == dto.ProductoId &&
                    s.RestauranteId == dto.RestauranteId &&
                    s.Lote == dto.Lote)
                .FirstOrDefaultAsync();

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
                Id = await _context.GetNextSequenceAsync("movimientosInventario"),
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

            await _context.MovimientosInventario.InsertOneAsync(movimiento);

            // Actualizar stock
            await ActualizarStockAsync(movimiento);

            var response = _mapper.Map<MovimientoInventarioResponseDTO>(movimiento);
            return response;
        }

        public async Task<MovimientoInventarioResponseDTO> RevertirMovimientoAsync(int movimientoId, int usuarioId, string motivo)
        {
            // Buscar el movimiento original
            var movimientoOriginal = await _context.MovimientosInventario
                .Find(m => m.Id == movimientoId)
                .FirstOrDefaultAsync();

            if (movimientoOriginal == null)
            {
                throw new InvalidOperationException($"Movimiento con ID {movimientoId} no encontrado");
            }

            // Crear movimiento de reversión (inverso)
            var movimientoReversion = new MovimientoInventario
            {
                Id = await _context.GetNextSequenceAsync("movimientosInventario"),
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

            await _context.MovimientosInventario.InsertOneAsync(movimientoReversion);

            // Actualizar stock con la reversión
            await ActualizarStockAsync(movimientoReversion);

            var response = _mapper.Map<MovimientoInventarioResponseDTO>(movimientoReversion);
            return response;
        }

        private async Task ActualizarStockAsync(MovimientoInventario movimiento)
        {
            // Buscar stock existente
            var stock = await _context.StockLocal
                .Find(s =>
                    s.ProductoId == movimiento.ProductoId &&
                    s.RestauranteId == movimiento.RestauranteId &&
                    s.Lote == movimiento.Lote)
                .FirstOrDefaultAsync();

            if (movimiento.Tipo == "Entrada")
            {
                if (stock == null)
                {
                    // Crear nuevo registro de stock
                    stock = new StockLocal
                    {
                        Id = await _context.GetNextSequenceAsync("stockLocal"),
                        ProductoId = movimiento.ProductoId,
                        RestauranteId = movimiento.RestauranteId,
                        Lote = movimiento.Lote,
                        Cantidad = movimiento.Cantidad,
                        CostoUnitario = movimiento.CostoUnitario ?? 0,
                        FechaEntrada = DateTime.UtcNow
                    };
                    await _context.StockLocal.InsertOneAsync(stock);
                }
                else
                {
                    // Incrementar stock existente
                    var update = Builders<StockLocal>.Update.Inc(s => s.Cantidad, movimiento.Cantidad);
                    await _context.StockLocal.UpdateOneAsync(s => s.Id == stock.Id, update);
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
                var newQuantity = stock.Cantidad - movimiento.Cantidad;

                // Si el stock llega a 0, eliminar el registro
                if (newQuantity == 0)
                {
                    await _context.StockLocal.DeleteOneAsync(s => s.Id == stock.Id);
                }
                else
                {
                    var update = Builders<StockLocal>.Update.Set(s => s.Cantidad, newQuantity);
                    await _context.StockLocal.UpdateOneAsync(s => s.Id == stock.Id, update);
                }
            }

            // Si es una transferencia, actualizar stock del restaurante destino
            if (movimiento.RestauranteDestinoId.HasValue && movimiento.Tipo == "Salida")
            {
                var stockDestino = await _context.StockLocal
                    .Find(s =>
                        s.ProductoId == movimiento.ProductoId &&
                        s.RestauranteId == movimiento.RestauranteDestinoId.Value &&
                        s.Lote == movimiento.Lote)
                    .FirstOrDefaultAsync();

                if (stockDestino == null)
                {
                    // Crear stock en restaurante destino
                    stockDestino = new StockLocal
                    {
                        Id = await _context.GetNextSequenceAsync("stockLocal"),
                        ProductoId = movimiento.ProductoId,
                        RestauranteId = movimiento.RestauranteDestinoId.Value,
                        Lote = movimiento.Lote,
                        Cantidad = movimiento.Cantidad,
                        CostoUnitario = movimiento.CostoUnitario ?? 0,
                        FechaEntrada = DateTime.UtcNow
                    };
                    await _context.StockLocal.InsertOneAsync(stockDestino);
                }
                else
                {
                    // Incrementar stock en restaurante destino
                    var update = Builders<StockLocal>.Update.Inc(s => s.Cantidad, movimiento.Cantidad);
                    await _context.StockLocal.UpdateOneAsync(s => s.Id == stockDestino.Id, update);
                }
            }
        }
    }
}
