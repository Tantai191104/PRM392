using Microsoft.AspNetCore.Mvc;
using AuthService.Application.Services;
using AuthService.Application.DTOs;

namespace AuthService.Web.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthAppService _authService;

        public AuthController(AuthAppService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = await _authService.Register(dto.Email, dto.Password);
            return Ok(new { success = true, message = "User created", data = user });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var tokens = await _authService.Login(dto.Email, dto.Password);
            if (tokens == null) return Unauthorized(new { success = false, message = "Invalid credentials" });
            Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            // Return accessToken and refreshToken at top-level for easier client parsing
            return Ok(new
            {
                success = true,
                message = "Login success",
                accessToken = tokens.AccessToken,
                refreshToken = tokens.RefreshToken
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
                return Unauthorized(new { success = false, message = "No refresh token" });

            var tokens = await _authService.RefreshToken(refreshToken);
            if (tokens == null) return Unauthorized(new { success = false, message = "Invalid refresh token" });

            Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new { success = true, message = "Token refreshed", data = tokens });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
            {
                await _authService.Logout(refreshToken);
                Response.Cookies.Delete("refreshToken");
            }
            return Ok(new { success = true, message = "Logged out" });
        }
    }
}
