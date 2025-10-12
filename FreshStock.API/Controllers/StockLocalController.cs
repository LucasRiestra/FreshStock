using FreshStock.API.DTOs;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockLocalController : ControllerBase
    {
        private readonly IStockLocalService _stockLocalService;

        public StockLocalController(IStockLocalService stockLocalService)
        {
            _stockLocalService = stockLocalService;
        }

        // GET: api/stocklocal
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockLocalResponseDTO>>> GetAll()
        {
            var stocks = await _stockLocalService.GetAllAsync();
            return Ok(stocks);
        }

        // GET: api/stocklocal/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StockLocalResponseDTO>> GetById(int id)
        {
            var stock = await _stockLocalService.GetByIdAsync(id);

            if (stock == null)
                return NotFound(new { message = $"Stock con ID {id} no encontrado" });

            return Ok(stock);
        }

        // GET: api/stocklocal/restaurante/5
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<StockLocalResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var stocks = await _stockLocalService.GetByRestauranteIdAsync(restauranteId);
            return Ok(stocks);
        }

        // GET: api/stocklocal/producto/5
        [HttpGet("producto/{productoId}")]
        public async Task<ActionResult<IEnumerable<StockLocalResponseDTO>>> GetByProductoId(int productoId)
        {
            var stocks = await _stockLocalService.GetByProductoIdAsync(productoId);
            return Ok(stocks);
        }

        // GET: api/stocklocal/lote?productoId=5&restauranteId=1&lote=LOTE001
        [HttpGet("lote")]
        public async Task<ActionResult<StockLocalResponseDTO>> GetByLote(
            [FromQuery] int productoId,
            [FromQuery] int restauranteId,
            [FromQuery] string lote)
        {
            var stock = await _stockLocalService.GetByLoteAsync(productoId, restauranteId, lote);

            if (stock == null)
                return NotFound(new { message = "Stock no encontrado con los parámetros proporcionados" });

            return Ok(stock);
        }

        // POST: api/stocklocal
        [HttpPost]
        public async Task<ActionResult<StockLocalResponseDTO>> Create([FromBody] CreateStockLocalDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
        [HttpPut("{id}")]
        public async Task<ActionResult<StockLocalResponseDTO>> Update(int id, [FromBody] UpdateStockLocalDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var stock = await _stockLocalService.UpdateAsync(dto);

            if (stock == null)
                return NotFound(new { message = $"Stock con ID {id} no encontrado" });

            return Ok(stock);
        }

        // DELETE: api/stocklocal/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _stockLocalService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Stock con ID {id} no encontrado" });

            return NoContent();
        }
    }
}
