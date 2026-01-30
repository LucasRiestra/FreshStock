using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // POST - Crear nuevo proveedor
    public class CreateProveedorDTO
    {
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(20)]
        public string Telefono { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MaxLength(100)]
        public string Contacto { get; set; }

        // Lista de IDs de restaurantes a asociar
        // Si es null o vacía, el proveedor es global (disponible para todos)
        public List<int>? RestauranteIds { get; set; }
    }

    // PUT - Actualizar proveedor
    public class UpdateProveedorDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(20)]
        public string Telefono { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MaxLength(100)]
        public string Contacto { get; set; }

        [Required]
        public bool Activo { get; set; }

        // Lista de IDs de restaurantes a asociar
        // Si es null o vacía, el proveedor es global (disponible para todos)
        public List<int>? RestauranteIds { get; set; }
    }

    // GET - Respuesta
    public class ProveedorResponseDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Contacto { get; set; }
        public bool Activo { get; set; }
        public List<int>? RestauranteIds { get; set; }
    }
}
