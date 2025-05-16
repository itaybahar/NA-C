using System.Collections.Generic;
using System.Threading.Tasks;
using Domain_Project.Models;

namespace Domain_Project.Interfaces
{
    public interface IBlacklistService
    {
        Task<bool> IsTeamBlacklistedAsync(int teamId);
        Task AddToBlacklistAsync(int teamId, int blacklistedBy, string reason);
        Task RemoveFromBlacklistAsync(int teamId, int removedBy, string? notes = null);
        Task<List<Blacklist>> GetAllBlacklistedTeamsAsync(int systemUserId = 1);
        Task<Blacklist?> GetBlacklistEntryAsync(int teamId);
        Task<IEnumerable<Blacklist>> GetActiveBlacklistsAsync(int systemUserId = 1);
    }

}
