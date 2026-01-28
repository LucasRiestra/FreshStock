using FreshStock.API.DTOs;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ImportacionController : ControllerBase
    {
        private readonly IImportacionExcelService _importacionService;
        private readonly IPermisoService _permisoService;
        private readonly ILogger<ImportacionController> _logger;

        public ImportacionController(
            IImportacionExcelService importacionService,
            IPermisoService permisoService,
            ILogger<ImportacionController> logger)
        {
            _importacionService = importacionService;
            _permisoService = permisoService;
            _logger = logger;
        }

        /// <summary>
        /// Vista previa de la importación (no guarda nada)
        /// </summary>
        [HttpPost("preview")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ImportacionPreviewResultDTO>> Preview(
            IFormFile archivo,
            [FromForm] int proveedorId,
            [FromForm] int categoriaId,
            [FromForm] int restauranteId)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar permisos
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, restauranteId);
            if (!tienePermiso)
                return Forbid();

            // Validar archivo
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Debe proporcionar un archivo Excel" });

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest(new { message = "El archivo debe ser un Excel (.xlsx o .xls)" });

            try
            {
                var request = new ImportacionPreviewRequestDTO
                {
                    ProveedorId = proveedorId,
                    CategoriaId = categoriaId,
                    RestauranteId = restauranteId
                };

                var resultado = await _importacionService.PreviewAsync(archivo, request);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en preview de importación");
                return BadRequest(new { message = $"Error al procesar el archivo: {ex.Message}" });
            }
        }

        /// <summary>
        /// Ejecuta la importación guardando los productos
        /// </summary>
        [HttpPost("ejecutar")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ImportacionResultDTO>> Ejecutar(
            IFormFile archivo,
            [FromForm] int proveedorId,
            [FromForm] int categoriaId,
            [FromForm] int restauranteId,
            [FromForm] bool actualizarExistentes = false)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Verificar permisos
            var tienePermiso = await _permisoService.PuedeGestionarStockIdealAsync(usuarioId.Value, restauranteId);
            if (!tienePermiso)
                return Forbid();

            // Validar archivo
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Debe proporcionar un archivo Excel" });

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest(new { message = "El archivo debe ser un Excel (.xlsx o .xls)" });

            try
            {
                var request = new ImportacionEjecutarRequestDTO
                {
                    ProveedorId = proveedorId,
                    CategoriaId = categoriaId,
                    RestauranteId = restauranteId,
                    ActualizarExistentes = actualizarExistentes
                };

                var resultado = await _importacionService.EjecutarAsync(archivo, request);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ejecución de importación");
                return BadRequest(new { message = $"Error al importar: {ex.Message}" });
            }
        }

        /// <summary>
        /// Descarga la plantilla Excel vacía
        /// </summary>
        [HttpGet("plantilla")]
        [AllowAnonymous]
        public ActionResult DescargarPlantilla()
        {
            var archivo = _importacionService.GenerarPlantilla();
            return File(archivo,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "plantilla_productos.xlsx");
        }

        /// <summary>
        /// Exporta productos a Excel
        /// </summary>
        [HttpGet("exportar")]
        public async Task<ActionResult> Exportar(
            [FromQuery] int? proveedorId,
            [FromQuery] int? categoriaId,
            [FromQuery] int? restauranteId,
            [FromQuery] bool incluirStock = false)
        {
            var usuarioId = GetUsuarioIdFromToken();
            if (usuarioId == null)
                return Unauthorized(new { message = "No se pudo obtener el ID del usuario" });

            // Si se especifica restaurante, verificar permisos
            if (restauranteId.HasValue)
            {
                var tienePermiso = await _permisoService.TieneAccesoARestauranteAsync(usuarioId.Value, restauranteId.Value);
                var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
                if (!tienePermiso && !esSuperAdmin)
                    return Forbid();
            }
            else
            {
                // Sin restaurante específico, solo superadmin puede exportar todo
                var esSuperAdmin = await _permisoService.EsSuperAdminAsync(usuarioId.Value);
                if (!esSuperAdmin)
                    return Forbid();
            }

            try
            {
                var request = new ExportacionRequestDTO
                {
                    ProveedorId = proveedorId,
                    CategoriaId = categoriaId,
                    RestauranteId = restauranteId,
                    IncluirStock = incluirStock
                };

                var archivo = await _importacionService.ExportarAsync(request);

                var nombreArchivo = "productos";
                if (proveedorId.HasValue)
                    nombreArchivo += $"_prov{proveedorId}";
                if (categoriaId.HasValue)
                    nombreArchivo += $"_cat{categoriaId}";
                nombreArchivo += $"_{DateTime.UtcNow:yyyyMMdd}.xlsx";

                return File(archivo,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar productos");
                return BadRequest(new { message = $"Error al exportar: {ex.Message}" });
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
}
