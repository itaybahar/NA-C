using Domain_Project.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain_Project.DTOs
{
    public class UserDto
    {
        public int UserID { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Role { get; set; }
    }
    public class CheckoutRecordDto
    {
        public required string Id { get; set; }
        public required string EquipmentId { get; set; }
        public int TeamId { get; set; }
        public int UserId { get; set; }
        public string? EquipmentName { get; set; }
        public string? TeamName { get; set; }
        public string? UserName { get; set; }
        public string? UserRole { get; set; }
        public DateTime? CheckedOutAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public int Quantity { get; set; } = 1; // Default to 1 if not specified
        public string ItemCondition { get; set; } = "Good";
        public string ItemNotes { get; set; } = string.Empty;
        public int AvailableAfterOperation { get; set; }

    }

    public class UserLoginDto
    {
        [Required]
        public required string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }

    public class TeamDto
    {
        public int TeamID { get; set; }
        public required string TeamName { get; set; }
        public required string Description { get; set; }
        public bool IsActive { get; set; }
    }

            public class EquipmentDto
            {
                public int Id { get; set; }

                public string Name { get; set; } = string.Empty;

                public string? Description { get; set; }

                public string? SerialNumber { get; set; }

                public DateTime? PurchaseDate { get; set; }

                public decimal? Value { get; set; }

                public string Status { get; set; } = "Available";

                public string? Notes { get; set; }

                public int Quantity { get; set; }

                public string StorageLocation { get; set; } = string.Empty;
                public DateTime? LastUpdatedDate { get; internal set; }
                public int CategoryId { get; internal set; }
                public string? ModelNumber { get; internal set; }
        public int? CheckoutRecordsCount { get; set; }
    }

            public class EquipmentCheckoutDto
            {
                public int CheckoutID { get; set; }
                public int EquipmentID { get; set; }
                public int TeamID { get; set; }
                public DateTime? CheckoutDate { get; set; }
                public DateTime? ExpectedReturnDate { get; set; }
                public required string Status { get; set; }

            }

            public class EquipmentRequestDto
            {
                public int RequestID { get; set; }
                public required string EquipmentName { get; set; }
                public int Quantity { get; set; }
                public required string Urgency { get; set; }
                public required string Justification { get; set; }
                public required string Status { get; set; }
            }

            public class BlacklistDto
            {
                public int BlacklistID { get; set; }
                public int TeamID { get; set; }
                public required string ReasonForBlacklisting { get; set; }
                public DateTime BlacklistDate { get; set; }
            }

            public class AuthenticationResponseDto
            {
                public required string Token { get; set; }
                public required UserDto User { get; set; }
                public bool NeedsProfile { get; set; }
                public string Email { get; set; } = string.Empty;
            }

            public class UserRegistrationDto
            {
                public required string Username { get; set; }
                public required string Password { get; set; }
                public required string Email { get; set; }
                public required string FirstName { get; set; }
                public required string LastName { get; set; }
            }

            public class BlacklistCreateDto
            {
                [Required]
                public int TeamID { get; set; }

                [Required]
                public int BlacklistedBy { get; set; }

                [Required]
                [StringLength(200, MinimumLength = 10)]
                public required string ReasonForBlacklisting { get; set; }

                [MaxLength(500)]
                public string? Notes { get; set; }

                public DateTime BlacklistDate { get; set; } = DateTime.UtcNow;

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
                public required string TeamName { get; set; }

                [StringLength(100)]
                public required string Username { get; set; }

                [StringLength(50)]
                public required string AssignedRole { get; set; }

                public DateTime? JoinDate { get; set; }

                public bool IsActive { get; set; } = true;

                public required string FirstName { get; set; }
                public required string LastName { get; set; }
                public required string Email { get; set; }

                public bool IsValid()
                {
                    return TeamID > 0 &&
                           UserID > 0 &&
                           !string.IsNullOrWhiteSpace(Username);
                }

                public static TeamMemberDto FromModel(TeamMember member)
                {
                    return new TeamMemberDto
                    {
                        TeamID = member.TeamID,
                        UserID = member.UserID,
                        AssignedRole = member.AssignedRole ?? string.Empty, // Fixed null reference  
                        JoinDate = member.JoinDate,
                        IsActive = member.IsActive,
                        TeamName = member.Team?.TeamName ?? string.Empty, // Fixed property name to match 'TeamName'  
                        Username = member.User?.Username ?? string.Empty, // Ensure Username is set  
                        FirstName = member.User?.FirstName ?? string.Empty, // Ensure FirstName is set  
                        LastName = member.User?.LastName ?? string.Empty, // Ensure LastName is set  
                        Email = member.User?.Email ?? string.Empty // Ensure Email is set  
                    };
                }
            }

            public class RegisterDto
            {
                public required string Username { get; set; }
                public required string Email { get; set; }
                public required string Password { get; set; }
                public required string Role { get; set; }
            }

            public class TeamMemberCreateDto
            {
                [Required]
                public int TeamID { get; set; }

                [Required]
                public int UserID { get; set; }

                [StringLength(50)]
                public string? AssignedRole { get; set; }

                public bool IsValid()
                {
                    return TeamID > 0 && UserID > 0;
                }
            }
 
            public class AssignRoleDto
            {
                public string Role { get; set; } = string.Empty;
            }
        }
