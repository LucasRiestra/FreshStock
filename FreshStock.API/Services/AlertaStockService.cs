using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Enums;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class AlertaStockService : IAlertaStockService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;

        public AlertaStockService(MongoDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AlertaStockResponseDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            var alertas = await _context.AlertasStock
                .Find(a => a.RestauranteId == restauranteId)
                .SortByDescending(a => a.FechaCreacion)
                .ToListAsync();

            return await EnrichAlertasAsync(alertas);
        }

        public async Task<IEnumerable<AlertaStockResponseDTO>> GetNoLeidasByRestauranteIdAsync(int restauranteId)
        {
            var alertas = await _context.AlertasStock
                .Find(a => a.RestauranteId == restauranteId && !a.Leida)
                .SortByDescending(a => a.FechaCreacion)
                .ToListAsync();

            return await EnrichAlertasAsync(alertas);
        }

        public async Task<ResumenAlertasDTO> GetResumenByRestauranteIdAsync(int restauranteId)
        {
            var alertas = await _context.AlertasStock
                .Find(a => a.RestauranteId == restauranteId)
                .ToListAsync();

            return new ResumenAlertasDTO
            {
                TotalAlertas = alertas.Count,
                AlertasNoLeidas = alertas.Count(a => !a.Leida),
                AlertasCriticas = alertas.Count(a => a.TipoAlerta == TipoAlerta.StockCritico && !a.Leida),
                AlertasStockBajo = alertas.Count(a => a.TipoAlerta == TipoAlerta.StockBajo && !a.Leida),
                AlertasProximoCaducar = alertas.Count(a => a.TipoAlerta == TipoAlerta.ProximoCaducar && !a.Leida),
                AlertasSinStock = alertas.Count(a => a.TipoAlerta == TipoAlerta.SinStock && !a.Leida)
            };
        }

        public async Task<AlertaStockResponseDTO?> MarcarLeidaAsync(int alertaId, int usuarioId)
        {
            var alerta = await _context.AlertasStock
                .Find(a => a.Id == alertaId)
                .FirstOrDefaultAsync();

            if (alerta == null)
                return null;

            alerta.Leida = true;
            alerta.FechaLectura = DateTime.UtcNow;
            alerta.UsuarioLecturaId = usuarioId;

            await _context.AlertasStock.ReplaceOneAsync(a => a.Id == alertaId, alerta);

            var enriched = await EnrichAlertasAsync(new List<AlertaStock> { alerta });
            return enriched.FirstOrDefault();
        }

        public async Task<int> MarcarVariasLeidasAsync(MarcarAlertasLeidasDTO dto, int usuarioId)
        {
            var update = Builders<AlertaStock>.Update
                .Set(a => a.Leida, true)
                .Set(a => a.FechaLectura, DateTime.UtcNow)
                .Set(a => a.UsuarioLecturaId, usuarioId);

            var result = await _context.AlertasStock.UpdateManyAsync(
                a => dto.AlertaIds.Contains(a.Id),
                update
            );

            return (int)result.ModifiedCount;
        }

        public async Task<GeneracionAlertasResultDTO> GenerarAlertasAsync(int restauranteId)
        {
            var resultado = new GeneracionAlertasResultDTO();

            // Obtener último inventario completado
            var ultimoInventario = await _context.Inventarios
                .Find(i => i.RestauranteId == restauranteId && i.Estado == EstadoInventario.Completado)
                .SortByDescending(i => i.FechaFin)
                .FirstOrDefaultAsync();

            if (ultimoInventario == null)
                return resultado;

            // Obtener detalles del inventario
            var detalles = await _context.InventarioDetalles
                .Find(d => d.InventarioId == ultimoInventario.Id)
                .ToListAsync();
            var detallesDict = detalles.ToDictionary(d => d.ProductoId);

            // Obtener configuraciones de stock ideal
            var stockIdeales = await _context.StockIdealRestaurantes
                .Find(s => s.RestauranteId == restauranteId && s.Activo)
                .ToListAsync();

            // Obtener productos
            var productoIds = stockIdeales.Select(s => s.ProductoId).ToList();
            var productos = await _context.Productos
                .Find(p => productoIds.Contains(p.Id))
                .ToListAsync();
            var productosDict = productos.ToDictionary(p => p.Id);

            // Eliminar alertas anteriores no leídas para regenerar
            await _context.AlertasStock.DeleteManyAsync(
                a => a.RestauranteId == restauranteId && !a.Leida
            );

            var nuevasAlertas = new List<AlertaStock>();

            foreach (var stockIdeal in stockIdeales)
            {
                if (!productosDict.TryGetValue(stockIdeal.ProductoId, out var producto))
                    continue;

                var detalle = detallesDict.GetValueOrDefault(stockIdeal.ProductoId);
                var stockReal = detalle?.CantidadContada ?? 0;

                // Verificar si hay alerta
                TipoAlerta? tipoAlerta = null;
                string mensaje = "";

                if (stockReal == 0)
                {
                    tipoAlerta = TipoAlerta.SinStock;
                    mensaje = $"Sin stock de {producto.Nombre}";
                    resultado.AlertasSinStock++;
                }
                else if (stockReal < stockIdeal.StockMinimo)
                {
                    tipoAlerta = TipoAlerta.StockCritico;
                    mensaje = $"Stock crítico de {producto.Nombre}: {stockReal} {producto.UnidadMedida} (mínimo: {stockIdeal.StockMinimo})";
                    resultado.AlertasStockCritico++;
                }
                else if (stockReal < stockIdeal.StockIdeal * 0.5m)
                {
                    tipoAlerta = TipoAlerta.StockBajo;
                    mensaje = $"Stock bajo de {producto.Nombre}: {stockReal} {producto.UnidadMedida} (ideal: {stockIdeal.StockIdeal})";
                    resultado.AlertasStockBajo++;
                }

                if (tipoAlerta.HasValue)
                {
                    var alerta = new AlertaStock
                    {
                        Id = await _context.GetNextSequenceAsync("alertasStock"),
                        ProductoId = stockIdeal.ProductoId,
                        RestauranteId = restauranteId,
                        TipoAlerta = tipoAlerta.Value,
                        Mensaje = mensaje,
                        StockActual = stockReal,
                        StockMinimo = stockIdeal.StockMinimo,
                        FechaCreacion = DateTime.UtcNow,
                        Leida = false
                    };
                    nuevasAlertas.Add(alerta);
                }
            }

            // Verificar productos próximos a caducar (StockLocal)
            var stockLocal = await _context.StockLocal
                .Find(s => s.RestauranteId == restauranteId && s.FechaCaducidad != null)
                .ToListAsync();

            var fechaLimite = DateTime.UtcNow.AddDays(7);
            var proximosCaducar = stockLocal
                .Where(s => s.FechaCaducidad <= fechaLimite && s.Cantidad > 0)
                .ToList();

            foreach (var stock in proximosCaducar)
            {
                var producto = productosDict.GetValueOrDefault(stock.ProductoId);
                var diasRestantes = (stock.FechaCaducidad!.Value - DateTime.UtcNow).Days;

                var alerta = new AlertaStock
                {
                    Id = await _context.GetNextSequenceAsync("alertasStock"),
                    ProductoId = stock.ProductoId,
                    RestauranteId = restauranteId,
                    TipoAlerta = TipoAlerta.ProximoCaducar,
                    Mensaje = $"{producto?.Nombre ?? "Producto"} caduca en {diasRestantes} días (Lote: {stock.Lote})",
                    StockActual = stock.Cantidad,
                    FechaCaducidad = stock.FechaCaducidad,
                    FechaCreacion = DateTime.UtcNow,
                    Leida = false
                };
                nuevasAlertas.Add(alerta);
                resultado.AlertasProximoCaducar++;
            }

            if (nuevasAlertas.Any())
            {
                await _context.AlertasStock.InsertManyAsync(nuevasAlertas);
            }

            resultado.AlertasGeneradas = nuevasAlertas.Count;
            return resultado;
        }

        private async Task<List<AlertaStockResponseDTO>> EnrichAlertasAsync(List<AlertaStock> alertas)
        {
            if (!alertas.Any())
                return new List<AlertaStockResponseDTO>();

            var productoIds = alertas.Select(a => a.ProductoId).Distinct().ToList();
            var restauranteIds = alertas.Select(a => a.RestauranteId).Distinct().ToList();

            var productos = await _context.Productos
                .Find(p => productoIds.Contains(p.Id))
                .ToListAsync();
            var productosDict = productos.ToDictionary(p => p.Id);

            var restaurantes = await _context.Restaurantes
                .Find(r => restauranteIds.Contains(r.Id))
                .ToListAsync();
            var restaurantesDict = restaurantes.ToDictionary(r => r.Id);

            return alertas.Select(a =>
            {
                var producto = productosDict.GetValueOrDefault(a.ProductoId);
                var restaurante = restaurantesDict.GetValueOrDefault(a.RestauranteId);

                return new AlertaStockResponseDTO
                {
                    Id = a.Id,
                    ProductoId = a.ProductoId,
                    RestauranteId = a.RestauranteId,
                    TipoAlerta = a.TipoAlerta,
                    Mensaje = a.Mensaje,
                    StockActual = a.StockActual,
                    StockMinimo = a.StockMinimo,
                    FechaCaducidad = a.FechaCaducidad,
                    FechaCreacion = a.FechaCreacion,
                    Leida = a.Leida,
                    FechaLectura = a.FechaLectura,
                    UsuarioLecturaId = a.UsuarioLecturaId,
                    NombreProducto = producto?.Nombre,
                    NombreRestaurante = restaurante?.Nombre,
                    UnidadMedida = producto?.UnidadMedida
                };
            }).ToList();
        }
    }
}
