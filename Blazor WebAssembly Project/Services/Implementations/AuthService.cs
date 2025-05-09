using System.Net.Http.Json;
using Blazored.LocalStorage;
using Blazor_WebAssembly.Models.Auth;
using Blazor_WebAssembly_Project.Models.Auth;
using Blazor_WebAssembly.Services.Interfaces;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
        private readonly string _apiBaseUrl;

        public AuthService(HttpClient httpClient, Blazored.LocalStorage.ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;

            // Instead of directly setting the BaseAddress, store it for later use
            _apiBaseUrl = "https://localhost:5191/";
        }

        // ✅ התחברות עם מודל מלא
        public async Task<AuthenticationResponse> LoginAsync(LoginModel loginModel)
        {
            // Create a new HttpClient for this specific request with the correct base address
            using var client = new HttpClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var response = await client.PostAsJsonAsync("auth/login", loginModel);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
                if (result != null)
                {
                    await _localStorage.SetItemAsync("authToken", result.Token);
                    await _localStorage.SetItemAsync("user", result.User);
                    return result;
                }
            }

            return new AuthenticationResponse { Token = string.Empty, User = null! };
        }

        // ✅ התחברות עם שם משתמש וסיסמה
        public async Task<bool> Login(string username, string password)
        {
            var loginModel = new LoginModel
            {
                Username = username,
                Password = password
            };

            var result = await LoginAsync(loginModel);
            return result != null && !string.IsNullOrEmpty(result.Token);
        }

        // ✅ הרשמה עם מודל
        public async Task<bool> RegisterAsync(RegisterModel registerModel)
        {
            // Create a new HttpClient for this specific request with the correct base address
            using var client = new HttpClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var response = await client.PostAsJsonAsync("auth/register", registerModel);
            return response.IsSuccessStatusCode;
        }

        // ✅ הרשמה עם פרמטרים רגילים
        public async Task<bool> Register(string username, string email, string password)
        {
            var registerModel = new RegisterModel
            {
                Username = username,
                Email = email,
                Password = password,
                ConfirmPassword = password,
                Role = "User"
            };

            return await RegisterAsync(registerModel);
        }

        // ✅ שליחת קישור לאיפוס סיסמה
        public async Task<bool> SendPasswordResetEmail(string email)
        {
            // Create a new HttpClient for this specific request with the correct base address
            using var client = new HttpClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var response = await client.PostAsJsonAsync("auth/send-reset-email", new { Email = email });
            return response.IsSuccessStatusCode;
        }

        // ✅ התנתקות מלאה
        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("user");
        }

        // ✅ התנתקות מהירה
        public async Task<bool> Logout()
        {
            await LogoutAsync();
            return true;
        }

        // ✅ בדיקה אם יש טוקן
        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            return !string.IsNullOrEmpty(token);
        }

        // ✅ (אם תרצה בעתיד) תביא את תפקיד המשתמש
        public async Task<string> GetCurrentUserRoleAsync()
        {
            // ניתן להרחיב כאן לפי נתוני ה־JWT או ממשק ה־API
            return await Task.FromResult(string.Empty);
        }

        // ✅ (בהמשך) שינוי סיסמה
        public Task<bool> ChangePassword(string oldPassword, string newPassword)
        {
            // לא מומש עדיין, אפשר להוסיף לפי הצורך
            throw new NotImplementedException();
        }

        public Task<bool> IsUserInRoleAsync(string role)
        {
            throw new NotImplementedException();
        }
        // ✅ חדש: מימוש של GetTokenAsync
        public async Task<string> GetTokenAsync()
        {
            return await _localStorage.GetItemAsync<string>("authToken") ?? string.Empty;
        }
    }
}
