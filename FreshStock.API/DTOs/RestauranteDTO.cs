using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // POST - Crear nuevo restaurante
    public class CreateRestauranteDTO
    {
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(200)]
        public string Direccion { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }
    }

    // PUT - Actualizar restaurante
    public class UpdateRestauranteDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(200)]
        public string Direccion { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [Required]
        public bool Activo { get; set; }
    }

    // GET - Respuesta
    public class RestauranteResponseDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Direccion { get; set; }
        public string? Telefono { get; set; }
        public bool Activo { get; set; }
    }
}
