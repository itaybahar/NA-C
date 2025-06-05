using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Api
{
    public class DynamicApiUrlHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<DynamicApiUrlHandler> _logger;
        private string _discoveredApiUrl = string.Empty;

        public DynamicApiUrlHandler(
            IHttpClientFactory httpClientFactory,
            IJSRuntime jsRuntime,
            ILogger<DynamicApiUrlHandler> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HttpClient> GetClientWithDiscoveredApiAsync()
        {
            if (string.IsNullOrEmpty(_discoveredApiUrl))
            {
                try
                {
                    _discoveredApiUrl = await _jsRuntime.InvokeAsync<string>("apiConnection.getBaseUrl");
                    _logger.LogInformation($"Retrieved API URL from JS: {_discoveredApiUrl}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to get API URL from JS: {ex.Message}");
                    _discoveredApiUrl = "https://localhost:5191/";
                }
            }

            var client = _httpClientFactory.CreateClient("API");

            if (client.BaseAddress == null || client.BaseAddress.ToString() != _discoveredApiUrl)
            {
                client.BaseAddress = new Uri(_discoveredApiUrl);
                _logger.LogInformation($"Updated client base address to: {_discoveredApiUrl}");
            }

            return client;
        }
    }
} 