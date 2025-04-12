namespace Blazor_WebAssembly.Models.Auth
{
    public class AuthenticationResponse
    {
        public required string Token { get; set; }
        public required UserDto User { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}
