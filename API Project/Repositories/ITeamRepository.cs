using Domain_Project.Models;

public interface ITeamRepository
{
    Task<Team> GetByIdAsync(string teamId);
    Task UpdateAsync(Team team);
}
