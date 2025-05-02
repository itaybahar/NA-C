using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain_Project.DTOs;

namespace API_Project.Controllers
{
    [ApiController]
    public abstract class BaseController<TEntity, TRepository> : ControllerBase
        where TEntity : class
        where TRepository : IGenericRepository<TEntity>
    {
        protected readonly TRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        protected BaseController(TRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public virtual async Task<IActionResult> GetAll()
        {
            var entities = await _repository.GetAllAsync();
            return Ok(entities);
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] TEntity entity)
        {
            await _repository.AddAsync(entity);
            await _unitOfWork.CompleteAsync();
            return CreatedAtAction(nameof(GetById), new { id = GetEntityId(entity) }, entity);
        }

        [HttpPut("{id}")]
        public virtual async Task<IActionResult> Update(int id, [FromBody] TEntity entity)
        {
            if (id != GetEntityId(entity))
            {
                return BadRequest();
            }

            await _repository.UpdateAsync(entity);
            await _unitOfWork.CompleteAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            await _repository.DeleteAsync(entity);
            await _unitOfWork.CompleteAsync();
            return NoContent();
        }

        protected abstract int GetEntityId(TEntity entity);
    }

    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,WarehouseManager")] // Restrict access to authorized roles
    public class UsersController : BaseController<User, IGenericRepository<User>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UsersController(IGenericRepository<User> repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        protected override int GetEntityId(User entity) => entity.UserID;

        /// <summary>
        /// Assign a role to a user.
        /// </summary>
        /// <param name="userId">The ID of the user to assign the role to.</param>
        /// <param name="assignRoleDto">The role assignment data.</param>
        /// <returns>An IActionResult indicating success or failure.</returns>
        [HttpPost("{userId}/assign-role")]
        public async Task<IActionResult> AssignRole(int userId, [FromBody] AssignRoleDto assignRoleDto)
        {
            if (assignRoleDto == null || string.IsNullOrEmpty(assignRoleDto.Role))
            {
                return BadRequest("Invalid role data.");
            }

            // Validate the role
            if (!IsValidRole(assignRoleDto.Role))
            {
                return BadRequest($"Invalid role: {assignRoleDto.Role}. Valid roles are: WarehouseOperator, WarehouseManager, Admin.");
            }

            // Get the user
            var user = await _repository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            // Assign the role
            user.Role = assignRoleDto.Role;
            await _repository.UpdateAsync(user);
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = $"Role '{assignRoleDto.Role}' assigned to user '{user.Username}'." });
        }

        /// <summary>
        /// Get all users with optional role filtering.
        /// </summary>
        /// <param name="role">Optional role to filter users by.</param>
        /// <returns>A list of users.</returns>
        [HttpGet("filter")]  // Changed route to avoid conflict
        public async Task<IActionResult> GetAllUsers([FromQuery] string? role = null)
        {
            var users = string.IsNullOrEmpty(role)
                ? await _repository.GetAllAsync()
                : await _repository.FindAsync(u => u.Role == role);

            return Ok(users);
        }

        // Override the base GetAll method to avoid the conflict
        [HttpGet]
        public override async Task<IActionResult> GetAll()
        {
            // Call the implementation in the GetAllUsers method with no role filter
            return await GetAllUsers();
        }

        /// <summary>
        /// Validate if the provided role is valid.
        /// </summary>
        /// <param name="role">The role to validate.</param>
        /// <returns>True if the role is valid, otherwise false.</returns>
        private bool IsValidRole(string role)
        {
            return role is "WarehouseOperator" or "WarehouseManager" or "Admin";
        }
    }
}
