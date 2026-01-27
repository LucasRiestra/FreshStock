namespace FreshStock.API.Entities
{
    public class Usuario : BaseEntity
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool Activo { get; set; }
        public bool EsSuperAdmin { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
    }
}
