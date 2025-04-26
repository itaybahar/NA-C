// Add this to a new file: Blazor_WebAssembly_Project/Auth/AuthTokenHandler.cs
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Auth
{
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;

        public AuthTokenHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Get the token from local storage
            var token = await _localStorage.GetItemAsStringAsync("authToken");

            // If we have a token, add it to the request header
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Continue with the request
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
