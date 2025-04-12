using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models.Request;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;

namespace API_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public RegisterController(IAuthenticationService authService)
        {
            _authService = authService;
        }
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {

            using var reader = new StreamReader(Request.Body);
            var rawRequestBody = await reader.ReadToEndAsync();
            Console.WriteLine($"Raw Request Body: {rawRequestBody}");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userDto = new UserDto
                {
                    Username = request.Username,
                    Email = request.Email,
                    Role = string.IsNullOrEmpty(request.Role) ? "User" : request.Role,
                    FirstName = string.Empty,
                    LastName = string.Empty
                };

                await _authService.RegisterUserAsync(userDto, request.Password);
                return Ok(new { message = "Registration successful" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}