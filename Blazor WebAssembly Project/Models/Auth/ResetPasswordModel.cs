using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly.Models.Auth
{
    public class ResetPasswordModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Reset Token is required")]
        public required string ResetToken { get; set; }

        [Required(ErrorMessage = "New Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "New Password must be at least 6 characters long")]
        public required string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm New Password is required")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "New Passwords do not match")]
        public required string ConfirmNewPassword { get; set; }
    }
}