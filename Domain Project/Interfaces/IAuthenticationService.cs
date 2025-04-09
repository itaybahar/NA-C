using Domain_Project.DTOs;
using Domain_Project.Models;

namespace Domain_Project.Interfaces
{
    public interface IAuthenticationService
    {
        string GenerateJwtToken(User user);
        Task RegisterUserAsync(UserDto userDto, string password);
        bool ValidatePassword(User user, string password);
    }
}
