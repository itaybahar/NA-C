using System.Threading.Tasks;
using Blazor_WebAssembly.Models.Checkout;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface ICheckoutService
    {
        Task<List<EquipmentCheckoutModel>> GetAllCheckoutsAsync();
        Task<EquipmentCheckoutModel> GetCheckoutByIdAsync(int id);
        Task<bool> CreateCheckoutAsync(EquipmentCheckoutModel checkout);
        Task<bool> ReturnEquipmentAsync(int checkoutId);
        Task<List<EquipmentCheckoutModel>> GetActiveCheckoutsAsync();
        Task<List<EquipmentCheckoutModel>> GetOverdueCheckoutsAsync();
    }
}