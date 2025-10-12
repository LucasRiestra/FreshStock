namespace FreshStock.API.Entities
{
    public class Usuario : BaseEntity
    {
        public int RestauranteId { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Rol { get; set; } 
        public bool Activo { get; set; }
    }
}
