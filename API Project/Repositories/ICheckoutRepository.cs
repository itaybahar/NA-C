using Domain_Project.DTOs;
using Domain_Project.Models;
// Use a specific import to avoid ambiguity

public interface ICheckoutRepository
{
    Task AddAsync(CheckoutRecord record);
    Task<List<CheckoutRecord>> GetByTeamIdAsync(string teamId);
    Task<List<CheckoutRecord>> GetOverdueAsync(TimeSpan overdueTime);
    Task<bool> HasUnreturnedItemsAsync(string teamId);
    // Use the aliased type to avoid ambiguity
    Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync();
}
