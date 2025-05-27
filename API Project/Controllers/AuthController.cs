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
using System.Net.Http;
using System.Text.Json;

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

        private async Task<Domain_Project.DTOs.AuthenticationResponseDto> ConvertToAuthResponseDto(AuthenticateResult result)
        {
            if (!result.Succeeded)
                return new Domain_Project.DTOs.AuthenticationResponseDto
                {
                    Token = string.Empty,
                    User = new Domain_Project.DTOs.UserDto
                    {
                        UserID = 0,
                        Username = string.Empty,
                        Email = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        Role = string.Empty
                    },
                    NeedsProfile = true,
                    Email = string.Empty
                };

            var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return new Domain_Project.DTOs.AuthenticationResponseDto
                {
                    Token = string.Empty,
                    User = new Domain_Project.DTOs.UserDto
                    {
                        UserID = 0,
                        Username = string.Empty,
                        Email = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        Role = string.Empty
                    },
                    NeedsProfile = true,
                    Email = string.Empty
                };

            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return new Domain_Project.DTOs.AuthenticationResponseDto
                {
                    Token = string.Empty,
                    User = new Domain_Project.DTOs.UserDto
                    {
                        UserID = 0,
                        Username = string.Empty,
                        Email = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        Role = string.Empty
                    },
                    NeedsProfile = true,
                    Email = string.Empty
                };

            var token = _authService.GenerateJwtToken(user);

            return new Domain_Project.DTOs.AuthenticationResponseDto
            {
                Token = token,
                User = new Domain_Project.DTOs.UserDto
                {
                    UserID = user.UserID,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    Role = user.Role ?? string.Empty
                },
                NeedsProfile = false,
                Email = user.Email
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
                RedirectUri = "/auth/google-callback",
                AllowRefresh = true,
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10)
            };
            return Challenge(props, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                // Add debugging info
                Console.WriteLine("Google callback received");
                
                // Try to read directly from request query to check state
                var state = Request.Query["state"].ToString();
                var code = Request.Query["code"].ToString();
                
                Console.WriteLine($"State: {(string.IsNullOrEmpty(state) ? "NULL" : state.Substring(0, Math.Min(20, state.Length)) + "...")}");
                Console.WriteLine($"Code: {(string.IsNullOrEmpty(code) ? "NULL" : code.Substring(0, Math.Min(10, code.Length)) + "...")}");
                
                var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (!result.Succeeded)
                {
                    Console.WriteLine("Failed to authenticate with Google: " + result.Failure?.Message);
                    return Redirect("https://localhost:7176/login?error=google-auth-failed");
                }

                var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
                var name = result.Principal?.Identity?.Name ?? email;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
                {
                    Console.WriteLine("Missing email or name from Google auth");
                    return Redirect("https://localhost:7176/login?error=missing-user-info");
                }

                var user = await _userRepository.GetUserByEmailAsync(email);
                bool isNew = user == null;

                if (isNew)
                {
                    // Don't create a partial user here - let the complete profile form handle it
                    // For now, just generate a token that can be used for completing the profile
                    Console.WriteLine($"New user detected for email: {email}");
                    
                    var tempUser = new User { 
                        Email = email, 
                        Username = name,
                        Role = "User",
                        PasswordHash = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty 
                    };
                    
                    var tempToken = _authService.GenerateJwtToken(tempUser);
                    Console.WriteLine($"Generated temp token for new user. Token exists: {!string.IsNullOrEmpty(tempToken)}, Length: {tempToken?.Length ?? 0}");
                    
                    if (string.IsNullOrEmpty(tempToken))
                    {
                        Console.WriteLine("ERROR: Failed to generate temp token!");
                        return Redirect("https://localhost:7176/login?error=token-generation-failed");
                    }
                    
                    // Redirect with email for new user completion
                    var redirectUrl = $"https://localhost:7176/login?new=true&token={tempToken}&email={Uri.EscapeDataString(email)}";
                    Console.WriteLine($"Redirecting new user to: {redirectUrl.Substring(0, 50)}...");
                    Console.WriteLine($"Redirecting with token: {tempToken}");
                    return Redirect(redirectUrl);
                }
                
                // Existing user - generate token and redirect
                Console.WriteLine($"Existing user found: {user.Email}, UserID: {user.UserID}");
                var existingUserToken = _authService.GenerateJwtToken(user);
                Console.WriteLine($"Generated token for existing user. Token exists: {!string.IsNullOrEmpty(existingUserToken)}, Length: {existingUserToken?.Length ?? 0}");
                
                if (string.IsNullOrEmpty(existingUserToken))
                {
                    Console.WriteLine("ERROR: Failed to generate token for existing user!");
                    return Redirect("https://localhost:7176/login?error=token-generation-failed");
                }

                var existingUserRedirectUrl = $"https://localhost:7176/login?token={existingUserToken}&email={user.Email}";
                Console.WriteLine($"Redirecting existing user to: {existingUserRedirectUrl.Substring(0, 50)}...");
                Console.WriteLine($"Redirecting with token: {existingUserToken}");
                return Redirect(existingUserRedirectUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during Google callback: " + ex.Message);
                return Redirect("https://localhost:7176/login?error=google-callback-error");
            }
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

        [HttpPost("complete-google-profile")]
        public async Task<IActionResult> CompleteGoogleProfile([FromBody] CompleteGoogleProfileRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Username) ||
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.ConfirmPassword))
                {
                    return BadRequest(new { message = "All fields are required." });
                }

                if (request.Password != request.ConfirmPassword)
                {
                    return BadRequest(new { message = "Passwords do not match." });
                }

                // Check if user already exists
                var existingUser = await _userRepository.GetUserByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    // If user exists but has no role, assign default role
                    if (string.IsNullOrEmpty(existingUser.Role))
                    {
                        existingUser.Role = "User";
                        await _userRepository.UpdateAsync(existingUser);
                        await _userRepository.SaveChangesAsync();
                    }
                    return BadRequest(new { message = "User with this email already exists." });
                }

                var existingUsername = await _userRepository.GetUserByUsernameAsync(request.Username);
                if (existingUsername != null)
                {
                    return BadRequest(new { message = "Username is already taken." });
                }

                // Create new user with default role
                var user = new User
                {
                    Email = request.Email,
                    Username = request.Username,
                    Role = "User", // Ensure default role is set
                    PasswordHash = _authService.HashPassword(request.Password),
                    FirstName = request.Username.Split(' ').FirstOrDefault() ?? string.Empty,
                    LastName = request.Username.Split(' ').Skip(1).FirstOrDefault() ?? string.Empty,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();

                // Generate JWT token
                var token = _authService.GenerateJwtToken(user);

                // Create response DTO
                var response = new Domain_Project.DTOs.AuthenticationResponseDto
                {
                    Token = token,
                    User = new Domain_Project.DTOs.UserDto
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

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in complete-google-profile: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while completing your profile. Please try again." });
            }
        }

        [HttpGet("test-cookies")]
        public IActionResult TestCookies()
        {
            // Set a test cookie
            HttpContext.Response.Cookies.Append(
                "TestCookie",
                "CookieValue-" + DateTime.Now.ToString("HH:mm:ss"),
                new CookieOptions
                {
                    SameSite = SameSiteMode.None,
                    Secure = true,
                    HttpOnly = true,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5)
                }
            );
            
            return Ok(new
            {
                Message = "Test cookie set. Check browser developer tools to see if it was saved.",
                CookieSettings = new
                {
                    SameSite = "None",
                    Secure = true, 
                    HttpOnly = true
                },
                Timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("google-direct")]
        public IActionResult GoogleDirect()
        {
            var state = Guid.NewGuid().ToString("N");
            var redirectUri = "https://localhost:7235/auth/google-simple-callback";
            var googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                $"client_id=157978226290-21smsb9rka7244tf6jbe5k7bceaicfp6.apps.googleusercontent.com&" +
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                $"response_type=code&" +
                $"scope={Uri.EscapeDataString("openid email profile")}&" +
                $"state={state}&" +
                $"access_type=offline&" +
                $"prompt=consent";
            return Redirect(googleAuthUrl);
        }

        [HttpGet("google-simple-callback")]
        public async Task<IActionResult> GoogleSimpleCallback([FromQuery] string code, [FromQuery] string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                    return Redirect("https://localhost:7176/login?error=no-code");

                // Exchange code for access token
                using var httpClient = new HttpClient();
                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token");
                var parameters = new Dictionary<string, string>
                {
                    { "code", code },
                    { "client_id", "157978226290-21smsb9rka7244tf6jbe5k7bceaicfp6.apps.googleusercontent.com" },
                    { "client_secret", "GOCSPX-CzsSRlDyyJiWiVXGHjUOciOTYWL" },
                    { "redirect_uri", "https://localhost:7235/auth/google-simple-callback" },
                    { "grant_type", "authorization_code" }
                };
                tokenRequest.Content = new FormUrlEncodedContent(parameters);

                Console.WriteLine("Exchanging code for token...");
                var tokenResponse = await httpClient.SendAsync(tokenRequest);
                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                Console.WriteLine("Token response: " + tokenJson);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine("Token exchange failed");
                    return Redirect("https://localhost:7176/login?error=token-exchange-failed");
                }

                Console.WriteLine("Parsing token...");
                var tokenData = JsonDocument.Parse(tokenJson).RootElement;
                var accessToken = tokenData.GetProperty("access_token").GetString();

                Console.WriteLine("Getting user info...");
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var userInfoResponse = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
                Console.WriteLine("User info response: " + userInfoJson);

                if (!userInfoResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine("User info failed");
                    return Redirect("https://localhost:7176/login?error=userinfo-failed");
                }

                var userInfo = JsonDocument.Parse(userInfoJson).RootElement;
                var email = userInfo.GetProperty("email").GetString();
                var name = userInfo.GetProperty("name").GetString();

                // Find or create user in your DB
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    // Create a temporary token for profile completion
                    var tempUser = new User
                    {
                        Email = email,
                        Username = name,
                        Role = "User", // Ensure default role is set
                        PasswordHash = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty
                    };
                    var tempToken = _authService.GenerateJwtToken(tempUser);
                    return Redirect($"https://localhost:7176/login?new=true&token={tempToken}&email={Uri.EscapeDataString(email)}");
                }

                // Ensure existing user has a role
                if (string.IsNullOrEmpty(user.Role))
                {
                    user.Role = "User";
                    await _userRepository.UpdateAsync(user);
                    await _userRepository.SaveChangesAsync();
                    Console.WriteLine($"Assigned default role 'User' to existing user: {user.Email}");
                }

                // Generate token for existing user
                var token = _authService.GenerateJwtToken(user);
                return Redirect($"https://localhost:7176/login?token={token}&email={user.Email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GoogleSimpleCallback: {ex.Message}");
                return Redirect("https://localhost:7176/login?error=callback-failed");
            }
        }

        [HttpPost("google-jwt-login")]
        public async Task<IActionResult> GoogleJwtLogin([FromBody] string credential)
        {
            try
            {
                if (string.IsNullOrEmpty(credential))
                {
                    return BadRequest("Google credential is required");
                }

                Console.WriteLine($"Received Google credential: {credential}");
                var result = await _authService.AuthenticateWithGoogleAsync(credential);

                if (result == null)
                {
                    return BadRequest("Failed to authenticate with Google");
                }

                if (result.NeedsProfile)
                {
                    // For new users, return a temporary token and needsProfile flag
                    return Ok(new
                    {
                        needsProfile = true,
                        email = result.Email,
                        token = result.Token,
                        user = result.User // Include user info for pre-filling the profile form
                    });
                }

                // For existing users, return the full authentication response
                return Ok(new
                {
                    needsProfile = false,
                    token = result.Token,
                    user = result.User,
                    email = result.Email
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Google JWT login: {ex.Message}");
                return BadRequest($"Authentication failed: {ex.Message}");
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "AuthController is responding", timestamp = DateTime.UtcNow });
        }
    }
}
