using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain_Project.Interfaces;
using Domain_Project.Models;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "WarehouseManager,CentralManager")]
    public class UsersController : BaseController<User, IGenericRepository<User>>
    {
        public UsersController(IGenericRepository<User> repository) : base(repository) { }

        protected override int GetEntityId(User entity) => entity.UserID;

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var user = await _repository.FindAsync(u => u.Username == username);
            var userProfile = user.FirstOrDefault();

            if (userProfile == null)
            {
                return NotFound();
            }

            return Ok(userProfile);
        }

        [HttpGet("roles")]
        [Authorize(Roles = "WarehouseManager")]
        public async Task<IActionResult> GetUserRoles(int userId)
        {
            var userRoles = await _repository.FindAsync(u => u.UserID == userId);
            return Ok(userRoles);
        }
    }
}