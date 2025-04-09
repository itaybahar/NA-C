using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain_Project.Models
{
    public class AppUser
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
    }
    public class User
    {
        public int UserID { get; set; }

        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string? ResetToken { get; set; }
        public DateTime? LastLoginDate { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; }

        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpiration { get; set; }

        public string? Role { get; set; }

        public static implicit operator User(AppUser v)
        {
            if (v is null)
            {
                throw new ArgumentNullException(nameof(v));
            }

            return new User
            {
                Email = v.Email,
                Username = v.Username,
                Role = v.Role,
                // ניתן להשלים את שאר המיפויים בהתאם לצורך
            };
        }
    }


    public class UserRole
    {
        [Key]
        public int RoleID { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }
    }

    public class Team
    {
        [Key]
        public int TeamID { get; set; }

        [Required]
        [MaxLength(100)]
        public string TeamName { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class Equipment
    {
        [Key]
        public int EquipmentID { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }

        [MaxLength(50)]
        public string SerialNumber { get; set; }

        public DateTime? PurchaseDate { get; set; }
        public decimal? Value { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Available";

        public string Notes { get; set; }

        [Required]
        public int LastUpdatedBy { get; set; }
        public DateTime LastUpdatedDate { get; set; } = DateTime.Now;
    }

    public class EquipmentCheckout
    {
        [Key]
        public int CheckoutID { get; set; }

        [Required]
        public int EquipmentID { get; set; }

        [Required]
        public int TeamID { get; set; }

        [Required]
        public int CheckedOutBy { get; set; }

        [Required]
        public int IssuedBy { get; set; }

        public DateTime CheckoutDate { get; set; } = DateTime.Now;
        public DateTime ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }

        public int? ReturnApprovedBy { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "CheckedOut";

        public string Notes { get; set; }
    }

    public class Blacklist
    {
        [Key]
        public int BlacklistID { get; set; }

        [Required]
        public int TeamID { get; set; }

        [Required]
        public int BlacklistedBy { get; set; }

        [Required]
        [MaxLength(200)]
        public string ReasonForBlacklisting { get; set; }

        public DateTime BlacklistDate { get; set; } = DateTime.Now;
        public DateTime? RemovalDate { get; set; }
        public int? RemovedBy { get; set; }

        public string Notes { get; set; }
    }

    public class EquipmentRequest
    {
        [Key]
        public int RequestID { get; set; }

        [Required]
        public int RequestedBy { get; set; }

        public int? CategoryID { get; set; }

        [Required]
        [MaxLength(100)]
        public string EquipmentName { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        [Required]
        [MaxLength(20)]
        public string Urgency { get; set; } = "Normal";

        [Required]
        [MaxLength(200)]
        public string Justification { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public int? ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }

        public string Notes { get; set; }
    }

    public class UserRoleAssignment
    {
        [Required]
        public int UserID { get; set; }

        [Required]
        public int RoleID { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        // Optional navigation properties
        [ForeignKey(nameof(UserID))]
        public User User { get; set; }

        [ForeignKey(nameof(RoleID))]
        public UserRole UserRole { get; set; }
    }

    public class EquipmentCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string CategoryName { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "bit")]
        public bool IsActive { get; set; } = true;

        // Optional: Navigation property for related equipment
        public virtual ICollection<Equipment> Equipment { get; set; }

        // Validation method
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(CategoryName)
                   && CategoryName.Length >= 2
                   && CategoryName.Length <= 100;
        }
    }

    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; }

        [Required]
        public string Details { get; set; }

        [StringLength(50)]
        public string IPAddress { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime LogDate { get; set; } = DateTime.UtcNow;

        // Optional: Navigation property to User
        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        // Additional properties for more detailed logging
        [StringLength(100)]
        public string EntityName { get; set; }

        public int? EntityID { get; set; }

        [StringLength(20)]
        public string LogLevel { get; set; } = "Information";

        // Method to validate log entry
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Action)
                   && !string.IsNullOrWhiteSpace(Details)
                   && Action.Length <= 50;
        }

        // Method to create a log entry
        public static AuditLog Create(
            int userId,
            string action,
            string details,
            string? ipAddress = null,
            string? entityName = null,
            int? entityId = null)
        {
            return new AuditLog
            {
                UserID = userId,
                Action = action,
                Details = details,
                IPAddress = ipAddress,
                EntityName = entityName,
                EntityID = entityId
            };
        }
    }

    public class TeamMember
    {
        [Required]
        public int TeamID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? AssignedRole { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey(nameof(TeamID))]
        public virtual Team Team { get; set; }

        [ForeignKey(nameof(UserID))]
        public virtual User User { get; set; }

        // Composite key
        public object[] GetKeys() => new object[] { TeamID, UserID };

        // Validation method
        public bool IsValid()
        {
            return TeamID > 0 && UserID > 0;
        }

        // Method to create a team member assignment
        public static TeamMember Create(int teamId, int userId, string? assignedRole = null)
        {
            return new TeamMember
            {
                TeamID = teamId,
                UserID = userId,
                AssignedRole = assignedRole
            };
        }
    }
}