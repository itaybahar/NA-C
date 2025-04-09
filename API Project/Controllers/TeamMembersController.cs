using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "WarehouseManager")]
    public class TeamMembersController : BaseController<TeamMember, IGenericRepository<TeamMember>>
    {
        public TeamMembersController(IGenericRepository<TeamMember> repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork) { }

        protected override int GetEntityId(TeamMember entity) => entity.TeamID; // Assuming composite key

        [HttpPost("add")]
        public async Task<IActionResult> AddMemberToTeam([FromBody] TeamMemberDto teamMemberDto)
        {
            // Logic to add a member to a team
            return Ok();
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveMemberFromTeam(int teamId, int userId)
        {
            // Logic to remove a member from a team
            return Ok();
        }
    }
}
