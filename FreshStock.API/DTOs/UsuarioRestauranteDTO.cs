using System.ComponentModel.DataAnnotations;
using FreshStock.API.Enums;

namespace FreshStock.API.DTOs
{
    // POST - Asignar usuario a restaurante
    public class CreateUsuarioRestauranteDTO
    {
        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public int RestauranteId { get; set; }

        [Required]
        [EnumDataType(typeof(RolUsuario), ErrorMessage = "El rol debe ser Admin (1), Gerente (2) o Empleado (3)")]
        public RolUsuario Rol { get; set; }
    }

    // PUT - Actualizar rol de usuario en restaurante
    public class UpdateUsuarioRestauranteDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [EnumDataType(typeof(RolUsuario), ErrorMessage = "El rol debe ser Admin (1), Gerente (2) o Empleado (3)")]
        public RolUsuario Rol { get; set; }

        [Required]
        public bool Activo { get; set; }
    }

    // GET - Respuesta
    public class UsuarioRestauranteResponseDTO
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int RestauranteId { get; set; }
        public RolUsuario Rol { get; set; }
        public bool Activo { get; set; }

        // Datos adicionales para facilitar el uso en frontend
        public string? NombreUsuario { get; set; }
        public string? NombreRestaurante { get; set; }
    }
}
