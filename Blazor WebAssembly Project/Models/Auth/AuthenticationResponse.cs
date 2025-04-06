namespace Blazor_WebAssembly.Models.Auth
{
    public class AuthenticationResponse
    {
        public string Token { get; set; }
        public UserDto User { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}
