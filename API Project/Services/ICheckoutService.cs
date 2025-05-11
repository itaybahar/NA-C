using API_Project.Services;
using Domain_Project.Models;
using Domain_Project.DTOs;


public interface ICheckoutService
{
    Task CheckoutItemAsync(string teamId, string equipmentId);
    Task<List<CheckoutRecord>> GetUnreturnedByTeamAsync(string teamId);
    Task AutoBlacklistOverdueAsync();
    Task<List<EquipmentCheckout>> GetAllCheckoutsAsync();
    Task<EquipmentCheckout> GetCheckoutByIdAsync(int id);
    Task<bool> CreateCheckoutAsync(EquipmentCheckout checkout);
    Task<List<EquipmentCheckout>> GetActiveCheckoutsAsync();
    Task<List<EquipmentCheckout>> GetOverdueCheckoutsAsync();
    Task<bool> CheckoutEquipmentAsync(int teamId, int equipmentId, int userId, int quantity);
    Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync();
}
