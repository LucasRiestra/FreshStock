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
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IPermisoService _permisoService;

        public UsuarioController(IUsuarioService usuarioService, IPermisoService permisoService)
        {
            _usuarioService = usuarioService;
            _permisoService = permisoService;
        }

        // GET: api/usuario
        // Solo Admin o Gerente pueden ver todos los usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDTO>>> GetAll()
        {
            var currentUserId = GetUsuarioIdFromToken();
            if (currentUserId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin o Gerente
            var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(currentUserId.Value);
            if (!tienePermiso)
                return Forbid();

            var usuarios = await _usuarioService.GetAllAsync();
            return Ok(usuarios);
        }

        // GET: api/usuario/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioResponseDTO>> GetById(int id)
        {
            var currentUserId = GetUsuarioIdFromToken();
            if (currentUserId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Un usuario puede ver su propio perfil, o Admin/Gerente pueden ver cualquier usuario
            if (currentUserId.Value != id)
            {
                var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(currentUserId.Value);
                if (!tienePermiso)
                    return Forbid();
            }

            var usuario = await _usuarioService.GetByIdAsync(id);

            if (usuario == null)
                return NotFound(new { message = $"Usuario con ID {id} no encontrado" });

            return Ok(usuario);
        }

        // GET: api/usuario/restaurante/5
        // Obtiene los usuarios asignados a un restaurante
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var currentUserId = GetUsuarioIdFromToken();
            if (currentUserId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que el usuario tiene acceso al restaurante
            var tieneAcceso = await _permisoService.TieneAccesoARestauranteAsync(currentUserId.Value, restauranteId);
            if (!tieneAcceso)
                return Forbid();

            var usuarios = await _usuarioService.GetByRestauranteIdAsync(restauranteId);
            return Ok(usuarios);
        }

        // POST: api/usuario
        // Solo Admin o Gerente pueden crear usuarios
        [HttpPost]
        public async Task<ActionResult<UsuarioResponseDTO>> Create([FromBody] CreateUsuarioDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = GetUsuarioIdFromToken();
            if (currentUserId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin o Gerente
            var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(currentUserId.Value);
            if (!tienePermiso)
                return Forbid();

            var usuario = await _usuarioService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario);
        }

        // PUT: api/usuario/5
        // Solo Admin o Gerente pueden actualizar usuarios (o el propio usuario su perfil)
        [HttpPut("{id}")]
        public async Task<ActionResult<UsuarioResponseDTO>> Update(int id, [FromBody] UpdateUsuarioDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var currentUserId = GetUsuarioIdFromToken();
            if (currentUserId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Un usuario puede actualizar su propio perfil, o Admin/Gerente pueden actualizar cualquier usuario
            if (currentUserId.Value != id)
            {
                var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(currentUserId.Value);
                if (!tienePermiso)
                    return Forbid();
            }

            var usuario = await _usuarioService.UpdateAsync(dto);

            if (usuario == null)
                return NotFound(new { message = $"Usuario con ID {id} no encontrado" });

            return Ok(usuario);
        }

        // DELETE: api/usuario/5
        // Solo Admin o Gerente pueden eliminar usuarios
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var currentUserId = GetUsuarioIdFromToken();
            if (currentUserId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin o Gerente
            var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(currentUserId.Value);
            if (!tienePermiso)
                return Forbid();

            // No permitir que un usuario se elimine a sí mismo
            if (currentUserId.Value == id)
                return BadRequest(new { message = "No puedes eliminar tu propio usuario" });

            var result = await _usuarioService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Usuario con ID {id} no encontrado" });

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
