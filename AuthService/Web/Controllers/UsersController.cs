using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuthService.Infrastructure.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            return Ok(new { id = u.Id, email = u.Email, role = u.Role.ToString() });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _repo.GetAllAsync();
            var userDtos = new List<object>();
            foreach (var user in users)
            {
                userDtos.Add(new { id = user.Id, email = user.Email, role = user.Role.ToString() });
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
    }
}
