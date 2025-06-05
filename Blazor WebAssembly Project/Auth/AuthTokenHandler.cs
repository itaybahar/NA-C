using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;

namespace Blazor_WebAssembly.Auth
{
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
        private readonly ILogger<AuthTokenHandler> _logger;

        public AuthTokenHandler(
            Blazored.LocalStorage.ILocalStorageService localStorage,
            ILogger<AuthTokenHandler> logger)
        {
            _localStorage = localStorage;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                _logger.LogDebug("Retrieved token from storage: {TokenLength} chars", token?.Length ?? 0);

                if (!string.IsNullOrEmpty(token))
                {
                    _logger.LogInformation("Adding Bearer token to request headers for {Method} {Url}", 
                        request.Method, request.RequestUri);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    
                    // Add token debug info
                    var tokenParts = token.Split('.');
                    if (tokenParts.Length == 3)
                    {
                        _logger.LogDebug("Token structure appears valid (has 3 parts)");
                    }
                    else
                    {
                        _logger.LogWarning("Token structure may be invalid (does not have 3 parts)");
                    }
                }
                else
                {
                    _logger.LogWarning("No token found in local storage for {Method} request to: {Url}", 
                        request.Method, request.RequestUri);
                }

                var response = await base.SendAsync(request, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Received 401 Unauthorized response from: {Url}", request.RequestUri);
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