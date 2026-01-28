using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // Request para preview de importación
    public class ImportacionPreviewRequestDTO
    {
        [Required]
        public int ProveedorId { get; set; }

        [Required]
        public int CategoriaId { get; set; }

        [Required]
        public int RestauranteId { get; set; }
    }

    // Resultado del preview (sin guardar)
    public class ImportacionPreviewResultDTO
    {
        public int TotalFilas { get; set; }
        public int ProductosNuevos { get; set; }
        public int ProductosExistentes { get; set; }
        public int FilasConError { get; set; }
        public List<ProductoImportacionDTO> Productos { get; set; } = new();
        public List<ErrorImportacionDTO> Errores { get; set; } = new();
    }

    // DTO para cada producto parseado del Excel
    public class ProductoImportacionDTO
    {
        public int Fila { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal StockMinimo { get; set; }
        public decimal CostoUnitario { get; set; }
        public string? Descripcion { get; set; }
        public bool EsNuevo { get; set; }
        public int? ProductoExistenteId { get; set; }
        public bool TieneError { get; set; }
        public string? MensajeError { get; set; }
    }

    // Error de validación
    public class ErrorImportacionDTO
    {
        public int Fila { get; set; }
        public string Columna { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }

    // Request para ejecutar la importación
    public class ImportacionEjecutarRequestDTO
    {
        [Required]
        public int ProveedorId { get; set; }

        [Required]
        public int CategoriaId { get; set; }

        [Required]
        public int RestauranteId { get; set; }

        // Opción para actualizar productos existentes o solo crear nuevos
        public bool ActualizarExistentes { get; set; } = false;
    }

    // Resultado de la importación ejecutada
    public class ImportacionResultDTO
    {
        public bool Exitoso { get; set; }
        public int ProductosCreados { get; set; }
        public int ProductosActualizados { get; set; }
        public int ProductosOmitidos { get; set; }
        public int ErroresValidacion { get; set; }
        public List<ProductoImportadoDTO> Detalle { get; set; } = new();
        public List<ErrorImportacionDTO> Errores { get; set; } = new();
    }

    // Detalle de producto importado
    public class ProductoImportadoDTO
    {
        public int? Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty; // "Creado", "Actualizado", "Omitido"
    }

    // Request para exportar catálogo a Excel
    public class ExportacionRequestDTO
    {
        public int? ProveedorId { get; set; }
        public int? CategoriaId { get; set; }
        public int? RestauranteId { get; set; }
        public bool IncluirStock { get; set; } = false;
    }
}
