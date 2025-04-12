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
using Microsoft.EntityFrameworkCore;
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

        private async Task<Services.AuthenticationResponseDto> ConvertToAuthResponseDto(AuthenticateResult result)
        {
            if (!result.Succeeded)
                return new Services.AuthenticationResponseDto
                {
                    Token = string.Empty,
                    User = new UserDto
                    {
                        UserID = 0,
                        Username = string.Empty,
                        Email = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        Role = string.Empty
                    }
                };

            var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return new Services.AuthenticationResponseDto
                {
                    Token = string.Empty,
                    User = new UserDto
                    {
                        UserID = 0,
                        Username = string.Empty,
                        Email = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        Role = string.Empty
                    }
                };

            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return new Services.AuthenticationResponseDto
                {
                    Token = string.Empty,
                    User = new UserDto
                    {
                        UserID = 0,
                        Username = string.Empty,
                        Email = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        Role = string.Empty
                    }
                };

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
                    LastName = user.LastName ?? string.Empty,
                    Role = user.Role ?? string.Empty
                }
            };
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
        {
            try
            {
                Console.WriteLine("Login attempt for username: " + loginDto.Username);

                var user = await _userRepository.GetUserByUsernameAsync(loginDto.Username);
                if (user == null)
                {
                    Console.WriteLine("User not found: " + loginDto.Username);
                    return Unauthorized("Invalid username or password.");
                }

                var isValid = await _userRepository.ValidateUserCredentialsAsync(user.Username, loginDto.Password);
                if (!isValid)
                {
                    Console.WriteLine("Invalid password for username: " + loginDto.Username);
                    return Unauthorized("Invalid username or password.");
                }

                var token = _authService.GenerateJwtToken(user);
                Console.WriteLine("Token generated successfully for username: " + loginDto.Username);

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
            catch (Exception ex)
            {
                Console.WriteLine("Error during login: " + ex.Message);
                return StatusCode(500, $"Authentication error: {ex.Message}");
            }
        }


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
            bool isNew = false;

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Username = name,
                    Role = "User",
                    PasswordHash = string.Empty,
                    FirstName = string.Empty,
                    LastName = string.Empty
                };

                await _userRepository.AddAsync(user);
                isNew = true;
            }

            var authResponse = await ConvertToAuthResponseDto(result);
            if (authResponse == null || string.IsNullOrEmpty(authResponse.Token))
                return Redirect("https://localhost:7176/login?error=token-generation-failed");

            var redirectUrl = $"https://localhost:7176/login?token={authResponse.Token}";
            if (isNew)
                redirectUrl += "&new=true";

            return Redirect(redirectUrl);
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Normalize email to lowercase for consistent comparison
                var normalizedEmail = request.Email.Trim().ToLower();

                // Check if email already exists
                var existingUsername = await _userRepository.GetUserByUsernameAsync(request.Username);
                if (existingUsername != null)
                {
                    Console.WriteLine($"User with username {request.Username} already exists.");
                    return BadRequest(new { message = "Username is already taken." });
                }

                var userDto = new UserDto
                {
                    Username = request.Username,
                    Email = normalizedEmail,
                    Role = string.IsNullOrEmpty(request.Role) ? "User" : request.Role,
                    FirstName = string.Empty,
                    LastName = string.Empty
                };

                await _authService.RegisterUserAsync(userDto, request.Password);
                return Ok(new { message = "Registration successful" });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database update exception: {ex.Message}");
                return BadRequest(new { message = "Email or username already exists." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected exception: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }



        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            bool isJsonRequest = false;
            if (Request.Headers.TryGetValue("Accept", out StringValues acceptValues))
            {
                isJsonRequest = acceptValues.Any(h => h?.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (isJsonRequest)
                return Ok(new { message = "Logged out successfully" });

            return Redirect("https://localhost:7176/login");
        }
    }
}
