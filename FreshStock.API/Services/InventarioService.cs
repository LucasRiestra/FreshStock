using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Enums;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class InventarioService : IInventarioService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;
        private readonly IAlertaStockService _alertaService;
        private readonly IEmailService _emailService;

        public InventarioService(
            MongoDbContext context,
            IMapper mapper,
            IAlertaStockService alertaService,
            IEmailService emailService)
        {
            _context = context;
            _mapper = mapper;
            _alertaService = alertaService;
            _emailService = emailService;
        }

        #region Gestión de Inventarios

        public async Task<InventarioResponseDTO> CreateAsync(CreateInventarioDTO dto, int usuarioId)
        {
            // Verificar que no haya otro inventario en progreso para este restaurante
            var inventarioEnProgreso = await _context.Inventarios
                .Find(i => i.RestauranteId == dto.RestauranteId && i.Estado == EstadoInventario.EnProgreso)
                .FirstOrDefaultAsync();

            if (inventarioEnProgreso != null)
            {
                throw new InvalidOperationException($"Ya existe un inventario en progreso para este restaurante (ID: {inventarioEnProgreso.Id})");
            }

            var inventario = new Inventario
            {
                Id = await _context.GetNextSequenceAsync("inventarios"),
                RestauranteId = dto.RestauranteId,
                Nombre = dto.Nombre,
                FechaInicio = DateTime.UtcNow,
                Estado = EstadoInventario.EnProgreso,
                UsuarioId = usuarioId,
                Notas = dto.Notas
            };

            await _context.Inventarios.InsertOneAsync(inventario);

            return await BuildInventarioResponseAsync(inventario);
        }

        public async Task<InventarioResponseDTO?> GetByIdAsync(int id)
        {
            var inventario = await _context.Inventarios
                .Find(i => i.Id == id)
                .FirstOrDefaultAsync();

            if (inventario == null)
                return null;

            return await BuildInventarioResponseAsync(inventario);
        }

        public async Task<IEnumerable<InventarioResumenDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            var inventarios = await _context.Inventarios
                .Find(i => i.RestauranteId == restauranteId)
                .SortByDescending(i => i.FechaInicio)
                .ToListAsync();

            var result = new List<InventarioResumenDTO>();
            foreach (var inv in inventarios)
            {
                var contados = await _context.InventarioDetalles
                    .CountDocumentsAsync(d => d.InventarioId == inv.Id);

                result.Add(new InventarioResumenDTO
                {
                    Id = inv.Id,
                    Nombre = inv.Nombre,
                    FechaInicio = inv.FechaInicio,
                    FechaFin = inv.FechaFin,
                    Estado = inv.Estado,
                    ProductosContados = (int)contados
                });
            }

            return result;
        }

        public async Task<InventarioResponseDTO?> GetActualByRestauranteIdAsync(int restauranteId)
        {
            var inventario = await _context.Inventarios
                .Find(i => i.RestauranteId == restauranteId && i.Estado == EstadoInventario.Completado)
                .SortByDescending(i => i.FechaFin)
                .FirstOrDefaultAsync();

            if (inventario == null)
                return null;

            return await BuildInventarioResponseAsync(inventario);
        }

        public async Task<InventarioResponseDTO?> FinalizarAsync(int inventarioId, FinalizarInventarioDTO? dto)
        {
            var inventario = await _context.Inventarios
                .Find(i => i.Id == inventarioId)
                .FirstOrDefaultAsync();

            if (inventario == null)
                return null;

            if (inventario.Estado != EstadoInventario.EnProgreso)
            {
                throw new InvalidOperationException("Solo se pueden finalizar inventarios en progreso");
            }

            // Finalizar el inventario
            inventario.Estado = EstadoInventario.Completado;
            inventario.FechaFin = DateTime.UtcNow;
            if (dto?.Notas != null)
            {
                inventario.Notas = dto.Notas;
            }

            await _context.Inventarios.ReplaceOneAsync(i => i.Id == inventarioId, inventario);

            // Generar alertas y enviar emails automáticamente
            var resultadoAlertas = await _alertaService.GenerarAlertasAsync(inventario.RestauranteId);
            await _emailService.SendAlertaStockEmailAsync(inventario.RestauranteId, resultadoAlertas);

            // Si se solicitó actualizar el stock (para inventarios físicos que hacen "reset" del stock)
            if (dto?.ActualizarStock == true)
            {
                await ActualizarStockDesdeInventarioAsync(inventario);
            }

            return await BuildInventarioResponseAsync(inventario);
        }

        public async Task<InventarioResponseDTO?> CancelarAsync(int inventarioId)
        {
            var inventario = await _context.Inventarios
                .Find(i => i.Id == inventarioId)
                .FirstOrDefaultAsync();

            if (inventario == null)
                return null;

            if (inventario.Estado != EstadoInventario.EnProgreso)
            {
                throw new InvalidOperationException("Solo se pueden cancelar inventarios en progreso");
            }

            inventario.Estado = EstadoInventario.Cancelado;
            inventario.FechaFin = DateTime.UtcNow;

            await _context.Inventarios.ReplaceOneAsync(i => i.Id == inventarioId, inventario);

            return await BuildInventarioResponseAsync(inventario);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var inventario = await _context.Inventarios
                .Find(i => i.Id == id)
                .FirstOrDefaultAsync();

            if (inventario == null)
                return false;

            // Eliminar detalles asociados
            await _context.InventarioDetalles.DeleteManyAsync(d => d.InventarioId == id);

            // Eliminar cabecera del inventario
            var result = await _context.Inventarios.DeleteOneAsync(i => i.Id == id);

            return result.DeletedCount > 0;
        }

        #endregion

        #region Navegación para Conteo

        public async Task<IEnumerable<CategoriaConteoDTO>> GetCategoriasAsync(int inventarioId)
        {
            var inventario = await _context.Inventarios
                .Find(i => i.Id == inventarioId)
                .FirstOrDefaultAsync();

            if (inventario == null)
                throw new InvalidOperationException("Inventario no encontrado");

            // Obtener categorías asignadas al restaurante
            var categoriasAsignadas = await _context.RestauranteCategorias
                .Find(rc => rc.RestauranteId == inventario.RestauranteId && rc.Activo)
                .ToListAsync();

            var categoriaIds = categoriasAsignadas.Select(c => c.CategoriaId).ToList();

            // Obtener detalles de las categorías
            var categorias = await _context.Categorias
                .Find(c => categoriaIds.Contains(c.Id))
                .ToListAsync();

            // Obtener productos por categoría (de proveedores asignados al restaurante)
            var proveedoresAsignados = await _context.RestauranteProveedores
                .Find(rp => rp.RestauranteId == inventario.RestauranteId && rp.Activo)
                .ToListAsync();
            var proveedorIds = proveedoresAsignados.Select(p => p.ProveedorId).ToList();

            var productos = await _context.Productos
                .Find(p => categoriaIds.Contains(p.CategoriaId) && proveedorIds.Contains(p.ProveedorId) && p.Activo)
                .ToListAsync();

            // Obtener conteos ya realizados
            var detalles = await _context.InventarioDetalles
                .Find(d => d.InventarioId == inventarioId)
                .ToListAsync();
            var productosContados = detalles.Select(d => d.ProductoId).ToHashSet();

            var result = categorias.Select(cat =>
            {
                var productosCategoria = productos.Where(p => p.CategoriaId == cat.Id).ToList();
                var contados = productosCategoria.Count(p => productosContados.Contains(p.Id));

                return new CategoriaConteoDTO
                {
                    Id = cat.Id,
                    Nombre = cat.Nombre,
                    TotalProductos = productosCategoria.Count,
                    ProductosContados = contados,
                    Completada = productosCategoria.Count > 0 && contados == productosCategoria.Count
                };
            }).ToList();

            return result;
        }

        public async Task<IEnumerable<ProveedorConteoDTO>> GetProveedoresByCategoriaAsync(int inventarioId, int categoriaId)
        {
            var inventario = await _context.Inventarios
                .Find(i => i.Id == inventarioId)
                .FirstOrDefaultAsync();

            if (inventario == null)
                throw new InvalidOperationException("Inventario no encontrado");

            // Obtener proveedores asignados al restaurante
            var proveedoresAsignados = await _context.RestauranteProveedores
                .Find(rp => rp.RestauranteId == inventario.RestauranteId && rp.Activo)
                .ToListAsync();
            var proveedorIds = proveedoresAsignados.Select(p => p.ProveedorId).ToList();

            // Obtener productos de la categoría con esos proveedores
            var productos = await _context.Productos
                .Find(p => p.CategoriaId == categoriaId && proveedorIds.Contains(p.ProveedorId) && p.Activo)
                .ToListAsync();

            // Obtener proveedores que tienen productos en esta categoría
            var proveedorIdsConProductos = productos.Select(p => p.ProveedorId).Distinct().ToList();
            var proveedores = await _context.Proveedores
                .Find(p => proveedorIdsConProductos.Contains(p.Id))
                .ToListAsync();

            // Obtener conteos ya realizados
            var detalles = await _context.InventarioDetalles
                .Find(d => d.InventarioId == inventarioId && d.CategoriaId == categoriaId)
                .ToListAsync();
            var productosContados = detalles.Select(d => d.ProductoId).ToHashSet();

            var result = proveedores.Select(prov =>
            {
                var productosProveedor = productos.Where(p => p.ProveedorId == prov.Id).ToList();
                var contados = productosProveedor.Count(p => productosContados.Contains(p.Id));

                return new ProveedorConteoDTO
                {
                    Id = prov.Id,
                    Nombre = prov.Nombre,
                    TotalProductos = productosProveedor.Count,
                    ProductosContados = contados,
                    Completado = productosProveedor.Count > 0 && contados == productosProveedor.Count
                };
            }).ToList();

            return result;
        }

        public async Task<IEnumerable<ProductoConteoDTO>> GetProductosByProveedorAsync(int inventarioId, int proveedorId)
        {
            var inventario = await _context.Inventarios
                .Find(i => i.Id == inventarioId)
                .FirstOrDefaultAsync();

            if (inventario == null)
                throw new InvalidOperationException("Inventario no encontrado");

            // Obtener productos del proveedor
            var productos = await _context.Productos
                .Find(p => p.ProveedorId == proveedorId && p.Activo)
                .ToListAsync();

            // Obtener conteos ya realizados
            var detalles = await _context.InventarioDetalles
                .Find(d => d.InventarioId == inventarioId && d.ProveedorId == proveedorId)
                .ToListAsync();
            var detallesDict = detalles.ToDictionary(d => d.ProductoId);

            // Obtener stock actual del sistema para referencia
            var productoIds = productos.Select(p => p.Id).ToList();
            var stockActual = await _context.StockLocal
                .Find(s => s.RestauranteId == inventario.RestauranteId && productoIds.Contains(s.ProductoId))
                .ToListAsync();
            var stockPorProducto = stockActual
                .GroupBy(s => s.ProductoId)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Cantidad));

            // Obtener stock ideal para referencia
            var stockIdeales = await _context.StockIdealRestaurantes
                .Find(s => s.RestauranteId == inventario.RestauranteId && productoIds.Contains(s.ProductoId) && s.Activo)
                .ToListAsync();
            var idealDict = stockIdeales.ToDictionary(s => s.ProductoId);

            var result = productos.Select(prod =>
            {
                var detalle = detallesDict.GetValueOrDefault(prod.Id);
                var cantidadSistema = stockPorProducto.GetValueOrDefault(prod.Id, 0);
                var ideal = idealDict.GetValueOrDefault(prod.Id);

                return new ProductoConteoDTO
                {
                    Id = prod.Id,
                    Nombre = prod.Nombre,
                    UnidadMedida = prod.UnidadMedida,
                    ProveedorId = prod.ProveedorId,
                    CategoriaId = prod.CategoriaId,
                    CantidadSistema = cantidadSistema,
                    StockIdeal = ideal?.StockIdeal,
                    StockMinimo = ideal?.StockMinimo,
                    CantidadContada = detalle?.CantidadContada,
                    YaContado = detalle != null,
                    Observacion = detalle?.Observacion
                };
            }).ToList();

            return result;
        }

        #endregion

        #region Registro de Conteos

        public async Task<InventarioDetalleResponseDTO> RegistrarConteoAsync(int inventarioId, CreateInventarioDetalleDTO dto)
        {
            var inventario = await _context.Inventarios
                .Find(i => i.Id == inventarioId)
                .FirstOrDefaultAsync();

            if (inventario == null)
                throw new InvalidOperationException("Inventario no encontrado");

            if (inventario.Estado != EstadoInventario.EnProgreso)
                throw new InvalidOperationException("Solo se pueden registrar conteos en inventarios en progreso");

            return await RegistrarConteoInternoAsync(inventario, dto);
        }

        public async Task<IEnumerable<InventarioDetalleResponseDTO>> RegistrarConteosBulkAsync(int inventarioId, CreateInventarioDetalleBulkDTO dto)
        {
            var resultados = new List<InventarioDetalleResponseDTO>();
            
            var inventario = await _context.Inventarios
                .Find(i => i.Id == inventarioId)
                .FirstOrDefaultAsync();

            if (inventario == null)
                throw new InvalidOperationException($"Inventario con ID {inventarioId} no encontrado");

            if (inventario.Estado != EstadoInventario.EnProgreso)
                throw new InvalidOperationException("Solo se pueden registrar conteos en inventarios en progreso");

            foreach (var conteo in dto.Conteos)
            {
                try 
                {
                    // Llamamos a un método interno que no vuelva a verificar el inventario
                    var resultado = await RegistrarConteoInternoAsync(inventario, conteo);
                    resultados.Add(resultado);
                }
                catch (Exception ex)
                {
                    // Log the error for this specific product but continue with others if needed?
                    // For now, if any fails in bulk, we might want to know which one.
                    throw new InvalidOperationException($"Error procesando producto {conteo.ProductoId}: {ex.Message}");
                }
            }

            return resultados;
        }

        private async Task<InventarioDetalleResponseDTO> RegistrarConteoInternoAsync(Inventario inventario, CreateInventarioDetalleDTO dto)
        {
            // Obtener el producto
            var producto = await _context.Productos
                .Find(p => p.Id == dto.ProductoId)
                .FirstOrDefaultAsync();

            if (producto == null)
                throw new InvalidOperationException($"Producto con ID {dto.ProductoId} no encontrado");

            // Verificar si ya existe un conteo para este producto
            var existente = await _context.InventarioDetalles
                .Find(d => d.InventarioId == inventario.Id && d.ProductoId == dto.ProductoId)
                .FirstOrDefaultAsync();

            if (existente != null)
            {
                // Update existing
                existente.CantidadContada = dto.CantidadContada;
                existente.Observacion = dto.Observacion;
                existente.FechaConteo = DateTime.UtcNow;
                
                // Recalcular diferencia
                existente.Diferencia = dto.CantidadContada - (existente.CantidadSistema ?? 0);

                await _context.InventarioDetalles.ReplaceOneAsync(d => d.Id == existente.Id, existente);
                return await BuildDetalleResponseAsync(existente);
            }

            // Create new
            var stockActual = await _context.StockLocal
                .Find(s => s.RestauranteId == inventario.RestauranteId && s.ProductoId == dto.ProductoId)
                .ToListAsync();
            var cantidadSistema = stockActual.Sum(s => s.Cantidad);

            var detalle = new InventarioDetalle
            {
                Id = await _context.GetNextSequenceAsync("inventarioDetalles"),
                InventarioId = inventario.Id,
                ProductoId = dto.ProductoId,
                ProveedorId = producto.ProveedorId,
                CategoriaId = producto.CategoriaId,
                CantidadContada = dto.CantidadContada,
                CantidadSistema = cantidadSistema,
                Diferencia = dto.CantidadContada - cantidadSistema,
                Observacion = dto.Observacion,
                FechaConteo = DateTime.UtcNow
            };

            await _context.InventarioDetalles.InsertOneAsync(detalle);
            return await BuildDetalleResponseAsync(detalle);
        }

        public async Task<InventarioDetalleResponseDTO?> ActualizarConteoAsync(int inventarioId, UpdateInventarioDetalleDTO dto)
        {
            var inventario = await _context.Inventarios
                .Find(i => i.Id == inventarioId)
                .FirstOrDefaultAsync();

            if (inventario == null)
                throw new InvalidOperationException("Inventario no encontrado");

            if (inventario.Estado != EstadoInventario.EnProgreso)
                throw new InvalidOperationException("Solo se pueden actualizar conteos en inventarios en progreso");

            var detalle = await _context.InventarioDetalles
                .Find(d => d.Id == dto.Id && d.InventarioId == inventarioId)
                .FirstOrDefaultAsync();

            if (detalle == null)
                return null;

            detalle.CantidadContada = dto.CantidadContada;
            detalle.Diferencia = dto.CantidadContada - (detalle.CantidadSistema ?? 0);
            detalle.Observacion = dto.Observacion;
            detalle.FechaConteo = DateTime.UtcNow;

            await _context.InventarioDetalles.ReplaceOneAsync(d => d.Id == dto.Id, detalle);

            return await BuildDetalleResponseAsync(detalle);
        }

        #endregion

        #region Progreso y Detalles

        public async Task<ProgresoInventarioDTO> GetProgresoAsync(int inventarioId)
        {
            var inventario = await _context.Inventarios
                .Find(i => i.Id == inventarioId)
                .FirstOrDefaultAsync();

            if (inventario == null)
                throw new InvalidOperationException("Inventario no encontrado");

            // Obtener total de productos disponibles para este restaurante
            var proveedoresAsignados = await _context.RestauranteProveedores
                .Find(rp => rp.RestauranteId == inventario.RestauranteId && rp.Activo)
                .ToListAsync();
            var proveedorIds = proveedoresAsignados.Select(p => p.ProveedorId).ToList();

            var categoriasAsignadas = await _context.RestauranteCategorias
                .Find(rc => rc.RestauranteId == inventario.RestauranteId && rc.Activo)
                .ToListAsync();
            var categoriaIds = categoriasAsignadas.Select(c => c.CategoriaId).ToList();

            var productos = await _context.Productos
                .Find(p => proveedorIds.Contains(p.ProveedorId) && categoriaIds.Contains(p.CategoriaId) && p.Activo)
                .ToListAsync();

            var totalProductos = productos.Count;

            // Obtener productos contados
            var detalles = await _context.InventarioDetalles
                .Find(d => d.InventarioId == inventarioId)
                .ToListAsync();
            var productosContados = detalles.Count;
            var productosContadosIds = detalles.Select(d => d.ProductoId).ToHashSet();

            // Calcular progreso por categoría
            var categorias = await _context.Categorias
                .Find(c => categoriaIds.Contains(c.Id))
                .ToListAsync();

            var categoriasPendientes = categorias.Select(cat =>
            {
                var productosCategoria = productos.Where(p => p.CategoriaId == cat.Id).ToList();
                var pendientes = productosCategoria.Count(p => !productosContadosIds.Contains(p.Id));

                return new CategoriasProgresoDTO
                {
                    CategoriaId = cat.Id,
                    NombreCategoria = cat.Nombre,
                    Pendientes = pendientes
                };
            }).Where(c => c.Pendientes > 0).ToList();

            var porcentaje = totalProductos > 0 ? (decimal)productosContados / totalProductos * 100 : 0;

            return new ProgresoInventarioDTO
            {
                InventarioId = inventarioId,
                TotalProductos = totalProductos,
                ProductosContados = productosContados,
                PorcentajeProgreso = Math.Round(porcentaje, 2),
                CategoriasPendientes = categoriasPendientes
            };
        }

        public async Task<IEnumerable<InventarioDetalleResponseDTO>> GetDetallesAsync(int inventarioId)
        {
            var inventario = await _context.Inventarios.Find(i => i.Id == inventarioId).FirstOrDefaultAsync();
            if (inventario == null) return new List<InventarioDetalleResponseDTO>();

            var detalles = await _context.InventarioDetalles
                .Find(d => d.InventarioId == inventarioId)
                .ToListAsync();

            var productoIds = detalles.Select(d => d.ProductoId).Distinct().ToList();
            
            // Obtener stock ideal para referencia
            var stockIdeales = await _context.StockIdealRestaurantes
                .Find(s => s.RestauranteId == inventario.RestauranteId && productoIds.Contains(s.ProductoId) && s.Activo)
                .ToListAsync();
            var idealDict = stockIdeales.ToDictionary(s => s.ProductoId);

            var result = new List<InventarioDetalleResponseDTO>();
            foreach (var detalle in detalles)
            {
                var response = await BuildDetalleResponseAsync(detalle);
                if (idealDict.TryGetValue(detalle.ProductoId, out var ideal))
                {
                    response.StockIdeal = ideal.StockIdeal;
                    response.StockMinimo = ideal.StockMinimo;
                }
                result.Add(response);
            }

            return result;
        }

        #endregion

        #region Helpers

        private async Task<InventarioResponseDTO> BuildInventarioResponseAsync(Inventario inventario)
        {
            var restaurante = await _context.Restaurantes.Find(r => r.Id == inventario.RestauranteId).FirstOrDefaultAsync();
            var usuario = await _context.Usuarios.Find(u => u.Id == inventario.UsuarioId).FirstOrDefaultAsync();

            var progreso = await GetProgresoAsync(inventario.Id);

            return new InventarioResponseDTO
            {
                Id = inventario.Id,
                RestauranteId = inventario.RestauranteId,
                Nombre = inventario.Nombre,
                FechaInicio = inventario.FechaInicio,
                FechaFin = inventario.FechaFin,
                Estado = inventario.Estado,
                UsuarioId = inventario.UsuarioId,
                Notas = inventario.Notas,
                NombreRestaurante = restaurante?.Nombre,
                NombreUsuario = usuario?.Nombre,
                TotalProductos = progreso.TotalProductos,
                ProductosContados = progreso.ProductosContados,
                PorcentajeProgreso = progreso.PorcentajeProgreso
            };
        }

        private async Task<InventarioDetalleResponseDTO> BuildDetalleResponseAsync(InventarioDetalle detalle)
        {
            var producto = await _context.Productos.Find(p => p.Id == detalle.ProductoId).FirstOrDefaultAsync();
            var proveedor = await _context.Proveedores.Find(p => p.Id == detalle.ProveedorId).FirstOrDefaultAsync();
            var categoria = await _context.Categorias.Find(c => c.Id == detalle.CategoriaId).FirstOrDefaultAsync();
            
            // Obtener stock ideal para este restaurante y producto
            var inventario = await _context.Inventarios.Find(i => i.Id == detalle.InventarioId).FirstOrDefaultAsync();
            StockIdealRestaurante? ideal = null;
            if (inventario != null)
            {
                ideal = await _context.StockIdealRestaurantes
                    .Find(s => s.RestauranteId == inventario.RestauranteId && s.ProductoId == detalle.ProductoId && s.Activo)
                    .FirstOrDefaultAsync();
            }

            var response = new InventarioDetalleResponseDTO
            {
                Id = detalle.Id,
                InventarioId = detalle.InventarioId,
                ProductoId = detalle.ProductoId,
                ProveedorId = detalle.ProveedorId,
                CategoriaId = detalle.CategoriaId,
                CantidadContada = detalle.CantidadContada,
                CantidadSistema = detalle.CantidadSistema,
                StockIdeal = ideal?.StockIdeal,
                StockMinimo = ideal?.StockMinimo,
                Diferencia = detalle.Diferencia,
                Observacion = detalle.Observacion,
                FechaConteo = detalle.FechaConteo,
                NombreProducto = producto?.Nombre,
                NombreProveedor = proveedor?.Nombre,
                NombreCategoria = categoria?.Nombre,
                UnidadMedida = producto?.UnidadMedida
            };

            return response;
        }

        private async Task ActualizarStockDesdeInventarioAsync(Inventario inventario)
        {
            // Obtener todos los detalles del inventario
            var detalles = await _context.InventarioDetalles
                .Find(d => d.InventarioId == inventario.Id)
                .ToListAsync();

            // Obtener lista de productos para saber sus costos
            var productoIds = detalles.Select(d => d.ProductoId).Distinct().ToList();
            var productos = await _context.Productos
                .Find(p => productoIds.Contains(p.Id))
                .ToListAsync();
            var productosDict = productos.ToDictionary(p => p.Id);

            foreach (var detalle in detalles)
            {
                // Buscar si existe stock para este producto en este restaurante
                // Asumimos Lote "INVENTARIO" y FechaCaducidad null por defecto al venir de un conteo general
                // En una versión más avanzada, el conteo podría incluir lote y caducidad
                
                var stock = await _context.StockLocal
                    .Find(s => s.RestauranteId == inventario.RestauranteId && s.ProductoId == detalle.ProductoId)
                    .FirstOrDefaultAsync();

                if (stock != null)
                {
                    // Si existe, actualizamos la cantidad directamente (Sobrescritura)
                    var diferencia = detalle.CantidadContada - stock.Cantidad;
                    
                    if (diferencia != 0)
                    {
                        var update = Builders<StockLocal>.Update
                            .Set(s => s.Cantidad, detalle.CantidadContada);
                            
                        await _context.StockLocal.UpdateOneAsync(s => s.Id == stock.Id, update);

                        // Registrar Movimiento de Ajuste
                        var movimiento = new MovimientoInventario
                        {
                            Id = await _context.GetNextSequenceAsync("movimientosInventario"),
                            Tipo = diferencia > 0 ? "Entrada" : "Salida",
                            ProductoId = detalle.ProductoId,
                            RestauranteId = inventario.RestauranteId,
                            Cantidad = Math.Abs(diferencia),
                            Lote = stock.Lote ?? "SIN_LOTE",
                            Motivo = $"Ajuste por Inventario #{inventario.Id}",
                            CostoUnitario = stock.CostoUnitario,
                            UsuarioId = inventario.UsuarioId,
                            Fecha = DateTime.UtcNow
                        };
                        await _context.MovimientosInventario.InsertOneAsync(movimiento);
                    }
                }
                else if (detalle.CantidadContada > 0)
                {
                    // Si no existe y se contó algo, creamos el stock
                    var producto = productosDict.ContainsKey(detalle.ProductoId) ? productosDict[detalle.ProductoId] : null;
                    
                    var newStock = new StockLocal
                    {
                        Id = await _context.GetNextSequenceAsync("stockLocal"),
                        ProductoId = detalle.ProductoId,
                        RestauranteId = inventario.RestauranteId,
                        Cantidad = detalle.CantidadContada,
                        Lote = "INICIAL", // Lote por defecto
                        CostoUnitario = producto?.CostoUnitario ?? 0,
                        FechaEntrada = DateTime.UtcNow
                    };
                    
                    await _context.StockLocal.InsertOneAsync(newStock);

                    // Registrar Movimiento de Entrada Inicial
                    var movimiento = new MovimientoInventario
                    {
                        Id = await _context.GetNextSequenceAsync("movimientosInventario"),
                        Tipo = "Entrada",
                        ProductoId = detalle.ProductoId,
                        RestauranteId = inventario.RestauranteId,
                        Cantidad = detalle.CantidadContada,
                        Lote = "INICIAL",
                        Motivo = $"Inventario Inicial #{inventario.Id}",
                        CostoUnitario = producto?.CostoUnitario ?? 0,
                        UsuarioId = inventario.UsuarioId,
                        Fecha = DateTime.UtcNow
                    };
                    await _context.MovimientosInventario.InsertOneAsync(movimiento);
                }
            }
        }

        #endregion
    }
}
