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
}
