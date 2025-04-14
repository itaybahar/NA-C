using Blazor_WebAssembly.Models.Team;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface ITeamService
    {
        Task<List<TeamModel>> GetAllTeamsAsync();
        Task<TeamModel> GetTeamByIdAsync(int id);
        Task<bool> AddTeam(TeamModel team);
        Task<bool> UpdateTeamAsync(TeamModel team);
        Task<bool> DeleteTeamAsync(int id);

        Task<List<TeamModel>> GetBlacklistedTeamsAsync();
        Task<bool> IsBlacklistedAsync(string teamId);
        Task AddToBlacklistAsync(string teamId);
        Task<bool> RemoveFromBlacklistAsync(string teamId);
    }
}
