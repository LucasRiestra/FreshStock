using FreshStock.API.DTOs.Auth;

namespace FreshStock.API.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request);
        Task<FreshStock.API.DTOs.UsuarioResponseDTO> RegisterAsync(RegisterRequestDTO request);
        Task<LoginResponseDTO?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
    }
}
