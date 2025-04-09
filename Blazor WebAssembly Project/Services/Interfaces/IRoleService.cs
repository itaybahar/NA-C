using static Blazor_WebAssembly_Project.Pages.RequestRole;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleRequestHistoryItem>> GetPendingRequestsAsync();
        Task<bool> ApproveRequestAsync(int requestId, string notes);
        Task<bool> RejectRequestAsync(int requestId, string notes);
    }
}
