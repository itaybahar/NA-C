// Add to Domain_Project.Interfaces
public interface IRoleRequestService
{
    Task<RoleChangeRequest> CreateRoleChangeRequestAsync(int userId, string requestedRole, string reason);
    Task<IEnumerable<RoleChangeRequest>> GetPendingRequestsAsync();
    Task<IEnumerable<RoleChangeRequest>> GetUserRequestsAsync(int userId);
    Task<bool> ApproveRequestAsync(int requestId, int adminUserId, string? notes = null);
    Task<bool> RejectRequestAsync(int requestId, int adminUserId, string? notes = null);
}
