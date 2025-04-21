using Domain_Project.DTOs;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Fetches all users from the server.
        /// </summary>
        /// <returns>A list of UserDto objects.</returns>
        Task<List<UserDto>> GetUsersAsync();

        /// <summary>
        /// Updates the role of a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user to update.</param>
        /// <param name="newRole">The new role to assign to the user.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateUserRoleAsync(int userId, string newRole);
    }
}
