// File: Blazor WebAssembly Project/Utilities/BrowserConsoleLogger/IEquipmentReturnService.cs
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IEquipmentReturnService
    {
        Task<bool> UpdateReturnedEquipmentAsync(int equipmentId, int checkoutId, int userId, string condition = "Good", string notes = "");
        Task<bool> UpdateReturnedEquipmentByTeamAsync(int equipmentId, int teamId, int userId, string condition = "Good", string notes = "");
        Task<int?> GetCheckoutIdByTeamAndEquipmentAsync(int teamId, int equipmentId);
        Task<bool> UpdateTeamAmountAsync(int teamId, int equipmentId);
    }

}
