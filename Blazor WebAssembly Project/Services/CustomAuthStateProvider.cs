using Blazor_WebAssembly.Services.Interfaces;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace Blazor_WebAssembly.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private const string TokenStorageKey = "authToken";
        private const string RefreshTokenStorageKey = "refreshToken";
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
        private readonly ILogger<CustomAuthStateProvider> _logger;

        public CustomAuthStateProvider(Blazored.LocalStorage.ILocalStorageService localStorage, ILogger<CustomAuthStateProvider> logger)
        {
            _localStorage = localStorage;
            _logger = logger;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string savedToken = string.Empty;
            try
            {
                _logger.LogInformation("🔍 Checking token in local storage...");
                savedToken = await _localStorage.GetItemAsync<string>(TokenStorageKey);
                _logger.LogDebug("Retrieved token: {TokenLength} chars", savedToken?.Length ?? 0);

                if (string.IsNullOrWhiteSpace(savedToken) || await IsTokenExpired(savedToken))
                {
                    _logger.LogWarning("❌ Token is either missing or expired.");
                    var anonymousPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
                    return new AuthenticationState(anonymousPrincipal);
                }

                _logger.LogInformation("✅ Token is valid, parsing claims...");
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(savedToken), "jwt"));
                _logger.LogInformation("User roles: {Roles}", string.Join(", ", claimsPrincipal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)));
                return new AuthenticationState(claimsPrincipal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Error loading authentication state");
                var anonymousPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
                return new AuthenticationState(anonymousPrincipal);
            }
        }

        public async Task MarkUserAsAuthenticated(string token)
        {
            _logger.LogInformation("💾 Saving token and updating auth state...");
            await _localStorage.SetItemAsync(TokenStorageKey, token);
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(authenticatedUser)));
        }

        public async Task MarkUserAsLoggedOut()
        {
            _logger.LogInformation("🚪 Logging out user...");
            await _localStorage.RemoveItemAsync(TokenStorageKey);
            await _localStorage.RemoveItemAsync(RefreshTokenStorageKey);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            var claims = new List<Claim>();

            if (handler.CanReadToken(jwt))
            {
                _logger.LogDebug("Parsing JWT using JwtSecurityTokenHandler");
                var token = handler.ReadJwtToken(jwt);
                return token.Claims;
            }
            else
            {
                _logger.LogDebug("Fallback to manual JWT parsing");
                // Fallback to manual parsing for non-standard JWT format
                var payload = jwt.Split('.')[1];
                var jsonBytes = ParseBase64WithoutPadding(payload);
                var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

                if (keyValuePairs is null) return claims;

                // Track claims we've already added to avoid duplicates
                var addedClaims = new HashSet<string>();

                // Add all claims from the token as-is
                foreach (var kvp in keyValuePairs)
                {
                    _logger.LogDebug("JWT claim: {Key} = {Value}", kvp.Key, kvp.Value);

                    // Special handling for user ID
                    if (kvp.Key.Equals("userid", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Equals("sub", StringComparison.OrdinalIgnoreCase))
                    {
                        var claimValue = kvp.Value?.ToString() ?? string.Empty;

                        // Add as standard NameIdentifier if not already added
                        var nameIdKey = $"{ClaimTypes.NameIdentifier}:{claimValue}";
                        if (!addedClaims.Contains(nameIdKey))
                        {
                            claims.Add(new Claim(ClaimTypes.NameIdentifier, claimValue));
                            addedClaims.Add(nameIdKey);
                        }

                        // Also add as UserID for direct matching
                        var userIdKey = $"UserID:{claimValue}";
                        if (!addedClaims.Contains(userIdKey))
                        {
                            claims.Add(new Claim("UserID", claimValue));
                            addedClaims.Add(userIdKey);
                        }
                    }
                    else
                    {
                        // Handle other claims
                        var claimValue = kvp.Value?.ToString() ?? string.Empty;
                        var claimKey = $"{kvp.Key}:{claimValue}";
                        if (!addedClaims.Contains(claimKey))
                        {
                            claims.Add(new Claim(kvp.Key, claimValue));
                            addedClaims.Add(claimKey);
                        }
                    }
                }

                // Map standard claims
                MapStandardClaim(keyValuePairs, "sub", ClaimTypes.NameIdentifier, claims, addedClaims);
                MapStandardClaim(keyValuePairs, "name", ClaimTypes.Name, claims, addedClaims);
                if (!MapStandardClaim(keyValuePairs, "unique_name", ClaimTypes.Name, claims, addedClaims))
                {
                    MapStandardClaim(keyValuePairs, "name", ClaimTypes.Name, claims, addedClaims);
                }
                MapStandardClaim(keyValuePairs, "email", ClaimTypes.Email, claims, addedClaims);

                // Handle roles
                if (keyValuePairs.TryGetValue("role", out var roles) || keyValuePairs.TryGetValue(ClaimTypes.Role, out roles))
                {
                    if (roles is JsonElement element)
                    {
                        if (element.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var role in element.EnumerateArray())
                            {
                                var roleValue = role.GetString() ?? string.Empty;
                                var roleKey = $"{ClaimTypes.Role}:{roleValue}";
                                if (!addedClaims.Contains(roleKey))
                                {
                                    claims.Add(new Claim(ClaimTypes.Role, roleValue));
                                    addedClaims.Add(roleKey);
                                }
                            }
                        }
                        else
                        {
                            var roleValue = element.ToString() ?? string.Empty;
                            var roleKey = $"{ClaimTypes.Role}:{roleValue}";
                            if (!addedClaims.Contains(roleKey))
                            {
                                claims.Add(new Claim(ClaimTypes.Role, roleValue));
                                addedClaims.Add(roleKey);
                            }
                        }
                    }
                    else
                    {
                        var roleValue = roles.ToString()?.Trim() ?? string.Empty;
                        var roleKey = $"{ClaimTypes.Role}:{roleValue}";
                        if (!addedClaims.Contains(roleKey))
                        {
                            claims.Add(new Claim(ClaimTypes.Role, roleValue));
                            addedClaims.Add(roleKey);
                        }
                    }
                }

                return claims;
            }
        }

        private bool MapStandardClaim(Dictionary<string, object> claims, string sourceKey, string targetClaimType,
            List<Claim> targetClaims, HashSet<string> addedClaims)
        {
            if (claims.TryGetValue(sourceKey, out var value) && value != null)
            {
                var claimValue = value.ToString() ?? string.Empty;
                var claimKey = $"{targetClaimType}:{claimValue}";

                if (!addedClaims.Contains(claimKey))
                {
                    targetClaims.Add(new Claim(targetClaimType, claimValue));
                    addedClaims.Add(claimKey);
                    return true;
                }
            }

            return false;
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

        private async Task<bool> IsTokenExpired(string token = null)
        {
            try
            {
                token ??= await _localStorage.GetItemAsync<string>(TokenStorageKey);

                if (string.IsNullOrEmpty(token))
                    return true;

                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwtToken = handler.ReadJwtToken(token);
                    // Add 30-second buffer to avoid edge cases
                    return jwtToken.ValidTo <= DateTime.UtcNow.AddSeconds(30);
                }

                // Fallback to manual parsing
                var payload = token.Split('.')[1];
                var jsonBytes = ParseBase64WithoutPadding(payload);
                var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

                if (keyValuePairs != null && keyValuePairs.TryGetValue("exp", out var exp))
                {
                    var expirationTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(exp));
                    // Add 30-second buffer to avoid edge cases
                    return expirationTime <= DateTime.UtcNow.AddSeconds(30);
                }

                return true; // Assume expired if we can't parse it
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token expiration");
                return true; // Assume expired if there's an error
            }
        }

        public async Task<bool> RefreshTokenIfNeeded()
        {
            if (await IsTokenExpired())
            {
                _logger.LogInformation("🔄 Token expired, attempting refresh...");
                // Implement your token refresh logic here
                // For example:
                // var refreshToken = await _localStorage.GetItemAsync<string>(RefreshTokenStorageKey);
                // var result = await _authService.RefreshTokenAsync(refreshToken);
                // if (result.Success) {
                //    await MarkUserAsAuthenticated(result.Token);
                //    return true;
                // }

                _logger.LogWarning("❌ Token refresh not implemented, logging out user");
                await MarkUserAsLoggedOut();
                return false;
            }

            return true;
        }
    }
}
