using SharedKernel.Constants;
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
            var result = await _authService.Register(dto);
            if (!result.Success)
                return BadRequest(new { success = false, code = ErrorCodes.BusinessRule, message = result.Error });

            return Ok(new { success = true, message = "User created", user = result.User });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.Login(dto.Email, dto.Password);
            if (result == null)
                return Unauthorized(new { success = false, code = ErrorCodes.Unauthorized, message = "Invalid credentials" });

            if (!string.IsNullOrEmpty(result.Error))
                return Unauthorized(new { success = false, code = ErrorCodes.Unauthorized, message = result.Error });

            // set refresh token cookie
            Response.Cookies.Append("refreshToken", result.Tokens!.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            // Return structured response: tokens + sanitized user (no passwordHash)
            return Ok(new
            {
                success = true,
                message = "Login success",
                tokens = result.Tokens,
                user = result.User
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
                return Unauthorized(new { success = false, code = ErrorCodes.Unauthorized, message = "No refresh token" });

            var tokens = await _authService.RefreshToken(refreshToken);
            if (tokens == null)
                return Unauthorized(new { success = false, code = ErrorCodes.Unauthorized, message = "Invalid refresh token" });

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
