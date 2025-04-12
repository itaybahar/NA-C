using System.ComponentModel.DataAnnotations;

namespace Domain_Project.Models.Request
{
    public class LoginRequest
    {
        [Required]
        public required string Username { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}