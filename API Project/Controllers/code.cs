using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [Authorize(Roles = "WarehouseManager,CentralManager")]
    public class UsersController : BaseController<User, IGenericRepository<User>>
    {
        public UsersController(IGenericRepository<User> repository, IUnitOfWork unitOfWork)
            : base(repository, unitOfWork) { }

        protected override int GetEntityId(User entity) => entity.Username.GetHashCode();

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var users = await _repository.FindAsync(u => u.Username == username);
            var userProfile = users.FirstOrDefault();

            if (userProfile == null)
            {
                return NotFound();
            }

            return Ok(userProfile);
        }

        [HttpGet("user-roles/{userId}")]
        [Authorize(Roles = "WarehouseManager")]
        public async Task<IActionResult> GetUserRoles(int userId)
        {
            var userRoles = await _repository.FindAsync(u => u.Username.GetHashCode() == userId);
            return Ok(userRoles);
        }
    }

    [Route("api/user-profiles")]
    [Authorize(Roles = "WarehouseManager,CentralManager")]
    public class UserProfilesController : BaseController<User, IGenericRepository<User>>
    {
        public UserProfilesController(IGenericRepository<User> repository, IUnitOfWork unitOfWork)
            : base(repository, unitOfWork) { }

        protected override int GetEntityId(User entity) => entity.Username.GetHashCode();

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var users = await _repository.FindAsync(u => u.Username == username);
            var userProfile = users.FirstOrDefault();

            if (userProfile == null)
            {
                return NotFound();
            }

            return Ok(userProfile);
        }

        [HttpGet("user-roles/{userId}")]
        [Authorize(Roles = "WarehouseManager")]
        public async Task<IActionResult> GetUserRoles(int userId)
        {
            var userRoles = await _repository.FindAsync(u => u.Username.GetHashCode() == userId);
            return Ok(userRoles);
        }
    }
}
