// Complete implementation of AuthenticationServiceAdapter with conversion between DTO types
using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models;

public class AuthenticationServiceAdapter : Domain_Project.Interfaces.IAuthenticationService
{
    private readonly API_Project.Services.AuthenticationService _authService;
    private readonly IUserRepository _userRepository;

    public AuthenticationServiceAdapter(API_Project.Services.AuthenticationService authService, IUserRepository userRepository)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public string GenerateJwtToken(User user)
    {
        return _authService.GenerateJwtToken(user);
    }

    public async Task RegisterUserAsync(UserDto userDto, string password)
    {
        await _authService.RegisterUserAsync(userDto, password);
    }

    // Properly implemented with correct parameter handling
    public async Task<Domain_Project.DTOs.AuthenticationResponseDto> AuthenticateAsync(UserLoginDto loginDto)
    {
        var result = await _authService.AuthenticateAsync(loginDto);
        return ConvertToAuthResponseDto(result);
    }

    public async Task<Domain_Project.DTOs.AuthenticationResponseDto> AuthenticateWithGoogleAsync(string googleToken)
    {
        var result = await _authService.AuthenticateWithGoogleAsync(googleToken);
        return ConvertToAuthResponseDto(result);
    }

    public async Task SendPasswordResetEmailAsync(string email)
    {
        await _authService.SendPasswordResetEmailAsync(email);
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        return await _authService.ResetPasswordAsync(token, newPassword);
    }

    // Helper method to convert between the two AuthenticationResponseDto types
    private static Domain_Project.DTOs.AuthenticationResponseDto ConvertToAuthResponseDto(API_Project.Services.AuthenticationResponseDto source)
    {
        if (source == null)
            return null;

        return new Domain_Project.DTOs.AuthenticationResponseDto
        {
            Token = source.Token,
            User = source.User
        };
    }

    // Implement ValidatePassword method using the UserRepository
    public bool ValidatePassword(User user, string password)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrEmpty(password))
            return false;

        return _userRepository.ValidateUserCredentialsAsync(user.Username, password).GetAwaiter().GetResult();
    }
}
