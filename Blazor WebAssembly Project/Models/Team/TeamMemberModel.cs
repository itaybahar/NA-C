using System.ComponentModel.DataAnnotations;


namespace Blazor_WebAssembly.Models.Team
{
    public class TeamMemberModel
    {
        public int TeamID { get; set; }
        public int UserID { get; set; }

        [StringLength(50)]
        public string? AssignedRole { get; set; }

        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}