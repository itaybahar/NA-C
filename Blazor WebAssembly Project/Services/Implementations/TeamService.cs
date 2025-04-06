using System.Net.Http.Json;
using Blazor_WebAssembly.Models.Team;
using Blazor_WebAssembly.Services.Interfaces;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class TeamService : ITeamService
    {
        private readonly HttpClient _httpClient;

        public TeamService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://localhost:7235/api/");
        }

        public async Task<List<TeamModel>> GetAllTeamsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TeamModel>>("teams");
            }
            catch (Exception ex)
            {
                // Log the exception
                return new List<TeamModel>();
            }
        }

        public async Task<TeamModel> GetTeamByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<TeamModel>($"teams/{id}");
            }
            catch (Exception ex)
            {
                // Log the exception
                return null;
            }
        }

        public async Task<bool> CreateTeamAsync(TeamModel team)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("teams", team);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<bool> UpdateTeamAsync(TeamModel team)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"teams/{team.TeamID}", team);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<bool> DeleteTeamAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"teams/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<List<TeamMemberModel>> GetTeamMembersAsync(int teamId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TeamMemberModel>>($"teams/{teamId}/members");
            }
            catch (Exception ex)
            {
                // Log the exception
                return new List<TeamMemberModel>();
            }
        }

        public async Task<bool> AddTeamMemberAsync(TeamMemberModel teamMember)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("teams/members", teamMember);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<bool> RemoveTeamMemberAsync(int teamId, int userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"teams/{teamId}/members/{userId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }
    }
}