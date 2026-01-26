using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // POST - Asignar categoría a restaurante
    public class CreateRestauranteCategoriaDTO
    {
        [Required]
        public int RestauranteId { get; set; }

        [Required]
        public int CategoriaId { get; set; }
    }

    // PUT - Actualizar asignación
    public class UpdateRestauranteCategoriaDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public bool Activo { get; set; }
    }

    // GET - Respuesta
    public class RestauranteCategoriaResponseDTO
    {
        public int Id { get; set; }
        public int RestauranteId { get; set; }
        public int CategoriaId { get; set; }
        public bool Activo { get; set; }

        // Datos adicionales para facilitar el uso en frontend
        public string? NombreRestaurante { get; set; }
        public string? NombreCategoria { get; set; }
    }
}
