using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs.Auth
{
    public class RegisterRequestDTO
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        public string Password { get; set; }

        [Required(ErrorMessage = "El rol es requerido")]
        [RegularExpression("^(Admin|Gerente|Empleado)$", ErrorMessage = "El rol debe ser Admin, Gerente o Empleado")]
        public string Rol { get; set; }
    }
}
