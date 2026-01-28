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
    public class ComparativaStockController : ControllerBase
    {
        private readonly IComparativaStockService _comparativaService;
        private readonly IPermisoService _permisoService;

        public ComparativaStockController(
            IComparativaStockService comparativaService,
            IPermisoService permisoService)
        {
            _comparativaService = comparativaService;
            _permisoService = permisoService;
        }

        // GET: api/comparativastock/restaurante/5
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<ComparativaStockDTO>> GetByRestaurante(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            try
            {
                var comparativa = await _comparativaService.GetComparativaByRestauranteAsync(restauranteId);
                return Ok(comparativa);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/comparativastock/restaurante/5/categoria/3
        [HttpGet("restaurante/{restauranteId}/categoria/{categoriaId}")]
        public async Task<ActionResult<ComparativaStockDTO>> GetByCategoria(int restauranteId, int categoriaId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            try
            {
                var comparativa = await _comparativaService.GetComparativaByCategoriaAsync(restauranteId, categoriaId);
                return Ok(comparativa);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/comparativastock/restaurante/5/criticos
        [HttpGet("restaurante/{restauranteId}/criticos")]
        public async Task<ActionResult<IEnumerable<ComparativaProductoDTO>>> GetCriticos(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            try
            {
                var criticos = await _comparativaService.GetProductosCriticosAsync(restauranteId);
                return Ok(criticos);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/comparativastock/restaurante/5/bajos
        [HttpGet("restaurante/{restauranteId}/bajos")]
        public async Task<ActionResult<IEnumerable<ComparativaProductoDTO>>> GetBajos(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            try
            {
                var bajos = await _comparativaService.GetProductosBajosAsync(restauranteId);
                return Ok(bajos);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/comparativastock/restaurante/5/historial?desde=2026-01-01
        [HttpGet("restaurante/{restauranteId}/historial")]
        public async Task<ActionResult<IEnumerable<HistorialComparativaDTO>>> GetHistorial(
            int restauranteId,
            [FromQuery] DateTime? desde = null)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            try
            {
                var historial = await _comparativaService.GetHistorialAsync(restauranteId, desde);
                return Ok(historial);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
