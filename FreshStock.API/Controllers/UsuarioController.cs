using FreshStock.API.DTOs;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        // GET: api/usuario
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDTO>>> GetAll()
        {
            var usuarios = await _usuarioService.GetAllAsync();
            return Ok(usuarios);
        }

        // GET: api/usuario/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioResponseDTO>> GetById(int id)
        {
            var usuario = await _usuarioService.GetByIdAsync(id);

            if (usuario == null)
                return NotFound(new { message = $"Usuario con ID {id} no encontrado" });

            return Ok(usuario);
        }

        // GET: api/usuario/restaurante/5
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var usuarios = await _usuarioService.GetByRestauranteIdAsync(restauranteId);
            return Ok(usuarios);
        }

        // POST: api/usuario
        [HttpPost]
        public async Task<ActionResult<UsuarioResponseDTO>> Create([FromBody] CreateUsuarioDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = await _usuarioService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario);
        }

        // PUT: api/usuario/5
        [HttpPut("{id}")]
        public async Task<ActionResult<UsuarioResponseDTO>> Update(int id, [FromBody] UpdateUsuarioDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var usuario = await _usuarioService.UpdateAsync(dto);

            if (usuario == null)
                return NotFound(new { message = $"Usuario con ID {id} no encontrado" });

            return Ok(usuario);
        }

        // DELETE: api/usuario/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _usuarioService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Usuario con ID {id} no encontrado" });

            return NoContent();
        }
    }
}
