using Domain_Project.DTOs;
using Domain_Project.DTOs.Domain_Project.DTOs.Domain_Project.Models;
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
        private readonly IUnitOfWork _unitOfWork;

        public TeamMembersController(IGenericRepository<TeamMember> repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
            _unitOfWork = unitOfWork; // Assign the injected unitOfWork to a private field
        }

        protected override int GetEntityId(TeamMember entity) => entity.TeamID; // Assuming composite key

        [HttpPost("add")]
        public async Task<IActionResult> AddMemberToTeam([FromBody] TeamMemberDto teamMemberDto)
        {
            if (teamMemberDto == null || !teamMemberDto.IsValid())
            {
                return BadRequest("Invalid team member data.");
            }

            var teamMember = TeamMember.Create(teamMemberDto.TeamID, teamMemberDto.UserID, teamMemberDto.AssignedRole);
            teamMember.JoinDate = DateTime.UtcNow;
            teamMember.IsActive = true;

            await _repository.AddAsync(teamMember);
            await _unitOfWork.CompleteAsync();

            return Ok("Team member added successfully.");
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveMemberFromTeam(int teamId, int userId)
        {
            var teamMember = (await _repository.FindAsync(tm => tm.TeamID == teamId && tm.UserID == userId)).FirstOrDefault();
            if (teamMember == null)
            {
                return NotFound("Team member not found.");
            }

            await _repository.DeleteAsync(teamMember);
            await _unitOfWork.CompleteAsync();

            return Ok("Team member removed successfully.");
        }
    }
}
