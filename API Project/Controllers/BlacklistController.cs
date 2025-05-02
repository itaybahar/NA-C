using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "WarehouseManager,CentralManager")]
    public class BlacklistController : BaseController<Blacklist, IGenericRepository<Blacklist>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public BlacklistController(IGenericRepository<Blacklist> repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
            _unitOfWork = unitOfWork; // Assign the injected unitOfWork to a private field
        }

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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!blacklistDto.IsValidReason())
            {
                return BadRequest("The reason for blacklisting is invalid.");
            }

            var blacklist = new Blacklist
            {
                TeamID = blacklistDto.TeamID,
                BlacklistedBy = blacklistDto.BlacklistedBy,
                ReasonForBlacklisting = blacklistDto.ReasonForBlacklisting,
                Notes = blacklistDto.Notes,
                BlacklistDate = blacklistDto.BlacklistDate
            };

            await _repository.AddAsync(blacklist);
            await _unitOfWork.CompleteAsync(); // Ensure changes are saved to the database

            return CreatedAtAction(nameof(GetById), new { id = blacklist.BlacklistID }, blacklist);
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
            await _unitOfWork.CompleteAsync(); // Ensure changes are saved to the database

            return Ok(blacklist);
        }
    }
}
