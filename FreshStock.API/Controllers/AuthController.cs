using FreshStock.API.DTOs.Auth;
using FreshStock.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Iniciar sesión con email y contraseña
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] LoginRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.LoginAsync(request);

            if (response == null)
                return Unauthorized(new { message = "Email o contraseña incorrectos" });

            return Ok(response);
        }

        /// <summary>
        /// Registrar un nuevo usuario
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<FreshStock.API.DTOs.UsuarioResponseDTO>> Register([FromBody] RegisterRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var usuario = await _authService.RegisterAsync(request);
                return CreatedAtAction(nameof(Register), new { id = usuario.Id }, usuario);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Renovar el access token usando el refresh token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDTO>> RefreshToken([FromBody] RefreshTokenRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (response == null)
                return Unauthorized(new { message = "Refresh token inválido o expirado" });

            return Ok(response);
        }

        /// <summary>
        /// Revocar el refresh token actual (logout)
        /// </summary>
        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequestDTO request)
        {
            var result = await _authService.RevokeTokenAsync(request.RefreshToken);

            if (!result)
                return BadRequest(new { message = "No se pudo revocar el token" });

            return Ok(new { message = "Token revocado exitosamente" });
        }
    }
}
