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
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaService _categoriaService;
        private readonly IRestauranteCategoriaService _restauranteCategoriaService;
        private readonly IPermisoService _permisoService;

        public CategoriaController(
            ICategoriaService categoriaService,
            IRestauranteCategoriaService restauranteCategoriaService,
            IPermisoService permisoService)
        {
            _categoriaService = categoriaService;
            _restauranteCategoriaService = restauranteCategoriaService;
            _permisoService = permisoService;
        }

        // GET: api/categoria
        // Retorna categorías filtradas por usuario (Admin ve todo, resto solo sus restaurantes)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaResponseDTO>>> GetAll()
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Si es Admin, ve todas las categorías
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);
            if (esAdmin)
            {
                var todasCategorias = await _categoriaService.GetAllAsync();
                return Ok(todasCategorias);
            }

            // Si no es Admin, solo ve categorías de sus restaurantes
            var categorias = await _restauranteCategoriaService.GetCategoriasByUsuarioAsync(usuarioId.Value);
            return Ok(categorias);
        }

        // GET: api/categoria/restaurante/5
        // Retorna las categorías de un restaurante específico
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<CategoriaResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar acceso al restaurante
            var tieneAcceso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);

            if (!tieneAcceso && !esAdmin)
                return Forbid();

            var categorias = await _restauranteCategoriaService.GetCategoriasByRestauranteIdAsync(restauranteId);
            return Ok(categorias);
        }

        // GET: api/categoria/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoriaResponseDTO>> GetById(int id)
        {
            var categoria = await _categoriaService.GetByIdAsync(id);

            if (categoria == null)
                return NotFound(new { message = $"Categoría con ID {id} no encontrada" });

            return Ok(categoria);
        }

        // POST: api/categoria
        // Solo Admin o Gerente pueden crear categorías
        [HttpPost]
        public async Task<ActionResult<CategoriaResponseDTO>> Create([FromBody] CreateCategoriaDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin o Gerente en al menos un restaurante
            var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(usuarioId.Value);
            if (!tienePermiso)
                return Forbid();

            var categoria = await _categoriaService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria);
        }

        // POST: api/categoria/crear-y-asignar
        // Crea la categoría y la asigna a un restaurante en una sola operación
        [HttpPost("crear-y-asignar/{restauranteId}")]
        public async Task<ActionResult<CategoriaResponseDTO>> CreateYAsignar(int restauranteId, [FromBody] CreateCategoriaDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar permiso en el restaurante específico
            var tienePermiso = await _permisoService.PuedeCrearCategoriasAsync(usuarioId.Value, restauranteId);
            if (!tienePermiso)
                return Forbid();

            // Crear la categoría
            var categoria = await _categoriaService.CreateAsync(dto);

            // Asignarla al restaurante
            await _restauranteCategoriaService.CreateAsync(new CreateRestauranteCategoriaDTO
            {
                RestauranteId = restauranteId,
                CategoriaId = categoria.Id
            });

            return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria);
        }

        // PUT: api/categoria/5
        // Solo Admin o Gerente pueden actualizar categorías
        [HttpPut("{id}")]
        public async Task<ActionResult<CategoriaResponseDTO>> Update(int id, [FromBody] UpdateCategoriaDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin o Gerente
            var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(usuarioId.Value);
            if (!tienePermiso)
                return Forbid();

            var categoria = await _categoriaService.UpdateAsync(dto);

            if (categoria == null)
                return NotFound(new { message = $"Categoría con ID {id} no encontrada" });

            return Ok(categoria);
        }

        // DELETE: api/categoria/5
        // Solo Admin o Gerente pueden eliminar categorías
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que sea Admin o Gerente
            var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(usuarioId.Value);
            if (!tienePermiso)
                return Forbid();

            var result = await _categoriaService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Categoría con ID {id} no encontrada" });

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
