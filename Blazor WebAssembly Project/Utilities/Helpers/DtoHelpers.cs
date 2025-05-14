using Domain_Project.DTOs;
using System.Collections.Generic;

namespace Blazor_WebAssembly.Helpers
{
    public static class DtoHelpers
    {
        /// <summary>
        /// Ensures all required properties of UserDto have non-null values
        /// </summary>
        public static List<UserDto> EnsureValidUserDtos(List<UserDto> users)
        {
            foreach (var user in users)
            {
                // Ensure Role is never null
                user.Role = user.Role ?? "User";

                // Ensure other required properties are valid too
                user.Username = user.Username ?? string.Empty;
                user.Email = user.Email ?? string.Empty;
                user.FirstName = user.FirstName ?? string.Empty;
                user.LastName = user.LastName ?? string.Empty;
            }

            return users;
        }
    }
}
