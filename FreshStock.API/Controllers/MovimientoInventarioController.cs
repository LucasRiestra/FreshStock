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
    public class MovimientoInventarioController : ControllerBase
    {
        private readonly IMovimientoInventarioService _movimientoService;
        private readonly IPermisoService _permisoService;
        private readonly IUsuarioRestauranteService _usuarioRestauranteService;

        public MovimientoInventarioController(
            IMovimientoInventarioService movimientoService,
            IPermisoService permisoService,
            IUsuarioRestauranteService usuarioRestauranteService)
        {
            _movimientoService = movimientoService;
            _permisoService = permisoService;
            _usuarioRestauranteService = usuarioRestauranteService;
        }

        // GET: api/movimientoinventario
        // Retorna movimientos filtrados por usuario (Admin ve todo, resto solo sus restaurantes)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MovimientoInventarioResponseDTO>>> GetAll()
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Si es Admin, ve todos los movimientos
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);
            if (esAdmin)
            {
                var todosMovimientos = await _movimientoService.GetAllAsync();
                return Ok(todosMovimientos);
            }

            // Si no es Admin, solo ve movimientos de sus restaurantes
            var restauranteIds = await _usuarioRestauranteService.GetRestaurantesIdsByUsuarioAsync(usuarioId.Value);
            var movimientos = await _movimientoService.GetByRestaurantesIdsAsync(restauranteIds);
            return Ok(movimientos);
        }

        // GET: api/movimientoinventario/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MovimientoInventarioResponseDTO>> GetById(int id)
        {
            var movimiento = await _movimientoService.GetByIdAsync(id);

            if (movimiento == null)
                return NotFound(new { message = $"Movimiento con ID {id} no encontrado" });

            // Verificar acceso al restaurante del movimiento
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            var tieneAcceso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, movimiento.RestauranteId);
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);

            if (!tieneAcceso && !esAdmin)
                return Forbid();

            return Ok(movimiento);
        }

        // GET: api/movimientoinventario/restaurante/5
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<MovimientoInventarioResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar acceso al restaurante
            var tieneAcceso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);

            if (!tieneAcceso && !esAdmin)
                return Forbid();

            var movimientos = await _movimientoService.GetByRestauranteIdAsync(restauranteId);
            return Ok(movimientos);
        }

        // GET: api/movimientoinventario/producto/5
        [HttpGet("producto/{productoId}")]
        public async Task<ActionResult<IEnumerable<MovimientoInventarioResponseDTO>>> GetByProductoId(int productoId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Si es Admin, ve todos los movimientos del producto
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);
            if (esAdmin)
            {
                var todosMovimientos = await _movimientoService.GetByProductoIdAsync(productoId);
                return Ok(todosMovimientos);
            }

            // Si no es Admin, filtrar por sus restaurantes
            var restauranteIds = await _usuarioRestauranteService.GetRestaurantesIdsByUsuarioAsync(usuarioId.Value);
            var movimientos = await _movimientoService.GetByProductoIdAsync(productoId);
            var movimientosFiltrados = movimientos.Where(m => restauranteIds.Contains(m.RestauranteId));
            return Ok(movimientosFiltrados);
        }

        // GET: api/movimientoinventario/usuario/5
        [HttpGet("usuario/{targetUsuarioId}")]
        public async Task<ActionResult<IEnumerable<MovimientoInventarioResponseDTO>>> GetByUsuarioId(int targetUsuarioId)
        {
            var currentUserId = GetUsuarioIdFromToken();
            if (currentUserId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Un usuario puede ver sus propios movimientos
            if (currentUserId.Value == targetUsuarioId)
            {
                var misMovimientos = await _movimientoService.GetByUsuarioIdAsync(targetUsuarioId);
                return Ok(misMovimientos);
            }

            // Admin o Gerente pueden ver movimientos de otros usuarios
            var tienePermiso = await _permisoService.EsAdminOGerenteEnAlgunRestauranteAsync(currentUserId.Value);
            if (!tienePermiso)
                return Forbid();

            var movimientos = await _movimientoService.GetByUsuarioIdAsync(targetUsuarioId);
            return Ok(movimientos);
        }

        // POST: api/movimientoinventario
        // Cualquier usuario con acceso al restaurante puede crear movimientos
        [HttpPost]
        public async Task<ActionResult<MovimientoInventarioResponseDTO>> Create([FromBody] CreateMovimientoInventarioDTO dto)
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

            // Si es transferencia, verificar acceso al restaurante destino
            if (dto.RestauranteDestinoId.HasValue)
            {
                var tieneAccesoDestino = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, dto.RestauranteDestinoId.Value);
                var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);
                if (!tieneAccesoDestino && !esAdmin)
                    return Forbid();
            }

            try
            {
                var movimiento = await _movimientoService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = movimiento.Id }, movimiento);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/movimientoinventario/merma
        // Cualquier usuario con acceso al restaurante puede registrar merma
        [HttpPost("merma")]
        public async Task<ActionResult<MovimientoInventarioResponseDTO>> RegistrarMerma([FromBody] CreateMermaDTO dto)
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
                var movimiento = await _movimientoService.RegistrarMermaAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = movimiento.Id }, movimiento);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/movimientoinventario/5/revertir
        // Solo Admin o Gerente pueden revertir movimientos
        [HttpPost("{id}/revertir")]
        public async Task<ActionResult<MovimientoInventarioResponseDTO>> RevertirMovimiento(
            int id,
            [FromBody] RevertirMovimientoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Obtener el movimiento para verificar el restaurante
            var movimientoExistente = await _movimientoService.GetByIdAsync(id);
            if (movimientoExistente == null)
                return NotFound(new { message = $"Movimiento con ID {id} no encontrado" });

            // Solo Admin o Gerente pueden revertir movimientos
            var tienePermiso = await _permisoService.TieneRolMinimoEnRestauranteAsync(
                usuarioId.Value, movimientoExistente.RestauranteId, Enums.RolUsuario.Gerente);
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);

            if (!tienePermiso && !esAdmin)
                return Forbid();

            try
            {
                var movimiento = await _movimientoService.RevertirMovimientoAsync(id, usuarioId.Value, request.Motivo);
                return Ok(movimiento);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private int? GetUsuarioIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }
    }

    // DTO auxiliar para reversión
    public class RevertirMovimientoRequest
    {
        public string Motivo { get; set; } = string.Empty;
    }
}
