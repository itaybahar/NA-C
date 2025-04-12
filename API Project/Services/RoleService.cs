using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazor_WebAssembly.Services.Interfaces;
using Microsoft.JSInterop;

namespace Blazor_WebAssembly.Services
{
    public interface ILocalStorageService
    {
        Task<T> GetItemAsync<T>(string key);
        Task SetItemAsync<T>(string key, T value);
        Task RemoveItemAsync(string key);
    }

    public class LocalStorageService : ILocalStorageService
    {
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<T> GetItemAsync<T>(string key)
        {
            return await _jsRuntime.InvokeAsync<T>("localStorage.getItem", key);
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }

        public async Task RemoveItemAsync(string key)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
    }

    public class RoleService : IRoleService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        public RoleService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        private async Task SetAuthHeader()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<bool> RequestRoleChangeAsync(string requestedRole, string reason)
        {
            await SetAuthHeader();

            var requestModel = new
            {
                RequestedRole = requestedRole,
                Reason = reason
            };

            var response = await _httpClient.PostAsJsonAsync("api/role-requests", requestModel);
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<RoleRequestHistoryItem>> GetUserRequestHistoryAsync()
        {
            await SetAuthHeader();

            var response = await _httpClient.GetAsync("api/role-requests/my-requests");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<RoleRequestHistoryItem>>() ?? new List<RoleRequestHistoryItem>();
            }

            return new List<RoleRequestHistoryItem>();
        }

        public async Task<IEnumerable<RoleRequestHistoryItem>> GetPendingRequestsAsync()
        {
            await SetAuthHeader();

            var response = await _httpClient.GetAsync("api/role-requests/pending");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<RoleRequestHistoryItem>>() ?? new List<RoleRequestHistoryItem>();
            }

            return new List<RoleRequestHistoryItem>();
        }

        public async Task<bool> ApproveRequestAsync(int requestId, string notes)
        {
            await SetAuthHeader();

            var requestModel = new
            {
                Notes = notes
            };

            var response = await _httpClient.PostAsJsonAsync($"api/role-requests/{requestId}/approve", requestModel);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RejectRequestAsync(int requestId, string notes)
        {
            await SetAuthHeader();

            var requestModel = new
            {
                Notes = notes
            };

            var response = await _httpClient.PostAsJsonAsync($"api/role-requests/{requestId}/reject", requestModel);
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> AssignRoleAsync(int userId, string role)
        {
            await SetAuthHeader();

            var requestModel = new
            {
                Role = role
            };

            var response = await _httpClient.PostAsJsonAsync($"api/users/{userId}/assign-role", requestModel);
            return response.IsSuccessStatusCode;
        }

    }
}
