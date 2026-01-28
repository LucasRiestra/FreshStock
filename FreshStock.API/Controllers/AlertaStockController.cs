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
    public class AlertaStockController : ControllerBase
    {
        private readonly IAlertaStockService _alertaService;
        private readonly IPermisoService _permisoService;

        public AlertaStockController(
            IAlertaStockService alertaService,
            IPermisoService permisoService)
        {
            _alertaService = alertaService;
            _permisoService = permisoService;
        }

        // GET: api/alertastock/restaurante/5
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<AlertaStockResponseDTO>>> GetByRestaurante(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            var alertas = await _alertaService.GetByRestauranteIdAsync(restauranteId);
            return Ok(alertas);
        }

        // GET: api/alertastock/restaurante/5/no-leidas
        [HttpGet("restaurante/{restauranteId}/no-leidas")]
        public async Task<ActionResult<IEnumerable<AlertaStockResponseDTO>>> GetNoLeidas(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            var alertas = await _alertaService.GetNoLeidasByRestauranteIdAsync(restauranteId);
            return Ok(alertas);
        }

        // GET: api/alertastock/restaurante/5/resumen
        [HttpGet("restaurante/{restauranteId}/resumen")]
        public async Task<ActionResult<ResumenAlertasDTO>> GetResumen(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
            if (!tienePermiso && !esSuperAdmin)
                return Forbid();

            var resumen = await _alertaService.GetResumenByRestauranteIdAsync(restauranteId);
            return Ok(resumen);
        }

        // POST: api/alertastock/5/marcar-leida
        [HttpPost("{id}/marcar-leida")]
        public async Task<ActionResult<AlertaStockResponseDTO>> MarcarLeida(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var alerta = await _alertaService.MarcarLeidaAsync(id, usuarioId.Value);
            if (alerta == null)
                return NotFound(new { message = $"Alerta con ID {id} no encontrada" });

            return Ok(alerta);
        }

        // POST: api/alertastock/marcar-leidas
        [HttpPost("marcar-leidas")]
        public async Task<ActionResult> MarcarVariasLeidas([FromBody] MarcarAlertasLeidasDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var count = await _alertaService.MarcarVariasLeidasAsync(dto, usuarioId.Value);
            return Ok(new { alertasMarcadas = count });
        }

        // POST: api/alertastock/generar/5
        [HttpPost("generar/{restauranteId}")]
        public async Task<ActionResult<GeneracionAlertasResultDTO>> GenerarAlertas(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Solo Admin, Gerente o SuperAdmin pueden generar alertas
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, restauranteId);
            if (!tienePermiso)
                return Forbid();

            var resultado = await _alertaService.GenerarAlertasAsync(restauranteId);
            return Ok(resultado);
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
