using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FreshStock.API.Services
{
    public class ProductoService : IProductoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ProductoService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProductoResponseDTO>> GetAllAsync()
        {
            var productos = await _context.Productos
                .Where(p => p.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<ProductoResponseDTO>>(productos);
            return response;
        }

        public async Task<ProductoResponseDTO?> GetByIdAsync(int id)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
                return null;

            var response = _mapper.Map<ProductoResponseDTO>(producto);
            return response;
        }

        public async Task<IEnumerable<ProductoResponseDTO>> GetByCategoriaIdAsync(int categoriaId)
        {
            var productos = await _context.Productos
                .Where(p => p.CategoriaId == categoriaId && p.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<ProductoResponseDTO>>(productos);
            return response;
        }

        public async Task<IEnumerable<ProductoResponseDTO>> GetByProveedorIdAsync(int proveedorId)
        {
            var productos = await _context.Productos
                .Where(p => p.ProveedorId == proveedorId && p.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<ProductoResponseDTO>>(productos);
            return response;
        }

        public async Task<ProductoResponseDTO> CreateAsync(CreateProductoDTO dto)
        {
            var producto = _mapper.Map<Producto>(dto);
            producto.Activo = true;

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<ProductoResponseDTO>(producto);
            return response;
        }

        public async Task<IEnumerable<ProductoResponseDTO>> CreateBulkAsync(CreateProductosBulkDTO dto)
        {
            // Validar que existan proveedor y categoría
            var proveedor = await _context.Proveedores.FindAsync(dto.ProveedorId);
            if (proveedor == null)
            {
                throw new InvalidOperationException($"Proveedor con ID {dto.ProveedorId} no encontrado");
            }

            var categoria = await _context.Categorias.FindAsync(dto.CategoriaId);
            if (categoria == null)
            {
                throw new InvalidOperationException($"Categoría con ID {dto.CategoriaId} no encontrado");
            }

            // Crear lista de productos
            var productos = dto.Productos.Select(item => new Producto
            {
                ProveedorId = dto.ProveedorId,
                CategoriaId = dto.CategoriaId,
                Nombre = item.Nombre,
                UnidadMedida = item.UnidadMedida,
                StockMinimo = item.StockMinimo,
                CostoUnitario = item.CostoUnitario,
                Activo = true
            }).ToList();

            _context.Productos.AddRange(productos);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<IEnumerable<ProductoResponseDTO>>(productos);
            return response;
        }

        public async Task<ProductoResponseDTO?> UpdateAsync(UpdateProductoDTO dto)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == dto.Id);

            if (producto == null)
                return null;

            _mapper.Map(dto, producto);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<ProductoResponseDTO>(producto);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
                return false;

            // Soft delete
            producto.Activo = false;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
