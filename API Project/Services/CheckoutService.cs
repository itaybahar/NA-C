using Domain_Project.Models;

public class CheckoutService : ICheckoutService
{
    private readonly ICheckoutRepository _repo;
    private readonly ITeamRepository _teamRepo;

    public CheckoutService(ICheckoutRepository repo, ITeamRepository teamRepo)
    {
        _repo = repo;
        _teamRepo = teamRepo;
    }

    public async Task CheckoutItemAsync(string teamId, string equipmentId)
    {
        var team = await _teamRepo.GetByIdAsync(teamId);
        if (team.IsBlacklisted)
            throw new InvalidOperationException("Team is blacklisted");

        var record = new CheckoutRecord
        {
            EquipmentId = equipmentId,
            TeamId = teamId
        };

        await _repo.AddAsync(record);
    }

    public async Task<List<CheckoutRecord>> GetUnreturnedByTeamAsync(string teamId)
    {
        return (await _repo.GetByTeamIdAsync(teamId))
            .Where(r => r.ReturnedAt == null).ToList();
    }

    public async Task AutoBlacklistOverdueAsync()
    {
        var overdue = await _repo.GetOverdueAsync(TimeSpan.FromHours(24));

        foreach (var record in overdue)
        {
            var team = await _teamRepo.GetByIdAsync(record.TeamId);
            if (!team.IsBlacklisted)
            {
                team.IsBlacklisted = true;
                await _teamRepo.UpdateAsync(team);
            }
        }
    }
}
