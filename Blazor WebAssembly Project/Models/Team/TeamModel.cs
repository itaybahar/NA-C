using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly.Models.Team
{
    public class TeamModel
    {
        public int TeamID { get; set; }

        [Required(ErrorMessage = "Team name is required")]
        [StringLength(100)]
        public string TeamName { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}