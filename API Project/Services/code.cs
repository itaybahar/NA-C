using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using API_Project.Configuration;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Domain_Project.DTOs;

namespace API_Project.Services
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResponseDto> AuthenticateAsync(UserLoginDto loginDto);
        Task<UserDto> RegisterUserAsync(UserDto userDto, string password);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly AuthenticationSettings _authSettings;

        public AuthenticationService(
            IUserRepository userRepository,
            AuthenticationSettings authSettings)
        {
            _userRepository = userRepository;
            _authSettings = authSettings;
        }

        public async Task<AuthenticationResponseDto> AuthenticateAsync(UserLoginDto loginDto)
        {
            // Validate credentials
            var isValidUser = await _userRepository.ValidateUserCredentialsAsync(
                loginDto.Username,
                loginDto.Password
            );

            if (!isValidUser)
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            // Get user details
            var user = await _userRepository.GetUserByUsernameAsync(loginDto.Username);

            // Get user roles
            var userRoles = await _userRepository.GetUserRolesAsync(user.UserID);

            // Generate token
            var token = GenerateJwtToken(user, userRoles);

            // Update last login date
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
                    LastName = user.LastName
                }
            };
        }

        public async Task<UserDto> RegisterUserAsync(UserDto userDto, string password)
        {
            // Check if username already exists
            var existingUser = await _userRepository.GetUserByUsernameAsync(userDto.Username);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Username already exists");
            }

            // Create new user
            var newUser = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                PasswordHash = password, // The repository will hash this
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            // Save user
            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            // Return DTO
            return new UserDto
            {
                UserID = newUser.UserID,
                Username = newUser.Username,
                Email = newUser.Email,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName
            };
        }

        private string GenerateJwtToken(User user, IEnumerable<UserRole> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            // Add roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
            }

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
    }

    // Additional DTOs for authentication
    public class AuthenticationResponseDto
    {
        public string Token { get; set; }
        public UserDto User { get; set; }
    }
}
