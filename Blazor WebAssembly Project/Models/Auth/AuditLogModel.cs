using System;
using System.ComponentModel.DataAnnotations;

namespace Blazor_WebAssembly.Models.Auth
{
    public class AuditLogModel
    {
        public int LogID { get; set; }

        [Required(ErrorMessage = "User ID is required")]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50)]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Action is required")]
        [StringLength(100)]
        public required string Action { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }

        [StringLength(50)]
        public string? IPAddress { get; set; }

        public DateTime LogDate { get; set; } = DateTime.UtcNow;

        [StringLength(20)]
        public string LogLevel { get; set; } = "Information";

        [StringLength(100)]
        public string? EntityName { get; set; }

        public int? EntityID { get; set; }

        public static AuditLogModel Create(
            int userId,
            string username,
            string action,
            string? details = null,
            string? ipAddress = null,
            string? entityName = null,
            int? entityId = null,
            string logLevel = "Information")
        {
            return new AuditLogModel
            {
                UserID = userId,
                Username = username,
                Action = action,
                Details = details,
                IPAddress = ipAddress,
                EntityName = entityName,
                EntityID = entityId,
                LogLevel = logLevel
            };
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Action)
                   && UserID > 0
                   && Action.Length <= 100;
        }

        public override string ToString()
        {
            return $"Log {LogID}: {Username} - {Action} at {LogDate}";
        }
    }
}