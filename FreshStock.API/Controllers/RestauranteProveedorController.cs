using FreshStock.API.DTOs;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RestauranteProveedorController : ControllerBase
    {
        private readonly IRestauranteProveedorService _restauranteProveedorService;

        public RestauranteProveedorController(IRestauranteProveedorService restauranteProveedorService)
        {
            _restauranteProveedorService = restauranteProveedorService;
        }

        // GET: api/restauranteproveedor
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RestauranteProveedorResponseDTO>>> GetAll()
        {
            var asignaciones = await _restauranteProveedorService.GetAllAsync();
            return Ok(asignaciones);
        }

        // GET: api/restauranteproveedor/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RestauranteProveedorResponseDTO>> GetById(int id)
        {
            var asignacion = await _restauranteProveedorService.GetByIdAsync(id);

            if (asignacion == null)
                return NotFound(new { message = $"Asignación con ID {id} no encontrada" });

            return Ok(asignacion);
        }

        // GET: api/restauranteproveedor/restaurante/5
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<RestauranteProveedorResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var asignaciones = await _restauranteProveedorService.GetByRestauranteIdAsync(restauranteId);
            return Ok(asignaciones);
        }

        // GET: api/restauranteproveedor/proveedor/5
        [HttpGet("proveedor/{proveedorId}")]
        public async Task<ActionResult<IEnumerable<RestauranteProveedorResponseDTO>>> GetByProveedorId(int proveedorId)
        {
            var asignaciones = await _restauranteProveedorService.GetByProveedorIdAsync(proveedorId);
            return Ok(asignaciones);
        }

        // GET: api/restauranteproveedor/proveedores/restaurante/5
        // Obtiene los proveedores asignados a un restaurante específico
        [HttpGet("proveedores/restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<ProveedorResponseDTO>>> GetProveedoresByRestauranteId(int restauranteId)
        {
            var proveedores = await _restauranteProveedorService.GetProveedoresByRestauranteIdAsync(restauranteId);
            return Ok(proveedores);
        }

        // GET: api/restauranteproveedor/proveedores/usuario/5
        // Obtiene los proveedores de todos los restaurantes a los que el usuario tiene acceso
        [HttpGet("proveedores/usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<ProveedorResponseDTO>>> GetProveedoresByUsuario(int usuarioId)
        {
            var proveedores = await _restauranteProveedorService.GetProveedoresByUsuarioAsync(usuarioId);
            return Ok(proveedores);
        }

        // POST: api/restauranteproveedor
        [HttpPost]
        public async Task<ActionResult<RestauranteProveedorResponseDTO>> Create([FromBody] CreateRestauranteProveedorDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var asignacion = await _restauranteProveedorService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = asignacion.Id }, asignacion);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // PUT: api/restauranteproveedor/5
        [HttpPut("{id}")]
        public async Task<ActionResult<RestauranteProveedorResponseDTO>> Update(int id, [FromBody] UpdateRestauranteProveedorDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var asignacion = await _restauranteProveedorService.UpdateAsync(dto);

            if (asignacion == null)
                return NotFound(new { message = $"Asignación con ID {id} no encontrada" });

            return Ok(asignacion);
        }

        // DELETE: api/restauranteproveedor/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _restauranteProveedorService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Asignación con ID {id} no encontrada" });

            return NoContent();
        }
    }
}
