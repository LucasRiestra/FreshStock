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

            // Soft delete
            var update = Builders<Producto>.Update.Set(p => p.Activo, false);
            await _context.Productos.UpdateOneAsync(p => p.Id == id, update);

            return true;
        }
    }
}
