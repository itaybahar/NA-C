using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Blazor_WebAssembly.Models.Auth;
using Blazor_WebAssembly_Project.Models;
using Blazor_WebAssembly.Services.Interfaces;
using Blazor_WebAssembly_Project.Models.Auth;

namespace Blazor_WebAssembly.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;

        public AuthService(HttpClient httpClient, Blazored.LocalStorage.ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        public async Task<bool> CompleteGoogleProfileAsync(CompleteProfileModel model, string? token)
        {
            try
            {
                // If token is provided in the method, use it, otherwise use the one from the model
                if (!string.IsNullOrEmpty(token))
                {
                    model.Token = token;
                }

                var response = await _httpClient.PostAsJsonAsync("auth/complete-google-profile", model);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
                    if (result != null)
                    {
                        await _localStorage.SetItemAsync("authToken", result.Token);
                        await _localStorage.SetItemAsync("user", result.User);
                        return true;
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception(error);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error completing Google profile: {ex.Message}");
                throw;
            }
        }

        public async Task<AuthenticationResponse> LoginAsync(LoginModel loginModel)
        {
            var response = await _httpClient.PostAsJsonAsync("auth/login", loginModel);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
            }
            throw new Exception(await response.Content.ReadAsStringAsync());
        }

        public async Task<bool> Login(string username, string password)
        {
            var model = new LoginModel { Username = username, Password = password };
            var response = await LoginAsync(model);
            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                await _localStorage.SetItemAsync("authToken", response.Token);
                await _localStorage.SetItemAsync("user", response.User);
                return true;
            }
            return false;
        }

        public async Task<bool> Register(string username, string email, string password)
        {
            var model = new RegisterModel 
            { 
                Username = username, 
                Email = email, 
                Password = password,
                ConfirmPassword = password
            };
            return await RegisterAsync(model);
        }

        public async Task<bool> RegisterAsync(RegisterModel registerModel)
        {
            var response = await _httpClient.PostAsJsonAsync("auth/register", registerModel);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("user");
            return true;
        }

        public async Task<bool> ChangePassword(string oldPassword, string newPassword)
        {
            var model = new ChangePasswordModel 
            { 
                CurrentPassword = oldPassword,
                NewPassword = newPassword,
                ConfirmNewPassword = newPassword
            };
            var response = await _httpClient.PostAsJsonAsync("auth/change-password", model);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> IsUserInRoleAsync(string role)
        {
            var user = await _localStorage.GetItemAsync<UserDto>("user");
            return user?.Role?.Equals(role, StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public async Task<bool> SendPasswordResetEmail(string email)
        {
            var response = await _httpClient.PostAsJsonAsync("auth/forgot-password", new { Email = email });
            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetTokenAsync()
        {
            return await _localStorage.GetItemAsync<string>("authToken");
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetTokenAsync();
            return !string.IsNullOrEmpty(token);
        }

        public async Task<AuthenticationResponse> LoginWithGoogleAsync(string credential)
        {
            var response = await _httpClient.PostAsJsonAsync("auth/google-jwt-login", credential);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
            }
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
    }
} 