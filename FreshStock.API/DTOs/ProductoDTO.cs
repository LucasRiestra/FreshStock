using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // POST - Crear nuevo producto
    public class CreateProductoDTO
    {
        [Required]
        public int ProveedorId { get; set; }

        [Required]
        public int CategoriaId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(20)]
        public string UnidadMedida { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal StockMinimo { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal CostoUnitario { get; set; }
    }

    // PUT - Actualizar producto
    public class UpdateProductoDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int ProveedorId { get; set; }

        [Required]
        public int CategoriaId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(20)]
        public string UnidadMedida { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal StockMinimo { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal CostoUnitario { get; set; }

        [Required]
        public bool Activo { get; set; }
    }

    // GET - Respuesta
    public class ProductoResponseDTO
    {
        public int Id { get; set; }
        public int ProveedorId { get; set; }
        public int CategoriaId { get; set; }
        public string Nombre { get; set; }
        public string UnidadMedida { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal CostoUnitario { get; set; }
        public bool Activo { get; set; }
    }

    // POST - Crear múltiples productos a la vez
    public class CreateProductosBulkDTO
    {
        [Required]
        public int ProveedorId { get; set; }

        [Required]
        public int CategoriaId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe proporcionar al menos un producto")]
        public List<ProductoItemDTO> Productos { get; set; }
    }

    public class ProductoItemDTO
    {
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(20)]
        public string UnidadMedida { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal StockMinimo { get; set; }

        [Required]
        [Range(0, 999999.99)]
        public decimal CostoUnitario { get; set; }
    }
}
