using FreshStock.API.DTOs;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RestauranteCategoriaController : ControllerBase
    {
        private readonly IRestauranteCategoriaService _restauranteCategoriaService;

        public RestauranteCategoriaController(IRestauranteCategoriaService restauranteCategoriaService)
        {
            _restauranteCategoriaService = restauranteCategoriaService;
        }

        // GET: api/restaurantecategoria
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RestauranteCategoriaResponseDTO>>> GetAll()
        {
            var asignaciones = await _restauranteCategoriaService.GetAllAsync();
            return Ok(asignaciones);
        }

        // GET: api/restaurantecategoria/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RestauranteCategoriaResponseDTO>> GetById(int id)
        {
            var asignacion = await _restauranteCategoriaService.GetByIdAsync(id);

            if (asignacion == null)
                return NotFound(new { message = $"Asignación con ID {id} no encontrada" });

            return Ok(asignacion);
        }

        // GET: api/restaurantecategoria/restaurante/5
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<RestauranteCategoriaResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var asignaciones = await _restauranteCategoriaService.GetByRestauranteIdAsync(restauranteId);
            return Ok(asignaciones);
        }

        // GET: api/restaurantecategoria/categoria/5
        [HttpGet("categoria/{categoriaId}")]
        public async Task<ActionResult<IEnumerable<RestauranteCategoriaResponseDTO>>> GetByCategoriaId(int categoriaId)
        {
            var asignaciones = await _restauranteCategoriaService.GetByCategoriaIdAsync(categoriaId);
            return Ok(asignaciones);
        }

        // GET: api/restaurantecategoria/categorias/restaurante/5
        // Obtiene las categorías asignadas a un restaurante específico
        [HttpGet("categorias/restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<CategoriaResponseDTO>>> GetCategoriasByRestauranteId(int restauranteId)
        {
            var categorias = await _restauranteCategoriaService.GetCategoriasByRestauranteIdAsync(restauranteId);
            return Ok(categorias);
        }

        // GET: api/restaurantecategoria/categorias/usuario/5
        // Obtiene las categorías de todos los restaurantes a los que el usuario tiene acceso
        [HttpGet("categorias/usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<CategoriaResponseDTO>>> GetCategoriasByUsuario(int usuarioId)
        {
            var categorias = await _restauranteCategoriaService.GetCategoriasByUsuarioAsync(usuarioId);
            return Ok(categorias);
        }

        // POST: api/restaurantecategoria
        [HttpPost]
        public async Task<ActionResult<RestauranteCategoriaResponseDTO>> Create([FromBody] CreateRestauranteCategoriaDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var asignacion = await _restauranteCategoriaService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = asignacion.Id }, asignacion);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // PUT: api/restaurantecategoria/5
        [HttpPut("{id}")]
        public async Task<ActionResult<RestauranteCategoriaResponseDTO>> Update(int id, [FromBody] UpdateRestauranteCategoriaDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var asignacion = await _restauranteCategoriaService.UpdateAsync(dto);

            if (asignacion == null)
                return NotFound(new { message = $"Asignación con ID {id} no encontrada" });

            return Ok(asignacion);
        }

        // DELETE: api/restaurantecategoria/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _restauranteCategoriaService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Asignación con ID {id} no encontrada" });

            return NoContent();
        }
    }
}
