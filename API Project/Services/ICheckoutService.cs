using API_Project.Services;
using Domain_Project.Models;
using Domain_Project.DTOs;

public interface ICheckoutService
{
    Task CheckoutItemAsync(string teamId, string equipmentId);
    Task<List<CheckoutRecord>> GetUnreturnedByTeamAsync(string teamId);
    Task AutoBlacklistOverdueAsync(int systemUserId = 1);

    Task<List<EquipmentCheckout>> GetAllCheckoutsAsync();
    Task<EquipmentCheckout> GetCheckoutByIdAsync(int id);
    Task<bool> CreateCheckoutAsync(EquipmentCheckout checkout);
    Task<List<EquipmentCheckout>> GetActiveCheckoutsAsync();
    Task<List<EquipmentCheckout>> GetOverdueCheckoutsAsync();
    Task<(bool Success, string? ErrorMessage, string? OverdueEquipmentName, List<EquipmentCheckout>? OverdueItems)> CheckoutEquipmentAsync(int teamId, int equipmentId, int userId, int quantity);

    Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync();

    // Methods for equipment quantity management
    Task<int> GetInUseQuantityForEquipmentAsync(int equipmentId);
    Task<int> GetAvailableQuantityForEquipmentAsync(int equipmentId, int totalQuantity);
    Task<bool> ReturnEquipmentAsync(int checkoutId);
    Task<bool> AddAdminHistoryRecordAsync(CheckoutRecordDto record);
    
    // Methods for team and overdue handling
    Task<Team> GetTeamByIdAsync(int teamId);
    Task<List<EquipmentCheckout>> GetOverdueItemsByTeamAsync(int teamId);
    Task<List<BlacklistedTeamDto>> GetBlacklistedTeamsAsync();
}
