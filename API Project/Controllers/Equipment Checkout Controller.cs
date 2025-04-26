using DocumentFormat.OpenXml.Wordprocessing;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;

namespace API_Project.Controllers
{
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
                var checkoutRecords = await _checkoutRepository.GetCheckoutHistoryAsync();

                if (checkoutRecords == null || !checkoutRecords.Any())
                {
                    Console.WriteLine("No checkout history found.");
                    return NotFound("No checkout history found.");
                }

                Console.WriteLine($"Checkout history count: {checkoutRecords.Count}");
                Console.WriteLine($"Checkout history count: {checkoutRecords}");

                return new JsonResult(checkoutRecords, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
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
            if (request == null || request.EquipmentID <= 0 || request.TeamID <= 0)
            {
                return BadRequest("Valid Equipment ID and Team ID are required");
            }

            var checkout = new EquipmentCheckout
            {
                EquipmentID = request.EquipmentID,
                TeamID = request.TeamID,
                UserID = request.UserId,
                CheckoutDate = DateTime.UtcNow,
                ExpectedReturnDate = request.ExpectedReturnDate ?? DateTime.UtcNow.AddDays(7),
                Status = "CheckedOut",
                Notes = request.Notes,
            };

            try
            {
                await _repository.AddAsync(checkout);
                await _unitOfWork.CompleteAsync();
                return CreatedAtAction(nameof(GetById), new { id = checkout.CheckoutID }, checkout);
            }
            catch (Exception ex)
            {
                // Enhanced error logging
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
        
    }
}
