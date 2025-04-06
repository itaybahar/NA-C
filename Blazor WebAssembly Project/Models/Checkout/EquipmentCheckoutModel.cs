using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly.Models.Checkout
{
    public class EquipmentCheckoutModel
    {
        public int CheckoutID { get; set; }

        [Required(ErrorMessage = "Equipment is required")]
        public int EquipmentID { get; set; }

        [Required(ErrorMessage = "Team is required")]
        public int TeamID { get; set; }

        public int CheckedOutBy { get; set; }
        public int IssuedBy { get; set; }

        public DateTime CheckoutDate { get; set; } = DateTime.UtcNow;
        public DateTime ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }

        public string Status { get; set; } = "CheckedOut";
    }
}