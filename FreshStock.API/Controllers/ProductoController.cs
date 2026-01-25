using FreshStock.API.DTOs;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _productoService;

        public ProductoController(IProductoService productoService)
        {
            _productoService = productoService;
        }

        // GET: api/producto
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoResponseDTO>>> GetAll()
        {
            var productos = await _productoService.GetAllAsync();
            return Ok(productos);
        }

        // GET: api/producto/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoResponseDTO>> GetById(int id)
        {
            var producto = await _productoService.GetByIdAsync(id);

            if (producto == null)
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });

            return Ok(producto);
        }

        // GET: api/producto/categoria/5
        [HttpGet("categoria/{categoriaId}")]
        public async Task<ActionResult<IEnumerable<ProductoResponseDTO>>> GetByCategoriaId(int categoriaId)
        {
            var productos = await _productoService.GetByCategoriaIdAsync(categoriaId);
            return Ok(productos);
        }

        // GET: api/producto/proveedor/5
        [HttpGet("proveedor/{proveedorId}")]
        public async Task<ActionResult<IEnumerable<ProductoResponseDTO>>> GetByProveedorId(int proveedorId)
        {
            var productos = await _productoService.GetByProveedorIdAsync(proveedorId);
            return Ok(productos);
        }

        // POST: api/producto
        [HttpPost]
        public async Task<ActionResult<ProductoResponseDTO>> Create([FromBody] CreateProductoDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var producto = await _productoService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto);
        }

        // POST: api/producto/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<IEnumerable<ProductoResponseDTO>>> CreateBulk([FromBody] CreateProductosBulkDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var productos = await _productoService.CreateBulkAsync(dto);
                return Ok(new { message = $"{productos.Count()} productos creados exitosamente", productos });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/producto/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ProductoResponseDTO>> Update(int id, [FromBody] UpdateProductoDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var producto = await _productoService.UpdateAsync(dto);

            if (producto == null)
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });

            return Ok(producto);
        }

        // DELETE: api/producto/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _productoService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Producto con ID {id} no encontrado" });

            return NoContent();
        }
    }
}
