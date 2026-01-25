namespace FreshStock.API.DTOs.Auth
{
    public class LoginResponseDTO
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expiration { get; set; }
        public FreshStock.API.DTOs.UsuarioResponseDTO Usuario { get; set; }
    }
}
