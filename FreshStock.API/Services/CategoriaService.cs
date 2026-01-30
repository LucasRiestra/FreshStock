using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;

        public CategoriaService(MongoDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoriaResponseDTO>> GetAllAsync()
        {
            var categorias = await _context.Categorias
                .Find(_ => true)
                .ToListAsync();

            var response = new List<CategoriaResponseDTO>();
            foreach (var categoria in categorias)
            {
                var dto = _mapper.Map<CategoriaResponseDTO>(categoria);
                dto.RestauranteIds = await GetRestauranteIdsByCategoriaAsync(categoria.Id);
                response.Add(dto);
            }

            return response;
        }

        public async Task<CategoriaResponseDTO?> GetByIdAsync(int id)
        {
            var categoria = await _context.Categorias
                .Find(c => c.Id == id)
                .FirstOrDefaultAsync();

            if (categoria == null)
                return null;

            var response = _mapper.Map<CategoriaResponseDTO>(categoria);
            response.RestauranteIds = await GetRestauranteIdsByCategoriaAsync(id);
            return response;
        }

        private async Task<List<int>> GetRestauranteIdsByCategoriaAsync(int categoriaId)
        {
            var asociaciones = await _context.RestauranteCategorias
                .Find(rc => rc.CategoriaId == categoriaId && rc.Activo)
                .ToListAsync();

            return asociaciones.Select(a => a.RestauranteId).ToList();
        }

        public async Task<CategoriaResponseDTO> CreateAsync(CreateCategoriaDTO dto)
        {
            var categoria = _mapper.Map<Categoria>(dto);
            categoria.Id = await _context.GetNextSequenceAsync("categorias");

            await _context.Categorias.InsertOneAsync(categoria);

            // Crear asociaciones con restaurantes si se especifican
            if (dto.RestauranteIds != null && dto.RestauranteIds.Any())
            {
                foreach (var restauranteId in dto.RestauranteIds)
                {
                    var asociacion = new RestauranteCategoria
                    {
                        Id = await _context.GetNextSequenceAsync("restauranteCategorias"),
                        RestauranteId = restauranteId,
                        CategoriaId = categoria.Id,
                        Activo = true
                    };
                    await _context.RestauranteCategorias.InsertOneAsync(asociacion);
                }
            }

            var response = _mapper.Map<CategoriaResponseDTO>(categoria);
            response.RestauranteIds = dto.RestauranteIds ?? new List<int>();
            return response;
        }

        public async Task<CategoriaResponseDTO?> UpdateAsync(UpdateCategoriaDTO dto)
        {
            var categoria = await _context.Categorias
                .Find(c => c.Id == dto.Id)
                .FirstOrDefaultAsync();

            if (categoria == null)
                return null;

            // Actualizar nombre
            categoria.Nombre = dto.Nombre;
            await _context.Categorias.ReplaceOneAsync(c => c.Id == dto.Id, categoria);

            // Gestionar asociaciones con restaurantes
            if (dto.RestauranteIds != null)
            {
                // Obtener asociaciones actuales
                var asociacionesActuales = await _context.RestauranteCategorias
                    .Find(rc => rc.CategoriaId == dto.Id && rc.Activo)
                    .ToListAsync();

                var idsActuales = asociacionesActuales.Select(a => a.RestauranteId).ToList();

                // Eliminar asociaciones que ya no están en la lista
                var idsAEliminar = idsActuales.Except(dto.RestauranteIds).ToList();
                if (idsAEliminar.Any())
                {
                    await _context.RestauranteCategorias.DeleteManyAsync(
                        rc => rc.CategoriaId == dto.Id && idsAEliminar.Contains(rc.RestauranteId));
                }

                // Agregar nuevas asociaciones
                var idsAAgregar = dto.RestauranteIds.Except(idsActuales).ToList();
                foreach (var restauranteId in idsAAgregar)
                {
                    var nuevaAsociacion = new RestauranteCategoria
                    {
                        Id = await _context.GetNextSequenceAsync("restauranteCategorias"),
                        RestauranteId = restauranteId,
                        CategoriaId = dto.Id,
                        Activo = true
                    };
                    await _context.RestauranteCategorias.InsertOneAsync(nuevaAsociacion);
                }
            }

            var response = _mapper.Map<CategoriaResponseDTO>(categoria);
            response.RestauranteIds = await GetRestauranteIdsByCategoriaAsync(dto.Id);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var categoria = await _context.Categorias
                .Find(c => c.Id == id)
                .FirstOrDefaultAsync();

            if (categoria == null)
                return false;

            // Obtener todos los productos de esta categoría para eliminar sus dependencias
            var productos = await _context.Productos
                .Find(p => p.CategoriaId == id)
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

            // Eliminar todos los productos de la categoría
            await _context.Productos.DeleteManyAsync(p => p.CategoriaId == id);

            // Eliminar asignaciones restaurante-categoría
            await _context.RestauranteCategorias.DeleteManyAsync(rc => rc.CategoriaId == id);

            // Eliminar detalles de inventario de la categoría
            await _context.InventarioDetalles.DeleteManyAsync(d => d.CategoriaId == id);

            // Eliminar la categoría (hard delete)
            await _context.Categorias.DeleteOneAsync(c => c.Id == id);

            return true;
        }
    }
}
