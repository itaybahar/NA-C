namespace Blazor_WebAssembly.Models.Auth
{
    public class UserDto
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}