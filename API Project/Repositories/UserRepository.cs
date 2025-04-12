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

        // Updated to match interface signature (non-nullable return type)
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            // Log the query being executed
            var query = _context.Users
                .Where(u => u.Username == username)
                .ToQueryString();
            Console.WriteLine($"Generated SQL Query for GetUserByUsernameAsync: {query}");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user; // Simply return null if the user doesn't exist
        }


        // Updated to match interface signature (non-nullable return type)
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            // Normalize email to lowercase
            var normalizedEmail = email.ToLower();

            // Log the query being executed
            var query = _context.Users
                .Where(u => u.Email.ToLower() == normalizedEmail)
                .ToQueryString();
            Console.WriteLine($"Generated SQL Query for GetUserByEmailAsync: {query}");

            var user = await _context.Users
                .AsNoTracking() // Optional: Avoid tracking for read-only queries
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            return user; // Return null if no user found
        }

        public async Task<bool> ValidateUserCredentialsAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;

            using var sha256 = SHA256.Create();
            var hashedPassword = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return user.PasswordHash == hashedPassword;
        }

        public async Task<IEnumerable<UserRole>> GetUserRolesAsync(int userId)
        {
            try
            {
                // First attempt: Using raw SQL to avoid navigation property issues
                var sql = @"
                    SELECT ur.* 
                    FROM UserRoles ur
                    JOIN UserRoleAssignments ura ON ur.RoleID = ura.RoleID
                    WHERE ura.UserID = {0}";

                return await _context.UserRoles
                    .FromSqlRaw(sql, userId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error retrieving user roles via SQL: {ex.Message}");

                try
                {
                    // Second attempt: Try using explicit join syntax
                    return await _context.UserRoleAssignments
                        .Where(ura => ura.UserID == userId)
                        .Join(
                            _context.UserRoles,
                            ura => ura.RoleID,
                            ur => ur.RoleID,
                            (ura, ur) => ur)
                        .ToListAsync();
                }
                catch (Exception joinEx)
                {
                    // Log the second exception
                    Console.WriteLine($"Error retrieving user roles via join: {joinEx.Message}");

                    // Return empty list as fallback
                    return Enumerable.Empty<UserRole>();
                }
            }
        }

        public async Task<User> GetUserByResetTokenAsync(string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == token);
            return user ?? throw new KeyNotFoundException($"User with reset token '{token}' not found.");
        }

        // Fixing SaveChangesAsync method
        public new async Task SaveChangesAsync(User user)
        {
            _context.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
