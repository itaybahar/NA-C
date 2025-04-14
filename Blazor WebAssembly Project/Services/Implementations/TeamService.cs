using Blazor_WebAssembly.Models.Team;
using Blazor_WebAssembly.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class TeamService : ITeamService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly IJSRuntime _jsRuntime;


        public TeamService(HttpClient httpClient, IAuthService authService, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _authService = authService;
            _jsRuntime = jsRuntime;

        }

        private async Task AddAuthorizationHeaderAsync()
        {
            var token = await _authService.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                // Log the token for debugging (remove in production)
                Console.WriteLine($"Using token: {token.Substring(0, Math.Min(20, token.Length))}...");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Log all headers being sent
                Console.WriteLine("Headers being sent:");
                foreach (var header in _httpClient.DefaultRequestHeaders)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
            }
            else
            {
                Console.WriteLine("No authentication token available!");
            }
        }


        public async Task<List<TeamModel>> GetAllTeamsAsync()
        {
            await AddAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync("api/teams/details");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to load teams: {response.ReasonPhrase}");
            }

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API Response: {content}");

            return JsonSerializer.Deserialize<List<TeamModel>>(content) ?? new List<TeamModel>();
        }


        public async Task<TeamModel> GetTeamByIdAsync(int id)
        {
            await AddAuthorizationHeaderAsync();
            var team = await _httpClient.GetFromJsonAsync<TeamModel>($"api/teams/{id}");
            return team ?? new TeamModel { TeamName = string.Empty };
        }

        public async Task<bool> AddTeam(TeamModel team)
        {
            await AddAuthorizationHeaderAsync();

            // Log the token to confirm it's being sent
            var token = await _authService.GetTokenAsync();
            Console.WriteLine($"Token for AddTeam request: {token?.Substring(0, 20)}...");

            // Log the request data
            Console.WriteLine($"Sending team data: {JsonSerializer.Serialize(team)}");

            var response = await _httpClient.PostAsJsonAsync("api/teams/add", team);

            // If failed, log the response
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API error: {response.StatusCode}, Content: {errorContent}");
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTeamAsync(TeamModel team)
        {
            await AddAuthorizationHeaderAsync();
            var response = await _httpClient.PutAsJsonAsync($"api/teams/{team.TeamID}", team);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteTeamAsync(int id)
        {
            await AddAuthorizationHeaderAsync();
            var response = await _httpClient.DeleteAsync($"api/teams/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<TeamModel>> GetBlacklistedTeamsAsync()
        {
            await AddAuthorizationHeaderAsync();
            return await _httpClient.GetFromJsonAsync<List<TeamModel>>("api/teams/blacklisted") ?? new List<TeamModel>();
        }

        public async Task<bool> IsBlacklistedAsync(string teamId)
        {
            await AddAuthorizationHeaderAsync();
            var response = await _httpClient.GetFromJsonAsync<dynamic>($"api/teams/{teamId}/blacklist-status");
            return response?.isBlacklisted ?? false;
        }

        public async Task AddToBlacklistAsync(string teamId)
        {
            await AddAuthorizationHeaderAsync();
            var response = await _httpClient.PostAsync($"api/teams/{teamId}/blacklist", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> RemoveFromBlacklistAsync(string teamId)
        {
            await AddAuthorizationHeaderAsync();
            var response = await _httpClient.DeleteAsync($"api/teams/{teamId}/blacklist");
            return response.IsSuccessStatusCode;
        }
    }
}
