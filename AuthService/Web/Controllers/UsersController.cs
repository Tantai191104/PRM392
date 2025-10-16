using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuthService.Infrastructure.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;
using AuthService.Application.DTOs;

namespace AuthService.Web.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly UserRepository _repo;

        public UsersController(UserRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var u = await _repo.GetByIdAsync(id);
            if (u == null) return NotFound();
            var resp = new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                DisplayName = u.DisplayName,
                Phone = u.Phone,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                Address = u.Address,
                Role = u.Role.ToString(),
                IsActive = u.IsActive
            };
            return Ok(resp);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _repo.GetAllAsync();
            var userDtos = new List<UserResponseDto>();
            foreach (var user in users)
            {
                userDtos.Add(new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    DisplayName = user.DisplayName,
                    Phone = user.Phone,
                    AvatarUrl = user.AvatarUrl,
                    Bio = user.Bio,
                    Address = user.Address,
                    Role = user.Role.ToString(),
                    IsActive = user.IsActive
                });
            }
            return Ok(userDtos);
        }

        [HttpPost("{id}/ban")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BanUser(string id)
        {
            var user = await _repo.GetByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = false;
            await _repo.UpdateAsync(user);

            return Ok(new { message = "User banned successfully." });
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(string id, [FromBody] UpdateProfileDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            // Allow only the owner or Admin role to update the profile
            var callerId = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                           ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var isAdmin = User?.IsInRole("Admin") ?? false;
            if (!isAdmin && callerId != id)
                return Forbid("You are not allowed to update this profile.");

            var updated = await _repo.UpdateProfileAsync(id, dto);

            if (updated == null) return NotFound();

            var resp = new UserResponseDto
            {
                Id = updated.Id,
                Email = updated.Email,
                FullName = updated.FullName,
                DisplayName = updated.DisplayName,
                Phone = updated.Phone,
                AvatarUrl = updated.AvatarUrl,
                Bio = updated.Bio,
                Address = updated.Address,
                Role = updated.Role.ToString(),
                IsActive = updated.IsActive
            };

            return Ok(resp);
        }

        // GET /api/users/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var callerId = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                           ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (callerId == null) return Unauthorized();

            var user = await _repo.GetByIdAsync(callerId);
            if (user == null) return NotFound();

            var resp = new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                DisplayName = user.DisplayName,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                Bio = user.Bio,
                Address = user.Address,
                Role = user.Role.ToString(),
                IsActive = user.IsActive
            };

            return Ok(resp);
        }
    }
}
