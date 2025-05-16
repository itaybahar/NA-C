using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class BlacklistService : IBlacklistService
    {
        private readonly HttpClient _httpClient;

        public BlacklistService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TeamDto>> GetActiveBlacklistsAsync(int userId)
        {
            try
            {
                // Pass the user ID as a query parameter
                var blacklistEntries = await _httpClient.GetFromJsonAsync<List<BlacklistDto>>($"api/blacklist/active?userId={userId}");

                if (blacklistEntries == null || !blacklistEntries.Any())
                {
                    return new List<TeamDto>();
                }

                // Rest of the method remains the same...
                List<TeamDto> blacklistedTeams = new List<TeamDto>();

                foreach (var entry in blacklistEntries)
                {
                    try
                    {
                        // Get the team details for each blacklisted team
                        var teamResponse = await _httpClient.GetAsync($"api/teams/{entry.TeamID}");
                        if (teamResponse.IsSuccessStatusCode)
                        {
                            var team = await teamResponse.Content.ReadFromJsonAsync<TeamDto>();
                            if (team != null)
                            {
                                // Add the team to our list if it isn't already there
                                if (!blacklistedTeams.Any(t => t.TeamID == team.TeamID))
                                {
                                    blacklistedTeams.Add(team);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // If we can't get team details, create a minimal team entry
                        blacklistedTeams.Add(new TeamDto
                        {
                            TeamID = entry.TeamID,
                            TeamName = $"Team {entry.TeamID}",
                            Description = "Team information unavailable",
                            IsActive = true
                        });
                    }
                }

                return blacklistedTeams;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching active blacklists: {ex.Message}");
                return new List<TeamDto>();
            }
        }


        public async Task<bool> AddToBlacklistAsync(BlacklistCreateDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/blacklist/add", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveFromBlacklistAsync(int blacklistId)
        {
            // Keep this for backward compatibility
            var response = await _httpClient.PatchAsync($"api/blacklist/{blacklistId}/remove", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveFromBlacklistAsync(int blacklistId, int removedByUserId)
        {
            // New overload that passes the user ID
            var response = await _httpClient.PatchAsync($"api/blacklist/{blacklistId}/remove/{removedByUserId}", null);
            return response.IsSuccessStatusCode;
        }
    }
}
