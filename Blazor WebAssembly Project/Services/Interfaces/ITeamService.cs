
using static Blazor_WebAssembly_Project.Pages.TeamDetails;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface ITeamService
    {
        // Add the missing method
        Task<List<TeamDto>> GetBlacklistedTeamsAsync();

        // Your existing interface methods would be here
    }
}
