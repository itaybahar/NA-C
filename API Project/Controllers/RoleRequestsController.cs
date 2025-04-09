using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

[ApiController]
[Route("api/role-requests")]
[Authorize] // All users need to be authenticated
public class RoleRequestsController : ControllerBase
{
    private readonly IRoleRequestService _roleRequestService;

    public RoleRequestsController(IRoleRequestService roleRequestService)
    {
        _roleRequestService = roleRequestService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoleChangeRequest([FromBody] RoleChangeRequestDto requestDto)
    {
        // Get the current user's ID from the claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized("Invalid user ID");

        try
        {
            var request = await _roleRequestService.CreateRoleChangeRequestAsync(
                userId,
                requestDto.RequestedRole,
                requestDto.Reason
            );

            return Ok(new
            {
                message = "Role change request submitted successfully",
                requestId = request.RequestID
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpGet("my-requests")]
    public async Task<IActionResult> GetMyRoleRequests()
    {
        // Get the current user's ID from the claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized("Invalid user ID");

        var requests = await _roleRequestService.GetUserRequestsAsync(userId);
        return Ok(requests);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin,WarehouseManager")] // Only admins and warehouse managers can see pending requests
    public async Task<IActionResult> GetPendingRequests()
    {
        var requests = await _roleRequestService.GetPendingRequestsAsync();
        return Ok(requests);
    }

    [HttpPost("{requestId}/approve")]
    [Authorize(Roles = "Admin")] // Only admins can approve requests
    public async Task<IActionResult> ApproveRequest(int requestId, [FromBody] ProcessRequestDto processDto)
    {
        // Get the current admin's ID from the claims
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(adminIdClaim, out int adminId))
            return Unauthorized("Invalid admin ID");

        var result = await _roleRequestService.ApproveRequestAsync(requestId, adminId, processDto.Notes);
        if (result)
            return Ok(new { message = "Role change request approved successfully" });

        return NotFound("Request not found or already processed");
    }

    [HttpPost("{requestId}/reject")]
    [Authorize(Roles = "Admin")] // Only admins can reject requests
    public async Task<IActionResult> RejectRequest(int requestId, [FromBody] ProcessRequestDto processDto)
    {
        // Get the current admin's ID from the claims
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(adminIdClaim, out int adminId))
            return Unauthorized("Invalid admin ID");

        var result = await _roleRequestService.RejectRequestAsync(requestId, adminId, processDto.Notes);
        if (result)
            return Ok(new { message = "Role change request rejected successfully" });

        return NotFound("Request not found or already processed");
    }
}

public class RoleChangeRequestDto
{
    [Required]
    public string RequestedRole { get; set; } = string.Empty;

    [Required]
    [MinLength(10, ErrorMessage = "Please provide a detailed reason for your request")]
    public string Reason { get; set; } = string.Empty;
}

public class ProcessRequestDto
{
    public string? Notes { get; set; }
}
