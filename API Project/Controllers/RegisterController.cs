using Domain_Project.DTOs;
using Domain_Project.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using static Domain_Project.DTOs.TeamMemberDto;

namespace API_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<User> _signInManager;

        public RegisterController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByNameAsync(model.Username);
            if (existingUser != null)
                return BadRequest("Username already exists.");

            var user = new User
            {
                UserName = model.Username,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }

            await _userManager.AddToRoleAsync(user, model.Role);
            await _signInManager.SignInAsync(user, isPersistent: false);

            return Ok(new { Message = "User registered successfully." });
        }
    }
}
