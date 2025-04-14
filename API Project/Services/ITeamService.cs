using Domain_Project.Models;

public interface ITeamService
{
    Task<bool> IsBlacklistedAsync(string teamId);
    Task AddToBlacklistAsync(string teamId);
    Task<bool> RemoveFromBlacklistAsync(string teamId);
    Task<bool> AddTeam(Team team);
    Task<IEnumerable<Team>> GetAllTeamsAsync();
    Task<Team> GetTeamByIdAsync(int id);
    Task<Team> GetByStringIdAsync(string teamId);
}
