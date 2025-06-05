// Add this to API Project/Repositories/ITeamRepository.cs
using Domain_Project.Interfaces;
using Domain_Project.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ITeamRepository : IGenericRepository<Team>
{
    Task<Team> GetByStringIdAsync(string teamId);
    Task<Team> GetByIntIdAsync(int teamIdInt);
    Task<bool?> GetByIdAsync(string teamId);
    Task<IEnumerable<Team>> GetActiveTeamsAsync();
    Task<IEnumerable<Team>> GetBlacklistedTeamsAsync();
}
