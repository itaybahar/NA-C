using API_Project.Repositories;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace API_Project.Services
{
    public class BlacklistService : IBlacklistService
    {
        private readonly IBlacklistRepository _blacklistRepository;
        private readonly ICheckoutRepository _checkoutRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly ILogger<BlacklistService> _logger;

        public BlacklistService(
            IBlacklistRepository blacklistRepository,
            ICheckoutRepository checkoutRepository,
            ITeamRepository teamRepository,
            IEquipmentRepository equipmentRepository,
            ILogger<BlacklistService> logger)
        {
            _blacklistRepository = blacklistRepository;
            _checkoutRepository = checkoutRepository;
            _teamRepository = teamRepository;
            _equipmentRepository = equipmentRepository;
            _logger = logger;
        }

        /// <summary>
        /// Checks if a team is blacklisted.
        /// </summary>
        public async Task<bool> IsTeamBlacklistedAsync(int teamId)
        {
            return await _blacklistRepository.IsTeamBlacklistedAsync(teamId);
        }

        /// <summary>
        /// Adds a team to the blacklist.
        /// </summary>
        public async Task AddToBlacklistAsync(int teamId, int blacklistedBy, string reason)
        {
            if (await _blacklistRepository.IsTeamBlacklistedAsync(teamId))
            {
                throw new InvalidOperationException($"Team with ID {teamId} is already blacklisted.");
            }

            var blacklistEntry = new Blacklist
            {
                TeamID = teamId,
                BlacklistedBy = blacklistedBy,
                ReasonForBlacklisting = reason,
                BlacklistDate = DateTime.UtcNow
            };

            await _blacklistRepository.AddAsync(blacklistEntry);
        }

        /// <summary>
        /// Removes a team from the blacklist.
        /// </summary>
        public async Task RemoveFromBlacklistAsync(int teamId, int removedBy, string? notes = null)
        {
            var entry = await _blacklistRepository.GetByTeamIdAsync(teamId);
            if (entry == null)
            {
                throw new KeyNotFoundException($"Team with ID {teamId} is not blacklisted.");
            }

            entry.RemovalDate = DateTime.UtcNow;
            entry.RemovedBy = removedBy;
            entry.Notes = notes;

            await _blacklistRepository.RemoveAsync(entry.BlacklistID);
        }

        public async Task<List<Blacklist>> GetAllBlacklistedTeamsAsync(int systemUserId = 1)
        {
            try
            {
                // Make sure we have a valid user ID
                int validUserId = systemUserId > 0 ? systemUserId : 1;

                _logger.LogInformation("Getting all blacklisted teams with user ID {UserId}", validUserId);

                // First, check for overdue equipments and update blacklist status with a valid user ID
                await UpdateBlacklistStatusForOverdueEquipmentAsync(validUserId);

                // Get all blacklist entries including those that may have just been added
                var allEntries = await _blacklistRepository.GetAllAsync();
                _logger.LogInformation("Retrieved {Count} total blacklist entries", allEntries.Count);

                // Filter to active blacklist entries (RemovalDate is null)
                var activeEntries = allEntries.Where(b => b.RemovalDate == null).ToList();
                _logger.LogInformation("Found {Count} active blacklist entries", activeEntries.Count);

                // Log detailed information about all active blacklist entries
                foreach (var entry in activeEntries)
                {
                    _logger.LogInformation("Active blacklist entry: ID={Id}, TeamID={TeamId}, Date={Date}, Reason={Reason}",
                        entry.BlacklistID, entry.TeamID, entry.BlacklistDate, entry.ReasonForBlacklisting);
                }

                return activeEntries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blacklisted teams");
                return new List<Blacklist>();
            }
        }



        /// <summary>
        /// Retrieves all blacklisted teams, checking overdue equipment first and updating blacklist status.
        /// </summary>

        public async Task<IEnumerable<Blacklist>> GetActiveBlacklistsAsync(int systemUserId)
        {
            // Check overdue equipment first to ensure blacklist is up-to-date
            await UpdateBlacklistStatusForOverdueEquipmentAsync(systemUserId);

            // Get only active blacklist entries (RemovalDate is null)
            var allBlacklists = await _blacklistRepository.GetAllAsync();
            return allBlacklists.Where(b => b.RemovalDate == null);
        }


        /// <summary>
        /// Checks for overdue equipment and updates team blacklist status accordingly.
        /// </summary>
        // Update the UpdateBlacklistStatusForOverdueEquipmentAsync method in BlacklistService.cs
        private async Task UpdateBlacklistStatusForOverdueEquipmentAsync(int systemUserId)
        {
            try
            {
                _logger.LogInformation("Starting blacklist status update for teams with overdue equipment");

                // Ensure we have a valid user ID - never use 0
                int userIdToUse = systemUserId > 0 ? systemUserId : 1;
                _logger.LogInformation("Using user ID {UserId} for blacklist operations", userIdToUse);

                // Get current time for comparison
                var now = DateTime.UtcNow;

                // Define the overdue threshold (24 hours)
                var overdueThreshold = TimeSpan.FromHours(24); // This was seconds before - changed to hours
                _logger.LogInformation("Using overdue threshold of {Hours} hours", overdueThreshold.TotalHours);

                // Get all overdue checkouts
                var overdueCheckouts = await _checkoutRepository.GetOverdueAsync(overdueThreshold);
                _logger.LogInformation("Found {Count} total overdue items", overdueCheckouts.Count);

                // Log the IDs of all retrieved records
                if (overdueCheckouts.Any())
                {
                    _logger.LogInformation("Retrieved overdue record IDs: {RecordIds}",
                        string.Join(", ", overdueCheckouts.Select(r => r.Id)));
                }

                // Debugging: Check the database for active checkouts that might be overdue
                var activeCheckouts = await _checkoutRepository.GetActiveCheckoutsAsync();
                _logger.LogInformation("Found {Count} active checkouts in total", activeCheckouts.Count);

                var potentiallyOverdue = activeCheckouts.Where(c => c.ExpectedReturnDate < now).ToList();
                if (potentiallyOverdue.Any())
                {
                    _logger.LogInformation("Found {Count} potentially overdue active checkouts based on ExpectedReturnDate", potentiallyOverdue.Count);
                    foreach (var checkout in potentiallyOverdue)
                    {
                        _logger.LogInformation("Potentially overdue checkout: ID={Id}, TeamID={TeamId}, EquipmentId={EquipmentId}, ExpectedReturnDate={Date}",
                            checkout.CheckoutID, checkout.TeamID, checkout.EquipmentId, checkout.ExpectedReturnDate);
                    }
                }

                if (overdueCheckouts.Count == 0)
                {
                    _logger.LogInformation("No overdue equipment found, no blacklist updates needed");
                    return;
                }

                // Group overdue items by team
                var overdueTeamGroups = overdueCheckouts
                    .GroupBy(c => c.TeamId)
                    .ToList();

                _logger.LogInformation("Found {Count} teams with overdue equipment", overdueTeamGroups.Count);

                // Process each team with overdue equipment
                foreach (var teamGroup in overdueTeamGroups)
                {
                    int teamId = teamGroup.Key;

                    // Get team details
                    var team = await _teamRepository.GetByIntIdAsync(teamId);
                    if (team == null)
                    {
                        _logger.LogWarning("Team with ID {TeamId} not found, skipping blacklist check", teamId);
                        continue;
                    }

                    _logger.LogInformation("Processing team {TeamName} (ID: {TeamId}) with {Count} overdue items. Current blacklist status: {IsBlacklisted}",
                        team.TeamName, teamId, teamGroup.Count(), team.IsBlacklisted ? "Blacklisted" : "Not Blacklisted");

                    // Check if team is already blacklisted in the database
                    bool isAlreadyBlacklisted = await _blacklistRepository.IsTeamBlacklistedAsync(teamId);

                    if (isAlreadyBlacklisted != team.IsBlacklisted)
                    {
                        _logger.LogWarning("Inconsistency detected - DB blacklist status is {DbStatus} but team object status is {TeamStatus}",
                            isAlreadyBlacklisted ? "blacklisted" : "not blacklisted",
                            team.IsBlacklisted ? "blacklisted" : "not blacklisted");
                    }

                    // If team is not already blacklisted, add to blacklist
                    if (!isAlreadyBlacklisted)
                    {
                        // Find the oldest overdue item
                        var oldestOverdue = teamGroup.OrderBy(c => c.CheckedOutAt).First();

                        // Get equipment details if possible
                        string overdueEquipmentName = "Unknown Equipment";
                        try
                        {
                            if (oldestOverdue.Equipment != null)
                            {
                                overdueEquipmentName = oldestOverdue.Equipment.Name;
                            }
                            else
                            {
                                var equipment = await _equipmentRepository.GetEquipmentByIdAsync(oldestOverdue.EquipmentId);
                                if (equipment != null)
                                {
                                    overdueEquipmentName = equipment.Name;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error retrieving equipment name for Equipment ID {EquipmentId}", oldestOverdue.EquipmentId);
                        }

                        _logger.LogInformation("Creating blacklist entry for team {TeamName} (ID: {TeamId}) due to overdue equipment: {EquipmentName}",
                            team.TeamName, teamId, overdueEquipmentName);

                        // Create shorter reason and notes
                        string reason = $"Auto: Equipment overdue";
                        string notes = $"Equipment: {overdueEquipmentName}, ID: {oldestOverdue.EquipmentId}";

                        _logger.LogInformation("Creating blacklist with: UserID={UserId}, Reason='{Reason}', Notes='{Notes}'",
                            userIdToUse, reason, notes);

                        try
                        {
                            // Create blacklist entry with valid user ID and shorter reason
                            var blacklistEntry = new Blacklist
                            {
                                TeamID = teamId,
                                BlacklistedBy = userIdToUse, // Use valid user ID
                                ReasonForBlacklisting = reason, // Shorter reason
                                BlacklistDate = DateTime.UtcNow,
                                Notes = notes, // Details in notes
                                RemovalDate = null,
                                RemovedBy = null
                            };

                            _logger.LogInformation("About to save blacklist entry to database: {Entry}",
                                JsonSerializer.Serialize(blacklistEntry, new JsonSerializerOptions { WriteIndented = true }));

                            // Save to blacklist table
                            await _blacklistRepository.AddAsync(blacklistEntry);

                            // Update team status
                            team.IsBlacklisted = true;
                            await _teamRepository.UpdateAsync(team);

                            _logger.LogInformation("Team {TeamName} (ID: {TeamId}) has been blacklisted due to overdue equipment",
                                team.TeamName, teamId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to add blacklist entry for team {TeamId}. Error: {ErrorMessage}",
                                teamId, ex.Message);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Team {TeamName} (ID: {TeamId}) is already blacklisted",
                            team.TeamName, teamId);
                    }
                }

                _logger.LogInformation("Completed checking and updating blacklist status for teams with overdue equipment");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blacklist status for teams with overdue equipment");
                // Don't throw the exception - allow the caller to proceed with getting existing blacklist entries
            }
        }



        /// <summary>
        /// Retrieves the blacklist entry for a specific team.
        /// </summary>
        public async Task<Blacklist?> GetBlacklistEntryAsync(int teamId)
        {
            return await _blacklistRepository.GetByTeamIdAsync(teamId);
        }

    }
}
