using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private const string UsersApiBase = "api/users";

        public UserService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            try
            {
                // Use the filter endpoint to get users
                var response = await _httpClient.GetFromJsonAsync<List<UserDto>>($"{UsersApiBase}/filter");
                return response ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                // If filter endpoint fails, try the base endpoint as fallback
                try
                {
                    var response = await _httpClient.GetFromJsonAsync<List<UserDto>>(UsersApiBase);
                    return response ?? new List<UserDto>();
                }
                catch
                {
                    // Re-throw the original exception if both fail
                    throw ex;
                }
            }
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            var user = await _httpClient.GetFromJsonAsync<UserDto>($"{UsersApiBase}/{userId}");
            if (user == null)
                throw new KeyNotFoundException("User not found");
            return user;
        }

        public async Task UpdateUserRoleAsync(int userId, string newRole)
        {
            // Changing from /role to use the existing assign-role endpoint
            var response = await _httpClient.PostAsJsonAsync($"{UsersApiBase}/{userId}/assign-role",
                new { Role = newRole });
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateUserStatusAsync(int userId, bool isActive)
        {
            var response = await _httpClient.PutAsJsonAsync($"{UsersApiBase}/{userId}/status", new { IsActive = isActive });
            response.EnsureSuccessStatusCode();
        }
    }
}
