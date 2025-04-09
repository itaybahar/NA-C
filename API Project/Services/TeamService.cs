public class TeamService : ITeamService
{
    private readonly ITeamRepository _repo;
    private readonly ICheckoutRepository _checkoutRepo;

    public TeamService(ITeamRepository repo, ICheckoutRepository checkoutRepo)
    {
        _repo = repo;
        _checkoutRepo = checkoutRepo;
    }

    public async Task<bool> IsBlacklistedAsync(string teamId)
    {
        var team = await _repo.GetByIdAsync(teamId);
        return team?.IsBlacklisted ?? false;
    }

    public async Task AddToBlacklistAsync(string teamId)
    {
        var team = await _repo.GetByIdAsync(teamId);
        if (team != null && !team.IsBlacklisted)
        {
            team.IsBlacklisted = true;
            await _repo.UpdateAsync(team);
        }
    }

    public async Task<bool> RemoveFromBlacklistAsync(string teamId)
    {
        var hasUnreturned = await _checkoutRepo.HasUnreturnedItemsAsync(teamId);
        if (hasUnreturned) return false;

        var team = await _repo.GetByIdAsync(teamId);
        if (team != null && team.IsBlacklisted)
        {
            team.IsBlacklisted = false;
            await _repo.UpdateAsync(team);
            return true;
        }

        return false;
    }
}
