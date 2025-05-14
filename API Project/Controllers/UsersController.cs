using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain_Project.DTOs;
using System.Linq;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseController<User, IGenericRepository<User>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UsersController(IGenericRepository<User> repository, IUnitOfWork unitOfWork)
            : base(repository, unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        protected override int GetEntityId(User entity) => entity.UserID;

        /// <summary>
        /// Assign a role to a user.
        /// </summary>
        [HttpPost("{userId}/assign-role")]
        public async Task<IActionResult> AssignRole(int userId, [FromBody] AssignRoleDto assignRoleDto)
        {
            if (assignRoleDto == null || string.IsNullOrEmpty(assignRoleDto.Role))
                return BadRequest("Invalid role data.");

            if (!IsValidRole(assignRoleDto.Role))
                return BadRequest($"Invalid role: {assignRoleDto.Role}. Valid roles are: WarehouseOperator, WarehouseManager, Admin.");

            var user = await _repository.GetByIdAsync(userId);
            if (user == null)
                return NotFound($"User with ID {userId} not found.");

            user.Role = assignRoleDto.Role;
            await _repository.UpdateAsync(user);
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = $"Role '{assignRoleDto.Role}' assigned to user '{user.Username}'." });
        }

        /// <summary>
        /// Get all users as UserDto, with optional role filtering.
        /// </summary>
        [HttpGet("filter")]
        public async Task<IActionResult> GetAllWithRoleFilter([FromQuery] string? role = null)
        {
            var users = string.IsNullOrEmpty(role)
                ? await _repository.GetAllAsync()
                : await _repository.FindAsync(u => u.Role == role);

            var userDtos = users.Select(u => new UserDto
            {
                UserID = u.UserID,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role ?? ""
            }).ToList();

            return Ok(userDtos);
        }

        /// <summary>
        /// Validate if the provided role is valid.
        /// </summary>
        private bool IsValidRole(string role)
        {
            return role is "WarehouseOperator" or "WarehouseManager" or "Admin";
        }
    }
}
