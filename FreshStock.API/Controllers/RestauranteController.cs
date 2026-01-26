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
    public class RestauranteController : ControllerBase
    {
        private readonly IRestauranteService _restauranteService;
        private readonly IPermisoService _permisoService;
        private readonly IUsuarioRestauranteService _usuarioRestauranteService;

        public RestauranteController(
            IRestauranteService restauranteService,
            IPermisoService permisoService,
            IUsuarioRestauranteService usuarioRestauranteService)
        {
            _restauranteService = restauranteService;
            _permisoService = permisoService;
            _usuarioRestauranteService = usuarioRestauranteService;
        }

        // GET: api/restaurante
        // Retorna todos los restaurantes (público para listado general)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<RestauranteResponseDTO>>> GetAll()
        {
            var restaurantes = await _restauranteService.GetAllAsync();
            return Ok(restaurantes);
        }

        // GET: api/restaurante/mis-restaurantes
        // Retorna los restaurantes a los que el usuario tiene acceso
        [HttpGet("mis-restaurantes")]
        public async Task<ActionResult<IEnumerable<RestauranteResponseDTO>>> GetMisRestaurantes()
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var restauranteIds = await _usuarioRestauranteService.GetRestaurantesIdsByUsuarioAsync(usuarioId.Value);
            var restaurantes = new List<RestauranteResponseDTO>();

            foreach (var id in restauranteIds)
            {
                var restaurante = await _restauranteService.GetByIdAsync(id);
                if (restaurante != null)
                    restaurantes.Add(restaurante);
            }

            return Ok(restaurantes);
        }

        // GET: api/restaurante/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RestauranteResponseDTO>> GetById(int id)
        {
            var restaurante = await _restauranteService.GetByIdAsync(id);

            if (restaurante == null)
                return NotFound(new { message = $"Restaurante con ID {id} no encontrado" });

            return Ok(restaurante);
        }

        // POST: api/restaurante
        // Solo Admin puede crear restaurantes
        [HttpPost]
        public async Task<ActionResult<RestauranteResponseDTO>> Create([FromBody] CreateRestauranteDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin en al menos un restaurante
            var tienePermiso = await _permisoService.PuedeCrearRestaurantesAsync(usuarioId.Value);
            if (!tienePermiso)
                return Forbid();

            var restaurante = await _restauranteService.CreateAsync(dto);

            // Asignar automáticamente al creador como Admin del nuevo restaurante
            await _usuarioRestauranteService.CreateAsync(new CreateUsuarioRestauranteDTO
            {
                UsuarioId = usuarioId.Value,
                RestauranteId = restaurante.Id,
                Rol = Enums.RolUsuario.Admin
            });

            return CreatedAtAction(nameof(GetById), new { id = restaurante.Id }, restaurante);
        }

        // PUT: api/restaurante/5
        // Solo Admin del restaurante puede actualizarlo
        [HttpPut("{id}")]
        public async Task<ActionResult<RestauranteResponseDTO>> Update(int id, [FromBody] UpdateRestauranteDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin del restaurante
            var rol = await _permisoService.GetRolEnRestauranteAsync(usuarioId.Value, id);
            if (rol == null || rol != Enums.RolUsuario.Admin)
                return Forbid();

            var restaurante = await _restauranteService.UpdateAsync(dto);

            if (restaurante == null)
                return NotFound(new { message = $"Restaurante con ID {id} no encontrado" });

            return Ok(restaurante);
        }

        // DELETE: api/restaurante/5
        // Solo Admin del restaurante puede eliminarlo
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin del restaurante
            var rol = await _permisoService.GetRolEnRestauranteAsync(usuarioId.Value, id);
            if (rol == null || rol != Enums.RolUsuario.Admin)
                return Forbid();

            var result = await _restauranteService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Restaurante con ID {id} no encontrado" });

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
