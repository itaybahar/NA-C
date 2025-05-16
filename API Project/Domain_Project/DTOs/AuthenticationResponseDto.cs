using Domain_Project.DTOs;

namespace Domain_Project.DTOs
{
    /// <summary>
    /// Represents the authentication response data transferred to clients.
    /// Contains the authentication token and user information.
    /// </summary>
    public class AuthenticationResponseDto
    {
        /// <summary>
        /// The JWT authentication token for the authenticated user.
        /// Used for subsequent authenticated requests to the API.
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// User details of the authenticated user.
        /// </summary>
        public UserDto? User { get; set; }

        /// <summary>
        /// Creates a Domain layer authentication response DTO from an API layer DTO.
        /// </summary>
        /// <param name="apiDto">The API layer authentication response DTO</param>
        /// <returns>A new Domain layer authentication response DTO</returns>
        /// <exception cref="ArgumentNullException">Thrown when apiDto is null</exception>
        public static AuthenticationResponseDto FromApiDto(API_Project.Services.AuthenticationResponseDto apiDto)
        {
            if (apiDto == null)
                throw new ArgumentNullException(nameof(apiDto));

            return new AuthenticationResponseDto
            {
                Token = apiDto.Token,
                User = apiDto.User
            };
        }

        /// <summary>
        /// Creates a failed authentication response with no token or user information.
        /// </summary>
        /// <returns>An authentication response representing a failed authentication attempt</returns>
        public static AuthenticationResponseDto CreateFailedResponse()
        {
            return new AuthenticationResponseDto
            {
                Token = null,
                User = null
            };
        }
    }
}
