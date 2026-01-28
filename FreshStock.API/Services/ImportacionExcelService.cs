using ClosedXML.Excel;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class ImportacionExcelService : IImportacionExcelService
    {
        private readonly MongoDbContext _context;
        private readonly ILogger<ImportacionExcelService> _logger;

        // Columnas esperadas en el Excel
        private const int COL_NOMBRE = 1;
        private const int COL_UNIDAD_MEDIDA = 2;
        private const int COL_STOCK_MINIMO = 3;
        private const int COL_COSTO_UNITARIO = 4;
        private const int COL_DESCRIPCION = 5;

        public ImportacionExcelService(MongoDbContext context, ILogger<ImportacionExcelService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ImportacionPreviewResultDTO> PreviewAsync(IFormFile archivo, ImportacionPreviewRequestDTO request)
        {
            var result = new ImportacionPreviewResultDTO();

            using var stream = new MemoryStream();
            await archivo.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            var filaInicio = 2; // Asumimos que la fila 1 tiene headers
            var filaFin = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            // Obtener productos existentes para este proveedor/categoría
            var productosExistentes = await _context.Productos
                .Find(p => p.ProveedorId == request.ProveedorId &&
                          p.CategoriaId == request.CategoriaId &&
                          p.Activo)
                .ToListAsync();
            var nombresExistentes = productosExistentes
                .ToDictionary(p => p.Nombre.ToLowerInvariant(), p => p.Id);

            for (int fila = filaInicio; fila <= filaFin; fila++)
            {
                var row = worksheet.Row(fila);

                // Saltar filas vacías
                if (row.IsEmpty())
                    continue;

                result.TotalFilas++;
                var productoDto = ParsearFila(row, fila, nombresExistentes, result.Errores);

                if (productoDto.TieneError)
                {
                    result.FilasConError++;
                }
                else if (productoDto.EsNuevo)
                {
                    result.ProductosNuevos++;
                }
                else
                {
                    result.ProductosExistentes++;
                }

                result.Productos.Add(productoDto);
            }

            return result;
        }

        public async Task<ImportacionResultDTO> EjecutarAsync(IFormFile archivo, ImportacionEjecutarRequestDTO request)
        {
            var result = new ImportacionResultDTO { Exitoso = true };

            using var stream = new MemoryStream();
            await archivo.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            var filaInicio = 2;
            var filaFin = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            // Obtener productos existentes
            var productosExistentes = await _context.Productos
                .Find(p => p.ProveedorId == request.ProveedorId &&
                          p.CategoriaId == request.CategoriaId &&
                          p.Activo)
                .ToListAsync();
            var nombresExistentes = productosExistentes
                .ToDictionary(p => p.Nombre.ToLowerInvariant(), p => p);

            for (int fila = filaInicio; fila <= filaFin; fila++)
            {
                var row = worksheet.Row(fila);

                if (row.IsEmpty())
                    continue;

                var productoDto = ParsearFila(row, fila,
                    nombresExistentes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id),
                    result.Errores);

                if (productoDto.TieneError)
                {
                    result.ErroresValidacion++;
                    continue;
                }

                try
                {
                    if (productoDto.EsNuevo)
                    {
                        // Crear nuevo producto
                        var producto = new Producto
                        {
                            Id = await _context.GetNextSequenceAsync("productos"),
                            Nombre = productoDto.Nombre,
                            UnidadMedida = productoDto.UnidadMedida,
                            StockMinimo = productoDto.StockMinimo,
                            CostoUnitario = productoDto.CostoUnitario,
                            ProveedorId = request.ProveedorId,
                            CategoriaId = request.CategoriaId,
                            Activo = true
                        };

                        await _context.Productos.InsertOneAsync(producto);
                        result.ProductosCreados++;
                        result.Detalle.Add(new ProductoImportadoDTO
                        {
                            Id = producto.Id,
                            Nombre = producto.Nombre,
                            Accion = "Creado"
                        });
                    }
                    else if (request.ActualizarExistentes && productoDto.ProductoExistenteId.HasValue)
                    {
                        // Actualizar producto existente
                        var productoExistente = nombresExistentes[productoDto.Nombre.ToLowerInvariant()];
                        productoExistente.UnidadMedida = productoDto.UnidadMedida;
                        productoExistente.StockMinimo = productoDto.StockMinimo;
                        productoExistente.CostoUnitario = productoDto.CostoUnitario;

                        await _context.Productos.ReplaceOneAsync(p => p.Id == productoExistente.Id, productoExistente);
                        result.ProductosActualizados++;
                        result.Detalle.Add(new ProductoImportadoDTO
                        {
                            Id = productoExistente.Id,
                            Nombre = productoExistente.Nombre,
                            Accion = "Actualizado"
                        });
                    }
                    else
                    {
                        // Producto existente pero no se actualizan
                        result.ProductosOmitidos++;
                        result.Detalle.Add(new ProductoImportadoDTO
                        {
                            Id = productoDto.ProductoExistenteId,
                            Nombre = productoDto.Nombre,
                            Accion = "Omitido"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importando producto en fila {Fila}", fila);
                    result.Errores.Add(new ErrorImportacionDTO
                    {
                        Fila = fila,
                        Columna = "General",
                        Mensaje = $"Error al guardar: {ex.Message}"
                    });
                    result.ErroresValidacion++;
                }
            }

            result.Exitoso = result.ErroresValidacion == 0;
            return result;
        }

        public byte[] GenerarPlantilla()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Productos");

            // Headers
            worksheet.Cell(1, COL_NOMBRE).Value = "Nombre";
            worksheet.Cell(1, COL_UNIDAD_MEDIDA).Value = "UnidadMedida";
            worksheet.Cell(1, COL_STOCK_MINIMO).Value = "StockMinimo";
            worksheet.Cell(1, COL_COSTO_UNITARIO).Value = "CostoUnitario";
            worksheet.Cell(1, COL_DESCRIPCION).Value = "Descripcion";

            // Estilo de headers
            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Ejemplos
            worksheet.Cell(2, COL_NOMBRE).Value = "Coca-Cola 330ml";
            worksheet.Cell(2, COL_UNIDAD_MEDIDA).Value = "Unidad";
            worksheet.Cell(2, COL_STOCK_MINIMO).Value = 50;
            worksheet.Cell(2, COL_COSTO_UNITARIO).Value = 0.85;
            worksheet.Cell(2, COL_DESCRIPCION).Value = "Bebida gaseosa";

            worksheet.Cell(3, COL_NOMBRE).Value = "Aceite de Oliva 1L";
            worksheet.Cell(3, COL_UNIDAD_MEDIDA).Value = "Botella";
            worksheet.Cell(3, COL_STOCK_MINIMO).Value = 10;
            worksheet.Cell(3, COL_COSTO_UNITARIO).Value = 8.50;
            worksheet.Cell(3, COL_DESCRIPCION).Value = "Aceite extra virgen";

            // Ajustar anchos de columnas
            worksheet.Column(COL_NOMBRE).Width = 30;
            worksheet.Column(COL_UNIDAD_MEDIDA).Width = 15;
            worksheet.Column(COL_STOCK_MINIMO).Width = 15;
            worksheet.Column(COL_COSTO_UNITARIO).Width = 15;
            worksheet.Column(COL_DESCRIPCION).Width = 40;

            // Hoja de instrucciones
            var instrucciones = workbook.AddWorksheet("Instrucciones");
            instrucciones.Cell(1, 1).Value = "Instrucciones para importar productos";
            instrucciones.Cell(1, 1).Style.Font.Bold = true;
            instrucciones.Cell(1, 1).Style.Font.FontSize = 14;

            instrucciones.Cell(3, 1).Value = "1. Use la hoja 'Productos' para agregar sus productos";
            instrucciones.Cell(4, 1).Value = "2. La fila 1 contiene los headers - NO la modifique";
            instrucciones.Cell(5, 1).Value = "3. Comience a agregar productos desde la fila 2";
            instrucciones.Cell(6, 1).Value = "4. Elimine las filas de ejemplo antes de importar";
            instrucciones.Cell(8, 1).Value = "Columnas requeridas:";
            instrucciones.Cell(8, 1).Style.Font.Bold = true;
            instrucciones.Cell(9, 1).Value = "- Nombre: Nombre del producto (obligatorio)";
            instrucciones.Cell(10, 1).Value = "- UnidadMedida: Unidad, Kg, Litro, Caja, etc. (obligatorio)";
            instrucciones.Cell(11, 1).Value = "- StockMinimo: Cantidad mínima de alerta (obligatorio)";
            instrucciones.Cell(12, 1).Value = "- CostoUnitario: Costo por unidad (obligatorio)";
            instrucciones.Cell(13, 1).Value = "- Descripcion: Descripción del producto (opcional)";

            instrucciones.Column(1).Width = 60;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportarAsync(ExportacionRequestDTO request)
        {
            // Construir filtro
            var filterBuilder = Builders<Producto>.Filter;
            var filters = new List<FilterDefinition<Producto>>
            {
                filterBuilder.Eq(p => p.Activo, true)
            };

            if (request.ProveedorId.HasValue)
                filters.Add(filterBuilder.Eq(p => p.ProveedorId, request.ProveedorId.Value));

            if (request.CategoriaId.HasValue)
                filters.Add(filterBuilder.Eq(p => p.CategoriaId, request.CategoriaId.Value));

            var filter = filterBuilder.And(filters);
            var productos = await _context.Productos.Find(filter).ToListAsync();

            // Obtener nombres de proveedores y categorías
            var proveedorIds = productos.Select(p => p.ProveedorId).Distinct().ToList();
            var categoriaIds = productos.Select(p => p.CategoriaId).Distinct().ToList();

            var proveedores = await _context.Proveedores
                .Find(p => proveedorIds.Contains(p.Id))
                .ToListAsync();
            var categorias = await _context.Categorias
                .Find(c => categoriaIds.Contains(c.Id))
                .ToListAsync();

            var proveedorDict = proveedores.ToDictionary(p => p.Id, p => p.Nombre);
            var categoriaDict = categorias.ToDictionary(c => c.Id, c => c.Nombre);

            // Obtener stock si se requiere
            Dictionary<int, decimal>? stockDict = null;
            if (request.IncluirStock && request.RestauranteId.HasValue)
            {
                var productoIds = productos.Select(p => p.Id).ToList();
                var stocks = await _context.StockLocal
                    .Find(s => s.RestauranteId == request.RestauranteId.Value && productoIds.Contains(s.ProductoId))
                    .ToListAsync();
                stockDict = stocks
                    .GroupBy(s => s.ProductoId)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.Cantidad));
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Productos");

            // Headers
            var col = 1;
            worksheet.Cell(1, col++).Value = "ID";
            worksheet.Cell(1, col++).Value = "Nombre";
            worksheet.Cell(1, col++).Value = "UnidadMedida";
            worksheet.Cell(1, col++).Value = "StockMinimo";
            worksheet.Cell(1, col++).Value = "CostoUnitario";
            worksheet.Cell(1, col++).Value = "Descripcion";
            worksheet.Cell(1, col++).Value = "Proveedor";
            worksheet.Cell(1, col++).Value = "Categoria";
            if (request.IncluirStock)
                worksheet.Cell(1, col++).Value = "StockActual";

            // Estilo de headers
            var headerRange = worksheet.Range(1, 1, 1, col - 1);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Datos
            var fila = 2;
            foreach (var producto in productos)
            {
                col = 1;
                worksheet.Cell(fila, col++).Value = producto.Id;
                worksheet.Cell(fila, col++).Value = producto.Nombre;
                worksheet.Cell(fila, col++).Value = producto.UnidadMedida;
                worksheet.Cell(fila, col++).Value = producto.StockMinimo;
                worksheet.Cell(fila, col++).Value = producto.CostoUnitario;
                worksheet.Cell(fila, col++).Value = ""; // Descripcion no disponible en la entidad actual
                worksheet.Cell(fila, col++).Value = proveedorDict.GetValueOrDefault(producto.ProveedorId, "");
                worksheet.Cell(fila, col++).Value = categoriaDict.GetValueOrDefault(producto.CategoriaId, "");
                if (request.IncluirStock && stockDict != null)
                    worksheet.Cell(fila, col++).Value = stockDict.GetValueOrDefault(producto.Id, 0);
                fila++;
            }

            // Ajustar columnas
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private ProductoImportacionDTO ParsearFila(IXLRow row, int fila, Dictionary<string, int> nombresExistentes, List<ErrorImportacionDTO> errores)
        {
            var dto = new ProductoImportacionDTO { Fila = fila };

            // Nombre (obligatorio)
            var nombreCell = row.Cell(COL_NOMBRE);
            if (nombreCell.IsEmpty())
            {
                dto.TieneError = true;
                dto.MensajeError = "El nombre es obligatorio";
                errores.Add(new ErrorImportacionDTO
                {
                    Fila = fila,
                    Columna = "Nombre",
                    Mensaje = "El nombre es obligatorio"
                });
                return dto;
            }
            dto.Nombre = nombreCell.GetString().Trim();

            // Verificar si existe
            if (nombresExistentes.TryGetValue(dto.Nombre.ToLowerInvariant(), out var existenteId))
            {
                dto.EsNuevo = false;
                dto.ProductoExistenteId = existenteId;
            }
            else
            {
                dto.EsNuevo = true;
            }

            // UnidadMedida (obligatorio)
            var unidadCell = row.Cell(COL_UNIDAD_MEDIDA);
            if (unidadCell.IsEmpty())
            {
                dto.TieneError = true;
                dto.MensajeError = "La unidad de medida es obligatoria";
                errores.Add(new ErrorImportacionDTO
                {
                    Fila = fila,
                    Columna = "UnidadMedida",
                    Mensaje = "La unidad de medida es obligatoria"
                });
                return dto;
            }
            dto.UnidadMedida = unidadCell.GetString().Trim();

            // StockMinimo (obligatorio, numérico)
            var stockCell = row.Cell(COL_STOCK_MINIMO);
            if (stockCell.IsEmpty() || !stockCell.TryGetValue(out decimal stockMinimo) || stockMinimo < 0)
            {
                dto.TieneError = true;
                dto.MensajeError = "StockMinimo debe ser un número >= 0";
                errores.Add(new ErrorImportacionDTO
                {
                    Fila = fila,
                    Columna = "StockMinimo",
                    Mensaje = "StockMinimo debe ser un número >= 0"
                });
                return dto;
            }
            dto.StockMinimo = stockMinimo;

            // CostoUnitario (obligatorio, numérico)
            var costoCell = row.Cell(COL_COSTO_UNITARIO);
            if (costoCell.IsEmpty() || !costoCell.TryGetValue(out decimal costo) || costo < 0)
            {
                dto.TieneError = true;
                dto.MensajeError = "CostoUnitario debe ser un número >= 0";
                errores.Add(new ErrorImportacionDTO
                {
                    Fila = fila,
                    Columna = "CostoUnitario",
                    Mensaje = "CostoUnitario debe ser un número >= 0"
                });
                return dto;
            }
            dto.CostoUnitario = costo;

            // Descripcion (opcional)
            var descCell = row.Cell(COL_DESCRIPCION);
            if (!descCell.IsEmpty())
            {
                dto.Descripcion = descCell.GetString().Trim();
            }

            return dto;
        }
    }
}
