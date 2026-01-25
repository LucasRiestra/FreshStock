using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.DTOs.Auth;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace FreshStock.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IMapper mapper, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request)
        {
            // Buscar usuario por email
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Activo);

            if (usuario == null)
                return null;

            // Verificar password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
                return null;

            // Generar tokens
            var accessToken = GenerateJwtToken(usuario);
            var refreshToken = GenerateRefreshToken();

            // Guardar refresh token
            usuario.RefreshToken = refreshToken;
            usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            var usuarioDto = _mapper.Map<UsuarioResponseDTO>(usuario);

            return new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddHours(1),
                Usuario = usuarioDto
            };
        }

        public async Task<UsuarioResponseDTO> RegisterAsync(RegisterRequestDTO request)
        {
            // Verificar si el email ya existe
            var existingUser = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
                throw new InvalidOperationException("El email ya está registrado");

            // Verificar que el restaurante existe
            var restaurante = await _context.Restaurantes
                .FirstOrDefaultAsync(r => r.Id == request.RestauranteId && r.Activo);

            if (restaurante == null)
                throw new InvalidOperationException("El restaurante no existe o está inactivo");

            // Crear nuevo usuario
            var usuario = new Usuario
            {
                RestauranteId = request.RestauranteId,
                Nombre = request.Nombre,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Rol = request.Rol,
                Activo = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return _mapper.Map<UsuarioResponseDTO>(usuario);
        }

        public async Task<LoginResponseDTO?> RefreshTokenAsync(string refreshToken)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.Activo);

            if (usuario == null || usuario.RefreshTokenExpiry < DateTime.UtcNow)
                return null;

            // Generar nuevos tokens
            var accessToken = GenerateJwtToken(usuario);
            var newRefreshToken = GenerateRefreshToken();

            // Actualizar refresh token
            usuario.RefreshToken = newRefreshToken;
            usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            var usuarioDto = _mapper.Map<UsuarioResponseDTO>(usuario);

            return new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                Expiration = DateTime.UtcNow.AddHours(1),
                Usuario = usuarioDto
            };
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (usuario == null)
                return false;

            usuario.RefreshToken = null;
            usuario.RefreshTokenExpiry = null;
            await _context.SaveChangesAsync();

            return true;
        }

        private string GenerateJwtToken(Usuario usuario)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("JWT SecretKey no está configurada");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("RestauranteId", usuario.RestauranteId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
