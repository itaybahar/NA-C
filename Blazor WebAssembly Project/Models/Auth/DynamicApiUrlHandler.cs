using Blazor_WebAssembly.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Auth
{
    public class DynamicApiUrlHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IApiDiscoveryService _apiDiscoveryService;
        private bool _hasUpdatedUrl = false;

        public DynamicApiUrlHandler(
            IHttpClientFactory httpClientFactory,
            IApiDiscoveryService apiDiscoveryService)
        {
            _httpClientFactory = httpClientFactory;
            _apiDiscoveryService = apiDiscoveryService;
        }

        public async Task<HttpClient> GetClientWithDiscoveredApiAsync()
        {
            if (!_hasUpdatedUrl)
            {
                try
                {
                    var apiUrl = await _apiDiscoveryService.DiscoverApiUrl();
                    var client = _httpClientFactory.CreateClient("API");

                    // Update base address if needed
                    if (client.BaseAddress == null || client.BaseAddress.ToString() != apiUrl)
                    {
                        client.BaseAddress = new Uri(apiUrl);
                    }

                    _hasUpdatedUrl = true;
                    return client;
                }
                catch
                {
                    // Fall back to default client if discovery fails
                    return _httpClientFactory.CreateClient("API");
                }
            }

            return _httpClientFactory.CreateClient("API");
        }
    }
}
