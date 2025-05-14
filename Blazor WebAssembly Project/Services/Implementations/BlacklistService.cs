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

        public async Task<List<BlacklistDto>> GetActiveBlacklistsAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<BlacklistDto>>("api/blacklist/active");
            return result ?? new List<BlacklistDto>();
        }

        public async Task<bool> AddToBlacklistAsync(BlacklistCreateDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/blacklist/add", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveFromBlacklistAsync(int blacklistId)
        {
            var response = await _httpClient.PatchAsync($"api/blacklist/{blacklistId}/remove", null);
            return response.IsSuccessStatusCode;
        }
    }
}
