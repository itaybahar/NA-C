using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "WarehouseManager,CentralManager")]
    public class BlacklistController : BaseController<Blacklist, IGenericRepository<Blacklist>>
    {
        public BlacklistController(IGenericRepository<Blacklist> repository) : base(repository) { }

        protected override int GetEntityId(Blacklist entity) => entity.BlacklistID;

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveBlacklists()
        {
            var activeBlacklists = await _repository.FindAsync(b => b.RemovalDate == null);
            return Ok(activeBlacklists);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToBlacklist([FromBody] BlacklistCreateDto blacklistDto)
        {
            // Custom logic to create a blacklist entry
            return Ok();
        }

        [HttpPatch("{blacklistId}/remove")]
        public async Task<IActionResult> RemoveFromBlacklist(int blacklistId)
        {
            var blacklist = await _repository.GetByIdAsync(blacklistId);
            if (blacklist == null)
            {
                return NotFound();
            }

            blacklist.RemovalDate = DateTime.UtcNow;
            await _repository.UpdateAsync(blacklist);
            await _repository.SaveChangesAsync();

            return Ok(blacklist);
        }
    }
}
