using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace API_Project.Services
{
    public class GoogleUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool VerifiedEmail { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GivenName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
    }

    public class AuthenticationResponseDto
    {
        public required string Token { get; set; }
        public required UserDto User { get; set; }
    }

    public class AuthenticationService : Domain_Project.Interfaces.IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly Configuration.AuthenticationSettings _authSettings;
        private readonly IEmailService _emailService;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthenticationService(IUserRepository userRepository,
                                     Configuration.AuthenticationSettings authSettings,
                                     IEmailService emailService,
                                     IHttpClientFactory httpClientFactory)
        {
            _userRepository = userRepository;
            _authSettings = authSettings;
            _emailService = emailService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<AuthenticationResponseDto> AuthenticateAsync(string email, UserLoginDto loginDto)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                throw new UnauthorizedAccessException("Email not found");

            return await AuthenticateInternalAsync(user, loginDto.Password);
        }

        public async Task<AuthenticationResponseDto> AuthenticateAsync(UserLoginDto loginDto)
        {
            var user = await _userRepository.GetUserByUsernameAsync(loginDto.Username);
            if (user == null)
                throw new UnauthorizedAccessException("Username not found");

            return await AuthenticateInternalAsync(user, loginDto.Password);
        }

        public async Task<AuthenticationResponseDto> AuthenticateWithGoogleAsync(string googleToken)
        {
            // Verify the Google token and get user info
            var googleUserInfo = await VerifyGoogleTokenAsync(googleToken);
            if (googleUserInfo == null)
                throw new UnauthorizedAccessException("Invalid Google token");

            // Check if user exists by email
            var user = await _userRepository.GetUserByEmailAsync(googleUserInfo.Email);

            // If user doesn't exist, create a new one
            if (user == null)
            {
                user = new User
                {
                    Email = googleUserInfo.Email,
                    Username = googleUserInfo.Email, // Use email as username initially
                    FirstName = googleUserInfo.GivenName,
                    LastName = googleUserInfo.FamilyName,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    // Set a random password since user will login via Google
                    PasswordHash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                    Role = "User" // Default role
                };

                user = await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();
            }

            // Update login timestamp
            user.LastLoginDate = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            // Generate token and return response
            var userRoles = await _userRepository.GetUserRolesAsync(user.UserID);
            var token = GenerateJwtToken(user, userRoles);

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
                    Role = user.Role ?? string.Empty
                }
            };
        }

        private async Task<GoogleUserInfo?> VerifyGoogleTokenAsync(string token)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v3/tokeninfo?id_token={token}");

                if (!response.IsSuccessStatusCode)
                    return null;

                var userInfo = await response.Content.ReadFromJsonAsync<GoogleUserInfo>();
                if (userInfo == null || string.IsNullOrEmpty(userInfo.Email) || !userInfo.VerifiedEmail)
                    return null;

                return userInfo;
            }
            catch
            {
                return null;
            }
        }

        private async Task<AuthenticationResponseDto> AuthenticateInternalAsync(User user, string password)
        {
            var isValid = await _userRepository.ValidateUserCredentialsAsync(user.Username, password);
            if (!isValid)
                throw new UnauthorizedAccessException("Invalid password");

            var userRoles = await _userRepository.GetUserRolesAsync(user.UserID);
            var token = GenerateJwtToken(user, userRoles);

            user.LastLoginDate = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

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
                    Role = user.Role ?? string.Empty
                }
            };
        }

        public async Task RegisterUserAsync(UserDto userDto, string password)
        {
            if (await _userRepository.GetUserByEmailAsync(userDto.Email) != null)
                throw new InvalidOperationException("Email is already in use");

            if (await _userRepository.GetUserByUsernameAsync(userDto.Username) != null)
                throw new InvalidOperationException("Username is already taken");

            var newUser = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                FirstName = string.Empty,
                LastName = string.Empty,
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                PasswordHash = password, // Will be hashed by repo
                Role = "User" // Force "User" role regardless of what was sent in the DTO
            };

            var createdUser = await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();
        }


        public async Task SendPasswordResetEmailAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return;

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            user.PasswordResetToken = token;
            user.PasswordResetExpiration = DateTime.UtcNow.AddHours(24);

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            var subject = "Password Reset Request";
            var body = $"Please use the following token to reset your password: {token}";

            await _emailService.SendEmailAsync(user.Email, subject, body);
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _userRepository.GetUserByResetTokenAsync(token);
            if (user == null || user.PasswordResetExpiration < DateTime.UtcNow)
                return false;

            user.PasswordHash = newPassword;
            user.PasswordResetToken = null;
            user.PasswordResetExpiration = DateTime.MinValue;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        public string GenerateJwtToken(User user)
        {
            var roles = _userRepository.GetUserRolesAsync(user.UserID).Result;
            return GenerateJwtToken(user, roles);
        }

        private string GenerateJwtToken(User user, IEnumerable<UserRole> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            // Add Role claim from user.Role if it exists
            if (!string.IsNullOrEmpty(user.Role))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role));
            }

            // Add any additional roles from the roles collection
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.RoleName)));

            var credentials = new SigningCredentials(
                _authSettings.GetSymmetricSecurityKey(),
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: _authSettings.Issuer,
                audience: _authSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_authSettings.ExpirationInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool ValidatePassword(User user, string password)
        {
            throw new NotImplementedException();
        }
    }
}
