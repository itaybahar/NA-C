using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Blazor_WebAssembly.Models.Auth;
using Blazor_WebAssembly.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class AuditLogService : IAuditLogService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuditLogService> _logger;
        private readonly string _baseUrl = "api/auditlog";

        public AuditLogService(HttpClient httpClient, ILogger<AuditLogService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<AuditLogModel>> GetUserAuditLogsAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/user/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<AuditLogModel>>() ?? [];
                }

                _logger.LogWarning("Failed to retrieve audit logs for user {UserId}: {StatusCode}",
                    userId, response.StatusCode);
                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
                return [];
            }
        }

        public async Task<List<AuditLogModel>> FilterAuditLogsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? action = null)
        {
            try
            {
                // Building query parameters
                var queryParams = new List<string>();

                if (startDate.HasValue)
                    queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");

                if (endDate.HasValue)
                    queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

                if (!string.IsNullOrWhiteSpace(action))
                    queryParams.Add($"action={Uri.EscapeDataString(action)}");

                var queryString = queryParams.Count > 0
                    ? "?" + string.Join("&", queryParams)
                    : "";

                var response = await _httpClient.GetAsync($"{_baseUrl}/filter{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<AuditLogModel>>() ?? [];
                }

                _logger.LogWarning("Failed to filter audit logs: {StatusCode}", response.StatusCode);
                return [];
            }
            catch (Exception ex)
            {
                var filters = new
                {
                    startDate,
                    endDate,
                    action
                };

                _logger.LogError(ex, "Error filtering audit logs with filters: {Filters}",
                    JsonSerializer.Serialize(filters));
                return [];
            }
        }
    }
}
