using System.Net.Http.Json;
using System.Text.Json;
using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserService> _logger;

        public UserService(HttpClient httpClient, ILogger<UserService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<List<UserDto>> GetUsersAsync()
        {
            try
            {
                _logger.LogInformation("Fetching users from the server...");
                var response = await _httpClient.GetAsync("api/users");

                if (response.IsSuccessStatusCode)
                {
                    var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
                    _logger.LogInformation($"Successfully fetched {users?.Count ?? 0} users.");
                    return users ?? new List<UserDto>();
                }
                else
                {
                    _logger.LogError($"Failed to fetch users. Status code: {response.StatusCode}");
                    throw new Exception($"Failed to fetch users. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching users.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdateUserRoleAsync(int userId, string newRole)
        {
            try
            {
                _logger.LogInformation($"Updating role for user ID {userId} to {newRole}...");
                var payload = new { Role = newRole };
                var response = await _httpClient.PutAsJsonAsync($"api/users/{userId}/role", payload);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully updated role for user ID {userId} to {newRole}.");
                }
                else
                {
                    _logger.LogError($"Failed to update role for user ID {userId}. Status code: {response.StatusCode}");
                    throw new Exception($"Failed to update role. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating role for user ID {userId}.");
                throw;
            }
        }
    }
}
