using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly.Models.Equipment
{
    public class EquipmentModel
    {
        public int EquipmentID { get; set; }

        [Required(ErrorMessage = "Equipment name is required")]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryID { get; set; }

        [StringLength(50)]
        public string SerialNumber { get; set; }

        public DateTime? PurchaseDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Value must be a positive number")]
        public decimal? Value { get; set; }

        [Required]
        public string Status { get; set; } = "Available";
    }
}