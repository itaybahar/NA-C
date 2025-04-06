using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain_Project.Interfaces;
using Domain_Project.Models;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TeamsController : BaseController<Team, IGenericRepository<Team>>
    {
        public TeamsController(IGenericRepository<Team> repository) : base(repository) { }

        protected override int GetEntityId(Team entity) => entity.TeamID;

        [HttpGet("{teamId}/members")]
        [Authorize(Roles = "WarehouseManager")]
        public async Task<IActionResult> GetTeamMembers(int teamId)
        {
            var teamMembers = await _repository.FindAsync(t => t.TeamID == teamId);
            return Ok(teamMembers);
        }

        [HttpPost("{teamId}/assign-member")]
        [Authorize(Roles = "WarehouseManager")]
        public async Task<IActionResult> AssignMemberToTeam(int teamId, [FromBody] int userId)
        {
            // Logic to assign a user to a team
            // This would typically involve creating a TeamMember record
            return Ok();
        }
    }
}