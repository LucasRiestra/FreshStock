using FreshStock.API.DTOs;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RestauranteController : ControllerBase
    {
        private readonly IRestauranteService _restauranteService;

        public RestauranteController(IRestauranteService restauranteService)
        {
            _restauranteService = restauranteService;
        }

        // GET: api/restaurante
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<RestauranteResponseDTO>>> GetAll()
        {
            var restaurantes = await _restauranteService.GetAllAsync();
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
        [HttpPost]
        public async Task<ActionResult<RestauranteResponseDTO>> Create([FromBody] CreateRestauranteDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var restaurante = await _restauranteService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = restaurante.Id }, restaurante);
        }

        // PUT: api/restaurante/5
        [HttpPut("{id}")]
        public async Task<ActionResult<RestauranteResponseDTO>> Update(int id, [FromBody] UpdateRestauranteDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var restaurante = await _restauranteService.UpdateAsync(dto);

            if (restaurante == null)
                return NotFound(new { message = $"Restaurante con ID {id} no encontrado" });

            return Ok(restaurante);
        }

        // DELETE: api/restaurante/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _restauranteService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Restaurante con ID {id} no encontrado" });

            return NoContent();
        }
    }
}
