using System.Collections.Generic;
using System.Threading.Tasks;
using Domain_Project.DTOs;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IBlacklistService
    {
        Task<List<BlacklistDto>> GetActiveBlacklistsAsync();
        Task<bool> AddToBlacklistAsync(BlacklistCreateDto dto);
        Task<bool> RemoveFromBlacklistAsync(int blacklistId);
    }
}
