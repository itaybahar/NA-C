using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    public class BlacklistController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BlacklistController> _logger;
        private readonly IBlacklistService _blacklistService;

        public BlacklistController(
            IUnitOfWork unitOfWork,
            ILogger<BlacklistController> logger,
            IBlacklistService blacklistService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _blacklistService = blacklistService;
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var entry = await _blacklistService.GetBlacklistEntryAsync(id);
            if (entry == null)
            {
                return NotFound();
            }
            return Ok(entry);
        }

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveBlacklists([FromQuery] int userId = 1)
        {
            try
            {
                // Ensure we have a valid user ID
                int validUserId = userId > 0 ? userId : 1;
                _logger.LogInformation("Getting active blacklists with user ID {UserId} (raw received value: {RawUserId})",
                    validUserId, userId);

                // This will trigger the overdue checkout check and blacklist table update
                var activeBlacklists = await _blacklistService.GetActiveBlacklistsAsync(validUserId);

                // Ensure we only return unique entries in case there are duplicates
                var uniqueEntries = activeBlacklists.GroupBy(b => b.TeamID)
                    .Select(g => g.First())
                    .ToList();

                _logger.LogInformation("Returning {Count} active blacklist entries", uniqueEntries.Count);
                return Ok(uniqueEntries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active blacklists");
                return StatusCode(500, "An error occurred while getting active blacklists");
            }
        }



        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllBlacklists([FromQuery] int userId = 1)
        {
            try
            {
                var allBlacklists = await _blacklistService.GetAllBlacklistedTeamsAsync(userId);

                // Create a dictionary to remove duplicates by TeamID
                var uniqueEntries = new Dictionary<int, Blacklist>();
                foreach (var entry in allBlacklists)
                {
                    // Only keep the most recent blacklist entry for each team
                    if (!uniqueEntries.ContainsKey(entry.TeamID) ||
                        entry.BlacklistDate > uniqueEntries[entry.TeamID].BlacklistDate)
                    {
                        uniqueEntries[entry.TeamID] = entry;
                    }
                }

                return Ok(uniqueEntries.Values);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all blacklisted teams");
                return StatusCode(500, "An error occurred while getting all blacklisted teams");
            }
        }


        [HttpPost("add")]
        [AllowAnonymous]
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

            try
            {
                await _blacklistService.AddToBlacklistAsync(
                    blacklistDto.TeamID,
                    blacklistDto.BlacklistedBy,
                    blacklistDto.ReasonForBlacklisting);

                await _unitOfWork.CompleteAsync();

                return Created(string.Empty, new { TeamId = blacklistDto.TeamID });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding team to blacklist");
                return StatusCode(500, "An error occurred while blacklisting the team");
            }
        }

        [HttpPatch("{teamId}/remove")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveFromBlacklist(int teamId, [FromQuery] int userId = 1)
        {
            try
            {
                // Use the blacklist service's method to remove a team from blacklist
                await _blacklistService.RemoveFromBlacklistAsync(teamId, userId);
                await _unitOfWork.CompleteAsync();
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing team {teamId} from blacklist");
                return StatusCode(500, "An error occurred while removing the team from blacklist");
            }
        }

        [HttpPatch("{blacklistId}/remove/{removedByUserId}")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveFromBlacklistWithUser(int blacklistId, int removedByUserId)
        {
            // Method that takes a blacklist ID instead of team ID
            _logger.LogInformation($"Removing blacklist entry: {blacklistId} by user {removedByUserId}");

            try
            {
                // Get the blacklist entry first to get the team ID
                var blacklist = await _blacklistService.GetBlacklistEntryAsync(blacklistId);
                if (blacklist == null)
                {
                    return NotFound($"Blacklist entry with ID {blacklistId} not found");
                }

                // Then call the service method that removes by team ID
                await _blacklistService.RemoveFromBlacklistAsync(blacklist.TeamID, removedByUserId);
                await _unitOfWork.CompleteAsync();

                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing blacklist entry {blacklistId} by user {removedByUserId}");
                return StatusCode(500, "An error occurred while removing the blacklist entry");
            }
        }
    }
}
