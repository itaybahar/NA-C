using Domain_Project.DTOs;

namespace Blazor_WebAssembly.Models.Auth
{
    public class AuthenticationResponse
    {
        public required string Token { get; set; }
        public required UserDto User { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool NeedsProfile { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
