using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain_Project.Interfaces;
using Domain_Project.Models;


namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "WarehouseOperative,WarehouseManager")]
    public class EquipmentCheckoutController : BaseController<EquipmentCheckout, IGenericRepository<EquipmentCheckout>>
    {
        public EquipmentCheckoutController(IGenericRepository<EquipmentCheckout> repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork) {}
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
    }
}