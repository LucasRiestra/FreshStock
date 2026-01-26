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
    public class ProveedorController : ControllerBase
    {
        private readonly IProveedorService _proveedorService;
        private readonly IRestauranteProveedorService _restauranteProveedorService;
        private readonly IPermisoService _permisoService;

        public ProveedorController(
            IProveedorService proveedorService,
            IRestauranteProveedorService restauranteProveedorService,
            IPermisoService permisoService)
        {
            _proveedorService = proveedorService;
            _restauranteProveedorService = restauranteProveedorService;
            _permisoService = permisoService;
        }

        // GET: api/proveedor
        // Retorna proveedores filtrados por usuario (Admin ve todo, resto solo sus restaurantes)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProveedorResponseDTO>>> GetAll()
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Si es Admin, ve todos los proveedores
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);
            if (esAdmin)
            {
                var todosProveedores = await _proveedorService.GetAllAsync();
                return Ok(todosProveedores);
            }

            // Si no es Admin, solo ve proveedores de sus restaurantes
            var proveedores = await _restauranteProveedorService.GetProveedoresByUsuarioAsync(usuarioId.Value);
            return Ok(proveedores);
        }

        // GET: api/proveedor/restaurante/5
        // Retorna los proveedores de un restaurante específico
        [HttpGet("restaurante/{restauranteId}")]
        public async Task<ActionResult<IEnumerable<ProveedorResponseDTO>>> GetByRestauranteId(int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar acceso al restaurante
            var tieneAcceso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId);
            var esAdmin = await _permisoService.EsAdminEnAlgunRestauranteAsync(usuarioId.Value);

            if (!tieneAcceso && !esAdmin)
                return Forbid();

            var proveedores = await _restauranteProveedorService.GetProveedoresByRestauranteIdAsync(restauranteId);
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
        // Solo Admin o Gerente pueden crear proveedores
        [HttpPost]
        public async Task<ActionResult<ProveedorResponseDTO>> Create([FromBody] CreateProveedorDTO dto)
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

            var proveedor = await _proveedorService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = proveedor.Id }, proveedor);
        }

        // POST: api/proveedor/crear-y-asignar/5
        // Crea el proveedor y lo asigna a un restaurante en una sola operación
        [HttpPost("crear-y-asignar/{restauranteId}")]
        public async Task<ActionResult<ProveedorResponseDTO>> CreateYAsignar(int restauranteId, [FromBody] CreateProveedorDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar permiso en el restaurante específico
            var tienePermiso = await _permisoService.PuedeCrearProveedoresAsync(usuarioId.Value, restauranteId);
            if (!tienePermiso)
                return Forbid();

            // Crear el proveedor
            var proveedor = await _proveedorService.CreateAsync(dto);

            // Asignarlo al restaurante
            await _restauranteProveedorService.CreateAsync(new CreateRestauranteProveedorDTO
            {
                RestauranteId = restauranteId,
                ProveedorId = proveedor.Id
            });

            return CreatedAtAction(nameof(GetById), new { id = proveedor.Id }, proveedor);
        }

        // PUT: api/proveedor/5
        // Solo Admin o Gerente pueden actualizar proveedores
        [HttpPut("{id}")]
        public async Task<ActionResult<ProveedorResponseDTO>> Update(int id, [FromBody] UpdateProveedorDTO dto)
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

            var proveedor = await _proveedorService.UpdateAsync(dto);

            if (proveedor == null)
                return NotFound(new { message = $"Proveedor con ID {id} no encontrado" });

            return Ok(proveedor);
        }

        // DELETE: api/proveedor/5
        // Solo Admin o Gerente pueden eliminar proveedores
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

            var result = await _proveedorService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = $"Proveedor con ID {id} no encontrado" });

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
