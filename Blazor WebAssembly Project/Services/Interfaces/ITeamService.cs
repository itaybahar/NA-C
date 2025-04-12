using Blazor_WebAssembly.Models.Team;
using Domain_Project.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface ITeamService
    {
        Task<List<TeamDto>> GetTeamsAsync();
        Task<List<TeamModel>> GetAllTeamsAsync();
        Task<TeamModel> GetTeamByIdAsync(int id);
        Task<bool> CreateTeamAsync(TeamModel team);
        Task<bool> UpdateTeamAsync(TeamModel team);
        Task<bool> DeleteTeamAsync(int id);
        Task<List<TeamMemberModel>> GetTeamMembersAsync(int teamId);
        Task<bool> AddTeamMemberAsync(TeamMemberModel teamMember);
        Task<bool> RemoveTeamMemberAsync(int teamId, int userId);

        // New method to fetch blacklisted teams
        Task<List<TeamDto>> GetBlacklistedTeamsAsync();
    }
}
