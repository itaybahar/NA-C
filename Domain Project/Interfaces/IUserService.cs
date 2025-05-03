using Domain_Project.DTOs;
using Domain_Project.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain_Project.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Gets a user by their email address
        /// </summary>
        Task<User> GetUserByEmailAsync(string email);

        /// <summary>
        /// Gets a user by their username
        /// </summary>
        Task<User> GetUserByUsernameAsync(string username);

        /// <summary>
        /// Adds a new user to the system
        /// </summary>
        Task AddUserAsync(User user);

        /// <summary>
        /// Updates user information
        /// </summary>
        Task UpdateUserAsync(User user);

        /// <summary>
        /// Updates a user's role and returns whether the operation was successful
        /// </summary>
        Task<bool> UpdateUserRoleAsync(int userId, string role);

        /// <summary>
        /// Gets all users in the system
        /// </summary>
        Task<List<UserDto>> GetAllUsersAsync();

        /// <summary>
        /// Updates a user's active status
        /// </summary>
        Task<bool> UpdateUserStatusAsync(int userId, bool isActive);

        /// <summary>
        /// Gets a user by their ID
        /// </summary>
        Task<User> GetUserByIdAsync(int userId);
    }
}
