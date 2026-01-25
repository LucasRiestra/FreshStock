using FreshStock.API.DTOs;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaService _categoriaService;

        public CategoriaController(ICategoriaService categoriaService)
        {
            _categoriaService = categoriaService;
        }

        // GET: api/categoria
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaResponseDTO>>> GetAll()
        {
            var categorias = await _categoriaService.GetAllAsync();
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
        [HttpPost]
        public async Task<ActionResult<CategoriaResponseDTO>> Create([FromBody] CreateCategoriaDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var categoria = await _categoriaService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria);
        }

        // DELETE: api/categoria/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _categoriaService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Categoría con ID {id} no encontrada" });

            return NoContent();
        }
    }
}
