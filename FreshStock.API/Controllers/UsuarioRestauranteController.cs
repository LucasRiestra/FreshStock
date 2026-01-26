using FreshStock.API.DTOs;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsuarioRestauranteController : ControllerBase
    {
        private readonly IUsuarioRestauranteService _usuarioRestauranteService;

        public UsuarioRestauranteController(IUsuarioRestauranteService usuarioRestauranteService)
        {
            _usuarioRestauranteService = usuarioRestauranteService;
        }

        // GET: api/usuariorestaurante
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioRestauranteResponseDTO>>> GetAll()
        {
            var asignaciones = await _usuarioRestauranteService.GetAllAsync();
            return Ok(asignaciones);
        }

        // GET: api/usuariorestaurante/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioRestauranteResponseDTO>> GetById(int id)
        {
            var asignacion = await _usuarioRestauranteService.GetByIdAsync(id);

            if (asignacion == null)
                return NotFound(new { message = $"Asignación con ID {id} no encontrada" });

            return Ok(asignacion);
        }

        // GET: api/usuariorestaurante/usuario/5
        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<UsuarioRestauranteResponseDTO>>> GetByUsuarioId(int usuarioId)
        {
            var asignaciones = await _usuarioRestauranteService.GetByUsuarioIdAsync(usuarioId);
            return Ok(asignaciones);
        }

        // GET: api/usuariorestaurante/restaurante/5
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<UsuarioRestauranteResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var asignaciones = await _usuarioRestauranteService.GetByRestauranteIdAsync(restauranteId);
            return Ok(asignaciones);
        }

        // GET: api/usuariorestaurante/usuario/5/restaurante/3
        [HttpGet("usuario/{usuarioId}/restaurante/{restauranteId}")]
        public async Task<ActionResult<UsuarioRestauranteResponseDTO>> GetByUsuarioAndRestaurante(int usuarioId, int restauranteId)
        {
            var asignacion = await _usuarioRestauranteService.GetByUsuarioAndRestauranteAsync(usuarioId, restauranteId);

            if (asignacion == null)
                return NotFound(new { message = "El usuario no está asignado a este restaurante" });

            return Ok(asignacion);
        }

        // GET: api/usuariorestaurante/restaurantes/usuario/5
        [HttpGet("restaurantes/usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<int>>> GetRestaurantesIdsByUsuario(int usuarioId)
        {
            var restauranteIds = await _usuarioRestauranteService.GetRestaurantesIdsByUsuarioAsync(usuarioId);
            return Ok(restauranteIds);
        }

        // POST: api/usuariorestaurante
        [HttpPost]
        public async Task<ActionResult<UsuarioRestauranteResponseDTO>> Create([FromBody] CreateUsuarioRestauranteDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var asignacion = await _usuarioRestauranteService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = asignacion.Id }, asignacion);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // PUT: api/usuariorestaurante/5
        [HttpPut("{id}")]
        public async Task<ActionResult<UsuarioRestauranteResponseDTO>> Update(int id, [FromBody] UpdateUsuarioRestauranteDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var asignacion = await _usuarioRestauranteService.UpdateAsync(dto);

            if (asignacion == null)
                return NotFound(new { message = $"Asignación con ID {id} no encontrada" });

            return Ok(asignacion);
        }

        // DELETE: api/usuariorestaurante/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _usuarioRestauranteService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Asignación con ID {id} no encontrada" });

            return NoContent();
        }
    }
}
