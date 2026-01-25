using FreshStock.API.DTOs;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MovimientoInventarioController : ControllerBase
    {
        private readonly IMovimientoInventarioService _movimientoService;

        public MovimientoInventarioController(IMovimientoInventarioService movimientoService)
        {
            _movimientoService = movimientoService;
        }

        // GET: api/movimientoinventario
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MovimientoInventarioResponseDTO>>> GetAll()
        {
            var movimientos = await _movimientoService.GetAllAsync();
            return Ok(movimientos);
        }

        // GET: api/movimientoinventario/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MovimientoInventarioResponseDTO>> GetById(int id)
        {
            var movimiento = await _movimientoService.GetByIdAsync(id);

            if (movimiento == null)
                return NotFound(new { message = $"Movimiento con ID {id} no encontrado" });

            return Ok(movimiento);
        }

        // GET: api/movimientoinventario/restaurante/5
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<MovimientoInventarioResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var movimientos = await _movimientoService.GetByRestauranteIdAsync(restauranteId);
            return Ok(movimientos);
        }

        // GET: api/movimientoinventario/producto/5
        [HttpGet("producto/{productoId}")]
        public async Task<ActionResult<IEnumerable<MovimientoInventarioResponseDTO>>> GetByProductoId(int productoId)
        {
            var movimientos = await _movimientoService.GetByProductoIdAsync(productoId);
            return Ok(movimientos);
        }

        // GET: api/movimientoinventario/usuario/5
        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<MovimientoInventarioResponseDTO>>> GetByUsuarioId(int usuarioId)
        {
            var movimientos = await _movimientoService.GetByUsuarioIdAsync(usuarioId);
            return Ok(movimientos);
        }

        // POST: api/movimientoinventario
        [HttpPost]
        public async Task<ActionResult<MovimientoInventarioResponseDTO>> Create([FromBody] CreateMovimientoInventarioDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
        [HttpPost("merma")]
        public async Task<ActionResult<MovimientoInventarioResponseDTO>> RegistrarMerma([FromBody] CreateMermaDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
        [HttpPost("{id}/revertir")]
        public async Task<ActionResult<MovimientoInventarioResponseDTO>> RevertirMovimiento(
            int id,
            [FromBody] RevertirMovimientoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var movimiento = await _movimientoService.RevertirMovimientoAsync(id, request.UsuarioId, request.Motivo);
                return Ok(movimiento);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // DTO auxiliar para reversión
    public class RevertirMovimientoRequest
    {
        public int UsuarioId { get; set; }
        public string Motivo { get; set; }
    }
}
