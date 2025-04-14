using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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

    public class AuthenticationService : IAuthenticationService
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
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _authSettings = authSettings ?? throw new ArgumentNullException(nameof(authSettings));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
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
            var googleUserInfo = await VerifyGoogleTokenAsync(googleToken);
            if (googleUserInfo == null)
                throw new UnauthorizedAccessException("Invalid Google token");

            var user = await _userRepository.GetUserByEmailAsync(googleUserInfo.Email);

            if (user == null)
            {
                user = new User
                {
                    Email = googleUserInfo.Email,
                    Username = googleUserInfo.Email,
                    FirstName = googleUserInfo.GivenName,
                    LastName = googleUserInfo.FamilyName,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    PasswordHash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                    Role = "User"
                };

                user = await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();
            }

            user.LastLoginDate = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

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
                    Role = user.Role ?? "User"
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
                    Role = user.Role ?? "User"
                }
            };
        }

        public async Task RegisterUserAsync(UserDto userDto, string password)
        {
            var email = userDto.Email.Trim().ToLower();

            var existingUserByEmail = await _userRepository.GetUserByEmailAsync(email);
            if (existingUserByEmail != null)
                throw new InvalidOperationException("Email is already in use");

            var existingUserByUsername = await _userRepository.GetUserByUsernameAsync(userDto.Username);
            if (existingUserByUsername != null)
                throw new InvalidOperationException("Username is already taken");

            using var sha256 = SHA256.Create();
            var hashedPassword = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));

            var newUser = new User
            {
                Username = userDto.Username,
                Email = email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                PasswordHash = hashedPassword,
                Role = "User"
            };

            await _userRepository.AddAsync(newUser);
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

            using var sha256 = SHA256.Create();
            user.PasswordHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(newPassword)));
            user.PasswordResetToken = null;
            user.PasswordResetExpiration = null;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _authSettings.Issuer,
                audience: _authSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_authSettings.ExpirationInMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateJwtToken(User user, IEnumerable<UserRole> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString())
            };

            if (!string.IsNullOrEmpty(user.Role))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role));
            }

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.RoleName)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _authSettings.Issuer,
                audience: _authSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_authSettings.ExpirationInMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool ValidatePassword(User user, string password)
        {
            using var sha256 = SHA256.Create();
            var hashedPassword = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return user.PasswordHash == hashedPassword;
        }

        public Task<string?> GetTokenAsync()
        {
            throw new NotImplementedException();
        }
    }
}
