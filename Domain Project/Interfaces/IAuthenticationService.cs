using Domain_Project.DTOs;
using Domain_Project.Models;
using System.Threading.Tasks;

namespace Domain_Project.Interfaces
{
    public interface IAuthenticationService
    {
        string GenerateJwtToken(User user);
        Task<string?> GetTokenAsync();
        string HashPassword(string password);
        Task RegisterUserAsync(UserDto userDto, string password);
        bool ValidatePassword(User user, string password);
        Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string credential);
        Task<AuthenticationResponseDto> AuthenticateWithGoogleAsync(string googleToken);
    }

    public class GoogleUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GivenName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
    }
}
