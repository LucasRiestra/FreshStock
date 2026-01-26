using FreshStock.API.DTOs;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _productoService;
        private readonly IPermisoService _permisoService;

        public ProductoController(IProductoService productoService, IPermisoService permisoService)
        {
            _productoService = productoService;
            _permisoService = permisoService;
        }

        // GET: api/producto
        // Retorna productos filtrados por usuario (Admin ve todo, resto solo de sus restaurantes)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoResponseDTO>>> GetAll()
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Si es Admin, ve todos los productos
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);
            if (esAdmin)
            {
                var todosProductos = await _productoService.GetAllAsync();
                return Ok(todosProductos);
            }

            // Si no es Admin, solo ve productos de sus restaurantes
            var productos = await _productoService.GetByUsuarioIdAsync(usuarioId.Value);
            return Ok(productos);
        }

        // GET: api/producto/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoResponseDTO>> GetById(int id)
        {
            var producto = await _productoService.GetByIdAsync(id);

            if (producto == null)
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });

            return Ok(producto);
        }

        // GET: api/producto/categoria/5
        [HttpGet("categoria/{categoriaId}")]
        public async Task<ActionResult<IEnumerable<ProductoResponseDTO>>> GetByCategoriaId(int categoriaId)
        {
            var productos = await _productoService.GetByCategoriaIdAsync(categoriaId);
            return Ok(productos);
        }

        // GET: api/producto/proveedor/5
        [HttpGet("proveedor/{proveedorId}")]
        public async Task<ActionResult<IEnumerable<ProductoResponseDTO>>> GetByProveedorId(int proveedorId)
        {
            var productos = await _productoService.GetByProveedorIdAsync(proveedorId);
            return Ok(productos);
        }

        // GET: api/producto/restaurante/5
        // Obtiene productos de los proveedores asignados a un restaurante
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<ProductoResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que el usuario tiene acceso al restaurante
            var tieneAcceso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            if (!tieneAcceso)
            {
                // Si es Admin, puede ver cualquier restaurante
                var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);
                if (!esAdmin)
                    return Forbid();
            }

            var productos = await _productoService.GetByRestauranteIdAsync(restauranteId);
            return Ok(productos);
        }

        // POST: api/producto
        // Solo Admin o Gerente pueden crear productos
        [HttpPost]
        public async Task<ActionResult<ProductoResponseDTO>> Create([FromBody] CreateProductoDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin o Gerente
            var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(usuarioId.Value);
            if (!tienePermiso)
                return Forbid();

            var producto = await _productoService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto);
        }

        // POST: api/producto/bulk
        // Solo Admin o Gerente pueden crear productos en lote
        [HttpPost("bulk")]
        public async Task<ActionResult<IEnumerable<ProductoResponseDTO>>> CreateBulk([FromBody] CreateProductosBulkDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin o Gerente
            var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(usuarioId.Value);
            if (!tienePermiso)
                return Forbid();

            try
            {
                var productos = await _productoService.CreateBulkAsync(dto);
                return Ok(new { message = $"{productos.Count()} productos creados exitosamente", productos });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/producto/5
        // Solo Admin o Gerente pueden actualizar productos
        [HttpPut("{id}")]
        public async Task<ActionResult<ProductoResponseDTO>> Update(int id, [FromBody] UpdateProductoDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin o Gerente
            var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(usuarioId.Value);
            if (!tienePermiso)
                return Forbid();

            var producto = await _productoService.UpdateAsync(dto);

            if (producto == null)
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });

            return Ok(producto);
        }

        // DELETE: api/producto/5
        // Solo Admin o Gerente pueden eliminar productos
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin o Gerente
            var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(usuarioId.Value);
            if (!tienePermiso)
                return Forbid();

            var result = await _productoService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });

            return NoContent();
        }

        private int? GetUsuarioIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }
    }
}
