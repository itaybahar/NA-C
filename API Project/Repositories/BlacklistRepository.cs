using Domain_Project.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_Project.Repositories
{
    public class BlacklistRepository : IBlacklistRepository
    {
        private readonly List<Blacklist> _blacklist = new();

        public async Task AddAsync(Blacklist blacklist)
        {
            // Simulate async database operation
            await Task.Run(() => _blacklist.Add(blacklist));
        }

        public async Task RemoveAsync(int blacklistId)
        {
            // Simulate async database operation
            var entry = _blacklist.FirstOrDefault(b => b.BlacklistID == blacklistId);
            if (entry != null)
            {
                await Task.Run(() => _blacklist.Remove(entry));
            }
        }

        public async Task<Blacklist?> GetByTeamIdAsync(int teamId)
        {
            // Simulate async database operation
            return await Task.FromResult(_blacklist.FirstOrDefault(b => b.TeamID == teamId));
        }

        public async Task<List<Blacklist>> GetAllAsync()
        {
            // Simulate async database operation
            return await Task.FromResult(_blacklist.ToList());
        }

        public async Task<bool> IsBlacklistedAsync(int teamId)
        {
            // Simulate async database operation
            return await Task.FromResult(_blacklist.Any(b => b.TeamID == teamId));
        }
    }
}
