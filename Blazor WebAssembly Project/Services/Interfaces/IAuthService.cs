using System.Threading.Tasks;
using Blazor_WebAssembly_Project.Models;
using Blazor_WebAssembly.Models.Auth;
using Blazor_WebAssembly_Project.Models.Auth;
using Blazor_WebAssembly_Project.Models;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthenticationResponse> LoginAsync(LoginModel loginModel);
        Task<bool> Login(string username, string password);
        Task<bool> Register(string username, string email, string password);
        Task<bool> RegisterAsync(RegisterModel registerModel);
        Task<bool> Logout();
        Task<bool> ChangePassword(string oldPassword, string newPassword);
        Task<bool> IsUserInRoleAsync(string role);
        // ✅ חדש: שליחת קישור לאיפוס סיסמה
        Task<bool> SendPasswordResetEmail(string email);
        // ✅ חדש: מחזיר את הטוקן הנוכחי
        Task<string> GetTokenAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<bool> CompleteGoogleProfileAsync(Blazor_WebAssembly.Models.Auth.CompleteProfileModel model, string? token);
        Task<AuthenticationResponse> LoginWithGoogleAsync(string credential);
    }
}
