using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain_Project.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(128)]
        public string PasswordHash { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastLoginDate { get; set; }
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
}

