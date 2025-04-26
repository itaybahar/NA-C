using Blazor_WebAssembly.Models.Checkout;
using Blazor_WebAssembly.Models.Equipment;
using Domain_Project.DTOs;
using Domain_Project.DTOs.Domain_Project.DTOs.Domain_Project.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

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
