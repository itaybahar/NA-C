using System.Net.Http;
using System.Net.Http.Json; // Added for GetFromJsonAsync, PostAsJsonAsync, and PutAsJsonAsync
using System.Collections.Generic;
using System.Threading.Tasks;
using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs; // Added for TeamDto
using Blazor_WebAssembly.Models.Team; // Added for TeamModel and TeamMemberModel

namespace Blazor_WebAssembly.Services.Implementations
{
    public class TeamService : ITeamService
    {
        private readonly HttpClient _httpClient;

        public TeamService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TeamDto>> GetTeamsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<TeamDto>>("api/teams") ?? new List<TeamDto>();
        }

        public async Task<List<TeamModel>> GetAllTeamsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<TeamModel>>("api/teams/details") ?? new List<TeamModel>();
        }

        public async Task<TeamModel> GetTeamByIdAsync(int id)
        {
            var team = await _httpClient.GetFromJsonAsync<TeamModel>($"api/teams/{id}");
            return team ?? new TeamModel { TeamName = string.Empty }; // Ensure required property is set
        }

        public async Task<bool> CreateTeamAsync(TeamModel team)
        {
            var response = await _httpClient.PostAsJsonAsync("api/teams", team);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTeamAsync(TeamModel team)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/teams/{team.TeamID}", team);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteTeamAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/teams/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<TeamMemberModel>> GetTeamMembersAsync(int teamId)
        {
            return await _httpClient.GetFromJsonAsync<List<TeamMemberModel>>($"api/teams/{teamId}/members") ?? new List<TeamMemberModel>();
        }

        public async Task<bool> AddTeamMemberAsync(TeamMemberModel teamMember)
        {
            var response = await _httpClient.PostAsJsonAsync("api/teams/members", teamMember);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveTeamMemberAsync(int teamId, int userId)
        {
            var response = await _httpClient.DeleteAsync($"api/teams/{teamId}/members/{userId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<TeamDto>> GetBlacklistedTeamsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<TeamDto>>("api/teams/blacklisted") ?? new List<TeamDto>();
        }

        public async Task<bool> IsBlacklistedAsync(string teamId)
        {
            return await _httpClient.GetFromJsonAsync<bool>($"api/teams/{teamId}/is-blacklisted");
        }

        public async Task AddToBlacklistAsync(string teamId)
        {
            var response = await _httpClient.PostAsync($"api/teams/{teamId}/blacklist", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> RemoveFromBlacklistAsync(string teamId)
        {
            var response = await _httpClient.DeleteAsync($"api/teams/{teamId}/blacklist");
            return response.IsSuccessStatusCode;
        }
    }
}

