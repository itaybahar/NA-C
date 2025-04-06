using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API_Project.Controllers
{
    [ApiController]
    public abstract class BaseController<TEntity, TRepository> : ControllerBase
        where TEntity : class
        where TRepository : IGenericRepository<TEntity>
    {
        protected readonly TRepository _repository;

        protected BaseController(TRepository repository)
        {
            _repository = repository;
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
            await _repository.SaveChangesAsync();
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
            await _repository.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            await _repository.SaveChangesAsync();
            return NoContent();
        }

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

        protected abstract int GetEntityId(TEntity entity);
    }
}