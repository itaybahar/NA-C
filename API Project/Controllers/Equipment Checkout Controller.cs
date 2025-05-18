using API_Project.Repositories;
using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class EquipmentCheckoutController : BaseController<EquipmentCheckout, IGenericRepository<EquipmentCheckout>>
    {
        private readonly ICheckoutRepository _checkoutRepository;
        private readonly ICheckoutService _checkoutService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EquipmentCheckoutController> _logger;

        public EquipmentCheckoutController(
            IGenericRepository<EquipmentCheckout> repository,
            IUnitOfWork unitOfWork,
            ICheckoutRepository checkoutRepository,
            ICheckoutService checkoutService,
            ILogger<EquipmentCheckoutController> logger) : base(repository, unitOfWork)
        {
            _checkoutRepository = checkoutRepository;
            _checkoutService = checkoutService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        protected override int GetEntityId(EquipmentCheckout entity) => entity.CheckoutID;

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveCheckouts()
        {
            try
            {
                // Using the service instead of repository
                var activeCheckouts = await _checkoutService.GetActiveCheckoutsAsync();
                return Ok(activeCheckouts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active checkouts");
                return StatusCode(500, $"Error retrieving active checkouts: {ex.Message}");
            }
        }

        [HttpGet("get-checkout-id")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCheckoutId([FromQuery] int teamId, [FromQuery] int equipmentId)
        {
            try
            {
                // Find active checkouts that match both teamId and equipmentId
                var matchingCheckouts = await _repository.FindAsync(c =>
                    c.TeamID == teamId &&
                    c.EquipmentId == equipmentId &&
                    c.Status == "CheckedOut");

                // Return the first matching checkout ID, or -1 if none found
                var checkoutId = matchingCheckouts.FirstOrDefault()?.CheckoutID ?? -1;

                return Ok(checkoutId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving checkout ID for team {TeamId} and equipment {EquipmentId}", teamId, equipmentId);
                return StatusCode(500, $"Error retrieving checkout ID: {ex.Message}");
            }
        }

        [HttpGet("overdue")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOverdueCheckouts()
        {
            try
            {
                // Using the service instead of repository
                var overdueCheckouts = await _checkoutService.GetOverdueCheckoutsAsync();
                return Ok(overdueCheckouts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue checkouts");
                return StatusCode(500, $"Error retrieving overdue checkouts: {ex.Message}");
            }
        }

        [HttpGet("team/{teamId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTeamCheckouts(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                return BadRequest("Team ID is required");
            }

            try
            {
                // Getting unreturned items for a specific team
                var teamCheckouts = await _checkoutService.GetUnreturnedByTeamAsync(teamId);
                return Ok(teamCheckouts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team checkouts for team ID {TeamId}", teamId);
                return StatusCode(500, $"Error retrieving team checkouts: {ex.Message}");
            }
        }

        [HttpGet("equipment/{equipmentId}/in-use-quantity")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInUseQuantity(int equipmentId)
        {
            try
            {
                // Using the service instead of repository
                var inUseQuantity = await _checkoutService.GetInUseQuantityForEquipmentAsync(equipmentId);
                _logger.LogInformation("Returning in-use quantity for equipment ID {EquipmentId}: {Quantity}", equipmentId, inUseQuantity);
                return Ok(inUseQuantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving in-use quantity for equipment ID {EquipmentId}", equipmentId);
                return StatusCode(500, $"Error retrieving in-use quantity: {ex.Message}");
            }
        }

        [HttpPost("return/{checkoutId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ReturnEquipmentAsync(int checkoutId)
        {
            try
            {
                // Using the service instead of repository
                bool success = await _checkoutService.ReturnEquipmentAsync(checkoutId);

                if (!success)
                {
                    return NotFound($"Checkout with ID {checkoutId} not found or could not be returned.");
                }

                // Get the updated checkout after successful return
                var checkout = await _checkoutService.GetCheckoutByIdAsync(checkoutId);
                return Ok(checkout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning equipment for checkout ID {CheckoutId}", checkoutId);
                return StatusCode(500, $"Failed to return equipment: {ex.Message}");
            }
        }

        // Add this method to support PUT requests
        [HttpPut("return/{checkoutId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ReturnEquipmentPut(int checkoutId)
        {
            try
            {
                // Using the service instead of repository
                bool success = await _checkoutService.ReturnEquipmentAsync(checkoutId);

                if (!success)
                {
                    return NotFound($"Checkout with ID {checkoutId} not found or could not be returned.");
                }

                // Get the updated checkout after successful return
                var checkout = await _checkoutService.GetCheckoutByIdAsync(checkoutId);
                return Ok(checkout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning equipment for checkout ID {CheckoutId}", checkoutId);
                return StatusCode(500, $"Failed to return equipment: {ex.Message}");
            }
        }


        [HttpGet("equipment/batch-in-use-quantity")]
        public async Task<ActionResult<Dictionary<int, int>>> GetBatchInUseQuantities([FromQuery] int[] equipmentIds)
        {
            if (equipmentIds == null || equipmentIds.Length == 0)
            {
                return BadRequest("No equipment IDs provided");
            }

            try
            {
                var result = new Dictionary<int, int>();

                foreach (var id in equipmentIds)
                {
                    var quantity = await _checkoutService.GetInUseQuantityForEquipmentAsync(id);
                    result.Add(id, quantity);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving batch in-use quantities");
                return StatusCode(500, $"Error retrieving batch in-use quantities: {ex.Message}");
            }
        }

        [HttpGet("history/detailed")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetailedCheckoutHistory()
        {
            try
            {
                _logger.LogInformation("Received request for detailed checkout history");

                // Using the service instead of repository
                var checkoutRecords = await _checkoutService.GetCheckoutHistoryAsync();

                // Return an empty list instead of NotFound when no records exist
                if (checkoutRecords == null)
                {
                    _logger.LogWarning("No checkout history found or null result returned");
                    return Ok(new List<CheckoutRecordDto>()); // Return empty list
                }

                foreach (var record in checkoutRecords)
                {
                    if (record.Quantity <= 0)
                    {
                        record.Quantity = 1; // Default to 1 for backward compatibility
                    }
                }

                _logger.LogInformation("Returning detailed checkout history with {RecordCount} records", checkoutRecords.Count);
                return Ok(checkoutRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving detailed checkout history");
                return StatusCode(500, $"Error retrieving detailed checkout history: {ex.Message}");
            }
        }

        [HttpGet("history")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCheckoutHistory()
        {
            try
            {
                _logger.LogInformation("Received request for checkout history");
                var checkoutRecords = await _checkoutService.GetCheckoutHistoryAsync();

                // Always return OK with a list (empty if no records)
                return Ok(checkoutRecords ?? new List<CheckoutRecordDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving checkout history");
                return StatusCode(500, $"Error retrieving checkout history: {ex.Message}");
            }
        }


        [HttpPost("checkout")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckoutEquipment([FromBody] EquipmentCheckoutRequest request)
        {
            if (request == null)
            {
                _logger.LogWarning("Request body was null");
                return BadRequest(new { Success = false, Message = "Request body is required" });
            }

            if (request.EquipmentID <= 0)
            {
                _logger.LogWarning($"Invalid EquipmentID: {request.EquipmentID}");
                return BadRequest(new { Success = false, Message = "Valid Equipment ID is required" });
            }

            if (request.TeamID <= 0)
            {
                _logger.LogWarning($"Invalid TeamID: {request.TeamID}");
                return BadRequest(new { Success = false, Message = "Valid Team ID is required" });
            }

            if (request.Quantity <= 0)
            {
                _logger.LogWarning($"Invalid Quantity: {request.Quantity}");
                return BadRequest(new { Success = false, Message = "Valid Quantity is required" });
            }

            try
            {
                // Log the request for debugging
                _logger.LogInformation("Checkout request received: Equipment ID={EquipmentId}, Team ID={TeamId}, User ID={UserId}, Quantity={Quantity}",
                    request.EquipmentID, request.TeamID, request.UserId, request.Quantity);

                // Using the service instead of direct repository access
                var (success, errorMessage, overdueEquipmentName, overdueItems) = await _checkoutService.CheckoutEquipmentAsync(
                    request.TeamID,
                    request.EquipmentID,
                    request.UserId,
                    request.Quantity);

                if (!success)
                {
                    // Check if this is a blacklist error (contains team blacklisted info)
                    bool isBlacklistError = errorMessage?.Contains("blacklisted", StringComparison.OrdinalIgnoreCase) == true;

                    if (isBlacklistError)
                    {
                        // Return a 200 OK with the blacklist information instead of a 400 Bad Request
                        return Ok(new
                        {
                            Success = false,
                            IsBlacklisted = true,
                            Message = errorMessage,
                            OverdueEquipmentName = overdueEquipmentName,
                            OverdueItems = overdueItems?.Select(item => new {
                                CheckoutID = item.CheckoutID,
                                EquipmentId = item.EquipmentId,
                                EquipmentName = item.Equipment?.Name,
                                CheckoutDate = item.CheckoutDate,
                                ExpectedReturnDate = item.ExpectedReturnDate,
                                DaysOverdue = (DateTime.UtcNow - item.ExpectedReturnDate).TotalDays
                            })
                        });
                    }
                    else
                    {
                        // For non-blacklist errors, continue returning BadRequest
                        return BadRequest(new
                        {
                            Success = false,
                            Message = errorMessage,
                            OverdueEquipmentName = overdueEquipmentName
                        });
                    }
                }

                // Get all active checkouts for this team to find the one we just created
                var activeCheckouts = await _checkoutService.GetActiveCheckoutsAsync();
                var checkout = activeCheckouts
                    .Where(c => c.TeamID == request.TeamID && c.EquipmentId == request.EquipmentID)
                    .OrderByDescending(c => c.CheckoutDate)
                    .FirstOrDefault();

                if (checkout == null)
                {
                    _logger.LogWarning("Checkout was successful but unable to retrieve the created checkout record");
                    return Ok(new { Success = true, Message = "Checkout successful" });
                }

                return CreatedAtAction(nameof(GetById), new { id = checkout.CheckoutID }, new
                {
                    Success = true,
                    Checkout = checkout
                });
            }
            catch (Exception ex)
            {
                // Log the full exception details
                _logger.LogError(ex, "Error in checkout process");

                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Inner exception in checkout process");
                }

                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Failed to checkout equipment: {ex.Message}"
                });
            }
        }


        private int GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                throw new InvalidOperationException("User ID claim is missing or invalid.");
            }

            return userId;
        }

        public class EquipmentReturnRequest
        {
            public string? Condition { get; set; }
            public string? Notes { get; set; }
            public DateTime? ReturnDate { get; set; }
        }

        public class EquipmentCheckoutRequest
        {
            public int EquipmentID { get; set; }
            public int TeamID { get; set; }
            public int UserId { get; set; }
            public DateTime? ExpectedReturnDate { get; set; } = DateTime.UtcNow.AddDays(7);
            public DateTime? CheckoutDate { get; set; } = DateTime.UtcNow;
            public string? Notes { get; set; }
            public int Quantity { get; set; }
        }

        [HttpGet("blacklisted-teams")]
        public async Task<ActionResult<List<BlacklistedTeamDto>>> GetBlacklistedTeams()
        {
            try
            {
                var blacklistedTeams = await _checkoutService.GetBlacklistedTeamsAsync();
                return Ok(blacklistedTeams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blacklisted teams");
                return StatusCode(500, "An error occurred while retrieving blacklisted teams");
            }
        }
    }
}
