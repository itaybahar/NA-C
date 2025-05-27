using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Auth
{
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly ILogger<AuthTokenHandler> _logger;

        public AuthTokenHandler(
            ILocalStorageService localStorage,
            ILogger<AuthTokenHandler> logger)
        {
            _localStorage = localStorage;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get the token from local storage
                var token = await _localStorage.GetItemAsStringAsync("authToken");
                _logger.LogDebug("Retrieved token from storage: {TokenLength} chars", token?.Length ?? 0);

                if (!string.IsNullOrEmpty(token))
                {
                    _logger.LogInformation("Adding Bearer token to request headers");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                else
                {
                    _logger.LogWarning("No token found in local storage for request to: {Url}", request.RequestUri);
                }

                // Continue with the request
                var response = await base.SendAsync(request, cancellationToken);

                // Check for 401 Unauthorized response
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Received 401 Unauthorized response from: {Url}", request.RequestUri);
                    // You could implement token refresh logic here if needed
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AuthTokenHandler for request to: {Url}", request.RequestUri);
                throw;
            }
        }
    }
}
