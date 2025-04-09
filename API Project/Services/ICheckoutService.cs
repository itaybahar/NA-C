using Domain_Project.Models;

    
public interface ICheckoutService
{
    Task CheckoutItemAsync(string teamId, string equipmentId);
    Task<List<CheckoutRecord>> GetUnreturnedByTeamAsync(string teamId);
    Task AutoBlacklistOverdueAsync();
}
