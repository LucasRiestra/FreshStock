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
    public class InventarioController : ControllerBase
    {
        private readonly IInventarioService _inventarioService;
        private readonly IPermisoService _permisoService;

        public InventarioController(
            IInventarioService inventarioService,
            IPermisoService permisoService)
        {
            _inventarioService = inventarioService;
            _permisoService = permisoService;
        }

        #region Gestión de Inventarios

        // POST: api/inventario/nuevo
        [HttpPost("nuevo")]
        public async Task<ActionResult<InventarioResponseDTO>> Create([FromBody] CreateInventarioDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar permisos (Admin, Gerente o SuperAdmin)
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, dto.RestauranteId);
            if (!tienePermiso)
                return Forbid();

            try
            {
                var inventario = await _inventarioService.CreateAsync(dto, usuarioId.Value);
                return CreatedAtAction(nameof(GetById), new { id = inventario.Id }, inventario);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/inventario/5
        [HttpGet("{id}")]
        public async Task<ActionResult<InventarioResponseDTO>> GetById(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound(new { message = $"Inventario con ID {id} no encontrado" });

            // Verificar permisos
            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, inventario.RestauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            return Ok(inventario);
        }

        // GET: api/inventario/restaurante/5
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<InventarioResumenDTO>>> GetByRestauranteId(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            var inventarios = await _inventarioService.GetByRestauranteIdAsync(restauranteId);
            return Ok(inventarios);
        }

        // GET: api/inventario/restaurante/5/actual
        [HttpGet("restaurante/{restauranteId}/actual")]
        public async Task<ActionResult<InventarioResponseDTO>> GetActualByRestauranteId(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            var inventario = await _inventarioService.GetActualByRestauranteIdAsync(restauranteId);
            if (inventario == null)
                return NotFound(new { message = "No hay inventarios completados para este restaurante" });

            return Ok(inventario);
        }

        // POST: api/inventario/5/finalizar
        [HttpPost("{id}/finalizar")]
        public async Task<ActionResult<InventarioResponseDTO>> Finalizar(int id, [FromBody] FinalizarInventarioDTO? dto)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound(new { message = $"Inventario con ID {id} no encontrado" });

            // Solo Admin, Gerente o SuperAdmin pueden finalizar
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, inventario.RestauranteId);
            if (!tienePermiso)
                return Forbid();

            try
            {
                var resultado = await _inventarioService.FinalizarAsync(id, dto);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/inventario/5/cancelar
        [HttpPost("{id}/cancelar")]
        public async Task<ActionResult<InventarioResponseDTO>> Cancelar(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound(new { message = $"Inventario con ID {id} no encontrado" });

            // Solo Admin, Gerente o SuperAdmin pueden cancelar
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, inventario.RestauranteId);
            if (!tienePermiso)
                return Forbid();

            try
            {
                var resultado = await _inventarioService.CancelarAsync(id);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/inventario/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound(new { message = $"Inventario con ID {id} no encontrado" });

            // Solo Admin, Gerente o SuperAdmin pueden eliminar
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, inventario.RestauranteId);
            if (!tienePermiso)
                return Forbid();

            var resultado = await _inventarioService.DeleteAsync(id);
            if (!resultado)
                return BadRequest(new { message = "No se pudo eliminar el inventario" });

            return NoContent();
        }

        #endregion

        #region Navegación para Conteo

        // GET: api/inventario/5/categorias
        [HttpGet("{id}/categorias")]
        public async Task<ActionResult<IEnumerable<CategoriaConteoDTO>>> GetCategorias(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound(new { message = $"Inventario con ID {id} no encontrado" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, inventario.RestauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            try
            {
                var categorias = await _inventarioService.GetCategoriasAsync(id);
                return Ok(categorias);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/inventario/5/categoria/3/proveedores
        [HttpGet("{id}/categoria/{categoriaId}/proveedores")]
        public async Task<ActionResult<IEnumerable<ProveedorConteoDTO>>> GetProveedoresByCategoria(int id, int categoriaId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound(new { message = $"Inventario con ID {id} no encontrado" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, inventario.RestauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            try
            {
                var proveedores = await _inventarioService.GetProveedoresByCategoriaAsync(id, categoriaId);
                return Ok(proveedores);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/inventario/5/proveedor/2/productos
        [HttpGet("{id}/proveedor/{proveedorId}/productos")]
        public async Task<ActionResult<IEnumerable<ProductoConteoDTO>>> GetProductosByProveedor(int id, int proveedorId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound(new { message = $"Inventario con ID {id} no encontrado" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, inventario.RestauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            try
            {
                var productos = await _inventarioService.GetProductosByProveedorAsync(id, proveedorId);
                return Ok(productos);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Registro de Conteos

        // POST: api/inventario/5/contar
        [HttpPost("{id}/contar")]
        public async Task<ActionResult<InventarioDetalleResponseDTO>> RegistrarConteo(int id, [FromBody] CreateInventarioDetalleDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound(new { message = $"Inventario con ID {id} no encontrado" });

            // Cualquier usuario con acceso al restaurante puede registrar conteos
            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, inventario.RestauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            try
            {
                var detalle = await _inventarioService.RegistrarConteoAsync(id, dto);
                return Ok(detalle);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/inventario/5/contar/bulk
        [HttpPost("{id}/contar/bulk")]
        public async Task<ActionResult<IEnumerable<InventarioDetalleResponseDTO>>> RegistrarConteosBulk(int id, [FromBody] CreateInventarioDetalleBulkDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound(new { message = $"Inventario con ID {id} no encontrado" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, inventario.RestauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            try
            {
                var detalles = await _inventarioService.RegistrarConteosBulkAsync(id, dto);
                return Ok(detalles);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/inventario/5/contar/10
        [HttpPut("{id}/contar/{detalleId}")]
        public async Task<ActionResult<InventarioDetalleResponseDTO>> ActualizarConteo(int id, int detalleId, [FromBody] UpdateInventarioDetalleDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (detalleId != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound(new { message = $"Inventario con ID {id} no encontrado" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, inventario.RestauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            try
            {
                var detalle = await _inventarioService.ActualizarConteoAsync(id, dto);
                if (detalle == null)
                    return NotFound(new { message = $"Detalle con ID {detalleId} no encontrado" });

                return Ok(detalle);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Progreso y Detalles

        // GET: api/inventario/5/progreso
        [HttpGet("{id}/progreso")]
        public async Task<ActionResult<ProgresoInventarioDTO>> GetProgreso(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound(new { message = $"Inventario con ID {id} no encontrado" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, inventario.RestauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            try
            {
                var progreso = await _inventarioService.GetProgresoAsync(id);
                return Ok(progreso);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/inventario/5/detalles
        [HttpGet("{id}/detalles")]
        public async Task<ActionResult<IEnumerable<InventarioDetalleResponseDTO>>> GetDetalles(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound(new { message = $"Inventario con ID {id} no encontrado" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, inventario.RestauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            var detalles = await _inventarioService.GetDetallesAsync(id);
            return Ok(detalles);
        }

        #endregion

        private int? GetUsuarioIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }
    }
}
