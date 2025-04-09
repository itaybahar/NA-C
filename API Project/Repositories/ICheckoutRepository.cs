using Domain_Project.Models;

public interface ICheckoutRepository
{
    Task AddAsync(CheckoutRecord record);
    Task<List<CheckoutRecord>> GetByTeamIdAsync(string teamId);
    Task<List<CheckoutRecord>> GetOverdueAsync(TimeSpan overdueTime);
    Task<bool> HasUnreturnedItemsAsync(string teamId);
}
