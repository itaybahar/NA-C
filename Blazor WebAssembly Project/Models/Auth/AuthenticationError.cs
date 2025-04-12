namespace Blazor_WebAssembly.Models.Auth
{
    public class AuthenticationError
    {
        public required string ErrorCode { get; set; }
        public required string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}