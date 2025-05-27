using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly_Project.Models
{
    public class CompleteProfileModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        [MinLength(4, ErrorMessage = "Username must be at least 4 characters")]
        public string Username { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        public string? Token { get; set; }
    }
} 