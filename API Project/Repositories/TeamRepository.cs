using API_Project.Data;
using Domain_Project.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_Project.Repositories
{
    public class TeamRepository : GenericRepository<Team>, ITeamRepository
    {
        public TeamRepository(EquipmentManagementContext context) : base(context)
        {
        }

        public async Task AddAsync(Team team)
        {
            await _dbSet.AddAsync(team);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Team>> GetAllAsync()
        {
            // Log the query being executed
            var query = _dbSet.ToQueryString();
            Console.WriteLine($"Generated SQL Query for GetAllAsync: {query}");

            return await _dbSet.ToListAsync();
        }

        public async Task<Team> GetByStringIdAsync(string teamId)
        {
            if (int.TryParse(teamId, out int id))
            {
                // Log the query being executed
                var query = _dbSet.Where(t => t.TeamID == id).ToQueryString();
                Console.WriteLine($"Generated SQL Query for GetByStringIdAsync: {query}");

                return await _dbSet.FindAsync(id);
            }
            return null;
        }

        public async Task<Team> GetByIntIdAsync(int teamIdInt)
        {
            // Log the query being executed
            var query = _dbSet.Where(t => t.TeamID == teamIdInt).ToQueryString();
            Console.WriteLine($"Generated SQL Query for GetByIntIdAsync: {query}");

            return await _dbSet.FindAsync(teamIdInt);
        }

        public async Task UpdateAsync(Team team)
        {
            _dbSet.Update(team);
            await _context.SaveChangesAsync();
        }

        public async Task<bool?> GetByIdAsync(string teamId)
        {
            if (int.TryParse(teamId, out int id))
            {
                // Log the query being executed
                var query = _dbSet.Where(t => t.TeamID == id).ToQueryString();
                Console.WriteLine($"Generated SQL Query for GetByIdAsync: {query}");

                var team = await _dbSet.FindAsync(id);
                return team != null;
            }
            return null;
        }

        public async Task<IEnumerable<Team>> GetActiveTeamsAsync()
        {
            // Log the query being executed
            var query = _dbSet.Where(t => t.IsActive).ToQueryString();
            Console.WriteLine($"Generated SQL Query for GetActiveTeamsAsync: {query}");

            return await _dbSet.Where(t => t.IsActive).ToListAsync();
        }

        public async Task<IEnumerable<Team>> GetBlacklistedTeamsAsync()
        {
            // Log the query being executed
            var query = _dbSet.Where(t => t.IsBlacklisted).ToQueryString();
            Console.WriteLine($"Generated SQL Query for GetBlacklistedTeamsAsync: {query}");

            return await _dbSet.Where(t => t.IsBlacklisted).ToListAsync();
        }
    }
}
