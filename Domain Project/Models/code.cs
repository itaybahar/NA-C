using Domain_Project.DTOs;
using Domain_Project.DTOs;
using Domain_Project.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain_Project.Models
{
    public class AppUser
    {
        public required string Email { get; set; }
        public required string Username { get; set; }
        public required string Role { get; set; }
    }

    public class User
    {
        public int UserID { get; set; }

        public required string Email { get; set; } = string.Empty;
        public required string Username { get; set; } = string.Empty;
        public required string PasswordHash { get; set; } = string.Empty;

        public string? ResetToken { get; set; }
        public DateTime? LastLoginDate { get; set; }

        public required string FirstName { get; set; } = string.Empty;
        public required string LastName { get; set; } = string.Empty;

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
                PasswordHash = string.Empty, // Default value
                FirstName = string.Empty,   // Default value
                LastName = string.Empty     // Default value
            };
        }
    }

    public class UserRole
    {
        [Key]
        public int RoleID { get; set; }

        [Required]
        [MaxLength(50)]
        public required string RoleName { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }
    }

    public class Team
    {
        [Key]
        public int TeamID { get; set; }

        [Required]
        [MaxLength(100)]
        public required string TeamName { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public bool IsBlacklisted { get; set; }
    }

    public enum EquipmentStatus
    {
        Available,
        CheckedOut,
        Maintenance,
        Damaged,
        Lost
    }

    public class Equipment
    {
        [Key]
        public int Id { get; set; }
        
        [NotMapped] // This tells EF Core not to map this property to a database column
        public int EquipmentID
        {
            get { return Id; }
            set { Id = value; }
        }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? SerialNumber { get; set; }

        public DateTime? PurchaseDate { get; set; }

        public decimal? Value { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Available";

        public string? Notes { get; set; }

        public DateTime? LastUpdatedDate { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [MaxLength(100)]
        public required string StorageLocation { get; set; } = string.Empty;


        // Add JsonIgnore to break the circular reference
        //[JsonIgnore]
        [Required]
        public required List<CheckoutRecord> CheckoutRecords { get; set; } = new List<CheckoutRecord>();

        public int CategoryId { get; set; }
        [NotMapped] // Add this attribute
        public required string ModelNumber { get; set; } = string.Empty;
        //public int EquipmentID { get; set; }

        public static implicit operator Equipment(EquipmentDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new Equipment
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                SerialNumber = dto.SerialNumber,
                PurchaseDate = dto.PurchaseDate,
                Value = dto.Value,
                Status = dto.Status ?? "Available",
                Notes = dto.Notes,
                LastUpdatedDate = dto.LastUpdatedDate ?? DateTime.UtcNow,
                Quantity = dto.Quantity,
                StorageLocation = dto.StorageLocation,
                CategoryId = dto.CategoryId,
                ModelNumber = dto.ModelNumber ?? string.Empty,
                CheckoutRecords = new List<CheckoutRecord>() // Explicitly initializing the required property
            };
        }
    }

}


public class EquipmentCheckout
{
    [Key]
    public int CheckoutID { get; set; }

    // Change property name to match EF Core's naming convention
    // and add explicit column mapping
    [ForeignKey("Equipment")]
    [Column("EquipmentID")] // Explicitly specify the column name in the database
    public int EquipmentId { get; set; }

    // Add navigation property to establish relationship
    public virtual Equipment? Equipment { get; set; }

    public int TeamID { get; set; }
    public int UserID { get; set; }

    [Required]
    public DateTime CheckoutDate { get; set; } = DateTime.UtcNow;

    public DateTime? ReturnDate { get; set; }

    [Required]
    public DateTime ExpectedReturnDate { get; set; } = DateTime.UtcNow.AddDays(7);

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "CheckedOut";

    public DateTime? ActualReturnDate { get; set; }
    public string? ReturnCondition { get; set; }
    public string? Notes { get; set; }

    [Required]
    public int Quantity { get; set; } = 1;
}



public class CheckoutRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public int EquipmentId { get; set; }

    // Make navigation property nullable to avoid serialization issues
    [JsonIgnore]
    public Equipment? Equipment { get; set; }

    public int TeamId { get; set; }

    // Make navigation property nullable with a default value
    [JsonIgnore]
    public Team? Team { get; set; } = new Team { TeamName = string.Empty };

    public int UserId { get; set; }
    public string User { get; set; } = string.Empty;

    public DateTime CheckedOutAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAt { get; set; }
    public int Quantity { get; set; } = 1;
}

public class Checkout
    {
        public int Id { get; set; } // Unique identifier for the checkout

        public required int EquipmentId { get; set; } // ID of the equipment being checked out

        public required int TeamId { get; set; } // ID of the team performing the checkout
        public required Team Team { get; set; } // Navigation property for the team

        public required int UserId { get; set; } // ID of the team performing the checkout

        public DateTime CheckoutDate { get; set; } = DateTime.UtcNow; // Date of checkout
        public DateTime? ReturnDate { get; set; } // Date of return, if returned

        public bool IsReturned { get; set; } = false; // Indicates if the equipment has been returned
    public int Quantity { get; set; }
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
        public required string ReasonForBlacklisting { get; set; }

        public DateTime BlacklistDate { get; set; } = DateTime.Now;
        public DateTime? RemovalDate { get; set; }
        public int? RemovedBy { get; set; }

        public string? Notes { get; set; }
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
        public required string EquipmentName { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        [Required]
        [MaxLength(20)]
        public required string Urgency { get; set; } = "Normal";

        [Required]
        [MaxLength(200)]
        public required string Justification { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(20)]
        public required string Status { get; set; } = "Pending";

        public int? ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }

        public string? Notes { get; set; }
    }

    public class UserRoleAssignment
    {
        [Required]
        public int UserID { get; set; }

        [Required]
        public int RoleID { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserID))]
        public required User User { get; set; } = new User
        {
            Email = string.Empty,
            Username = string.Empty,
            PasswordHash = string.Empty,
            FirstName = string.Empty,
            LastName = string.Empty
        };

        [ForeignKey(nameof(RoleID))]
        public required UserRole UserRole { get; set; }
    }

    public class EquipmentCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public required string CategoryName { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "bit")]
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

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
        public required string Action { get; set; }

        [Required]
        public required string Details { get; set; }

        [StringLength(50)]
        public string? IPAddress { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime LogDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserID")]
        public required User User { get; set; } = new User
        {
            Email = string.Empty,
            Username = string.Empty,
            PasswordHash = string.Empty,
            FirstName = string.Empty,
            LastName = string.Empty
        };

        [StringLength(100)]
        public string? EntityName { get; set; }

        public int? EntityID { get; set; }

        [StringLength(20)]
        public string? LogLevel { get; set; } = "Information";

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Action)
                   && !string.IsNullOrWhiteSpace(Details)
                   && Action.Length <= 50;
        }

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
                EntityID = entityId,
                User = new User
                {
                    Email = string.Empty,
                    Username = string.Empty,
                    PasswordHash = string.Empty,
                    FirstName = string.Empty,
                    LastName = string.Empty
                }
            };
        }
    // Similarly, update the CheckoutRecord class
    public class CheckoutRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required int EquipmentId { get; set; }

        [JsonIgnore] // Add JsonIgnore here too
        public required Equipment Equipment { get; set; }

        public required int TeamId { get; set; }

        [JsonIgnore] // Add JsonIgnore to break circular reference with Team
        public required Team Team { get; set; } = new Team
        {
            TeamName = string.Empty
        };

        public DateTime CheckedOutAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReturnedAt { get; set; }
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

        [ForeignKey(nameof(TeamID))]
        public required Team Team { get; set; } = new Team
        {
            TeamName = string.Empty
        };

        [ForeignKey(nameof(UserID))]
        public required User User { get; set; } = new User
        {
            Email = string.Empty,
            Username = string.Empty,
            PasswordHash = string.Empty,
            FirstName = string.Empty,
            LastName = string.Empty
        };

        public object[] GetKeys() => new object[] { TeamID, UserID };

        public bool IsValid()
        {
            return TeamID > 0 && UserID > 0;
        }

        public static TeamMember Create(int teamId, int userId, string? assignedRole = null)
        {
            return new TeamMember
            {
                TeamID = teamId,
                UserID = userId,
                AssignedRole = assignedRole,
                Team = new Team
                {
                    TeamName = string.Empty
                },
                User = new User
                {
                    Email = string.Empty,
                    Username = string.Empty,
                    PasswordHash = string.Empty,
                    FirstName = string.Empty,
                    LastName = string.Empty
                }
            };
        }
    }

