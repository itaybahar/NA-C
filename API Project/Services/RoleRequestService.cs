using API_Project.Repositories;
using Domain_Project.Interfaces;

public class RoleRequestService : IRoleRequestService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleChangeRequestRepository _requestRepository;

    public RoleRequestService(IUserRepository userRepository, IRoleChangeRequestRepository requestRepository)
    {
        _userRepository = userRepository;
        _requestRepository = requestRepository;
    }

    public async Task<RoleChangeRequest> CreateRoleChangeRequestAsync(int userId, string requestedRole, string reason)
    {
        // Validate requested role
        if (!IsValidRole(requestedRole))
            throw new ArgumentException($"Invalid role: {requestedRole}. Valid roles are: WarehouseOperator, WarehouseManager, Admin");

        // Get the user
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new ArgumentException($"User with ID {userId} not found");

        // Check if user already has the requested role
        if (user.Role == requestedRole)
            throw new InvalidOperationException($"User already has the {requestedRole} role");

        // Check if there's already a pending request for this user and role
        var existingRequest = await _requestRepository.FindAsync(r =>
            r.UserID == userId &&
            r.RequestedRole == requestedRole &&
            r.Status == RequestStatus.Pending);

        if (existingRequest.Any())
            throw new InvalidOperationException("A pending request for this role already exists");

        // Create the request
        var request = new RoleChangeRequest
        {
            UserID = userId,
            CurrentRole = user.Role ?? "User",
            RequestedRole = requestedRole,
            Reason = reason,
            RequestDate = DateTime.UtcNow,
            Status = RequestStatus.Pending
        };

        await _requestRepository.AddAsync(request);
        await _requestRepository.SaveChangesAsync(request);

        return request;
    }

    public async Task<IEnumerable<RoleChangeRequest>> GetPendingRequestsAsync()
    {
        return await _requestRepository.FindAsync(r => r.Status == RequestStatus.Pending);
    }

    public async Task<IEnumerable<RoleChangeRequest>> GetUserRequestsAsync(int userId)
    {
        return await _requestRepository.FindAsync(r => r.UserID == userId);
    }

    public async Task<bool> ApproveRequestAsync(int requestId, int adminUserId, string? notes = null)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null || request.Status != RequestStatus.Pending)
            return false;

        // Get the user
        var user = await _userRepository.GetByIdAsync(request.UserID);
        if (user == null)
            return false;

        // Update user role
        user.Role = request.RequestedRole;
        await _userRepository.UpdateAsync(user);

        // Update the request
        request.Status = RequestStatus.Approved;
        request.ProcessedDate = DateTime.UtcNow;
        request.ProcessedByUserID = adminUserId;
        request.AdminNotes = notes;

        await _requestRepository.UpdateAsync(request);
        await _userRepository.SaveChangesAsync();
        await _requestRepository.SaveChangesAsync(request);

        return true;
    }

    public async Task<bool> RejectRequestAsync(int requestId, int adminUserId, string? notes = null)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null || request.Status != RequestStatus.Pending)
            return false;

        // Update the request
        request.Status = RequestStatus.Rejected;
        request.ProcessedDate = DateTime.UtcNow;
        request.ProcessedByUserID = adminUserId;
        request.AdminNotes = notes;

        await _requestRepository.UpdateAsync(request);
        await _requestRepository.SaveChangesAsync(request);

        return true;
    }

    private bool IsValidRole(string role)
    {
        return role is "WarehouseOperator" or "WarehouseManager" or "Admin";
    }
}
