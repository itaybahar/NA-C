using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain_Project.Interfaces;
using Domain_Project.Models;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "WarehouseManager,WarehouseOperative")]
    public class EquipmentController : BaseController<Equipment, IGenericRepository<Equipment>>
    {
        public EquipmentController(IGenericRepository<Equipment> repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork) { }

        protected override int GetEntityId(Equipment entity) => entity.EquipmentID;

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableEquipment()
        {
            var availableEquipment = await _repository.FindAsync(e => e.Status == "Available");
            return Ok(availableEquipment);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetEquipmentByCategory(int categoryId)
        {
            var categoryEquipment = await _repository.FindAsync(e => e.CategoryID == categoryId);
            return Ok(categoryEquipment);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "WarehouseManager")]
        public async Task<IActionResult> UpdateEquipmentStatus(int id, [FromBody] string newStatus)
        {
            var equipment = await _repository.GetByIdAsync(id);
            if (equipment == null)
            {
                return NotFound();
            }

            equipment.Status = newStatus;
            await _repository.UpdateAsync(equipment);
            await _repository.SaveChangesAsync(equipment);

            return Ok(equipment);
        }
    }
}