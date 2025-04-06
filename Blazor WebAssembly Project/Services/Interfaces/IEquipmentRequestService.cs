using System.Threading.Tasks;
using Blazor_WebAssembly.Models.Equipment;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IEquipmentRequestService
    {
        Task<List<EquipmentRequestModel>> GetPendingRequestsAsync();
        Task<bool> CreateEquipmentRequestAsync(EquipmentRequestModel request);
        Task<bool> ApproveRequestAsync(int requestId);
        Task<bool> RejectRequestAsync(int requestId, string reason);
    }
}