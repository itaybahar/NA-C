using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class ApiDiscoveryService : IApiDiscoveryService
    {
        private readonly ILogger<ApiDiscoveryService> _logger;
        private readonly IConfiguration _configuration;
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
        private static string _cachedApiUrl;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static readonly List<string> _defaultApiUrls = new List<string> {
            "https://localhost:5191/",
            "https://localhost:7235/",
            "https://localhost:5001/",
            "https://localhost:5002/"
        };

        public ApiDiscoveryService(
            ILogger<ApiDiscoveryService> logger,
            IConfiguration configuration,
            Blazored.LocalStorage.ILocalStorageService localStorage)
        {
            _logger = logger;
            _configuration = configuration;
            _localStorage = localStorage;
        }

        public string ApiBaseUrl => _cachedApiUrl ?? _configuration["ApiBaseUrl"] ?? _defaultApiUrls[0];

        public async Task<string> DiscoverApiUrl()
        {
            // Return cached URL if available
            if (!string.IsNullOrEmpty(_cachedApiUrl))
            {
                return _cachedApiUrl;
            }

            // Try to get from localStorage
            try
            {
                var storedUrl = await _localStorage.GetItemAsync<string>("api_baseUrl");
                if (!string.IsNullOrEmpty(storedUrl))
                {
                    _logger.LogInformation($"Using stored API URL: {storedUrl}");
                    _cachedApiUrl = storedUrl;
                    return storedUrl;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error accessing localStorage: {ex.Message}");
            }

            // Use semaphore to prevent multiple discovery attempts at the same time
            await _semaphore.WaitAsync();

            try
            {
                // Double-check cache after acquiring semaphore
                if (!string.IsNullOrEmpty(_cachedApiUrl))
                {
                    return _cachedApiUrl;
                }

                var possibleUrls = new List<string>();

                // First try configuration value
                var configUrl = _configuration["ApiBaseUrl"];
                if (!string.IsNullOrEmpty(configUrl))
                {
                    possibleUrls.Add(configUrl);
                }

                // Then add default URLs
                possibleUrls.AddRange(_defaultApiUrls);

                // Create a client with short timeout for discovery
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Blazor-ApiDiscovery");

                foreach (var url in possibleUrls)
                {
                    try
                    {
                        _logger.LogInformation($"Trying API URL: {url}");

                        // Use health endpoint with cache-busting parameter
                        var testUrl = url.TrimEnd('/') + "/health?t=" + DateTime.Now.Ticks;
                        var response = await httpClient.GetAsync(testUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation($"API server found at: {url}");
                            _cachedApiUrl = url;

                            // Store in localStorage for future use
                            try
                            {
                                await _localStorage.SetItemAsync("api_baseUrl", url);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Failed to store API URL in localStorage: {ex.Message}");
                            }

                            return url;
                        }

                        _logger.LogWarning($"API server at {url} returned {response.StatusCode}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Could not connect to {url}: {ex.Message}");
                    }
                }

                // If we get here, all connection attempts failed
                _logger.LogWarning($"No API server detected, using fallback: {possibleUrls[0]}");
                _cachedApiUrl = possibleUrls[0];
                return _cachedApiUrl;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
