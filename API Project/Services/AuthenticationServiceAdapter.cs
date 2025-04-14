using API_Project.Services;
using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models;

public class AuthenticationServiceAdapter : Domain_Project.Interfaces.IAuthenticationService
{
    private readonly API_Project.Services.AuthenticationService _authService;
    private readonly IUserRepository _userRepository;
    private readonly AuthenticationService _authenticationService;
    private readonly HttpClient _httpClient;

    // Constructor for API layer services
    public AuthenticationServiceAdapter(API_Project.Services.AuthenticationService authService, IUserRepository userRepository)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _authenticationService = authService; // Initialize to avoid nullability issues
        _httpClient = new HttpClient(); // Default HttpClient initialization
    }

    // Constructor for Blazor WebAssembly services
    public AuthenticationServiceAdapter(AuthenticationService authenticationService, HttpClient httpClient)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authService = authenticationService; // Initialize to avoid nullability issues
        _userRepository = null!; // Mark as non-nullable since it's not used in this context
    }

    public string GenerateJwtToken(User user)
    {
        return _authService.GenerateJwtToken(user);
    }

    public async Task RegisterUserAsync(UserDto userDto, string password)
    {
        await _authService.RegisterUserAsync(userDto, password);
    }

    public async Task<Domain_Project.DTOs.AuthenticationResponseDto> AuthenticateAsync(UserLoginDto loginDto)
    {
        // Get the user
        var user = await _userRepository.GetUserByUsernameAsync(loginDto.Username);

        // Check if user exists
        if (user == null)
        {
            return Domain_Project.DTOs.AuthenticationResponseDto.CreateFailedResponse();
        }

        // Validate credentials
        var isValid = await _userRepository.ValidateUserCredentialsAsync(user.Username, loginDto.Password);
        if (!isValid)
        {
            return Domain_Project.DTOs.AuthenticationResponseDto.CreateFailedResponse();
        }

        // Continue with authentication
        var roles = await _userRepository.GetUserRolesAsync(user.UserID);
        var token = _authService.GenerateJwtToken(user);

        // Update last login date
        user.LastLoginDate = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        // Create API layer DTO
        var apiResponse = new API_Project.Services.AuthenticationResponseDto
        {
            Token = token,
            User = new UserDto
            {
                UserID = user.UserID,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role ?? "User" // Default to "User" if role is null
            }
        };

        // Convert to Domain layer DTO
        return Domain_Project.DTOs.AuthenticationResponseDto.FromApiDto(apiResponse);
    }

    public async Task<Domain_Project.DTOs.AuthenticationResponseDto> AuthenticateWithGoogleAsync(string googleToken)
    {
        var result = await _authService.AuthenticateWithGoogleAsync(googleToken);
        return Domain_Project.DTOs.AuthenticationResponseDto.FromApiDto(result);
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
            throw new ArgumentNullException(nameof(source));

        return Domain_Project.DTOs.AuthenticationResponseDto.FromApiDto(source);
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

    public Task<string?> GetTokenAsync()
    {
        throw new NotImplementedException();
    }
}
