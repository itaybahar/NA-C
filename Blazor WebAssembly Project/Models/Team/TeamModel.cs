using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Blazor_WebAssembly.Models.Team
{
    public class TeamModel
    {
        [JsonPropertyName("teamID")]
        public int TeamID { get; set; }

        [Required(ErrorMessage = "Team name is required")]
        [StringLength(100)]

        [JsonPropertyName("teamName")]
        public required string TeamName { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsBlacklisted { get; set; } = false;
    }
}
