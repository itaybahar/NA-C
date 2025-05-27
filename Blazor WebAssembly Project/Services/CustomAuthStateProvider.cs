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
                _logger.LogInformation("🔍 Starting authentication state check...");
                
                // Check if token exists in storage
                savedToken = await _localStorage.GetItemAsync<string>(TokenStorageKey);
                _logger.LogInformation("Token from storage: {TokenExists}", !string.IsNullOrEmpty(savedToken));
                
                if (string.IsNullOrEmpty(savedToken))
                {
                    _logger.LogWarning("❌ No token found in storage");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Log token details (safely)
                _logger.LogInformation("Token length: {Length}, First 10 chars: {Preview}...", 
                    savedToken.Length,
                    savedToken.Substring(0, Math.Min(10, savedToken.Length)));

                // Check token expiration
                var isExpired = await IsTokenExpired(savedToken);
                if (isExpired)
                {
                    _logger.LogWarning("❌ Token is expired");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                _logger.LogInformation("✅ Token is valid, parsing claims...");
                var claims = ParseClaimsFromJwt(savedToken);
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
                
                // Log claims for debugging
                foreach (var claim in claims)
                {
                    _logger.LogDebug("Claim: {Type} = {Value}", claim.Type, claim.Value);
                }

                // Log role claims specifically
                var roleClaims = claimsPrincipal.Claims
                    .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                    .Select(c => c.Value)
                    .ToList();

                _logger.LogInformation("Found {Count} role claims: {Roles}", 
                    roleClaims.Count,
                    string.Join(", ", roleClaims));

                // Log authentication status
                _logger.LogInformation("Authentication status: {IsAuthenticated}, Identity name: {Name}",
                    claimsPrincipal.Identity?.IsAuthenticated,
                    claimsPrincipal.Identity?.Name);
                
                return new AuthenticationState(claimsPrincipal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Error in GetAuthenticationStateAsync: {Message}", ex.Message);
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
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
                _logger.LogInformation("🔍 Checking token expiration...");

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("❌ Token is null or empty");
                    return true;
                }

                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    _logger.LogWarning("❌ Token does not have 3 parts, not a valid JWT");
                    return true; // Not a valid JWT
                }

                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwtToken = handler.ReadJwtToken(token);
                    var expirationTime = jwtToken.ValidTo;
                    var currentTime = DateTime.UtcNow;
                    var isExpired = expirationTime <= currentTime.AddSeconds(30);

                    _logger.LogInformation(
                        "Token expiration check - Expires: {ExpirationTime}, Current: {CurrentTime}, IsExpired: {IsExpired}",
                        expirationTime,
                        currentTime,
                        isExpired);

                    return isExpired;
                }

                _logger.LogWarning("⚠️ Could not read token as JWT, attempting manual parsing");
                // Fallback to manual parsing
                try
                {
                    var payload = token.Split('.')[1];
                    var jsonBytes = ParseBase64WithoutPadding(payload);
                    var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

                    if (keyValuePairs != null && keyValuePairs.TryGetValue("exp", out var exp))
                    {
                        var expirationTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(exp));
                        var currentTime = DateTimeOffset.UtcNow;
                        var isExpired = expirationTime <= currentTime.AddSeconds(30);

                        _logger.LogInformation(
                            "Manual token expiration check - Expires: {ExpirationTime}, Current: {CurrentTime}, IsExpired: {IsExpired}",
                            expirationTime,
                            currentTime,
                            isExpired);

                        return isExpired;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ Error during manual JWT parsing");
                    // If manual parsing fails, treat as expired
                    return true;
                }

                _logger.LogWarning("❌ Could not determine token expiration");
                return true; // Assume expired if we can't parse it
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Error checking token expiration");
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
