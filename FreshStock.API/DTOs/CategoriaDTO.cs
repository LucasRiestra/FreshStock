using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // POST - Crear nueva categoría
    public class CreateCategoriaDTO
    {
        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; }

        // Lista de IDs de restaurantes a asociar
        // Si es null o vacía, la categoría es global (disponible para todos)
        public List<int>? RestauranteIds { get; set; }
    }

    // PUT - Actualizar categoría
    public class UpdateCategoriaDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; }

        // Lista de IDs de restaurantes a asociar
        // Si es null o vacía, la categoría es global (disponible para todos)
        public List<int>? RestauranteIds { get; set; }
    }

    // GET - Respuesta
    public class CategoriaResponseDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public List<int>? RestauranteIds { get; set; }
    }
}
