using Microsoft.EntityFrameworkCore;
using API_Project.Data;
using API_Project.Repositories;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace API_Project.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(EquipmentManagementContext context) : base(context)
        {
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> ValidateUserCredentialsAsync(string username, string password)
        {
            var user = await GetUserByUsernameAsync(username);
            if (user == null)
                return false;

            // Hash the input password and compare with stored hash
            return VerifyPasswordHash(password, user.PasswordHash);
        }

        public async Task<IEnumerable<UserRole>> GetUserRolesAsync(int userId)
        {   
            // This is a placeholder. In a real implementation, you'd join with UserRoleAssignments
            return await _context.UserRoles
                .Where(ur => _context.UserRoleAssignments
                    .Any(ura => ura.UserID == userId && ura.RoleID == ur.RoleID))
                .ToListAsync();
        }

        // Utility method to hash passwords
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Utility method to verify password hash
        private bool VerifyPasswordHash(string inputPassword, string storedHash)
        {
            var inputHash = HashPassword(inputPassword);
            return inputHash == storedHash;
        }

        // Override AddAsync to hash password before saving
        public override async Task AddAsync(User user)
        {
            // Hash the password before saving
            user.PasswordHash = HashPassword(user.PasswordHash);
            await base.AddAsync(user);
        }
    }
}
