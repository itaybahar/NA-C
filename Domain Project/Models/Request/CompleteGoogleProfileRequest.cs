namespace Domain_Project.Models.Request
{
    public class CompleteGoogleProfileRequest
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string? Token { get; set; } 
    }
}