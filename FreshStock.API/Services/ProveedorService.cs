using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class ProveedorService : IProveedorService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;

        public ProveedorService(MongoDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProveedorResponseDTO>> GetAllAsync()
        {
            var proveedores = await _context.Proveedores
                .Find(p => p.Activo)
                .ToListAsync();

            var response = new List<ProveedorResponseDTO>();
            foreach (var proveedor in proveedores)
            {
                var dto = _mapper.Map<ProveedorResponseDTO>(proveedor);
                dto.RestauranteIds = await GetRestauranteIdsByProveedorAsync(proveedor.Id);
                response.Add(dto);
            }

            return response;
        }

        public async Task<ProveedorResponseDTO?> GetByIdAsync(int id)
        {
            var proveedor = await _context.Proveedores
                .Find(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (proveedor == null)
                return null;

            var response = _mapper.Map<ProveedorResponseDTO>(proveedor);
            response.RestauranteIds = await GetRestauranteIdsByProveedorAsync(id);
            return response;
        }

        private async Task<List<int>> GetRestauranteIdsByProveedorAsync(int proveedorId)
        {
            var asociaciones = await _context.RestauranteProveedores
                .Find(rp => rp.ProveedorId == proveedorId && rp.Activo)
                .ToListAsync();

            return asociaciones.Select(a => a.RestauranteId).ToList();
        }

        public async Task<ProveedorResponseDTO> CreateAsync(CreateProveedorDTO dto)
        {
            var proveedor = _mapper.Map<Proveedor>(dto);
            proveedor.Id = await _context.GetNextSequenceAsync("proveedores");
            proveedor.Activo = true;

            await _context.Proveedores.InsertOneAsync(proveedor);

            // Crear asociaciones con restaurantes si se especifican
            if (dto.RestauranteIds != null && dto.RestauranteIds.Any())
            {
                foreach (var restauranteId in dto.RestauranteIds)
                {
                    var asociacion = new RestauranteProveedor
                    {
                        Id = await _context.GetNextSequenceAsync("restauranteProveedores"),
                        RestauranteId = restauranteId,
                        ProveedorId = proveedor.Id,
                        Activo = true
                    };
                    await _context.RestauranteProveedores.InsertOneAsync(asociacion);
                }
            }

            var response = _mapper.Map<ProveedorResponseDTO>(proveedor);
            response.RestauranteIds = dto.RestauranteIds ?? new List<int>();
            return response;
        }

        public async Task<ProveedorResponseDTO?> UpdateAsync(UpdateProveedorDTO dto)
        {
            var proveedor = await _context.Proveedores
                .Find(p => p.Id == dto.Id)
                .FirstOrDefaultAsync();

            if (proveedor == null)
                return null;

            _mapper.Map(dto, proveedor);
            await _context.Proveedores.ReplaceOneAsync(p => p.Id == dto.Id, proveedor);

            // Gestionar asociaciones con restaurantes
            if (dto.RestauranteIds != null)
            {
                // Obtener asociaciones actuales
                var asociacionesActuales = await _context.RestauranteProveedores
                    .Find(rp => rp.ProveedorId == dto.Id && rp.Activo)
                    .ToListAsync();

                var idsActuales = asociacionesActuales.Select(a => a.RestauranteId).ToList();

                // Eliminar asociaciones que ya no están en la lista
                var idsAEliminar = idsActuales.Except(dto.RestauranteIds).ToList();
                if (idsAEliminar.Any())
                {
                    await _context.RestauranteProveedores.DeleteManyAsync(
                        rp => rp.ProveedorId == dto.Id && idsAEliminar.Contains(rp.RestauranteId));
                }

                // Agregar nuevas asociaciones
                var idsAAgregar = dto.RestauranteIds.Except(idsActuales).ToList();
                foreach (var restauranteId in idsAAgregar)
                {
                    var nuevaAsociacion = new RestauranteProveedor
                    {
                        Id = await _context.GetNextSequenceAsync("restauranteProveedores"),
                        RestauranteId = restauranteId,
                        ProveedorId = dto.Id,
                        Activo = true
                    };
                    await _context.RestauranteProveedores.InsertOneAsync(nuevaAsociacion);
                }
            }

            var response = _mapper.Map<ProveedorResponseDTO>(proveedor);
            response.RestauranteIds = await GetRestauranteIdsByProveedorAsync(dto.Id);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var proveedor = await _context.Proveedores
                .Find(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (proveedor == null)
                return false;

            // Obtener todos los productos de este proveedor para eliminar sus dependencias
            var productos = await _context.Productos
                .Find(p => p.ProveedorId == id)
                .ToListAsync();

            foreach (var producto in productos)
            {
                // Eliminar dependencias de cada producto
                await _context.StockLocal.DeleteManyAsync(s => s.ProductoId == producto.Id);
                await _context.MovimientosInventario.DeleteManyAsync(m => m.ProductoId == producto.Id);
                await _context.StockIdealRestaurantes.DeleteManyAsync(si => si.ProductoId == producto.Id);
                await _context.InventarioDetalles.DeleteManyAsync(d => d.ProductoId == producto.Id);
                await _context.AlertasStock.DeleteManyAsync(a => a.ProductoId == producto.Id);
            }

            // Eliminar todos los productos del proveedor
            await _context.Productos.DeleteManyAsync(p => p.ProveedorId == id);

            // Eliminar asignaciones restaurante-proveedor
            await _context.RestauranteProveedores.DeleteManyAsync(rp => rp.ProveedorId == id);

            // Eliminar detalles de inventario del proveedor
            await _context.InventarioDetalles.DeleteManyAsync(d => d.ProveedorId == id);

            // Eliminar el proveedor (hard delete)
            await _context.Proveedores.DeleteOneAsync(p => p.Id == id);

            return true;
        }
    }
}
