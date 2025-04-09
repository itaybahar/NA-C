using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly_Project.Models.Auth;

public class RegisterModel
{
    [Required]
    public required string Username { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MinLength(6)]
    public required string Password { get; set; }

    [Required]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public required string ConfirmPassword { get; set; }

    public string? Role { get; set; }
}
