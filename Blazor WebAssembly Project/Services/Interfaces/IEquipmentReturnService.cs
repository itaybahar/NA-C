using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IEquipmentReturnService
    {
        Task<bool> UpdateReturnedEquipmentAsync(int equipmentId, int checkoutId, int userId, int quantity, string condition = "Good", string notes = "");
        Task<bool> UpdateReturnedEquipmentByTeamAsync(int equipmentId, int teamId, int userId, int quantity, string condition = "Good", string notes = "");
        Task<int?> GetCheckoutIdByTeamAndEquipmentAsync(int teamId, int equipmentId);
        Task<bool> UpdateTeamAmountAsync(int teamId, int equipmentId, int quantity);

        // Methods for tracking available equipment quantities
        Task<int> GetInUseQuantityForEquipmentAsync(int equipmentId);
        Task<int> GetAvailableQuantityForEquipmentAsync(int equipmentId, int totalQuantity);
    }
}
