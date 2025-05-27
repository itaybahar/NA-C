using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly.Models.Auth
{
    public class CompleteProfileModel
    {
        [Required(ErrorMessage = "כתובת אימייל נדרשת")]
        [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "שם פרטי נדרש")]
        [MinLength(2, ErrorMessage = "שם פרטי חייב להכיל לפחות 2 תווים")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "שם משפחה נדרש")]
        [MinLength(2, ErrorMessage = "שם משפחה חייב להכיל לפחות 2 תווים")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "מספר טלפון נדרש")]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "מספר טלפון לא תקין")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "סיסמה נדרשת")]
        [MinLength(6, ErrorMessage = "הסיסמה חייבת להכיל לפחות 6 תווים")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "אימות סיסמה נדרש")]
        [Compare("Password", ErrorMessage = "הסיסמאות אינן תואמות")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? Token { get; set; }
    }
} 