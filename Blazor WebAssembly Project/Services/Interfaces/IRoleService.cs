
namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IRoleService
    {
        Task<bool> ApproveRequestAsync(int requestId, string notes);
        Task<bool> RejectRequestAsync(int requestId, string notes);
    }
}
