using System.Collections.Generic;
using System.Threading.Tasks;
using Domain_Project.Models;

namespace Domain_Project.Interfaces
{
    public interface IBlacklistService
    {
        /// <summary>
        /// Retrieves all active blacklist entries (entries without a removal date).
        /// </summary>
        Task<IEnumerable<Blacklist>> GetActiveBlacklistsAsync();

        /// <summary>
        /// Adds a team to the blacklist.
        /// </summary>
        Task AddToBlacklistAsync(int teamId, int blacklistedBy, string reason);

        /// <summary>
        /// Removes a team from the blacklist.
        /// </summary>
        Task RemoveFromBlacklistAsync(int teamId, int removedBy, string? notes = null);

        /// <summary>
        /// Checks if a team is blacklisted.
        /// </summary>
        Task<bool> IsTeamBlacklistedAsync(int teamId);

        /// <summary>
        /// Retrieves all blacklisted teams.
        /// </summary>
        Task<List<Blacklist>> GetAllBlacklistedTeamsAsync();

        /// <summary>
        /// Retrieves the blacklist entry for a specific team.
        /// </summary>
        Task<Blacklist?> GetBlacklistEntryAsync(int teamId);
    }
}
