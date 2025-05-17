using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly_Project.Models.Auth;

public class RegisterModel
{
    [Required(ErrorMessage = "שדה שם המשתמש הוא חובה")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "שדה האימייל הוא חובה")]
    [EmailAddress(ErrorMessage = "כתובת האימייל אינה תקינה")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "שדה הסיסמה הוא חובה")]
    [MinLength(6, ErrorMessage = "הסיסמה חייבת להכיל לפחות 6 תווים")]
    public required string Password { get; set; }

    [Required(ErrorMessage = "יש לאשר את הסיסמה")]
    [Compare("Password", ErrorMessage = "הסיסמאות אינן תואמות")]
    public required string ConfirmPassword { get; set; }

    public string? Role { get; set; }
}
