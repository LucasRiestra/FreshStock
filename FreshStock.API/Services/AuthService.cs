using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.DTOs.Auth;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;
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
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AuthService(MongoDbContext context, IMapper mapper, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request)
        {
            // Buscar usuario por email
            var usuario = await _context.Usuarios
                .Find(u => u.Email == request.Email && u.Activo)
                .FirstOrDefaultAsync();

            if (usuario == null)
                return null;

            // Verificar password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
                return null;

            // Generar tokens
            var accessToken = GenerateJwtToken(usuario);
            var refreshToken = GenerateRefreshToken();

            // Guardar refresh token
            var update = Builders<Usuario>.Update
                .Set(u => u.RefreshToken, refreshToken)
                .Set(u => u.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7));
            await _context.Usuarios.UpdateOneAsync(u => u.Id == usuario.Id, update);

            usuario.RefreshToken = refreshToken;
            usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

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
                .Find(u => u.Email == request.Email)
                .FirstOrDefaultAsync();

            if (existingUser != null)
                throw new InvalidOperationException("El email ya está registrado");

            // Obtener el siguiente ID secuencial
            var newId = await _context.GetNextSequenceAsync("usuarios");

            // Crear nuevo usuario
            var usuario = new Usuario
            {
                Id = newId,
                Nombre = request.Nombre,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Rol = request.Rol,
                Activo = true
            };

            await _context.Usuarios.InsertOneAsync(usuario);

            return _mapper.Map<UsuarioResponseDTO>(usuario);
        }

        public async Task<LoginResponseDTO?> RefreshTokenAsync(string refreshToken)
        {
            var usuario = await _context.Usuarios
                .Find(u => u.RefreshToken == refreshToken && u.Activo)
                .FirstOrDefaultAsync();

            if (usuario == null || usuario.RefreshTokenExpiry < DateTime.UtcNow)
                return null;

            // Generar nuevos tokens
            var accessToken = GenerateJwtToken(usuario);
            var newRefreshToken = GenerateRefreshToken();

            // Actualizar refresh token
            var update = Builders<Usuario>.Update
                .Set(u => u.RefreshToken, newRefreshToken)
                .Set(u => u.RefreshTokenExpiry, DateTime.UtcNow.AddDays(7));
            await _context.Usuarios.UpdateOneAsync(u => u.Id == usuario.Id, update);

            usuario.RefreshToken = newRefreshToken;
            usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

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
                .Find(u => u.RefreshToken == refreshToken)
                .FirstOrDefaultAsync();

            if (usuario == null)
                return false;

            var update = Builders<Usuario>.Update
                .Set(u => u.RefreshToken, (string?)null)
                .Set(u => u.RefreshTokenExpiry, (DateTime?)null);
            await _context.Usuarios.UpdateOneAsync(u => u.Id == usuario.Id, update);

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
