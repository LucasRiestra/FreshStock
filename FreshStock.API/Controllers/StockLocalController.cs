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
    public class StockLocalController : ControllerBase
    {
        private readonly IStockLocalService _stockLocalService;
        private readonly IPermisoService _permisoService;
        private readonly IUsuarioRestauranteService _usuarioRestauranteService;

        public StockLocalController(
            IStockLocalService stockLocalService,
            IPermisoService permisoService,
            IUsuarioRestauranteService usuarioRestauranteService)
        {
            _stockLocalService = stockLocalService;
            _permisoService = permisoService;
            _usuarioRestauranteService = usuarioRestauranteService;
        }

        // GET: api/stocklocal
        // Retorna stock filtrado por usuario (Admin ve todo, resto solo sus restaurantes)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockLocalResponseDTO>>> GetAll()
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Si es Admin, ve todo el stock
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);
            if (esAdmin)
            {
                var todoStock = await _stockLocalService.GetAllAsync();
                return Ok(todoStock);
            }

            // Si no es Admin, solo ve stock de sus restaurantes
            var restauranteIds = await _usuarioRestauranteService.GetRestaurantesIdsByUsuarioAsync(usuarioId.Value);
            var stocks = await _stockLocalService.GetByRestaurantesIdsAsync(restauranteIds);
            return Ok(stocks);
        }

        // GET: api/stocklocal/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StockLocalResponseDTO>> GetById(int id)
        {
            var stock = await _stockLocalService.GetByIdAsync(id);

            if (stock == null)
                return NotFound(new { message = $"Stock con ID {id} no encontrado" });

            // Verificar acceso al restaurante del stock
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tieneAcceso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, stock.RestauranteId);
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);

            if (!tieneAcceso && !esAdmin)
                return Forbid();

            return Ok(stock);
        }

        // GET: api/stocklocal/restaurante/5
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<StockLocalResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar acceso al restaurante
            var tieneAcceso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);

            if (!tieneAcceso && !esAdmin)
                return Forbid();

            var stocks = await _stockLocalService.GetByRestauranteIdAsync(restauranteId);
            return Ok(stocks);
        }

        // GET: api/stocklocal/producto/5
        [HttpGet("producto/{productoId}")]
        public async Task<ActionResult<IEnumerable<StockLocalResponseDTO>>> GetByProductoId(int productoId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Si es Admin, ve todo el stock del producto
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);
            if (esAdmin)
            {
                var todoStock = await _stockLocalService.GetByProductoIdAsync(productoId);
                return Ok(todoStock);
            }

            // Si no es Admin, filtrar por sus restaurantes
            var restauranteIds = await _usuarioRestauranteService.GetRestaurantesIdsByUsuarioAsync(usuarioId.Value);
            var stocks = await _stockLocalService.GetByProductoIdAsync(productoId);
            var stocksFiltrados = stocks.Where(s => restauranteIds.Contains(s.RestauranteId));
            return Ok(stocksFiltrados);
        }

        // GET: api/stocklocal/lote?productoId=5&restauranteId=1&lote=LOTE001
        [HttpGet("lote")]
        public async Task<ActionResult<StockLocalResponseDTO>> GetByLote(
            [FromQuery] int productoId,
            [FromQuery] int restauranteId,
            [FromQuery] string lote)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar acceso al restaurante
            var tieneAcceso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);

            if (!tieneAcceso && !esAdmin)
                return Forbid();

            var stock = await _stockLocalService.GetByLoteAsync(productoId, restauranteId, lote);

            if (stock == null)
                return NotFound(new { message = "Stock no encontrado con los parámetros proporcionados" });

            return Ok(stock);
        }

        // POST: api/stocklocal
        // Cualquier usuario con acceso al restaurante puede gestionar inventario
        [HttpPost]
        public async Task<ActionResult<StockLocalResponseDTO>> Create([FromBody] CreateStockLocalDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar que tiene permiso para gestionar inventario en el restaurante
            var tienePermiso = await _permisoService.PuedeGestionarInventarioAsync(usuarioId.Value, dto.RestauranteId);
            if (!tienePermiso)
                return Forbid();

            try
            {
                var stock = await _stockLocalService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = stock.Id }, stock);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/stocklocal/5
        // Cualquier usuario con acceso al restaurante puede gestionar inventario
        [HttpPut("{id}")]
        public async Task<ActionResult<StockLocalResponseDTO>> Update(int id, [FromBody] UpdateStockLocalDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Obtener el stock para verificar el restaurante
            var stockExistente = await _stockLocalService.GetByIdAsync(id);
            if (stockExistente == null)
                return NotFound(new { message = $"Stock con ID {id} no encontrado" });

            // Verificar que tiene permiso para gestionar inventario en el restaurante
            var tienePermiso = await _permisoService.PuedeGestionarInventarioAsync(usuarioId.Value, stockExistente.RestauranteId);
            if (!tienePermiso)
                return Forbid();

            var stock = await _stockLocalService.UpdateAsync(dto);
            return Ok(stock);
        }

        // DELETE: api/stocklocal/5
        // Solo Admin o Gerente pueden eliminar stock
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Obtener el stock para verificar el restaurante
            var stockExistente = await _stockLocalService.GetByIdAsync(id);
            if (stockExistente == null)
                return NotFound(new { message = $"Stock con ID {id} no encontrado" });

            // Solo Admin o Gerente pueden eliminar stock
            var tienePermiso = await _permisoService.TieneRolMinimoEnRestauranteAsync(
                usuarioId.Value, stockExistente.RestauranteId, Enums.RolUsuario.Gerente);
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);

            if (!tienePermiso && !esAdmin)
                return Forbid();

            var result = await _stockLocalService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Stock con ID {id} no encontrado" });

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
