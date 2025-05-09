using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Domain_Project.Models;
using Domain_Project.DTOs;

namespace API_Project.Services
{
    // Define the EquipmentCheckoutModel locally since it's not available in this context
    public class EquipmentCheckoutModel
    {
        public int CheckoutID { get; set; }

        [Required(ErrorMessage = "Equipment is required")]
        public int EquipmentID { get; set; }

        [Required(ErrorMessage = "Team is required")]
        public int TeamID { get; set; }
        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; }

        public DateTime? CheckoutDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }

        public string Status { get; set; } = "CheckedOut";
        public string? Notes { get; set; }
    }
}
