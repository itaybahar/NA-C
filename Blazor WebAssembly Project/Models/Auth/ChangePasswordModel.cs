using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly.Models.Auth
{
    public class ChangePasswordModel
    {
        [Required(ErrorMessage = "Current Password is required")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "New Password must be at least 6 characters long")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm New Password is required")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "New Passwords do not match")]
        public string ConfirmNewPassword { get; set; }
    }
}