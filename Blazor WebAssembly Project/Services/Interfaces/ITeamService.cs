using System.Threading.Tasks;
using Blazor_WebAssembly.Models.Team;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface ITeamService
    {
        Task<List<TeamModel>> GetAllTeamsAsync();
        Task<TeamModel> GetTeamByIdAsync(int id);
        Task<bool> CreateTeamAsync(TeamModel team);
        Task<bool> UpdateTeamAsync(TeamModel team);
        Task<bool> DeleteTeamAsync(int id);
        Task<List<TeamMemberModel>> GetTeamMembersAsync(int teamId);
        Task<bool> AddTeamMemberAsync(TeamMemberModel teamMember);
        Task<bool> RemoveTeamMemberAsync(int teamId, int userId);
    }
}