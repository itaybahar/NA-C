using Domain_Project.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Domain_Project.DTOs
{
    public class UserDto
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
    }

    public class UserLoginDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class TeamDto
    {
        public int TeamID { get; set; }
        public string TeamName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class EquipmentDto
    {
        public int EquipmentID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SerialNumber { get; set; }
        public string Status { get; set; }
        public decimal? Value { get; set; }
    }

    public class EquipmentCheckoutDto
    {
        public int CheckoutID { get; set; }
        public int EquipmentID { get; set; }
        public int TeamID { get; set; }
        public DateTime CheckoutDate { get; set; }
        public DateTime ExpectedReturnDate { get; set; }
        public string Status { get; set; }
    }

    public class EquipmentRequestDto
    {
        public int RequestID { get; set; }
        public string EquipmentName { get; set; }
        public int Quantity { get; set; }
        public string Urgency { get; set; }
        public string Justification { get; set; }
        public string Status { get; set; }
    }

    public class BlacklistDto
    {
        public int BlacklistID { get; set; }
        public int TeamID { get; set; }
        public string ReasonForBlacklisting { get; set; }
        public DateTime BlacklistDate { get; set; }
    }
    public class AuthenticationResponseDto
    {
        public string Token { get; set; }
        public UserDto User { get; set; }
    }

    public class UserRegistrationDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
    public class BlacklistCreateDto
    {
        [Required]
        public int TeamID { get; set; }

        [Required]
        public int BlacklistedBy { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 10)]
        public string ReasonForBlacklisting { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Optional additional properties for more context
        public DateTime BlacklistDate { get; set; } = DateTime.UtcNow;

        // Validation to ensure reason is meaningful
        public bool IsValidReason()
        {
            return !string.IsNullOrWhiteSpace(ReasonForBlacklisting)
                   && ReasonForBlacklisting.Length >= 10;
        }
    }
    public class TeamMemberDto
    {
        [Required]
        public int TeamID { get; set; }

        [Required]
        public int UserID { get; set; }

        [StringLength(100)]
        public string TeamName { get; set; }

        [StringLength(100)]
        public string Username { get; set; }

        [StringLength(50)]
        public string AssignedRole { get; set; }

        public DateTime JoinDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Additional user details
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        // Validation method
        public bool IsValid()
        {
            return TeamID > 0 &&
                   UserID > 0 &&
                   !string.IsNullOrWhiteSpace(Username);
        }

        // Method to create a DTO from a domain model
        public static TeamMemberDto FromModel(TeamMember member)
        {
            return new TeamMemberDto
            {
                TeamID = member.TeamID,
                UserID = member.UserID,
                AssignedRole = member.AssignedRole,
                JoinDate = member.JoinDate,
                IsActive = member.IsActive
            };
        }
        public class RegisterDto
        {
            public string Username { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
        }

    }

    // DTO for creating a new team member
    public class TeamMemberCreateDto
    {
        [Required]
        public int TeamID { get; set; }

        [Required]
        public int UserID { get; set; }

        [StringLength(50)]
        public string? AssignedRole { get; set; }

        // Validation method
        public bool IsValid()
        {
            return TeamID > 0 && UserID > 0;
        }
    }
}
