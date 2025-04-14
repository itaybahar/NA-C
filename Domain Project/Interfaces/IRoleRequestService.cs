using Domain_Project.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain_Project.Interfaces
{
    public interface IRoleRequestService
    {
        /// <summary>
        /// Creates a new role change request for a user
        /// </summary>
        /// <param name="userId">The ID of the user requesting the role change</param>
        /// <param name="requestedRole">The role being requested</param>
        /// <param name="reason">The reason for the role change request</param>
        /// <returns>The created role change request</returns>
        Task<RoleChangeRequest> CreateRoleChangeRequestAsync(int userId, string requestedRole, string reason);

        /// <summary>
        /// Gets all role change requests for a specific user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>Collection of role change requests for the user</returns>
        Task<IEnumerable<RoleChangeRequest>> GetUserRequestsAsync(int userId);

        /// <summary>
        /// Gets all pending role change requests
        /// </summary>
        /// <returns>Collection of pending role change requests</returns>
        Task<IEnumerable<RoleChangeRequest>> GetPendingRequestsAsync();

        /// <summary>
        /// Approves a role change request
        /// </summary>
        /// <param name="requestId">ID of the request to approve</param>
        /// <param name="adminId">ID of the admin approving the request</param>
        /// <param name="notes">Optional notes from the admin</param>
        /// <returns>True if approved successfully, otherwise false</returns>
        Task<bool> ApproveRequestAsync(int requestId, int adminId, string? notes);

        /// <summary>
        /// Rejects a role change request
        /// </summary>
        /// <param name="requestId">ID of the request to reject</param>
        /// <param name="adminId">ID of the admin rejecting the request</param>
        /// <param name="notes">Optional notes from the admin</param>
        /// <returns>True if rejected successfully, otherwise false</returns>
        Task<bool> RejectRequestAsync(int requestId, int adminId, string? notes);
    }
}
