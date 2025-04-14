using Domain_Project.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API_Project.Repositories
{
    public interface IBlacklistRepository
    {
        Task AddAsync(Blacklist blacklist);
        Task RemoveAsync(int blacklistId);
        Task<Blacklist?> GetByTeamIdAsync(int teamId);
        Task<List<Blacklist>> GetAllAsync();
        Task<bool> IsBlacklistedAsync(int teamId);
    }
}
