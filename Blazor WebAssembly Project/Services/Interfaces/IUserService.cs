using Domain_Project.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Gets all users in the system
        /// </summary>
        Task<List<UserDto>> GetAllUsersAsync();

        /// <summary>
        /// Gets a specific user by ID
        /// </summary>
        Task<UserDto> GetUserByIdAsync(int userId);

        /// <summary>
        /// Updates a user's role
        /// </summary>
        Task UpdateUserRoleAsync(int userId, string newRole);

        /// <summary>
        /// Updates a user's active status
        /// </summary>
        Task UpdateUserStatusAsync(int userId, bool isActive);
    }
}
