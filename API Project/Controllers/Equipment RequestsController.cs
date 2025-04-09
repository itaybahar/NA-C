using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain_Project.Interfaces;
using Domain_Project.Models;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class EquipmentRequestsController : BaseController<EquipmentRequest, IGenericRepository<EquipmentRequest>>
    {
        public EquipmentRequestsController(IGenericRepository<EquipmentRequest> repository, IUnitOfWork unitOfWork)
            : base(repository, unitOfWork) { }

        protected override int GetEntityId(EquipmentRequest entity) => entity.RequestID;

        [HttpGet("pending")]
        [Authorize(Roles = "CentralManager")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var pendingRequests = await _repository.FindAsync(er => er.Status == "Pending");
            return Ok(pendingRequests);
        }

        [HttpPatch("{requestId}/approve")]
        [Authorize(Roles = "CentralManager")]
        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            request.Status = "Approved";
            request.ApprovalDate = DateTime.UtcNow;

            await _repository.UpdateAsync(request);
            // Removed SaveChangesAsync call

            return Ok(request);
        }

        [HttpPatch("{requestId}/reject")]
        [Authorize(Roles = "CentralManager")]
        public async Task<IActionResult> RejectRequest(int requestId, [FromBody] string reason)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            request.Status = "Rejected";
            request.Notes = reason;

            await _repository.UpdateAsync(request);
            // Removed SaveChangesAsync call

            return Ok(request);
        }
    }
}
