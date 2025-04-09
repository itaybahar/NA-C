using API_Project.Services;
using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Domain_Project.Models.Request;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;

namespace API_Project.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly Domain_Project.Interfaces.IAuthenticationService _authService;

        public AuthController(IUserRepository userRepository, Domain_Project.Interfaces.IAuthenticationService authService)
        {
            _userRepository = userRepository;
            _authService = authService;
        }

        private async Task<Services.AuthenticationResponseDto> GetAuthenticateResultAsync()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return await ConvertToAuthResponseDto(authenticateResult);
        }

        private async Task<Services.AuthenticationResponseDto> ConvertToAuthResponseDto(AuthenticateResult result)
        {
            if (!result.Succeeded)
                return new Services.AuthenticationResponseDto { Token = string.Empty, User = new UserDto() };

            var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return new Services.AuthenticationResponseDto { Token = string.Empty, User = new UserDto() };

            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return new Services.AuthenticationResponseDto { Token = string.Empty, User = new UserDto() };

            // Generate JWT token using your authentication service
            var token = _authService.GenerateJwtToken(user);

            return new Services.AuthenticationResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    UserID = user.UserID,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty
                    // Role, IsActive, and CreatedDate properties have been removed
                    // as they don't exist in UserDto class
                }
            };
        }

        private string GenerateJwtToken(User user)
        {
            // Here you would use your actual token generation logic
            // This would typically come from your authentication service
            return _authService.GenerateJwtToken(user);
        }

        // Fixed: Make this method private to avoid Swagger ambiguity
        private Domain_Project.Interfaces.IAuthenticationService Get_authService()
        {
            return _authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
        {
            try
            {
                var user = await _userRepository.GetUserByUsernameAsync(loginDto.Username);
                if (user == null)
                {
                    return Unauthorized("Invalid username or password.");
                }

                // Change this line - use ValidateUserCredentialsAsync from IUserRepository instead
                var isValid = await _userRepository.ValidateUserCredentialsAsync(user.Username, loginDto.Password);
                if (!isValid)
                {
                    return Unauthorized("Invalid username or password.");
                }

                var token = _authService.GenerateJwtToken(user);

                return Ok(new
                {
                    token,
                    user = new UserDto
                    {
                        UserID = user.UserID,
                        Username = user.Username,
                        Email = user.Email,
                        FirstName = user.FirstName ?? string.Empty,
                        LastName = user.LastName ?? string.Empty,
                        Role = user.Role ?? string.Empty
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized($"Invalid email or password. {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Authentication error: {ex.Message}");
            }
        }


        // ✅ Login with Google
        [HttpGet("login-google")]
        public IActionResult LoginWithGoogle()
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = "/auth/google-callback"
            };
            return Challenge(props, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return Redirect("https://localhost:7176/login?error=google-auth-failed");

            var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal?.Identity?.Name ?? email;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
                return Redirect("https://localhost:7176/login?error=missing-user-info");

            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Username = name,
                    Role = "User" // Default role
                };
                await _userRepository.AddAsync(user);
            }

            // Convert to our auth response DTO
            var authResponse = await ConvertToAuthResponseDto(result);
            if (authResponse == null || string.IsNullOrEmpty(authResponse.Token))
                return Redirect("https://localhost:7176/login?error=token-generation-failed");

            return Redirect($"https://localhost:7176/login?token={authResponse.Token}");
        }

        // ✅ Register new user without FirstName and LastName
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userDto = new UserDto
                {
                    Username = request.Username,
                    Email = request.Email,
                    // Removed FirstName and LastName as requested
                    Role = string.IsNullOrEmpty(request.Role) ? "User" : request.Role
                };

                await _authService.RegisterUserAsync(userDto, request.Password);
                return Ok(new { message = "Registration successful" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            // Sign out of cookie authentication used for Google login
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // For API clients using JWT tokens, the client should discard the token
            // Server-side doesn't need to do anything special for JWT invalidation

            // Fixed: StringValues comparison issue
            bool isJsonRequest = false;
            if (Request.Headers.TryGetValue("Accept", out StringValues acceptValues))
            {
                isJsonRequest = acceptValues.Any(h => h?.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (isJsonRequest)
            {
                return Ok(new { message = "Logged out successfully" });
            }

            // For browser clients, redirect to login page
            return Redirect("https://localhost:7176/login");
        }
    }
}
