using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class ProductoService : IProductoService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;

        public ProductoService(MongoDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // Obtener IDs de proveedores asignados a un restaurante
        private async Task<List<int>> GetProveedorIdsByRestauranteAsync(int restauranteId)
        {
            var asignaciones = await _context.RestauranteProveedores
                .Find(rp => rp.RestauranteId == restauranteId && rp.Activo)
                .ToListAsync();

            return asignaciones.Select(rp => rp.ProveedorId).ToList();
        }

        // Obtener IDs de categorías asignadas a un restaurante
        private async Task<List<int>> GetCategoriaIdsByRestauranteAsync(int restauranteId)
        {
            var asignaciones = await _context.RestauranteCategorias
                .Find(rc => rc.RestauranteId == restauranteId && rc.Activo)
                .ToListAsync();

            return asignaciones.Select(rc => rc.CategoriaId).ToList();
        }

        // Obtener IDs de restaurantes de un usuario
        private async Task<List<int>> GetRestauranteIdsByUsuarioAsync(int usuarioId)
        {
            var asignaciones = await _context.UsuarioRestaurantes
                .Find(ur => ur.UsuarioId == usuarioId && ur.Activo)
                .ToListAsync();

            return asignaciones.Select(ur => ur.RestauranteId).ToList();
        }

        public async Task<IEnumerable<ProductoResponseDTO>> GetAllAsync()
        {
            var productos = await _context.Productos
                .Find(p => p.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<ProductoResponseDTO>>(productos);
            return response;
        }

        public async Task<ProductoResponseDTO?> GetByIdAsync(int id)
        {
            var producto = await _context.Productos
                .Find(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (producto == null)
                return null;

            var response = _mapper.Map<ProductoResponseDTO>(producto);
            return response;
        }

        public async Task<IEnumerable<ProductoResponseDTO>> GetByCategoriaIdAsync(int categoriaId)
        {
            var productos = await _context.Productos
                .Find(p => p.CategoriaId == categoriaId && p.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<ProductoResponseDTO>>(productos);
            return response;
        }

        public async Task<IEnumerable<ProductoResponseDTO>> GetByProveedorIdAsync(int proveedorId)
        {
            var productos = await _context.Productos
                .Find(p => p.ProveedorId == proveedorId && p.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<ProductoResponseDTO>>(productos);
            return response;
        }

        // Obtener productos de los proveedores Y categorías asignados a un restaurante específico
        public async Task<IEnumerable<ProductoResponseDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            // 1. Obtener los proveedores asignados a este restaurante
            var proveedorIds = await GetProveedorIdsByRestauranteAsync(restauranteId);
            if (!proveedorIds.Any())
                return Enumerable.Empty<ProductoResponseDTO>();

            // 2. Obtener las categorías asignadas a este restaurante
            var categoriaIds = await GetCategoriaIdsByRestauranteAsync(restauranteId);
            if (!categoriaIds.Any())
                return Enumerable.Empty<ProductoResponseDTO>();

            // 3. Obtener los productos que pertenecen a esos proveedores Y esas categorías
            var productos = await _context.Productos
                .Find(p => proveedorIds.Contains(p.ProveedorId) &&
                          categoriaIds.Contains(p.CategoriaId) &&
                          p.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<ProductoResponseDTO>>(productos);
            return response;
        }

        // Obtener productos de un proveedor específico dentro del contexto de un restaurante
        public async Task<IEnumerable<ProductoResponseDTO>> GetByRestauranteYProveedorAsync(int restauranteId, int proveedorId)
        {
            // 1. Verificar que el proveedor está asignado al restaurante
            var proveedorIds = await GetProveedorIdsByRestauranteAsync(restauranteId);
            if (!proveedorIds.Contains(proveedorId))
                return Enumerable.Empty<ProductoResponseDTO>();

            // 2. Obtener las categorías asignadas al restaurante
            var categoriaIds = await GetCategoriaIdsByRestauranteAsync(restauranteId);
            if (!categoriaIds.Any())
                return Enumerable.Empty<ProductoResponseDTO>();

            // 3. Obtener productos de ese proveedor que estén en categorías del restaurante
            var productos = await _context.Productos
                .Find(p => p.ProveedorId == proveedorId &&
                          categoriaIds.Contains(p.CategoriaId) &&
                          p.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<ProductoResponseDTO>>(productos);
            return response;
        }

        // Obtener productos de una categoría específica dentro del contexto de un restaurante
        public async Task<IEnumerable<ProductoResponseDTO>> GetByRestauranteYCategoriaAsync(int restauranteId, int categoriaId)
        {
            // 1. Verificar que la categoría está asignada al restaurante
            var categoriaIds = await GetCategoriaIdsByRestauranteAsync(restauranteId);
            if (!categoriaIds.Contains(categoriaId))
                return Enumerable.Empty<ProductoResponseDTO>();

            // 2. Obtener los proveedores asignados al restaurante
            var proveedorIds = await GetProveedorIdsByRestauranteAsync(restauranteId);
            if (!proveedorIds.Any())
                return Enumerable.Empty<ProductoResponseDTO>();

            // 3. Obtener productos de esa categoría que sean de proveedores del restaurante
            var productos = await _context.Productos
                .Find(p => p.CategoriaId == categoriaId &&
                          proveedorIds.Contains(p.ProveedorId) &&
                          p.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<ProductoResponseDTO>>(productos);
            return response;
        }

        // Obtener productos de todos los restaurantes a los que el usuario tiene acceso
        public async Task<IEnumerable<ProductoResponseDTO>> GetByUsuarioIdAsync(int usuarioId)
        {
            // 1. Obtener los restaurantes del usuario
            var restauranteIds = await GetRestauranteIdsByUsuarioAsync(usuarioId);
            if (!restauranteIds.Any())
                return Enumerable.Empty<ProductoResponseDTO>();

            // 2. Obtener todos los proveedores de esos restaurantes
            var proveedorAsignaciones = await _context.RestauranteProveedores
                .Find(rp => restauranteIds.Contains(rp.RestauranteId) && rp.Activo)
                .ToListAsync();
            var proveedorIds = proveedorAsignaciones.Select(rp => rp.ProveedorId).Distinct().ToList();
            if (!proveedorIds.Any())
                return Enumerable.Empty<ProductoResponseDTO>();

            // 3. Obtener todas las categorías de esos restaurantes
            var categoriaAsignaciones = await _context.RestauranteCategorias
                .Find(rc => restauranteIds.Contains(rc.RestauranteId) && rc.Activo)
                .ToListAsync();
            var categoriaIds = categoriaAsignaciones.Select(rc => rc.CategoriaId).Distinct().ToList();
            if (!categoriaIds.Any())
                return Enumerable.Empty<ProductoResponseDTO>();

            // 4. Obtener los productos de esos proveedores Y categorías
            var productos = await _context.Productos
                .Find(p => proveedorIds.Contains(p.ProveedorId) &&
                          categoriaIds.Contains(p.CategoriaId) &&
                          p.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<ProductoResponseDTO>>(productos);
            return response;
        }

        public async Task<ProductoResponseDTO> CreateAsync(CreateProductoDTO dto)
        {
            var producto = _mapper.Map<Producto>(dto);
            producto.Id = await _context.GetNextSequenceAsync("productos");
            producto.Activo = true;

            await _context.Productos.InsertOneAsync(producto);

            var response = _mapper.Map<ProductoResponseDTO>(producto);
            return response;
        }

        public async Task<IEnumerable<ProductoResponseDTO>> CreateBulkAsync(CreateProductosBulkDTO dto)
        {
            // Validar que existan proveedor y categoría
            var proveedor = await _context.Proveedores
                .Find(p => p.Id == dto.ProveedorId)
                .FirstOrDefaultAsync();
            if (proveedor == null)
            {
                throw new InvalidOperationException($"Proveedor con ID {dto.ProveedorId} no encontrado");
            }

            var categoria = await _context.Categorias
                .Find(c => c.Id == dto.CategoriaId)
                .FirstOrDefaultAsync();
            if (categoria == null)
            {
                throw new InvalidOperationException($"Categoría con ID {dto.CategoriaId} no encontrado");
            }

            // Crear lista de productos con IDs secuenciales
            var productos = new List<Producto>();
            foreach (var item in dto.Productos)
            {
                var producto = new Producto
                {
                    Id = await _context.GetNextSequenceAsync("productos"),
                    ProveedorId = dto.ProveedorId,
                    CategoriaId = dto.CategoriaId,
                    Nombre = item.Nombre,
                    UnidadMedida = item.UnidadMedida,
                    StockMinimo = item.StockMinimo,
                    CostoUnitario = item.CostoUnitario,
                    Activo = true
                };
                productos.Add(producto);
            }

            await _context.Productos.InsertManyAsync(productos);

            var response = _mapper.Map<IEnumerable<ProductoResponseDTO>>(productos);
            return response;
        }

        public async Task<ProductoResponseDTO?> UpdateAsync(UpdateProductoDTO dto)
        {
            var producto = await _context.Productos
                .Find(p => p.Id == dto.Id)
                .FirstOrDefaultAsync();

            if (producto == null)
                return null;

            _mapper.Map(dto, producto);
            await _context.Productos.ReplaceOneAsync(p => p.Id == dto.Id, producto);

            var response = _mapper.Map<ProductoResponseDTO>(producto);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var producto = await _context.Productos
                .Find(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (producto == null)
                return false;

            // Eliminación en cascada - eliminar todos los registros relacionados
            await _context.StockLocal.DeleteManyAsync(s => s.ProductoId == id);
            await _context.MovimientosInventario.DeleteManyAsync(m => m.ProductoId == id);
            await _context.StockIdealRestaurantes.DeleteManyAsync(si => si.ProductoId == id);
            await _context.InventarioDetalles.DeleteManyAsync(d => d.ProductoId == id);
            await _context.AlertasStock.DeleteManyAsync(a => a.ProductoId == id);

            // Eliminar el producto (hard delete)
            await _context.Productos.DeleteOneAsync(p => p.Id == id);

            return true;
        }
    }
}
