using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly.Models.Checkout
{
    public class EquipmentCheckoutModel
    {
        private DateTime _checkoutDate = DateTime.UtcNow;

        public int CheckoutID { get; set; }

        [Required(ErrorMessage = "Equipment is required")]
        public int EquipmentID { get; set; }

        [Required(ErrorMessage = "Team is required")]
        public int TeamID { get; set; }
        [Required(ErrorMessage = "user id is required")]
        public int UserId { get; set; }

        public DateTime CheckoutDate
        {
            get => _checkoutDate;
            set
            {
                _checkoutDate = value;
                // When CheckoutDate changes, update ExpectedReturnDate accordingly
                ExpectedReturnDate = value.AddDays(7);
            }
        }

        public DateTime ExpectedReturnDate { get; set; } = DateTime.UtcNow.AddDays(7);

        public DateTime? ActualReturnDate { get; set; }

        public string Status { get; set; } = "CheckedOut";
        public EquipmentCheckoutModel()
        {
            // Set the default ExpectedReturnDate to one week after CheckoutDate
            ExpectedReturnDate = CheckoutDate.AddDays(7);
        }
    }
}