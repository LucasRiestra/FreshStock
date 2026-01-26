using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // POST - Asignar proveedor a restaurante
    public class CreateRestauranteProveedorDTO
    {
        [Required]
        public int RestauranteId { get; set; }

        [Required]
        public int ProveedorId { get; set; }
    }

    // PUT - Actualizar asignación
    public class UpdateRestauranteProveedorDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public bool Activo { get; set; }
    }

    // GET - Respuesta
    public class RestauranteProveedorResponseDTO
    {
        public int Id { get; set; }
        public int RestauranteId { get; set; }
        public int ProveedorId { get; set; }
        public bool Activo { get; set; }

        // Datos adicionales para facilitar el uso en frontend
        public string? NombreRestaurante { get; set; }
        public string? NombreProveedor { get; set; }
    }
}
