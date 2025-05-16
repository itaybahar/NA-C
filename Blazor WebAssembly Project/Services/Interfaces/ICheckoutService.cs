// Path should be: Blazor WebAssembly Project/Services/Interfaces/ICheckoutService.cs
using Domain_Project.DTOs;
using Domain_Project.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Blazor_WebAssembly.Services.Implementations.CheckoutService;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface ICheckoutService
    {
        Task<List<EquipmentCheckout>> GetActiveCheckoutsAsync();
        Task<List<EquipmentCheckout>> GetOverdueCheckoutsAsync();
        Task<bool> ReturnEquipmentAsync(int checkoutId);

        // Update the return type to match the implementation
        Task<(bool Success, string? ErrorMessage, string? OverdueEquipmentName, List<OverdueEquipmentInfo>? OverdueItems)>
            CheckoutEquipmentAsync(int teamId, int equipmentId, int userId, int quantity);

        Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync();
        Task<int> GetInUseQuantityForEquipmentAsync(int equipmentId);
        Task<int> GetAvailableQuantityForEquipmentAsync(int equipmentId, int totalQuantity);
        Task<bool> AddAdminHistoryRecordAsync(CheckoutRecordDto record);
    }
}
