using System.Collections.Generic;
using System.Threading.Tasks;
using Domain_Project.Models;

namespace Domain_Project.Interfaces
{
    public interface IBlacklistService
    {
        Task<IEnumerable<Blacklist>> GetActiveBlacklistsAsync();
        Task AddToBlacklistAsync(Blacklist blacklist);
        Task RemoveFromBlacklistAsync(int blacklistId);
    }
}
