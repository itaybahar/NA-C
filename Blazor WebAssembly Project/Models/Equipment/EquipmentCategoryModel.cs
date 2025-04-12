using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly.Models.Equipment
{
    public class EquipmentCategoryModel
    {
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100)]
        public required string CategoryName { get; set; }

        [StringLength(200)]
        public required string Description { get; set; }
    }
}