using Blazor_WebAssembly.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace Blazor_WebAssembly.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private const string TokenStorageKey = "authToken";
        private readonly IJSRuntimeService _jsRuntime;

        public CustomAuthStateProvider(IJSRuntimeService jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string savedToken = string.Empty;
            try
            {
                Console.WriteLine("🔍 Checking token in local storage...");
                savedToken = await _jsRuntime.GetItemFromLocalStorage(TokenStorageKey);

                if (string.IsNullOrWhiteSpace(savedToken))
                {
                    Console.WriteLine("❌ Token not found in local storage.");
                    var anonymousPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
                    return new AuthenticationState(anonymousPrincipal);
                }

                Console.WriteLine("✅ Token found, parsing claims...");
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(savedToken), "jwt"));
                Console.WriteLine("User roles: " + string.Join(", ", claimsPrincipal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)));
                return new AuthenticationState(claimsPrincipal);
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ Error loading authentication state: " + ex.Message);
                var anonymousPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
                return new AuthenticationState(anonymousPrincipal);
            }
        }


        public async Task MarkUserAsAuthenticated(string token)
        {
            Console.WriteLine("💾 Saving token and updating auth state...");
            await _jsRuntime.SetItemInLocalStorage(TokenStorageKey, token);
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(authenticatedUser)));
        }

        public async Task MarkUserAsLoggedOut()
        {
            Console.WriteLine("🚪 Logging out user...");
            await _jsRuntime.RemoveItemFromLocalStorage(TokenStorageKey);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        }
        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs is null) return claims;

            if (keyValuePairs.TryGetValue("sub", out var sub))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.ToString() ?? string.Empty));

            if (keyValuePairs.TryGetValue("unique_name", out var name) || keyValuePairs.TryGetValue("name", out name))
                claims.Add(new Claim(ClaimTypes.Name, name.ToString() ?? string.Empty));

            if (keyValuePairs.TryGetValue("email", out var email))
                claims.Add(new Claim(ClaimTypes.Email, email.ToString() ?? string.Empty));

            if (keyValuePairs.TryGetValue("role", out var roles) || keyValuePairs.TryGetValue(ClaimTypes.Role, out roles))
            {
                if (roles is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in element.EnumerateArray())
                            claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? string.Empty));
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, roles.ToString() ?? string.Empty));
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, roles.ToString()?.Trim() ?? string.Empty));
                }
            }
            else if (keyValuePairs.TryGetValue("roles", out var rolesAlternative))
            {
                // Handle alternative role claim name
                if (rolesAlternative is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in element.EnumerateArray())
                            claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? string.Empty));
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, element.GetString() ?? string.Empty));
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, rolesAlternative.ToString()?.Trim() ?? string.Empty));
                }
            }

            // Ensure all code paths return a value
            return claims;
        }


        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            return Convert.FromBase64String(base64);
        }
    }
}
