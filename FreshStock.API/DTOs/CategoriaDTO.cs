using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.DTOs
{
    // POST - Crear nueva categoría
    public class CreateCategoriaDTO
    {
        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; }
    }

    // GET - Respuesta
    public class CategoriaResponseDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
    }
}
