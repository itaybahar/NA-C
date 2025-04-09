using System.Threading.Tasks;
using System.Collections.Generic;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IRoleService
    {
        /// <summary>
        /// Requests a role change for the current user
        /// </summary>
        /// <param name="requestedRole">The role being requested</param>
        /// <param name="reason">Justification for the role change</param>
        /// <returns>True if the request was successfully submitted</returns>
        Task<bool> RequestRoleChangeAsync(string requestedRole, string reason);

        /// <summary>
        /// Gets all role change requests for the current user
        /// </summary>
        /// <returns>Collection of role request history items</returns>
        Task<IEnumerable<RoleRequestHistoryItem>> GetUserRequestHistoryAsync();

        /// <summary>
        /// Gets all pending role change requests (for admin/manager use)
        /// </summary>
        /// <returns>Collection of pending role request history items</returns>
        Task<IEnumerable<RoleRequestHistoryItem>> GetPendingRequestsAsync();

        /// <summary>
        /// Approves a role change request
        /// </summary>
        /// <param name="requestId">ID of the request to approve</param>
        /// <param name="notes">Optional notes about the approval</param>
        /// <returns>True if the request was successfully approved</returns>
        Task<bool> ApproveRequestAsync(int requestId, string notes);

        /// <summary>
        /// Rejects a role change request
        /// </summary>
        /// <param name="requestId">ID of the request to reject</param>
        /// <param name="notes">Notes explaining the rejection (recommended)</param>
        /// <returns>True if the request was successfully rejected</returns>
        Task<bool> RejectRequestAsync(int requestId, string notes);
    }

    public class RoleRequestHistoryItem
    {
        public int RequestID { get; set; }
        public int UserID { get; set; }
        public string CurrentRole { get; set; } = string.Empty;
        public string RequestedRole { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AdminNotes { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string? Username { get; set; }
    }
}
