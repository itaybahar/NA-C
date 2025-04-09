public interface ITeamService
{
    Task<bool> IsBlacklistedAsync(string teamId);
    Task AddToBlacklistAsync(string teamId);
    Task<bool> RemoveFromBlacklistAsync(string teamId);
}
