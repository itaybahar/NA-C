using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models;

namespace API_Project.Services
{
public class AuthenticationServiceAdapter : Domain_Project.Interfaces.IAuthenticationService
{
        private readonly AuthenticationService _authService;
    private readonly IUserRepository _userRepository;

        public AuthenticationServiceAdapter(AuthenticationService authService, IUserRepository userRepository)
    {
            _authService = authService;
            _userRepository = userRepository;
    }

    public string GenerateJwtToken(User user)
    {
        return _authService.GenerateJwtToken(user);
    }

        public Task<string?> GetTokenAsync()
        {
            return _authService.GetTokenAsync();
        }

        public string HashPassword(string password)
        {
            return _authService.HashPassword(password);
        }

    public async Task RegisterUserAsync(UserDto userDto, string password)
    {
        await _authService.RegisterUserAsync(userDto, password);
    }

        public bool ValidatePassword(User user, string password)
        {
            return _authService.ValidatePassword(user, password);
        }

        public async Task<Domain_Project.Interfaces.GoogleUserInfo?> ValidateGoogleTokenAsync(string credential)
    {
            var apiGoogleUserInfo = await _authService.ValidateGoogleTokenAsync(credential);
            
            if (apiGoogleUserInfo == null) return null;

            // Map API project's GoogleUserInfo to Domain project's GoogleUserInfo
            return new Domain_Project.Interfaces.GoogleUserInfo
            {
                Id = apiGoogleUserInfo.Id,
                Email = apiGoogleUserInfo.Email,
                GivenName = apiGoogleUserInfo.GivenName,
                FamilyName = apiGoogleUserInfo.FamilyName,
                Name = apiGoogleUserInfo.Name,
                Picture = apiGoogleUserInfo.Picture,
                EmailVerified = true // We know it's verified because the API service validates it
            };
        }

        public async Task<AuthenticationResponseDto> AuthenticateWithGoogleAsync(string googleToken)
        {
            try
            {
                var apiResponse = await _authService.AuthenticateWithGoogleAsync(googleToken);
                
                if (apiResponse == null)
                {
                    return new AuthenticationResponseDto
        {
                        Token = string.Empty,
            User = new UserDto
            {
                            UserID = 0,
                            Username = string.Empty,
                            Email = string.Empty,
                            FirstName = string.Empty,
                            LastName = string.Empty,
                            Role = "User"
                        },
                        NeedsProfile = true,
                        Email = string.Empty
                    };
                }
                
                // Map API project's response to Domain project's response
                return new AuthenticationResponseDto
                {
                    Token = apiResponse.Token ?? string.Empty,
                    User = apiResponse.User ?? new UserDto
                    {
                        UserID = 0,
                        Username = string.Empty,
                        Email = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        Role = "User"
                    },
                    NeedsProfile = apiResponse.NeedsProfile,
                    Email = apiResponse.Email ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in AuthenticateWithGoogleAsync: {ex.Message}");
                
                // Return a default response indicating failure
                return new AuthenticationResponseDto
                {
                    Token = string.Empty,
                    User = new UserDto
                    {
                        UserID = 0,
                        Username = string.Empty,
                        Email = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        Role = "User"
                    },
                    NeedsProfile = true,
                    Email = string.Empty
                };
            }
    }

        public async Task<AuthenticationResponseDto> AuthenticateAsync(string email, UserLoginDto loginDto)
    {
            return await _authService.AuthenticateAsync(email, loginDto);
        }

        public async Task<AuthenticationResponseDto> AuthenticateAsync(UserLoginDto loginDto)
        {
            return await _authService.AuthenticateAsync(loginDto);
    }

    public async Task SendPasswordResetEmailAsync(string email)
    {
        await _authService.SendPasswordResetEmailAsync(email);
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        return await _authService.ResetPasswordAsync(token, newPassword);
    }
    }
}
