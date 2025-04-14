using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using System;
using System.Threading.Tasks;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "WarehouseOperative,WarehouseManager")]
    public class EquipmentCheckoutController : BaseController<EquipmentCheckout, IGenericRepository<EquipmentCheckout>>
    {
        private readonly ICheckoutRepository _checkoutRepository;

        public EquipmentCheckoutController(
            IGenericRepository<EquipmentCheckout> repository,
            IUnitOfWork unitOfWork,
            ICheckoutRepository checkoutRepository) : base(repository, unitOfWork)
        {
            _checkoutRepository = checkoutRepository;
        }

        protected override int GetEntityId(EquipmentCheckout entity) => entity.CheckoutID;

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveCheckouts()
        {
            var activeCheckouts = await _repository.FindAsync(ec => ec.Status == "CheckedOut");
            return Ok(activeCheckouts);
        }

        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdueCheckouts()
        {
            var overdueCheckouts = await _repository.FindAsync(ec =>
                ec.Status == "CheckedOut" && ec.ExpectedReturnDate < DateTime.UtcNow);
            return Ok(overdueCheckouts);
        }

        [HttpGet("team/{teamId}")]
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
        public async Task<IActionResult> GetOverdueCheckoutsByTimespan([FromQuery] int days = 30)
        {
            var overdueTime = TimeSpan.FromDays(days);
            var overdueCheckouts = await _checkoutRepository.GetOverdueAsync(overdueTime);
            return Ok(overdueCheckouts);
        }

        [HttpGet("unreturned/team/{teamId}")]
        public async Task<IActionResult> HasUnreturnedItems(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                return BadRequest("Team ID is required");
            }

            bool hasUnreturned = await _checkoutRepository.HasUnreturnedItemsAsync(teamId);
            return Ok(hasUnreturned);
        }
    }
}
