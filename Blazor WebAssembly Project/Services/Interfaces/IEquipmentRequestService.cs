// In Blazor WebAssembly Project/Services/Interfaces/IEquipmentRequestService.cs
using Blazor_WebAssembly.Models.Equipment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IEquipmentRequestService
    {
        Task<List<EquipmentRequestModel>> GetPendingRequestsAsync();
        Task<bool> CreateEquipmentRequestAsync(EquipmentRequestModel request);
        Task<bool> ApproveRequestAsync(int requestId);
        Task<bool> RejectRequestAsync(int requestId, string reason);
        Task SendEquipmentRequestAsync(string message);

        // Add this new method declaration
        Task<List<EquipmentModel>> GetCheckedOutEquipmentByTeamAsync(int teamId);
    }
}
