using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "CentralManager")]
    public class UserRolesController : BaseController<UserRole, IGenericRepository<UserRole>>
    {
        public UserRolesController(IGenericRepository<UserRole> repository) : base(repository) { }

        protected override int GetEntityId(UserRole entity) => entity.RoleID;

        [HttpGet("assign")]
        public async Task<IActionResult> AssignRoleToUser(int userId, int roleId)
        {
            // Logic to assign a role to a user
            return Ok();
        }
    }
}
