using Domain_Project.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Interfaces
{
    // In Blazor WebAssembly Project/Services/Interfaces/IBlacklistService.cs
    public interface IBlacklistService
    {
        Task<List<TeamDto>> GetActiveBlacklistsAsync(int userId);
        Task<bool> AddToBlacklistAsync(BlacklistCreateDto dto);
        Task<bool> RemoveFromBlacklistAsync(int blacklistId);
        Task<bool> RemoveFromBlacklistAsync(int blacklistId, int removedByUserId);
    }


}
