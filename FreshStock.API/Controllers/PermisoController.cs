using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermisoController : ControllerBase
    {
        private readonly IPermisoService _permisoService;

        public PermisoController(IPermisoService permisoService)
        {
            _permisoService = permisoService;
        }

        // GET: api/permiso/mis-permisos
        // Obtiene todos los permisos del usuario autenticado
        [HttpGet("mis-permisos")]
        public async Task<ActionResult<PermisoUsuarioDTO>> GetMisPermisos()
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario del token" });

            var permisos = await _permisoService.GetPermisosUsuarioAsync(usuarioId.Value);
            return Ok(permisos);
        }

        // GET: api/permiso/usuario/5
        // Obtiene los permisos de un usuario específico (solo para admins)
        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<PermisoUsuarioDTO>> GetPermisosUsuario(int usuarioId)
        {
            var currentUserId = GetUsuarioIdFromToken();
            if (currentUserId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario del token" });

            // Verificar que el usuario actual es admin
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(currentUserId.Value);
            if (!esAdmin)
                return Forbid();

            var permisos = await _permisoService.GetPermisosUsuarioAsync(usuarioId);
            return Ok(permisos);
        }

        // GET: api/permiso/verificar/restaurante/5
        // Verifica si el usuario tiene acceso a un restaurante específico
        [HttpGet("verificar/restaurante/{restauranteId}")]
        public async Task<ActionResult<object>> VerificarAccesoRestaurante(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario del token" });

            var tieneAcceso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var rol = await _permisoService.GetRolEnRestauranteAsync(usuarioId.Value, restauranteId);

            return Ok(new
            {
                tieneAcceso,
                rol = rol?.ToString(),
                puedeCrearUsuarios = await _permisoService.PuedeCrearUsuariosAsync(usuarioId.Value, restauranteId),
                puedeCrearCategorias = await _permisoService.PuedeCrearCategoriasAsync(usuarioId.Value, restauranteId),
                puedeCrearProveedores = await _permisoService.PuedeCrearProveedoresAsync(usuarioId.Value, restauranteId),
                puedeGestionarInventario = await _permisoService.PuedeGestionarInventarioAsync(usuarioId.Value, restauranteId)
            });
        }

        // GET: api/permiso/puede-crear-restaurante
        // Verifica si el usuario puede crear nuevos restaurantes
        [HttpGet("puede-crear-restaurante")]
        public async Task<ActionResult<object>> PuedeCrearRestaurante()
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario del token" });

            var puede = await _permisoService.PuedeCrearRestaurantesAsync(usuarioId.Value);
            return Ok(new { puedeCrearRestaurante = puede });
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
