using API_Project.Repositories;
using Domain_Project.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

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
        var team = await _repo.GetByStringIdAsync(teamId); // Updated to match the renamed method
        return team?.IsBlacklisted ?? false;
    }

    public async Task AddToBlacklistAsync(string teamId)
    {
        var team = await _repo.GetByStringIdAsync(teamId); // Updated to match the renamed method
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

        var team = await _repo.GetByStringIdAsync(teamId); // Updated to match the renamed method
        if (team != null && team.IsBlacklisted)
        {
            team.IsBlacklisted = false;
            await _repo.UpdateAsync(team);
            return true;
        }

        return false;
    }

    public async Task<bool> AddTeam(Team team)
    {
        if (team == null)
        {
            Console.WriteLine("TeamService.AddTeam: Team is null.");
            return false;
        }

        try
        {
            Console.WriteLine($"TeamService.AddTeam: Adding team: {JsonSerializer.Serialize(team)}");
            await _repo.AddAsync(team);
            Console.WriteLine("TeamService.AddTeam: Team added successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TeamService.AddTeam: Exception occurred: {ex.Message}");
            return false;
        }
    }


    public async Task<IEnumerable<Team>> GetAllTeamsAsync()
    {
        return await _repo.GetAllAsync();
    }

    public async Task<Team> GetTeamByIdAsync(int id)
    {
        return await _repo.GetByIntIdAsync(id); // Updated to match the renamed method
    }
    // Adding the missing method implementation
    public async Task<Team> GetByStringIdAsync(string teamId)
    {
        return await _repo.GetByStringIdAsync(teamId);
    }
}
