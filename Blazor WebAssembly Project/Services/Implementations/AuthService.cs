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
        private readonly ILocalStorageService _localStorage;

        public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;

            // ודא שזה תואם לכתובת של ה־API שלך בפועל:
            _httpClient.BaseAddress = new Uri("https://localhost:5191/");
        }

        // ✅ התחברות עם מודל מלא
        public async Task<AuthenticationResponse> LoginAsync(LoginModel loginModel)
        {
            var response = await _httpClient.PostAsJsonAsync("auth/login", loginModel);

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
            var response = await _httpClient.PostAsJsonAsync("auth/register", registerModel);
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
            var response = await _httpClient.PostAsJsonAsync("auth/send-reset-email", new { Email = email });
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
    }
}
