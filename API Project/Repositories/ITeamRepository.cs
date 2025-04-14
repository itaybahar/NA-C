using Domain_Project.Models;

public interface ITeamRepository
{
    Task AddAsync(Team team);
    Task<IEnumerable<Team>> GetAllAsync();
    Task<Team> GetByStringIdAsync(string teamId); // Renamed for clarity
    Task<Team> GetByIntIdAsync(int teamIdInt);    // Renamed for clarity
    Task UpdateAsync(Team team);
    Task<bool?> GetByIdAsync(string teamId);
}
