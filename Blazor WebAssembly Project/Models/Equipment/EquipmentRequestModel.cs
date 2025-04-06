using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly.Models.Equipment
{
    public class EquipmentRequestModel
    {
        public int RequestID { get; set; }

        [Required(ErrorMessage = "Equipment name is required")]
        [StringLength(100)]
        public string EquipmentName { get; set; }

        public int? CategoryID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        [Required]
        public string Urgency { get; set; } = "Normal";

        [Required(ErrorMessage = "Justification is required")]
        [StringLength(200)]
        public string Justification { get; set; }

        public string Status { get; set; } = "Pending";
    }
}