using API_Project.Repositories;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace API_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class EquipmentCheckoutController : BaseController<EquipmentCheckout, IGenericRepository<EquipmentCheckout>>
    {
        private readonly ICheckoutRepository _checkoutRepository;
        private readonly IUnitOfWork _unitOfWork;

        public EquipmentCheckoutController(
            IGenericRepository<EquipmentCheckout> repository,
            IUnitOfWork unitOfWork,
            ICheckoutRepository checkoutRepository) : base(repository, unitOfWork)
        {
            _checkoutRepository = checkoutRepository;
            _unitOfWork = unitOfWork;
        }

        protected override int GetEntityId(EquipmentCheckout entity) => entity.CheckoutID;

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveCheckouts()
        {
            try
            {
                var activeCheckouts = await _repository.FindAsync(ec => ec.Status == "CheckedOut");
                return Ok(activeCheckouts);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving active checkouts: {ex.Message}");
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
                Console.Error.WriteLine($"Error retrieving checkout ID for team {teamId} and equipment {equipmentId}: {ex.Message}");
                return StatusCode(500, $"Error retrieving checkout ID: {ex.Message}");
            }
        }

        [HttpGet("overdue")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOverdueCheckouts()
        {
            try
            {
                var overdueCheckouts = await _repository.FindAsync(ec =>
                    ec.Status == "CheckedOut" && ec.ExpectedReturnDate < DateTime.UtcNow);
                return Ok(overdueCheckouts);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving overdue checkouts: {ex.Message}");
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
                var teamCheckouts = await _checkoutRepository.GetByTeamIdAsync(teamId);
                return Ok(teamCheckouts);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving team checkouts for team ID {teamId}: {ex.Message}");
                return StatusCode(500, $"Error retrieving team checkouts: {ex.Message}");
            }
        }

        [HttpGet("equipment/{equipmentId}/in-use-quantity")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInUseQuantity(int equipmentId)
        {
            try
            {
                var inUseQuantity = await _checkoutRepository.GetInUseQuantityForEquipmentAsync(equipmentId);
                Console.WriteLine($"Returning in-use quantity for equipment ID {equipmentId}: {inUseQuantity}");
                return Ok(inUseQuantity);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving in-use quantity for equipment ID {equipmentId}: {ex.Message}");
                return StatusCode(500, $"Error retrieving in-use quantity: {ex.Message}");
            }
        }

        [HttpPut("return/{checkoutId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ReturnEquipmentWithDetails(int checkoutId, [FromBody] EquipmentReturnRequest request)
        {
            try
            {
                var checkout = await _repository.GetByIdAsync(checkoutId);
                if (checkout == null)
                {
                    return NotFound($"Checkout with ID {checkoutId} not found.");
                }

                var equipment = await _unitOfWork.Repository<Equipment>().GetByIdAsync(checkout.EquipmentId);
                if (equipment == null)
                {
                    return NotFound($"Equipment with ID {checkout.EquipmentId} not found.");
                }

                // Update the checkout record
                checkout.Status = "Returned";
                checkout.ActualReturnDate = request.ReturnDate ?? DateTime.UtcNow;

                if (!string.IsNullOrEmpty(request.Condition))
                {
                    checkout.ReturnCondition = request.Condition;
                }

                if (!string.IsNullOrEmpty(request.Notes))
                {
                    checkout.Notes = request.Notes;
                }

                // Update the equipment quantity
                equipment.Quantity += checkout.Quantity;
                if (equipment.Quantity > 0)
                {
                    equipment.Status = "Available";
                }

                await _repository.UpdateAsync(checkout);
                await _unitOfWork.Repository<Equipment>().UpdateAsync(equipment);
                await _unitOfWork.CompleteAsync();

                return Ok(checkout);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error returning equipment: {ex.Message}");
                return StatusCode(500, $"Failed to return equipment: {ex.Message}");
            }
        }

        [HttpPost("return/{checkoutId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ReturnEquipment(int checkoutId)
        {
            try
            {
                var checkout = await _repository.GetByIdAsync(checkoutId);
                if (checkout == null)
                {
                    return NotFound($"Checkout with ID {checkoutId} not found.");
                }

                checkout.Status = "Returned";
                checkout.ActualReturnDate = DateTime.UtcNow;

                await _repository.UpdateAsync(checkout);
                await _repository.SaveChangesAsync(checkout);

                return Ok(checkout);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error returning equipment: {ex.Message}");
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
                    var quantity = await _checkoutRepository.GetInUseQuantityForEquipmentAsync(id);
                    result.Add(id, quantity);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving batch in-use quantities: {ex.Message}");
                return StatusCode(500, $"Error retrieving batch in-use quantities: {ex.Message}");
            }
        }

        [HttpGet("history/detailed")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetailedCheckoutHistory()
        {
            try
            {
                Console.WriteLine("Received request for detailed checkout history");

                var checkoutRecords = await _checkoutRepository.GetCheckoutHistoryAsync();

                if (checkoutRecords == null || !checkoutRecords.Any())
                {
                    Console.WriteLine("No checkout history found.");
                    return NotFound("No checkout history found.");
                }

                foreach (var record in checkoutRecords)
                {
                    if (record.Quantity <= 0)
                    {
                        record.Quantity = 1; // Default to 1 for backward compatibility
                    }
                }

                Console.WriteLine($"Returning detailed checkout history with {checkoutRecords.Count} records");
                return Ok(checkoutRecords);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving detailed checkout history: {ex.Message}");
                return StatusCode(500, $"Error retrieving detailed checkout history: {ex.Message}");
            }
        }

        [HttpPost("checkout")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckoutEquipment([FromBody] EquipmentCheckoutRequest request)
        {
            if (request == null || request.EquipmentID <= 0 || request.TeamID <= 0 || request.Quantity <= 0)
            {
                return BadRequest("Valid Equipment ID, Team ID, and Quantity are required");
            }

            try
            {
                // Log the request for debugging
                Console.WriteLine($"Checkout request received: Equipment ID={request.EquipmentID}, Team ID={request.TeamID}, User ID={request.UserId}, Quantity={request.Quantity}");

                // Fetch the equipment details
                var equipment = await _unitOfWork.Repository<Equipment>().GetByIdAsync(request.EquipmentID);
                if (equipment == null)
                {
                    Console.WriteLine($"Equipment with ID {request.EquipmentID} not found.");
                    return NotFound($"Equipment with ID {request.EquipmentID} not found.");
                }

                // Check if the requested quantity is available
                if (equipment.Quantity < request.Quantity)
                {
                    Console.WriteLine($"Requested quantity ({request.Quantity}) exceeds available quantity ({equipment.Quantity}).");
                    return BadRequest($"Requested quantity ({request.Quantity}) exceeds available quantity ({equipment.Quantity}).");
                }

                // Create the checkout record
                var checkout = new EquipmentCheckout
                {
                    EquipmentId = request.EquipmentID,
                    TeamID = request.TeamID,
                    UserID = request.UserId,
                    CheckoutDate = request.CheckoutDate ?? DateTime.UtcNow,
                    ExpectedReturnDate = request.ExpectedReturnDate ?? DateTime.UtcNow.AddDays(7),
                    Status = "CheckedOut",
                    Notes = request.Notes,
                    Quantity = request.Quantity
                };

                // Add the checkout record
                await _repository.AddAsync(checkout);

                // Update the equipment quantity
                equipment.Quantity -= request.Quantity;
                if (equipment.Quantity == 0)
                {
                    equipment.Status = "Unavailable";
                }

                await _unitOfWork.Repository<Equipment>().UpdateAsync(equipment);
                await _unitOfWork.CompleteAsync();

                return CreatedAtAction(nameof(GetById), new { id = checkout.CheckoutID }, checkout);
            }
            catch (Exception ex)
            {
                // Log the full exception details
                Console.Error.WriteLine($"Error in checkout process: {ex.Message}"); 
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"Error in checkout process: {ex.InnerException.Message}");
                }

                return StatusCode(500, $"Failed to checkout equipment: {ex.Message}");
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
    }
}
