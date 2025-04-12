using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly.Models.Equipment
{
    public class EquipmentRequestModel
    {
        public int RequestID { get; set; }

        [Required(ErrorMessage = "Equipment name is required")]
        [StringLength(100)]
        public required string EquipmentName { get; set; }

        public int? CategoryID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required]
        public required string Urgency { get; set; }

        [Required(ErrorMessage = "Justification is required")]
        [StringLength(200)]
        public required string Justification { get; set; }

        public required string Status { get; set; }
    }
}
