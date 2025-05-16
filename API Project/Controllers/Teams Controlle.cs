using API_Project.Repositories;
using API_Project.Services;
using Domain_Project.DTOs;
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
    [Route("api/[controller]")]
    [Authorize]
    public class TeamsController : BaseController<Team, IGenericRepository<Team>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITeamService _teamService;
        private readonly IBlacklistService _blacklistService;
        private readonly ICheckoutRepository _checkoutRepository;
        private readonly ILogger<TeamsController> _logger;


        public TeamsController(
            IGenericRepository<Team> repository,
            IUnitOfWork unitOfWork,
            ITeamService teamService,
            IBlacklistService blacklistService,
            ICheckoutRepository checkoutRepository,
            ILogger<TeamsController> logger) : base(repository, unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _teamService = teamService;
            _blacklistService = blacklistService;
            _checkoutRepository = checkoutRepository;
            _logger = logger;

        }

        protected override int GetEntityId(Team entity) => entity.TeamID;

        [HttpPost("add")]
        [AllowAnonymous]
        public async Task<IActionResult> AddTeam([FromBody] Team team)
        {
            if (team == null)
            {
                return BadRequest("Team data is required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _teamService.AddTeam(team);
                if (result)
                {
                    return Ok(new
                    {
                        message = "Team added successfully.",
                        team
                    });
                }
                else
                {
                    return BadRequest("Failed to add the team.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddTeam: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }





        [HttpGet("{id}")]
        [AllowAnonymous]
        public override async Task<IActionResult> GetById(int id)
        {
            try
            {
                var team = await _teamService.GetTeamByIdAsync(id);
                if (team == null)
                    return NotFound($"Team with ID {id} not found.");

                await CheckAndUpdateBlacklistStatus((Team)team);
                return Ok(team);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("string/{teamId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByStringId(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
                return BadRequest("Team ID cannot be empty.");

            try
            {
                var team = await _teamService.GetByStringIdAsync(teamId);
                if (team == null)
                    return NotFound($"Team with ID {teamId} not found.");

                if (team is Team fullTeam)
                    await CheckAndUpdateBlacklistStatus(fullTeam);

                return Ok(team);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("details")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTeams()
        {
            Console.WriteLine("Fetching all teams...");
            var teams = await _teamService.GetAllTeamsAsync();

            if (teams == null || !teams.Any())
            {
                Console.WriteLine("No teams found.");
                return Ok(new List<TeamDto>());
            }

            Console.WriteLine($"Found {teams.Count()} teams.");
            return Ok(teams);
        }


        [HttpGet("blacklisted")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlacklistedTeams([FromQuery] int userId = 1)
        {
            try
            {
                // Ensure userId is valid (never zero)
                int validUserId = userId > 0 ? userId : 1;

                _logger.LogInformation("Getting blacklisted teams with user ID {UserId}", validUserId);

                // Get blacklisted team entries
                var blacklistedTeams = await _teamService.GetBlacklistedTeamsAsync();

                // Create a dictionary to remove duplicates
                var uniqueTeams = new Dictionary<int, TeamDto>();

                foreach (var team in blacklistedTeams)
                {
                    // Only include truly blacklisted teams
                    if (team.IsBlacklisted && !uniqueTeams.ContainsKey(team.TeamID))
                    {
                        uniqueTeams[team.TeamID] = new TeamDto
                        {
                            TeamID = team.TeamID,
                            TeamName = team.TeamName ?? "Unknown Team",
                            Description = team.Description ?? string.Empty,
                            IsActive = team.IsActive
                        };
                    }
                }

                return Ok(uniqueTeams.Values);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blacklisted teams");
                return StatusCode(500, "An error occurred while retrieving blacklisted teams.");
            }
        }

        [HttpGet("check-overdue-equipment")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckOverdueEquipment()
        {
            try
            {
                var overdueThreshold = TimeSpan.FromSeconds(24);
                var overdueCheckouts = await _checkoutRepository.GetOverdueAsync(overdueThreshold);
                int blacklistedCount = 0;

                foreach (var checkout in overdueCheckouts)
                {
                    if (int.TryParse(checkout.TeamId.ToString(), out int teamIdInt))
                    {
                        var team = await _repository.GetByIdAsync(teamIdInt);
                        if (team != null && !team.IsBlacklisted)
                        {
                            await _blacklistService.AddToBlacklistAsync(
                                team.TeamID,
                                0,
                                $"Automatic blacklist due to equipment not returned within 24 hours. Equipment ID: {checkout.EquipmentId}"
                            );

                            team.IsBlacklisted = true;
                            await _repository.UpdateAsync(team);
                            blacklistedCount++;
                        }
                    }
                }

                await _unitOfWork.CompleteAsync();

                return Ok(new
                {
                    message = $"Checked for overdue equipment. {blacklistedCount} teams blacklisted automatically.",
                    overdueCount = overdueCheckouts.Count,
                    blacklistedCount = blacklistedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("{teamId}/remove-from-blacklist")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveFromBlacklist(int teamId, [FromBody] BlacklistRemovalRequest request)
        {
            if (request == null || request.RemovedBy <= 0)
                return BadRequest("Valid removal user ID is required.");

            try
            {
                var team = await _repository.GetByIdAsync(teamId);
                if (team == null)
                    return NotFound($"Team with ID {teamId} not found.");

                if (!await _blacklistService.IsTeamBlacklistedAsync(teamId))
                    return BadRequest($"Team with ID {teamId} is not blacklisted.");

                bool hasUnreturned = await _checkoutRepository.HasUnreturnedItemsAsync(team.TeamID.ToString());
                if (hasUnreturned)
                    return BadRequest($"Cannot remove Team with ID {teamId} from blacklist until all equipment is returned.");

                await _blacklistService.RemoveFromBlacklistAsync(teamId, request.RemovedBy, request.Notes);

                team.IsBlacklisted = false;
                await _repository.UpdateAsync(team);
                await _unitOfWork.CompleteAsync();

                return Ok($"Team with ID {teamId} has been removed from the blacklist.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{teamId}/blacklist-status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlacklistStatus(int teamId)
        {
            try
            {
                var team = await _repository.GetByIdAsync(teamId);
                if (team == null)
                    return NotFound($"Team with ID {teamId} not found.");

                await CheckAndUpdateBlacklistStatus(team);

                bool isBlacklisted = await _blacklistService.IsTeamBlacklistedAsync(teamId);
                var blacklistEntry = isBlacklisted ? await _blacklistService.GetBlacklistEntryAsync(teamId) : null;

                bool hasUnreturned = await _checkoutRepository.HasUnreturnedItemsAsync(team.TeamID.ToString());

                return Ok(new
                {
                    teamId,
                    teamName = team.TeamName,
                    isBlacklisted,
                    blacklistDetails = blacklistEntry,
                    hasUnreturnedEquipment = hasUnreturned
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("{teamId}/toggle-status")]
        [AllowAnonymous]
        public async Task<IActionResult> ToggleTeamStatus(int teamId)
        {
            try
            {
                var team = await _repository.GetByIdAsync(teamId);
                if (team == null)
                    return NotFound($"Team with ID {teamId} not found.");

                team.IsActive = !team.IsActive;
                await _repository.UpdateAsync(team);
                await _unitOfWork.CompleteAsync();

                return Ok(new
                {
                    teamId,
                    teamName = team.TeamName,
                    isActive = team.IsActive,
                    message = $"Team status has been set to {(team.IsActive ? "active" : "inactive")}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private async Task CheckAndUpdateBlacklistStatus(Team team)
        {
            if (team == null) return;

            try
            {
                var overdueThreshold = TimeSpan.FromSeconds(24);
                var overdueCheckouts = await _checkoutRepository.GetOverdueAsync(overdueThreshold);
                string teamIdString = team.TeamID.ToString();
                bool hasOverdueItems = overdueCheckouts.Any(c => c.TeamId.ToString() == teamIdString);

                if (hasOverdueItems && !team.IsBlacklisted)
                {
                    await _blacklistService.AddToBlacklistAsync(
                        team.TeamID, 0,
                        "Automatic blacklist due to equipment not returned within 24 hours"
                    );

                    team.IsBlacklisted = true;
                    await _repository.UpdateAsync(team);
                    await _unitOfWork.CompleteAsync();
                }
                else if (!hasOverdueItems && team.IsBlacklisted)
                {
                    bool hasAnyUnreturnedItems = await _checkoutRepository.HasUnreturnedItemsAsync(team.TeamID.ToString());
                    if (!hasAnyUnreturnedItems)
                    {
                        await _blacklistService.RemoveFromBlacklistAsync(
                            team.TeamID, 0,
                            "Automatic removal from blacklist after all equipment returned"
                        );

                        team.IsBlacklisted = false;
                        await _repository.UpdateAsync(team);
                        await _unitOfWork.CompleteAsync();
                    }
                }
            }
            catch (Exception) { }
        }

        private async Task UpdateAllTeamsBlacklistStatus(IEnumerable<Team> teams)
        {
            if (teams == null) return;

            try
            {
                var overdueThreshold = TimeSpan.FromSeconds(24);
                var overdueCheckouts = await _checkoutRepository.GetOverdueAsync(overdueThreshold);

                foreach (var team in teams)
                {
                    string teamIdString = team.TeamID.ToString();
                    bool hasOverdueItems = overdueCheckouts.Any(c => c.TeamId.ToString() == teamIdString);

                    if (hasOverdueItems && !team.IsBlacklisted)
                    {
                        await _blacklistService.AddToBlacklistAsync(
                            team.TeamID, 0,
                            "Automatic blacklist due to equipment not returned within 24 hours"
                        );

                        team.IsBlacklisted = true;
                        await _repository.UpdateAsync(team);
                    }
                    else if (!hasOverdueItems && team.IsBlacklisted)
                    {
                        bool hasAnyUnreturnedItems = await _checkoutRepository.HasUnreturnedItemsAsync(team.TeamID.ToString());
                        if (!hasAnyUnreturnedItems)
                        {
                            await _blacklistService.RemoveFromBlacklistAsync(
                                team.TeamID, 0,
                                "Automatic removal from blacklist after all equipment returned"
                            );

                            team.IsBlacklisted = false;
                            await _repository.UpdateAsync(team);
                        }
                    }
                }

                await _unitOfWork.CompleteAsync();
            }
            catch (Exception) { }
        }
    }

    public class BlacklistRequest
    {
        public int BlacklistedBy { get; set; }
        public required string Reason { get; set; }
    }

    public class BlacklistRemovalRequest
    {
        public int RemovedBy { get; set; }
        public string? Notes { get; set; }
    }
}