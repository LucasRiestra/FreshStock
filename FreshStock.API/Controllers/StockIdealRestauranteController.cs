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
    public class StockIdealRestauranteController : ControllerBase
    {
        private readonly IStockIdealRestauranteService _stockIdealService;
        private readonly IPermisoService _permisoService;
        private readonly IUsuarioRestauranteService _usuarioRestauranteService;

        public StockIdealRestauranteController(
            IStockIdealRestauranteService stockIdealService,
            IPermisoService permisoService,
            IUsuarioRestauranteService usuarioRestauranteService)
        {
            _stockIdealService = stockIdealService;
            _permisoService = permisoService;
            _usuarioRestauranteService = usuarioRestauranteService;
        }

        // GET: api/stockidealrestaurante
        // SuperAdmin ve todo, Admin/Gerente ven solo sus restaurantes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockIdealRestauranteResponseDTO>>> GetAll()
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // SuperAdmin ve todo
            if (await _permisoService.EsSuperAdminAsync(usuarioId.Value))
            {
                var todos = await _stockIdealService.GetAllAsync();
                return Ok(todos);
            }

            // Obtener restaurantes donde el usuario es Admin o Gerente
            var restauranteIds = await _usuarioRestauranteService.GetRestaurantesIdsByUsuarioAsync(usuarioId.Value);

            var resultados = new List<StockIdealRestauranteResponseDTO>();
            foreach (var restauranteId in restauranteIds)
            {
                var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, restauranteId);
                if (tienePermiso)
                {
                    var items = await _stockIdealService.GetByRestauranteIdAsync(restauranteId);
                    resultados.AddRange(items);
                }
            }

            return Ok(resultados);
        }

        // GET: api/stockidealrestaurante/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StockIdealRestauranteResponseDTO>> GetById(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var stockIdeal = await _stockIdealService.GetByIdAsync(id);
            if (stockIdeal == null)
                return NotFound(new { message = $"Stock ideal con ID {id} no encontrado" });

            // Verificar permisos
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, stockIdeal.RestauranteId);
            if (!tienePermiso)
                return Forbid();

            return Ok(stockIdeal);
        }

        // GET: api/stockidealrestaurante/restaurante/5
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<StockIdealRestauranteResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar permisos (Admin, Gerente del restaurante o SuperAdmin)
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, restauranteId);
            if (!tienePermiso)
                return Forbid();

            var items = await _stockIdealService.GetByRestauranteIdAsync(restauranteId);
            return Ok(items);
        }

        // GET: api/stockidealrestaurante/producto/5/restaurante/1
        [HttpGet("producto/{productoId}/restaurante/{restauranteId}")]
        public async Task<ActionResult<StockIdealRestauranteResponseDTO>> GetByProductoRestaurante(int productoId, int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, restauranteId);
            if (!tienePermiso)
                return Forbid();

            var stockIdeal = await _stockIdealService.GetByProductoRestauranteAsync(productoId, restauranteId);
            if (stockIdeal == null)
                return NotFound(new { message = "No existe configuración de stock ideal para este producto en este restaurante" });

            return Ok(stockIdeal);
        }

        // POST: api/stockidealrestaurante
        // Solo Admin, Gerente del restaurante o SuperAdmin
        [HttpPost]
        public async Task<ActionResult<StockIdealRestauranteResponseDTO>> Create([FromBody] CreateStockIdealRestauranteDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar permisos
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, dto.RestauranteId);
            if (!tienePermiso)
                return Forbid();

            try
            {
                var stockIdeal = await _stockIdealService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = stockIdeal.Id }, stockIdeal);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/stockidealrestaurante/bulk
        // Solo Admin, Gerente del restaurante o SuperAdmin
        [HttpPost("bulk")]
        public async Task<ActionResult<IEnumerable<StockIdealRestauranteResponseDTO>>> CreateBulk([FromBody] CreateStockIdealBulkDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar permisos
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, dto.RestauranteId);
            if (!tienePermiso)
                return Forbid();

            try
            {
                var stockIdeales = await _stockIdealService.CreateBulkAsync(dto);
                return Ok(stockIdeales);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/stockidealrestaurante/5
        // Solo Admin, Gerente del restaurante o SuperAdmin
        [HttpPut("{id}")]
        public async Task<ActionResult<StockIdealRestauranteResponseDTO>> Update(int id, [FromBody] UpdateStockIdealRestauranteDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Obtener el stock ideal existente para verificar el restaurante
            var stockExistente = await _stockIdealService.GetByIdAsync(id);
            if (stockExistente == null)
                return NotFound(new { message = $"Stock ideal con ID {id} no encontrado" });

            // Verificar permisos
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, stockExistente.RestauranteId);
            if (!tienePermiso)
                return Forbid();

            try
            {
                var stockIdeal = await _stockIdealService.UpdateAsync(dto);
                return Ok(stockIdeal);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/stockidealrestaurante/5
        // Solo Admin, Gerente del restaurante o SuperAdmin
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Obtener el stock ideal existente para verificar el restaurante
            var stockExistente = await _stockIdealService.GetByIdAsync(id);
            if (stockExistente == null)
                return NotFound(new { message = $"Stock ideal con ID {id} no encontrado" });

            // Verificar permisos
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, stockExistente.RestauranteId);
            if (!tienePermiso)
                return Forbid();

            var result = await _stockIdealService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Stock ideal con ID {id} no encontrado" });

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
