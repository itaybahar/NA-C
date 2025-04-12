using System.Collections.Generic;
using System.Threading.Tasks;
using Domain_Project.Models;

namespace Domain_Project.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> ValidateUserCredentialsAsync(string username, string password);
        Task<IEnumerable<UserRole>> GetUserRolesAsync(int userId);
        Task SaveChangesAsync();
        Task<User> GetUserByResetTokenAsync(string token);
    }
}
