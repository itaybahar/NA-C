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
    [Authorize(Roles = "WarehouseOperative,WarehouseManager,Admin")]
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
            var activeCheckouts = await _repository.FindAsync(ec => ec.Status == "CheckedOut");
            return Ok(activeCheckouts);
        }

        [HttpGet("overdue")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOverdueCheckouts()
        {
            var overdueCheckouts = await _repository.FindAsync(ec =>
                ec.Status == "CheckedOut" && ec.ExpectedReturnDate < DateTime.UtcNow);
            return Ok(overdueCheckouts);
        }

        [HttpGet("team/{teamId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTeamCheckouts(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                return BadRequest("Team ID is required");
            }

            var teamCheckouts = await _checkoutRepository.GetByTeamIdAsync(teamId);
            return Ok(teamCheckouts);
        }

        [HttpGet("equipment/{equipmentId}/in-use-quantity")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInUseQuantity(int equipmentId)
        {
            try
            {
                var inUseQuantity = await _checkoutRepository.GetInUseQuantityForEquipmentAsync(equipmentId);
                // Log the response to debug
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
            var checkout = await _repository.GetByIdAsync(checkoutId);
            if (checkout == null)
            {
                return NotFound();
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

            try
            {
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

        // Keep the original endpoint for backward compatibility
        [HttpPost("return/{checkoutId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ReturnEquipment(int checkoutId)
        {
            var checkout = await _repository.GetByIdAsync(checkoutId);
            if (checkout == null)
            {
                return NotFound();
            }

            checkout.Status = "Returned";
            checkout.ActualReturnDate = DateTime.UtcNow;

            await _repository.UpdateAsync(checkout);
            await _repository.SaveChangesAsync(checkout);

            return Ok(checkout);
        }

        // Define a class for the return request
        public class EquipmentReturnRequest
        {
            public string? Condition { get; set; }
            public string? Notes { get; set; }
            public DateTime? ReturnDate { get; set; }
        }
        // In your API controller
        [HttpGet("equipment/batch-in-use-quantity")]
        public async Task<ActionResult<Dictionary<int, int>>> GetBatchInUseQuantities([FromQuery] int[] equipmentIds)
        {
            if (equipmentIds == null || equipmentIds.Length == 0)
            {
                return BadRequest("No equipment IDs provided");
            }

            var result = new Dictionary<int, int>();

            foreach (var id in equipmentIds)
            {
                var quantity = await _checkoutRepository.GetInUseQuantityForEquipmentAsync(id);
                result.Add(id, quantity);
            }

            return Ok(result);
        }


        [HttpGet("overdue/timespan")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOverdueCheckoutsByTimespan([FromQuery] int days = 30)
        {
            var overdueTime = TimeSpan.FromDays(days);
            var overdueCheckouts = await _checkoutRepository.GetOverdueAsync(overdueTime);
            return Ok(overdueCheckouts);
        }

        [HttpGet("unreturned/team/{teamId}")]
        [AllowAnonymous]
        public async Task<IActionResult> HasUnreturnedItems(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                return BadRequest("Team ID is required");
            }

            bool hasUnreturned = await _checkoutRepository.HasUnreturnedItemsAsync(teamId);
            return Ok(hasUnreturned);
        }

        [HttpGet("history/detailed")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetailedCheckoutHistory()
        {
            try
            {
                // Log the request to help with debugging
                Console.WriteLine("Received request for detailed checkout history");

                var checkoutRecords = await _checkoutRepository.GetCheckoutHistoryAsync();

                if (checkoutRecords == null || !checkoutRecords.Any())
                {
                    Console.WriteLine("No checkout history found.");
                    return NotFound("No checkout history found.");
                }

                // Ensure quantity information is included in each record
                foreach (var record in checkoutRecords)
                {
                    // If the record doesn't have a quantity set, try to retrieve it
                    if (record.Quantity <= 0)
                    {
                        // Try to find the checkout by ID to get the quantity
                        if (int.TryParse(record.Id, out int checkoutId) && checkoutId > 0)
                        {
                            var checkout = await _repository.GetByIdAsync(checkoutId);
                            if (checkout != null)
                            {
                                record.Quantity = checkout.Quantity;
                            }
                            else
                            {
                                // Default to 1 for backward compatibility
                                record.Quantity = 1;
                            }
                        }
                        else
                        {
                            // Default to 1 for backward compatibility
                            record.Quantity = 1;
                        }
                    }
                }

                Console.WriteLine($"Checkout history count: {checkoutRecords.Count}");
                Console.WriteLine($"First record quantity: {checkoutRecords.FirstOrDefault()?.Quantity ?? 0}");

                // For debugging, log the JSON that will be returned
                var jsonResult = new JsonResult(checkoutRecords, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                Console.WriteLine($"Returning detailed checkout history with {checkoutRecords.Count} records");
                return jsonResult;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving detailed checkout history: {ex.Message}");
                return StatusCode(500, $"Error retrieving detailed checkout history: {ex.Message}");
            }
        }

        [HttpGet("get-checkout-id")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCheckoutIdByTeamAndEquipment(int teamId, int equipmentId)
        {
            try
            {
                // Change from calling a non-existent method to using the repository
                var checkoutId = await _checkoutRepository.GetCheckoutIdByTeamAndEquipmentAsync(teamId, equipmentId);

                if (checkoutId == null)
                {
                    return NotFound($"No active checkout found for team ID: {teamId} and equipment ID: {equipmentId}");
                }

                return Ok(checkoutId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving checkout ID: {ex.Message}");
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

            // Fetch the equipment details
            var equipment = await _unitOfWork.Repository<Equipment>().GetByIdAsync(request.EquipmentID);
            if (equipment == null)
            {
                return NotFound($"Equipment with ID {request.EquipmentID} not found.");
            }

            // Check if the requested quantity is available
            if (equipment.Quantity < request.Quantity)
            {
                return BadRequest($"Requested quantity ({request.Quantity}) exceeds available quantity ({equipment.Quantity}).");
            }

            // Create the checkout record
            var checkout = new EquipmentCheckout
            {
                EquipmentId = request.EquipmentID,
                TeamID = request.TeamID,
                UserID = request.UserId,
                CheckoutDate = DateTime.UtcNow,
                ExpectedReturnDate = request.ExpectedReturnDate ?? DateTime.UtcNow.AddDays(7),
                Status = "CheckedOut",
                Notes = request.Notes,
                Quantity = request.Quantity
            };

            try
            {
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
                var innerMessage = ex.InnerException?.Message ?? "No inner exception details available";
                Console.Error.WriteLine($"Database error in checkout: {innerMessage}");
                return StatusCode(500, $"Failed to checkout equipment: {ex.Message}");
            }
        }

        private int GetCurrentUserId()
        {
            // Ensure the user is authenticated
            if (User.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            // Retrieve the user ID from the claims
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserID");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                throw new InvalidOperationException("User ID claim is missing or invalid.");
            }

            return userId;
        }
    }

    public class EquipmentCheckoutRequest
    {
        public int EquipmentID { get; set; }
        public int TeamID { get; set; }
        public int UserId { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public DateTime? CheckoutDate { get; set; }
        public string? Notes { get; set; }
        public int Quantity { get; internal set; }
    }
}
