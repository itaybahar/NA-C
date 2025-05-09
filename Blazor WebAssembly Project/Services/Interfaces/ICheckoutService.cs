using Blazor_WebAssembly.Models.Checkout;
using Domain_Project.DTOs;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface ICheckoutService
    {
        Task<bool> ReturnEquipmentAsync(int checkoutId);
        Task<List<EquipmentCheckout>> GetActiveCheckoutsAsync();
        Task<List<EquipmentCheckout>> GetOverdueCheckoutsAsync();
        Task<bool> CheckoutEquipmentAsync(int teamId, int equipmentId, int userId, int quantity);
        Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync();
        Task<int> GetAvailableQuantityForEquipmentAsync(int equipmentId, int totalQuantity); // Updated to include totalQuantity
        Task<int> GetInUseQuantityForEquipmentAsync(int equipmentId);
    }


}
