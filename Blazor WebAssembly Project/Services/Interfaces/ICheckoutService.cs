using Blazor_WebAssembly.Models.Checkout;
using Domain_Project.DTOs;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface ICheckoutService
    {
        Task<bool> ReturnEquipmentAsync(int checkoutId);
        Task<List<EquipmentCheckoutModel>> GetActiveCheckoutsAsync();
        Task<List<EquipmentCheckoutModel>> GetOverdueCheckoutsAsync();
        Task CheckoutEquipmentAsync(int teamId, int equipmentId, int userId);
        Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync();
    }
}
