using Domain_Project.Models;

public interface ITeamRepository
{
    Task<Team> GetByIdAsync(string teamId);
    Task<Team> GetByIdAsync(int teamIdInt);
    Task UpdateAsync(Team team);
}
