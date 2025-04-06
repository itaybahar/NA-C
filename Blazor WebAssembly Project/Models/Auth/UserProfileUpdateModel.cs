using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly.Models.Auth
{
    public class UserProfileUpdateModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        public string PhoneNumber { get; set; }
    }
}