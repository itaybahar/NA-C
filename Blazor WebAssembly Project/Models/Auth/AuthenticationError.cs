namespace Blazor_WebAssembly.Models.Auth
{
    public class AuthenticationError
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}