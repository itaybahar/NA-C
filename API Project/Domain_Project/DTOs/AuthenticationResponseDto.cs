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
        /// Creates a new instance of the authentication response DTO.
        /// </summary>
        public AuthenticationResponseDto() { }

        /// <summary>
        /// Creates a new instance of the authentication response DTO with the specified token and user.
        /// </summary>
        /// <param name="token">The authentication token</param>
        /// <param name="user">The user details</param>
        public AuthenticationResponseDto(string token, UserDto user)
        {
            Token = token;
            User = user;
        }

        /// <summary>
        /// Converts an API_Project.Services.AuthenticationResponseDto to a Domain_Project.DTOs.AuthenticationResponseDto.
        /// Used for cross-layer translation between API and Domain layers.
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
        /// Creates an authentication response DTO with no token (for failed authentication).
        /// </summary>
        /// <returns>A new authentication response DTO with null values</returns>
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
