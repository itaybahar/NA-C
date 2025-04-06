using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "WarehouseManager")]
    public class EquipmentCategoriesController : BaseController<EquipmentCategory, IGenericRepository<EquipmentCategory>>
    {
        public EquipmentCategoriesController(IGenericRepository<EquipmentCategory> repository) : base(repository) { }

        protected override int GetEntityId(EquipmentCategory entity) => entity.CategoryID;

        [HttpGet("{categoryId}/equipment")]
        public async Task<IActionResult> GetEquipmentByCategory(int categoryId)
        {
            var equipment = await _repository.FindAsync(e => e.CategoryID == categoryId);
            return Ok(equipment);
        }
    }
}
