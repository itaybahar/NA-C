using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Auth
{
    public class CustomAuthorizationHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly ILogger<CustomAuthorizationHandler> _logger;

        public CustomAuthorizationHandler(
            ILocalStorageService localStorage,
            ILogger<CustomAuthorizationHandler> logger)
        {
            _localStorage = localStorage;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsStringAsync("authToken");

            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("Adding authentication token to request");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _logger.LogWarning("No authentication token available for API request");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
