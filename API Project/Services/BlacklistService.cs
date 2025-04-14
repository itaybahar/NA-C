using API_Project.Repositories;
using Domain_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain_Project.Interfaces;

namespace API_Project.Services
{
    public class BlacklistService : IBlacklistService
    {
        private readonly IBlacklistRepository _blacklistRepository;

        public BlacklistService(IBlacklistRepository blacklistRepository)
        {
            _blacklistRepository = blacklistRepository;
        }

        /// <summary>
        /// Checks if a team is blacklisted.
        /// </summary>
        public async Task<bool> IsTeamBlacklistedAsync(int teamId)
        {
            return await _blacklistRepository.IsBlacklistedAsync(teamId);
        }

        /// <summary>
        /// Adds a team to the blacklist.
        /// </summary>
        public async Task AddToBlacklistAsync(int teamId, int blacklistedBy, string reason)
        {
            if (await _blacklistRepository.IsBlacklistedAsync(teamId))
            {
                throw new InvalidOperationException($"Team with ID {teamId} is already blacklisted.");
            }

            var blacklistEntry = new Blacklist
            {
                TeamID = teamId,
                BlacklistedBy = blacklistedBy,
                ReasonForBlacklisting = reason,
                BlacklistDate = DateTime.UtcNow
            };

            await _blacklistRepository.AddAsync(blacklistEntry);
        }

        /// <summary>
        /// Removes a team from the blacklist.
        /// </summary>
        public async Task RemoveFromBlacklistAsync(int teamId, int removedBy, string? notes = null)
        {
            var entry = await _blacklistRepository.GetByTeamIdAsync(teamId);
            if (entry == null)
            {
                throw new KeyNotFoundException($"Team with ID {teamId} is not blacklisted.");
            }

            entry.RemovalDate = DateTime.UtcNow;
            entry.RemovedBy = removedBy;
            entry.Notes = notes;

            await _blacklistRepository.RemoveAsync(entry.BlacklistID);
        }

        /// <summary>
        /// Retrieves all blacklisted teams.
        /// </summary>
        public async Task<List<Blacklist>> GetAllBlacklistedTeamsAsync()
        {
            return await _blacklistRepository.GetAllAsync();
        }

        /// <summary>
        /// Retrieves the blacklist entry for a specific team.
        /// </summary>
        public async Task<Blacklist?> GetBlacklistEntryAsync(int teamId)
        {
            return await _blacklistRepository.GetByTeamIdAsync(teamId);
        }

        /// <summary>
        /// Retrieves all active blacklist entries (entries without a removal date).
        /// </summary>
        public async Task<IEnumerable<Blacklist>> GetActiveBlacklistsAsync()
        {
            var allBlacklists = await _blacklistRepository.GetAllAsync();
            return allBlacklists.Where(b => b.RemovalDate == null);
        }
    }
}
