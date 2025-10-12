using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // POST - Crear nuevo usuario
    public class CreateUsuarioDTO
    {
        [Required]
        public int RestauranteId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        [MaxLength(255)]
        public string Password { get; set; }

        [Required]
        [MaxLength(50)]
        public string Rol { get; set; }
    }

    // PUT - Actualizar usuario (sin password)
    public class UpdateUsuarioDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MaxLength(50)]
        public string Rol { get; set; }

        [Required]
        public bool Activo { get; set; }
    }

    // GET - Respuesta (sin password)
    public class UsuarioResponseDTO
    {
        public int Id { get; set; }
        public int RestauranteId { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public bool Activo { get; set; }
    }
}
