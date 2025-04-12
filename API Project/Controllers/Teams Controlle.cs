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
        private readonly IUnitOfWork _unitOfWork;

        public TeamsController(IGenericRepository<Team> repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

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
            // Validate the team exists
            var team = await _repository.GetByIdAsync(teamId);
            if (team == null)
            {
                return NotFound($"Team with ID {teamId} not found.");
            }

            // Logic to assign a user to a team
            // This would typically involve creating a TeamMember record
            var teamMember = new TeamMember
            {
                TeamID = teamId,
                UserID = userId,
                Team = team, // Required property
                User = new User
                {
                    UserID = userId,
                    Email = "placeholder@example.com", // Placeholder value
                    Username = "placeholderUsername", // Placeholder value
                    PasswordHash = "placeholderHash", // Placeholder value
                    FirstName = "PlaceholderFirstName", // Placeholder value
                    LastName = "PlaceholderLastName" // Placeholder value
                }
            };

            // Assuming there's a repository for TeamMember
            var teamMemberRepository = _unitOfWork.Repository<TeamMember>();
            await teamMemberRepository.AddAsync(teamMember);
            await _unitOfWork.CompleteAsync();

            return Ok($"User with ID {userId} assigned to Team with ID {teamId}.");
        }
    }
}