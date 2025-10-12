using FreshStock.API.DTOs;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProveedorController : ControllerBase
    {
        private readonly IProveedorService _proveedorService;

        public ProveedorController(IProveedorService proveedorService)
        {
            _proveedorService = proveedorService;
        }

        // GET: api/proveedor
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProveedorResponseDTO>>> GetAll()
        {
            var proveedores = await _proveedorService.GetAllAsync();
            return Ok(proveedores);
        }

        // GET: api/proveedor/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProveedorResponseDTO>> GetById(int id)
        {
            var proveedor = await _proveedorService.GetByIdAsync(id);

            if (proveedor == null)
                return NotFound(new { message = $"Proveedor con ID {id} no encontrado" });

            return Ok(proveedor);
        }

        // POST: api/proveedor
        [HttpPost]
        public async Task<ActionResult<ProveedorResponseDTO>> Create([FromBody] CreateProveedorDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var proveedor = await _proveedorService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = proveedor.Id }, proveedor);
        }

        // PUT: api/proveedor/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ProveedorResponseDTO>> Update(int id, [FromBody] UpdateProveedorDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "El ID de la URL no coincide con el ID del body" });

            var proveedor = await _proveedorService.UpdateAsync(dto);

            if (proveedor == null)
                return NotFound(new { message = $"Proveedor con ID {id} no encontrado" });

            return Ok(proveedor);
        }

        // DELETE: api/proveedor/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _proveedorService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Proveedor con ID {id} no encontrado" });

            return NoContent();
        }
    }
}
