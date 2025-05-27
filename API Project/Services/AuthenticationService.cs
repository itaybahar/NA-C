using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Domain_Project.DTOs;
using Domain_Project.Models;
using Domain_Project.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using API_Project.Configuration;
using API_Project.Repositories;

namespace API_Project.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly AuthenticationSettings _authSettings;
        private readonly IEmailService _emailService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IUserRepository userRepository,
            AuthenticationSettings authSettings,
            IEmailService emailService,
            IHttpClientFactory httpClientFactory,
            ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository;
            _authSettings = authSettings;
            _emailService = emailService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<Domain_Project.Interfaces.GoogleUserInfo?> ValidateGoogleTokenAsync(string credential)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"https://www.googleapis.com/oauth2/v3/tokeninfo?id_token={credential}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to validate token with Google. Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Google tokeninfo raw response: {Content}", content);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { 
                        new JsonStringEnumConverter(),
                        new BooleanConverter() // Custom converter for handling string booleans
                    }
                };

                var tokenInfo = JsonSerializer.Deserialize<GoogleUserInfo>(content, options);

                if (tokenInfo == null)
                {
                    _logger.LogWarning("Failed to deserialize Google token info");
                    return null;
                }

                _logger.LogDebug("Raw token info: {@TokenInfo}", tokenInfo);
                _logger.LogDebug("Deserialized token info - Email: {Email}, EmailVerified: {EmailVerified}, Id: {Id}, Name: {Name}",
                    tokenInfo.Email,
                    tokenInfo.EmailVerified,
                    tokenInfo.Id,
                    tokenInfo.Name);

                // Validate the token
                var expectedClientId = "157978226290-21smsb9rka7244tf6jbe5k7bceaicfp6.apps.googleusercontent.com";
                if (tokenInfo.Aud != expectedClientId)
                {
                    _logger.LogWarning("Invalid client ID in token. Expected: {ExpectedClientId}, Got: {ActualClientId}", 
                        expectedClientId, tokenInfo.Aud);
                    return null;
                }

                if (string.IsNullOrEmpty(tokenInfo.Email))
                {
                    _logger.LogWarning("Email is missing from token info. Raw token info: {@TokenInfo}", tokenInfo);
                    return null;
                }

                if (!tokenInfo.EmailVerified)
                {
                    _logger.LogWarning("Email is not verified for user: {Email}", tokenInfo.Email);
                    return null;
                }

                _logger.LogInformation("Successfully validated Google token for user: {Email}", tokenInfo.Email);

                var result = new Domain_Project.Interfaces.GoogleUserInfo
                {
                    Id = tokenInfo.Id,
                    Email = tokenInfo.Email,
                    GivenName = tokenInfo.GivenName,
                    FamilyName = tokenInfo.FamilyName,
                    Name = tokenInfo.Name,
                    Picture = tokenInfo.Picture,
                    EmailVerified = tokenInfo.EmailVerified
                };

                _logger.LogDebug("Created Domain GoogleUserInfo object: {@Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google token");
                return null;
            }
        }

        public async Task<AuthenticationResponseDto> AuthenticateWithGoogleAsync(string googleToken)
        {
            try
            {
                var googleUser = await ValidateGoogleTokenAsync(googleToken);
                if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
                {
                    _logger.LogWarning("Failed to validate Google token or missing email");
                    return new AuthenticationResponseDto
                    {
                        Token = string.Empty,
                        User = null,
                        NeedsProfile = true,
                        Email = string.Empty
                    };
                }

                var user = await _userRepository.GetUserByEmailAsync(googleUser.Email);
                if (user == null)
                {
                    _logger.LogInformation("Creating temporary token for new Google user: {Email}", googleUser.Email);
                    // Don't create the user yet, just return a temporary token for profile completion
                    return new AuthenticationResponseDto
                    {
                        Token = googleToken, // Use the Google token as a temporary token
                        User = new UserDto
                        {
                            UserID = 0,
                            Username = string.Empty,
                            Email = googleUser.Email,
                            FirstName = googleUser.GivenName ?? string.Empty,
                            LastName = googleUser.FamilyName ?? string.Empty,
                            Role = "User" // Default role for new users
                        },
                        NeedsProfile = true,
                        Email = googleUser.Email
                    };
                }

                // Ensure existing user has a role
                if (string.IsNullOrEmpty(user.Role))
                {
                    user.Role = "User"; // Default role
                    await _userRepository.UpdateAsync(user);
                    await _userRepository.SaveChangesAsync();
                    _logger.LogInformation("Assigned default role 'User' to existing user: {Email}", user.Email);
                }

                var token = GenerateJwtToken(user);
                return new AuthenticationResponseDto
                {
                    Token = token,
                    User = new UserDto
                    {
                        UserID = user.UserID,
                        Username = user.Username,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = user.Role
                    },
                    NeedsProfile = false,
                    Email = user.Email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AuthenticateWithGoogleAsync");
                return new AuthenticationResponseDto
                {
                    Token = string.Empty,
                    User = null,
                    NeedsProfile = true,
                    Email = string.Empty
                };
            }
        }

        public async Task<AuthenticationResponseDto> AuthenticateAsync(string email, UserLoginDto loginDto)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            return await AuthenticateInternalAsync(user, loginDto.Password);
        }

        public async Task<AuthenticationResponseDto> AuthenticateAsync(UserLoginDto loginDto)
        {
            var user = await _userRepository.GetUserByUsernameAsync(loginDto.Username);
            return await AuthenticateInternalAsync(user, loginDto.Password);
        }

        private async Task<AuthenticationResponseDto> AuthenticateInternalAsync(User? user, string password)
        {
            if (user == null || !ValidatePassword(user, password))
            {
                return new AuthenticationResponseDto
                {
                    Token = string.Empty,
                    User = null,
                    NeedsProfile = false,
                    Email = string.Empty
                };
            }

            var token = GenerateJwtToken(user);
            return new AuthenticationResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    UserID = user.UserID,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role
                },
                NeedsProfile = false,
                Email = user.Email
            };
        }

        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_authSettings.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("UserID", user.UserID.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public bool ValidatePassword(User user, string password)
        {
            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public async Task<string?> GetTokenAsync()
        {
            return null; // Implement if needed
        }

        public async Task RegisterUserAsync(UserDto userDto, string password)
        {
            var hashedPassword = HashPassword(password);
            var user = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                PasswordHash = hashedPassword,
                Role = "User",
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task SendPasswordResetEmailAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null) return;

            var token = GeneratePasswordResetToken();
            user.PasswordResetToken = token;
            user.PasswordResetExpiration = DateTime.UtcNow.AddHours(24);

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            var resetLink = $"https://localhost:7176/reset-password?token={token}";
            var emailBody = $"Click the following link to reset your password: {resetLink}";

            await _emailService.SendEmailAsync(email, "Password Reset Request", emailBody);
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _userRepository.GetUserByResetTokenAsync(token);
            if (user == null || user.PasswordResetExpiration < DateTime.UtcNow)
                return false;

            user.PasswordHash = HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetExpiration = null;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        private string GeneratePasswordResetToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "");
        }
    }
} 