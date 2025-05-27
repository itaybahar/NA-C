using Domain_Project.DTOs;
using Domain_Project.Models;

namespace API_Project.Services
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResponseDto> AuthenticateAsync(string email, UserLoginDto loginDto);
        Task<AuthenticationResponseDto> AuthenticateAsync(UserLoginDto loginDto);
        Task<AuthenticationResponseDto> AuthenticateWithGoogleAsync(string googleToken);
        Task RegisterUserAsync(UserDto userDto, string password);
        Task SendPasswordResetEmailAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
        string GenerateJwtToken(User user);
        bool ValidatePassword(User user, string password);
        Task<string?> GetTokenAsync();
        string HashPassword(string password);
        Task<Domain_Project.Interfaces.GoogleUserInfo?> ValidateGoogleTokenAsync(string credential);
    }
} 