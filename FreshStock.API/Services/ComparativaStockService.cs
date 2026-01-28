using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Enums;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class ComparativaStockService : IComparativaStockService
    {
        private readonly MongoDbContext _context;

        public ComparativaStockService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<ComparativaStockDTO> GetComparativaByRestauranteAsync(int restauranteId)
        {
            var restaurante = await _context.Restaurantes
                .Find(r => r.Id == restauranteId)
                .FirstOrDefaultAsync();

            if (restaurante == null)
                throw new InvalidOperationException("Restaurante no encontrado");

            // Obtener último inventario completado
            var ultimoInventario = await _context.Inventarios
                .Find(i => i.RestauranteId == restauranteId && i.Estado == EstadoInventario.Completado)
                .SortByDescending(i => i.FechaFin)
                .FirstOrDefaultAsync();

            // Obtener configuraciones de stock ideal
            var stockIdeales = await _context.StockIdealRestaurantes
                .Find(s => s.RestauranteId == restauranteId && s.Activo)
                .ToListAsync();

            // Obtener detalles del último inventario
            var detallesInventario = ultimoInventario != null
                ? await _context.InventarioDetalles
                    .Find(d => d.InventarioId == ultimoInventario.Id)
                    .ToListAsync()
                : new List<Entities.InventarioDetalle>();

            var detallesDict = detallesInventario.ToDictionary(d => d.ProductoId);

            // Obtener productos, proveedores y categorías
            var productoIds = stockIdeales.Select(s => s.ProductoId).ToList();
            var productos = await _context.Productos
                .Find(p => productoIds.Contains(p.Id))
                .ToListAsync();
            var productosDict = productos.ToDictionary(p => p.Id);

            var proveedorIds = productos.Select(p => p.ProveedorId).Distinct().ToList();
            var proveedores = await _context.Proveedores
                .Find(p => proveedorIds.Contains(p.Id))
                .ToListAsync();
            var proveedoresDict = proveedores.ToDictionary(p => p.Id);

            var categoriaIds = productos.Select(p => p.CategoriaId).Distinct().ToList();
            var categorias = await _context.Categorias
                .Find(c => categoriaIds.Contains(c.Id))
                .ToListAsync();
            var categoriasDict = categorias.ToDictionary(c => c.Id);

            // Construir comparativa
            var comparativaProductos = new List<ComparativaProductoDTO>();

            foreach (var stockIdeal in stockIdeales)
            {
                if (!productosDict.TryGetValue(stockIdeal.ProductoId, out var producto))
                    continue;

                var detalle = detallesDict.GetValueOrDefault(stockIdeal.ProductoId);
                var stockReal = detalle?.CantidadContada ?? 0;

                var comparativa = new ComparativaProductoDTO
                {
                    ProductoId = producto.Id,
                    NombreProducto = producto.Nombre,
                    UnidadMedida = producto.UnidadMedida,
                    ProveedorId = producto.ProveedorId,
                    NombreProveedor = proveedoresDict.GetValueOrDefault(producto.ProveedorId)?.Nombre ?? "",
                    CategoriaId = producto.CategoriaId,
                    NombreCategoria = categoriasDict.GetValueOrDefault(producto.CategoriaId)?.Nombre ?? "",
                    StockIdeal = stockIdeal.StockIdeal,
                    StockMinimo = stockIdeal.StockMinimo,
                    StockMaximo = stockIdeal.StockMaximo,
                    StockReal = stockReal,
                    DiferenciaIdeal = stockIdeal.StockIdeal - stockReal,
                    DiferenciaAbsoluta = Math.Abs(stockIdeal.StockIdeal - stockReal),
                    Estado = CalcularEstado(stockReal, stockIdeal.StockMinimo, stockIdeal.StockIdeal, stockIdeal.StockMaximo),
                    PorcentajeOcupacion = stockIdeal.StockIdeal > 0
                        ? Math.Round(stockReal / stockIdeal.StockIdeal * 100, 2)
                        : 0
                };

                comparativaProductos.Add(comparativa);
            }

            // Calcular resumen
            var resumen = new ResumenComparativaDTO
            {
                TotalProductos = comparativaProductos.Count,
                ProductosCriticos = comparativaProductos.Count(p => p.Estado == EstadoStock.Critico),
                ProductosBajos = comparativaProductos.Count(p => p.Estado == EstadoStock.Bajo),
                ProductosNormales = comparativaProductos.Count(p => p.Estado == EstadoStock.Normal),
                ProductosExceso = comparativaProductos.Count(p => p.Estado == EstadoStock.Exceso),
                ProductosSinInventariar = stockIdeales.Count - detallesDict.Count
            };

            return new ComparativaStockDTO
            {
                RestauranteId = restauranteId,
                NombreRestaurante = restaurante.Nombre,
                UltimoInventarioId = ultimoInventario?.Id,
                FechaUltimoInventario = ultimoInventario?.FechaFin,
                Productos = comparativaProductos,
                Resumen = resumen
            };
        }

        public async Task<ComparativaStockDTO> GetComparativaByCategoriaAsync(int restauranteId, int categoriaId)
        {
            var comparativaCompleta = await GetComparativaByRestauranteAsync(restauranteId);

            // Filtrar por categoría
            var productosFiltrados = comparativaCompleta.Productos
                .Where(p => p.CategoriaId == categoriaId)
                .ToList();

            // Recalcular resumen
            var resumen = new ResumenComparativaDTO
            {
                TotalProductos = productosFiltrados.Count,
                ProductosCriticos = productosFiltrados.Count(p => p.Estado == EstadoStock.Critico),
                ProductosBajos = productosFiltrados.Count(p => p.Estado == EstadoStock.Bajo),
                ProductosNormales = productosFiltrados.Count(p => p.Estado == EstadoStock.Normal),
                ProductosExceso = productosFiltrados.Count(p => p.Estado == EstadoStock.Exceso)
            };

            return new ComparativaStockDTO
            {
                RestauranteId = comparativaCompleta.RestauranteId,
                NombreRestaurante = comparativaCompleta.NombreRestaurante,
                UltimoInventarioId = comparativaCompleta.UltimoInventarioId,
                FechaUltimoInventario = comparativaCompleta.FechaUltimoInventario,
                Productos = productosFiltrados,
                Resumen = resumen
            };
        }

        public async Task<IEnumerable<ComparativaProductoDTO>> GetProductosCriticosAsync(int restauranteId)
        {
            var comparativa = await GetComparativaByRestauranteAsync(restauranteId);
            return comparativa.Productos
                .Where(p => p.Estado == EstadoStock.Critico)
                .OrderBy(p => p.PorcentajeOcupacion)
                .ToList();
        }

        public async Task<IEnumerable<ComparativaProductoDTO>> GetProductosBajosAsync(int restauranteId)
        {
            var comparativa = await GetComparativaByRestauranteAsync(restauranteId);
            return comparativa.Productos
                .Where(p => p.Estado == EstadoStock.Bajo || p.Estado == EstadoStock.Critico)
                .OrderBy(p => p.PorcentajeOcupacion)
                .ToList();
        }

        public async Task<IEnumerable<HistorialComparativaDTO>> GetHistorialAsync(int restauranteId, DateTime? desde = null)
        {
            var filtroBase = Builders<Entities.Inventario>.Filter.And(
                Builders<Entities.Inventario>.Filter.Eq(i => i.RestauranteId, restauranteId),
                Builders<Entities.Inventario>.Filter.Eq(i => i.Estado, EstadoInventario.Completado)
            );

            if (desde.HasValue)
            {
                filtroBase = Builders<Entities.Inventario>.Filter.And(
                    filtroBase,
                    Builders<Entities.Inventario>.Filter.Gte(i => i.FechaFin, desde.Value)
                );
            }

            var inventarios = await _context.Inventarios
                .Find(filtroBase)
                .SortByDescending(i => i.FechaFin)
                .ToListAsync();

            var historial = new List<HistorialComparativaDTO>();

            // Obtener configuraciones de stock ideal para calcular estadísticas
            var stockIdeales = await _context.StockIdealRestaurantes
                .Find(s => s.RestauranteId == restauranteId && s.Activo)
                .ToListAsync();
            var stockIdealesDict = stockIdeales.ToDictionary(s => s.ProductoId);

            foreach (var inventario in inventarios)
            {
                var detalles = await _context.InventarioDetalles
                    .Find(d => d.InventarioId == inventario.Id)
                    .ToListAsync();

                var criticos = 0;
                var bajos = 0;
                var cumplidos = 0;

                foreach (var detalle in detalles)
                {
                    if (stockIdealesDict.TryGetValue(detalle.ProductoId, out var stockIdeal))
                    {
                        var estado = CalcularEstado(detalle.CantidadContada, stockIdeal.StockMinimo, stockIdeal.StockIdeal, stockIdeal.StockMaximo);
                        if (estado == EstadoStock.Critico) criticos++;
                        else if (estado == EstadoStock.Bajo) bajos++;
                        else if (estado == EstadoStock.Normal) cumplidos++;
                    }
                }

                var totalConConfig = detalles.Count(d => stockIdealesDict.ContainsKey(d.ProductoId));
                var porcentajeCumplimiento = totalConConfig > 0
                    ? Math.Round((decimal)cumplidos / totalConConfig * 100, 2)
                    : 0;

                historial.Add(new HistorialComparativaDTO
                {
                    InventarioId = inventario.Id,
                    NombreInventario = inventario.Nombre,
                    Fecha = inventario.FechaFin ?? inventario.FechaInicio,
                    ProductosContados = detalles.Count,
                    ProductosCriticos = criticos,
                    ProductosBajos = bajos,
                    PorcentajeCumplimiento = porcentajeCumplimiento
                });
            }

            return historial;
        }

        private EstadoStock CalcularEstado(decimal stockReal, decimal stockMinimo, decimal stockIdeal, decimal stockMaximo)
        {
            if (stockReal < stockMinimo)
                return EstadoStock.Critico;

            if (stockReal < stockIdeal * 0.5m)
                return EstadoStock.Bajo;

            if (stockReal > stockMaximo)
                return EstadoStock.Exceso;

            return EstadoStock.Normal;
        }
    }
}
