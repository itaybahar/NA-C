using Microsoft.EntityFrameworkCore;
using API_Project.Data;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace API_Project.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(EquipmentManagementContext context) : base(context)
        {
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByResetTokenAsync(string token)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.ResetToken == token);
        }

        public async Task<IEnumerable<UserRole>> GetUserRolesAsync(int userId)
        {
            return await _context.UserRoles
                .Where(ur => _context.UserRoleAssignments
                    .Any(ura => ura.UserID == userId && ura.RoleID == ur.RoleID))
                .ToListAsync();
        }

        public async Task<bool> ValidateUserCredentialsAsync(string username, string password)
        {
            var user = await GetUserByUsernameAsync(username);
            if (user == null)
                return false;

            var inputHash = HashPassword(password);

            // DEBUG
            Console.WriteLine($"[ValidateUser] username: {username}");
            Console.WriteLine($"[ValidateUser] inputHash: {inputHash}");
            Console.WriteLine($"[ValidateUser] storedHash: {user.PasswordHash}");

            return inputHash == user.PasswordHash;
        }

        public override async Task<User> AddAsync(User user)
        {
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                user.PasswordHash = HashPassword(user.PasswordHash);
            }

            return await base.AddAsync(user);
        }

        public async Task<Domain_Project.Models.AppUser?> GetByEmailAsync(string? email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            var user = await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return null;

            return new Domain_Project.Models.AppUser
            {
                Email = user.Email,
                Username = user.Username,
                Role = user.Role ?? "User"
            };
        }

        public async Task CreateAsync(Domain_Project.Models.AppUser appUser)
        {
            if (appUser == null)
                throw new ArgumentNullException(nameof(appUser));

            var user = new User
            {
                Email = appUser.Email,
                Username = appUser.Username,
                FirstName = "",
                LastName = "",
                Role = appUser.Role ?? "User",
                PasswordHash = HashPassword("Temp123!") // סיסמה זמנית
            };

            await AddAsync(user);
            await SaveChangesAsync(user);
        }

        public async Task SaveChangesAsync(User user)
        {
            _context.Update(user);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        // Utility Methods

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
