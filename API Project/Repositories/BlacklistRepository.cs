using API_Project.Data;
using Domain_Project.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_Project.Repositories
{
    public class BlacklistRepository : IBlacklistRepository
    {
        private readonly EquipmentManagementContext _context;

        private readonly ILogger<CheckoutRepository> _logger;

        public BlacklistRepository(
            EquipmentManagementContext dbContext,
            ILogger<CheckoutRepository> logger)
        {
            _context = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AddAsync(Blacklist blacklist)
        {
            // Validate and fix BlacklistedBy - ensure it's never 0
            if (blacklist.BlacklistedBy <= 0)
            {
                // Use a valid default system user ID
                blacklist.BlacklistedBy = 1;
                _logger.LogWarning("Invalid BlacklistedBy value detected and corrected to 1: {BlacklistId}, Team: {TeamId}",
                    blacklist.BlacklistID, blacklist.TeamID);
            }

            // Validate the reason text length
            if (!string.IsNullOrEmpty(blacklist.ReasonForBlacklisting) && blacklist.ReasonForBlacklisting.Length > 200)
            {
                // Truncate reason to fit database column
                blacklist.ReasonForBlacklisting = blacklist.ReasonForBlacklisting.Substring(0, 197) + "...";
                _logger.LogWarning("ReasonForBlacklisting was truncated for blacklist entry for team {TeamId}", blacklist.TeamID);
            }

            await _context.Blacklists.AddAsync(blacklist);
            await _context.SaveChangesAsync();
        }
        public async Task RemoveAsync(int blacklistId)
        {
            var entry = await _context.Blacklists.FindAsync(blacklistId);
            if (entry != null)
            {
                // Instead of physically removing, set the removal date
                entry.RemovalDate = DateTime.UtcNow;
                _context.Blacklists.Update(entry);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Blacklist?> GetByTeamIdAsync(int teamId)
        {
            return await _context.Blacklists
                .Where(b => b.TeamID == teamId && b.RemovalDate == null)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Blacklist>> GetAllAsync()
        {
            return await _context.Blacklists.ToListAsync();
        }

        public async Task<bool> IsTeamBlacklistedAsync(int teamId)
        {
            return await _context.Blacklists
                .AnyAsync(b => b.TeamID == teamId && b.RemovalDate == null);
        }
    }
}
