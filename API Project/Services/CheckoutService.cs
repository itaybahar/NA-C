using API_Project.Repositories;
using DocumentFormat.OpenXml.Spreadsheet;
using Domain_Project.DTOs;
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
    public class CheckoutService : ICheckoutService
    {
        private readonly ICheckoutRepository _checkoutRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IBlacklistRepository _blacklistRepository;
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CheckoutService> _logger;
        private readonly IBlacklistService _blacklistService;

        public CheckoutService(
            ICheckoutRepository checkoutRepository,
            ITeamRepository teamRepository,
            IBlacklistRepository blacklistRepository,
            IEquipmentRepository equipmentRepository,
            IUnitOfWork unitOfWork,
            ILogger<CheckoutService> logger,
            IBlacklistService blacklistService)
        {
            _checkoutRepository = checkoutRepository;
            _teamRepository = teamRepository;
            _blacklistRepository = blacklistRepository;
            _equipmentRepository = equipmentRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _blacklistService = blacklistService;
        }

        public async Task<List<EquipmentCheckout>> GetAllCheckoutsAsync()
        {
            try
            {
                // Get all checkouts from the repository
                var checkouts = await _checkoutRepository.GetAllCheckoutsAsync();
                return checkouts.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all checkouts: {ErrorMessage}", ex.Message);
                return new List<EquipmentCheckout>();
            }
        }

        public async Task<EquipmentCheckout> GetCheckoutByIdAsync(int id)
        {
            try
            {
                return await _checkoutRepository.GetCheckoutByIdAsync(id) ??
                    new EquipmentCheckout { Status = "Unknown" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting checkout by ID {ID}: {ErrorMessage}", id, ex.Message);
                return new EquipmentCheckout { Status = "Unknown" };
            }
        }

        public async Task<bool> CreateCheckoutAsync(EquipmentCheckout checkout)
        {
            try
            {
                await _checkoutRepository.AddCheckoutAsync(checkout);
                await _unitOfWork.CompleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout: {ErrorMessage}", ex.Message);
                return false;
            }
        }


        public async Task<List<EquipmentCheckout>> GetActiveCheckoutsAsync()
        {
            try
            {
                return await _checkoutRepository.GetActiveCheckoutsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active checkouts: {ErrorMessage}", ex.Message);
                return new List<EquipmentCheckout>();
            }
        }

        public async Task<List<EquipmentCheckout>> GetOverdueCheckoutsAsync()
        {
            try
            {
                _logger.LogInformation("Starting overdue equipment check process");
                var now = DateTime.UtcNow;
                _logger.LogInformation("Current UTC time: {Time}", now);

                // Use 24 hours for threshold (not seconds)
                var overdueThreshold = TimeSpan.FromSeconds(24);
                _logger.LogInformation("Using overdue threshold of {Hours} hours", overdueThreshold.TotalHours);

                // Request overdue records from repository
                _logger.LogInformation("Requesting overdue records from repository");
                var overdueRecords = await _checkoutRepository.GetOverdueAsync(overdueThreshold);
                _logger.LogInformation("Retrieved {Count} potential overdue records", overdueRecords.Count);

                // Log the IDs of all retrieved records
                if (overdueRecords.Any())
                {
                    _logger.LogInformation("Retrieved record IDs: {RecordIds}",
                        string.Join(", ", overdueRecords.Select(r => r.Id)));
                }

                if (overdueRecords.Count == 0)
                {
                    _logger.LogInformation("No overdue equipment found");
                    return new List<EquipmentCheckout>();
                }

                // Convert CheckoutRecord objects to EquipmentCheckout objects
                _logger.LogInformation("Converting CheckoutRecord objects to EquipmentCheckout objects");
                List<EquipmentCheckout> overdueCheckouts = new List<EquipmentCheckout>();

                // Create test checkout if no real overdue checkouts
                if (!overdueRecords.Any())
                {
                    _logger.LogWarning("No real overdue records found - this may indicate a problem with overdue detection");
                }

                foreach (var record in overdueRecords)
                {
                    _logger.LogInformation("Processing overdue record: ID={Id}, EquipmentId={EquipmentId}, TeamId={TeamId}, CheckedOutAt={CheckedOutAt}",
                        record.Id, record.EquipmentId, record.TeamId, record.CheckedOutAt);

                    try
                    {
                        // Find the corresponding checkout in the database
                        int checkoutId = 0;
                        if (int.TryParse(record.Id, out checkoutId))
                        {
                            var checkout = await _checkoutRepository.GetCheckoutByIdAsync(checkoutId);

                            if (checkout != null)
                            {
                                _logger.LogInformation("Found corresponding EquipmentCheckout: CheckoutID={CheckoutId}, Status={Status}, ExpectedReturnDate={ExpectedReturnDate}, ActualReturnDate={ActualReturnDate}",
                                    checkout.CheckoutID, checkout.Status, checkout.ExpectedReturnDate, checkout.ReturnDate);

                                // Check that it hasn't been returned yet
                                if (checkout.Status != "Returned" && checkout.ReturnDate == null)
                                {
                                    // Confirm it's still overdue based on ExpectedReturnDate
                                    if (checkout.ExpectedReturnDate < now)
                                    {
                                        _logger.LogInformation("Checkout is confirmed overdue. ExpectedReturnDate: {ExpectedDate}, CurrentDate: {CurrentDate}, " +
                                            "DaysOverdue: {Days}",
                                            checkout.ExpectedReturnDate,
                                            now,
                                            (now - checkout.ExpectedReturnDate).TotalDays.ToString("F1"));

                                        overdueCheckouts.Add(checkout);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Checkout ID {CheckoutId} is not yet overdue: ExpectedReturnDate is {ExpectedDate}",
                                            checkout.CheckoutID, checkout.ExpectedReturnDate);
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Checkout ID {CheckoutId} is already returned or has ReturnDate set", checkout.CheckoutID);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Could not find EquipmentCheckout with ID {Id} in database", record.Id);
                            }
                        }
                        else
                        {
                            _logger.LogError("Failed to parse checkout ID: {Id} to integer", record.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing overdue record ID {Id}", record.Id);
                    }
                }

                _logger.LogInformation("Completed overdue equipment check. Found {Count} overdue items", overdueCheckouts.Count);

                // List equipment IDs and team IDs for troubleshooting
                if (overdueCheckouts.Any())
                {
                    var overdueDetails = overdueCheckouts.Select(oc =>
                        new { oc.CheckoutID, oc.EquipmentId, oc.TeamID, oc.CheckoutDate, oc.ExpectedReturnDate, DaysOverdue = (now - oc.ExpectedReturnDate).TotalDays });

                    _logger.LogInformation("Overdue details: {OverdueDetails}",
                        JsonSerializer.Serialize(overdueDetails, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    _logger.LogWarning("No overdue checkouts were found after filtering. Please check your checkout data.");
                }

                return overdueCheckouts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue checkouts: {ErrorMessage}", ex.Message);
                return new List<EquipmentCheckout>();
            }
        }


        public async Task<(bool Success, string? ErrorMessage, string? OverdueEquipmentName, List<EquipmentCheckout>? OverdueItems)> CheckoutEquipmentAsync(int teamId, int equipmentId, int userId, int quantity)
        {
            _logger.LogInformation("Starting CheckoutEquipmentAsync for Team ID: {TeamId}, Equipment ID: {EquipmentId}, User ID: {UserId}, Quantity: {Quantity}",
                teamId, equipmentId, userId, quantity);

            try
            {
                // Input validation
                if (teamId <= 0 || equipmentId <= 0 || userId <= 0 || quantity <= 0)
                {
                    _logger.LogWarning("Invalid input parameters: TeamId={TeamId}, EquipmentId={EquipmentId}, UserId={UserId}, Quantity={Quantity}",
                        teamId, equipmentId, userId, quantity);
                    return (false, "Invalid input parameters. All values must be greater than zero.", null, null);
                }

                // First, check if the team is already blacklisted
                _logger.LogDebug("Checking if team {TeamId} exists and is blacklisted", teamId);
                var team = await _teamRepository.GetByIntIdAsync(teamId);
                if (team == null)
                {
                    _logger.LogWarning("Team with ID {TeamId} not found", teamId);
                    return (false, $"Team with ID {teamId} not found.", null, null);
                }

                _logger.LogDebug("Team {TeamName} (ID: {TeamId}) found. Blacklist status: {IsBlacklisted}",
                    team.TeamName, teamId, team.IsBlacklisted);

                if (team.IsBlacklisted)
                {
                    // Get the blacklist entry
                    var blacklistEntry = await _blacklistRepository.GetByTeamIdAsync(teamId);
                    _logger.LogWarning("Team with ID {TeamId} is blacklisted and cannot check out equipment. Reason: {Reason}",
                        teamId, blacklistEntry?.ReasonForBlacklisting ?? "Unknown");

                    // Get the team's overdue items to identify the oldest one
                    var overdueItems = await GetOverdueItemsByTeamAsync(teamId);
                    string? overdueEquipmentName = null;

                    if (overdueItems.Any())
                    {
                        // Get the oldest overdue item
                        var oldestOverdue = overdueItems.OrderBy(e => e.CheckoutDate).First();

                        // Retrieve equipment name
                        try
                        {
                            if (oldestOverdue.Equipment != null)
                            {
                                overdueEquipmentName = oldestOverdue.Equipment.Name;
                            }
                            else
                            {
                                // If navigation property isn't loaded, try to get the equipment details
                                var overdueEquipment = await _equipmentRepository.GetEquipmentByIdAsync(oldestOverdue.EquipmentId);
                                if (overdueEquipment != null)
                                {
                                    overdueEquipmentName = overdueEquipment.Name;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error retrieving overdue equipment name for Equipment ID {EquipmentId}", oldestOverdue.EquipmentId);
                        }

                        _logger.LogInformation("Team {TeamId} is blacklisted with overdue equipment: {EquipmentName} (ID: {EquipmentId})",
                            teamId, overdueEquipmentName ?? "Unknown Equipment", oldestOverdue.EquipmentId);

                        try
                        {
                            _logger.LogInformation("add entry to blacklist table");

                            // First check if we already have a blacklist entry
                            var attemptedEquipment = await _equipmentRepository.GetEquipmentByIdAsync(equipmentId);
                            string equipmentName = attemptedEquipment?.Name ?? $"Equipment ID {equipmentId}";

                            // Create a new blacklist entry to track this failed attempt
                            var attemptEntry = new Blacklist
                            {
                                TeamID = teamId,
                                BlacklistedBy = userId,
                                ReasonForBlacklisting = $"Checkout attempt for {equipmentName} - Normal Operation",
                                BlacklistDate = DateTime.UtcNow,
                                Notes = $"Attempted to checkout {quantity} unit(s) of {equipmentName} (ID: {equipmentId})"
                            };

                            _logger.LogInformation("Team {TeamId} is blacklisted, added entry to blacklist table {attemptEntry}", teamId, attemptEntry);
                            // Add the entry to the repository
                            await _blacklistRepository.AddAsync(attemptEntry);

                            // Save changes
                            await _unitOfWork.CompleteAsync();

                            _logger.LogInformation("Added blacklist history entry for failed checkout attempt by team {TeamId} for equipment {EquipmentId}",
                                teamId, equipmentId);
                        }
                        catch (Exception ex)
                        {
                            // Log but don't fail the whole operation if recording the attempt fails
                            _logger.LogError(ex, "Failed to record blacklist history entry for team {TeamId}, equipment {EquipmentId}",
                                teamId, equipmentId);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Team {TeamId} is blacklisted but no specific overdue equipment was found", teamId);
                    }
                    // Attempt to record the failed checkout attempt in the blacklist history


                    return (false, $"Team {team.TeamName} is blacklisted and cannot check out equipment. Reason: {blacklistEntry?.ReasonForBlacklisting ?? "Unknown"}", overdueEquipmentName, overdueItems);
                }

                _logger.LogInformation("Team {TeamName} (ID: {TeamId}) is not blacklisted, proceeding with checkout",
                    team.TeamName, teamId);

                // Next, check for overdue equipment (more than 24 hours)
                _logger.LogDebug("Checking for overdue equipment for team ID {TeamId}", teamId);
                var overdueThreshold = TimeSpan.FromSeconds(24);
                _logger.LogDebug("Using overdue threshold of {Hours} hours", overdueThreshold.TotalHours);

                var overdueCheckouts = await _checkoutRepository.GetOverdueAsync(overdueThreshold);
                _logger.LogDebug("Found {Count} total overdue items across all teams", overdueCheckouts.Count);

                var teamOverdueItems = overdueCheckouts
                    .Where(c => c.TeamId == teamId)
                    .ToList();

                _logger.LogDebug("Team {TeamId} has {Count} overdue items", teamId, teamOverdueItems.Count);

                if (teamOverdueItems.Any())
                {
                    // Get the oldest overdue item
                    var oldestOverdue = teamOverdueItems.OrderBy(e => e.CheckedOutAt).First();
                    _logger.LogInformation("Team {TeamId} has overdue equipment. Oldest item: Equipment ID {EquipmentId}, Checked out at {CheckoutDate}",
                        teamId, oldestOverdue.EquipmentId, oldestOverdue.CheckedOutAt);

                    // Get equipment details to include in the response
                    string overdueEquipmentName = "Unknown Equipment";
                    try
                    {
                        if (oldestOverdue.Equipment != null)
                        {
                            overdueEquipmentName = oldestOverdue.Equipment.Name;
                        }
                        else
                        {
                            // If navigation property isn't loaded, try to get the equipment details
                            var overdueEquipment = await _equipmentRepository.GetEquipmentByIdAsync(oldestOverdue.EquipmentId);
                            if (overdueEquipment != null)
                            {
                                overdueEquipmentName = overdueEquipment.Name;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving overdue equipment name for Equipment ID {EquipmentId}", oldestOverdue.EquipmentId);
                        // Continue with the default name if there's an error
                    }

                    // Add to blacklist repository to create history entry
                    _logger.LogInformation("Creating blacklist entry for team {TeamId} due to overdue equipment", teamId);
                    var blacklistEntry = new Blacklist
                    {
                        TeamID = teamId,
                        BlacklistedBy = userId,
                        ReasonForBlacklisting = $"Automatic blacklist due to equipment not returned within 24 hours. Equipment: {overdueEquipmentName} (ID: {oldestOverdue.EquipmentId})",
                        BlacklistDate = DateTime.UtcNow
                    };

                    // Save to blacklist table through repository
                    _logger.LogDebug("Saving blacklist entry to database");
                    await _blacklistRepository.AddAsync(blacklistEntry);

                    // Update team status
                    _logger.LogDebug("Updating team {TeamId} blacklist status to true", teamId);
                    team.IsBlacklisted = true;
                    await _teamRepository.UpdateAsync(team);

                    // Save all changes
                    _logger.LogDebug("Committing changes to database");
                    await _unitOfWork.CompleteAsync();

                    _logger.LogWarning("Team with ID {TeamId} has overdue equipment and has been blacklisted", teamId);

                    // Format a user-friendly error message with equipment details
                    string errorMessage = $"Team {team.TeamName} has been blacklisted due to overdue equipment. " +
                        $"The oldest overdue item is {overdueEquipmentName}, " +
                        $"checked out on {oldestOverdue.CheckedOutAt:yyyy-MM-dd} " +
                        $"and is now {(DateTime.UtcNow - oldestOverdue.CheckedOutAt).TotalDays:F1} days overdue.";

                    // Get the full list of overdue items for this team
                    var fullOverdueItems = await GetOverdueItemsByTeamAsync(teamId);
                    return (false, errorMessage, overdueEquipmentName, fullOverdueItems);
                }

                // Check if equipment is available in sufficient quantity
                _logger.LogDebug("Checking if equipment ID {EquipmentId} exists and is available in sufficient quantity", equipmentId);
                var equipment = await _equipmentRepository.GetEquipmentByIdAsync(equipmentId);
                if (equipment == null)
                {
                    _logger.LogWarning("Equipment with ID {EquipmentId} not found", equipmentId);
                    return (false, $"Equipment with ID {equipmentId} not found.", null, null);
                }

                _logger.LogDebug("Equipment {EquipmentName} (ID: {EquipmentId}) found. Total quantity: {TotalQuantity}",
                    equipment.Name, equipmentId, equipment.Quantity);

                var inUseQuantity = await _checkoutRepository.GetInUseQuantityForEquipmentAsync(equipmentId);
                var availableQuantity = equipment.Quantity - inUseQuantity;

                _logger.LogDebug("Equipment ID {EquipmentId} has {InUseQuantity} units in use, {AvailableQuantity} units available",
                    equipmentId, inUseQuantity, availableQuantity);

                if (availableQuantity < quantity)
                {
                    _logger.LogWarning("Insufficient quantity available for equipment ID {EquipmentId}. Requested: {Requested}, Available: {Available}",
                        equipmentId, quantity, availableQuantity);
                    return (false, $"Insufficient quantity available for {equipment.Name}. Requested: {quantity}, Available: {availableQuantity}", null, null);
                }

                // Proceed with checkout if team is not blacklisted and has no overdue equipment
                _logger.LogInformation("All checks passed. Creating checkout record for team {TeamId}, equipment {EquipmentId}",
                    teamId, equipmentId);

                // Create the checkout record
                var checkout = new EquipmentCheckout
                {
                    EquipmentId = equipmentId,
                    TeamID = teamId,
                    UserID = userId,
                    Quantity = quantity,
                    ExpectedReturnDate = DateTime.UtcNow.AddSeconds(24),
                    CheckoutDate = DateTime.UtcNow,
                    Status = "CheckedOut",
                    Notes = "Checked out via system"
                };

                _logger.LogDebug("Adding checkout record to repository with expected return date {ExpectedReturnDate}",
                    checkout.ExpectedReturnDate);

                // Begin transaction to ensure data consistency
                try
                {
                    // First, add the checkout record
                    await _checkoutRepository.AddCheckoutAsync(checkout);

                    // Then, update the equipment quantity
                    equipment.Quantity -= quantity;

                    // If equipment quantity reaches zero, update status
                    if (equipment.Quantity == 0)
                    {
                        _logger.LogDebug("Equipment ID {EquipmentId} is now out of stock. Updating status to 'Unavailable'", equipmentId);
                        equipment.Status = "Unavailable";
                    }

                    // Update the equipment in the database
                    await _equipmentRepository.UpdateAsync(equipment);

                    // Commit all changes as a single transaction
                    _logger.LogDebug("Committing checkout transaction to database");
                    await _unitOfWork.CompleteAsync();

                    _logger.LogInformation("Successfully checked out {Quantity} unit(s) of equipment ID {EquipmentId} for team ID {TeamId}, checkout ID {CheckoutId}",
                        quantity, equipmentId, teamId, checkout.CheckoutID);
                    return (true, null, null, null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing checkout transaction: {ErrorMessage}", ex.Message);
                    return (false, $"Error processing checkout: {ex.Message}", null, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CheckoutEquipmentAsync for Team {TeamId}, Equipment {EquipmentId}: {ErrorMessage}",
                    teamId, equipmentId, ex.Message);

                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionMessage}", ex.InnerException.Message);
                }

                return (false, $"An unexpected error occurred: {ex.Message}", null, null);
            }
        }

        public async Task<List<CheckoutRecord>> GetUnreturnedByTeamAsync(string teamId)
        {
            _logger.LogInformation("Getting unreturned items for team ID {TeamID}", teamId);

            try
            {
                _logger.LogDebug("Calling repository to get unreturned items for team {TeamID}", teamId);
                var items = await _checkoutRepository.GetUnreturnedItemsForTeamAsync(teamId);
                _logger.LogInformation("Found {Count} unreturned items for team {TeamID}", items.Count, teamId);

                if (items.Any())
                {
                    _logger.LogDebug("Unreturned equipment IDs for team {TeamID}: {EquipmentIds}",
                        teamId, string.Join(", ", items.Select(i => i.EquipmentId)));
                }

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unreturned equipment for team {TeamID}: {ErrorMessage}",
                    teamId, ex.Message);
                return new List<CheckoutRecord>();
            }
        }

        public async Task CheckoutItemAsync(string teamId, string equipmentId)
        {
            _logger.LogInformation("Starting CheckoutItemAsync for Team ID: {TeamID}, Equipment ID: {EquipmentID}",
                teamId, equipmentId);

            try
            {
                _logger.LogDebug("Validating teamId and equipmentId parameters");
                if (int.TryParse(equipmentId, out int eqId) && int.TryParse(teamId, out int tId))
                {
                    _logger.LogDebug("Parameters validated. Team ID: {TeamID}, Equipment ID: {EquipmentID}", tId, eqId);

                    var checkout = new EquipmentCheckout
                    {
                        EquipmentId = eqId,
                        TeamID = tId,
                        UserID = 1, // Default user ID
                        Quantity = 1,
                        CheckoutDate = DateTime.UtcNow,
                        ExpectedReturnDate = DateTime.UtcNow.AddDays(7),
                        Status = "CheckedOut"
                    };

                    _logger.LogDebug("Creating checkout record with expected return date: {ExpectedReturnDate}", checkout.ExpectedReturnDate);
                    await _checkoutRepository.AddCheckoutAsync(checkout);

                    _logger.LogDebug("Committing changes to database");
                    await _unitOfWork.CompleteAsync();

                    _logger.LogInformation("Successfully checked out equipment ID {EquipmentID} for team ID {TeamID}",
                        equipmentId, teamId);
                }
                else
                {
                    _logger.LogWarning("Invalid parameters. Team ID: '{TeamID}', Equipment ID: '{EquipmentID}'",
                        teamId, equipmentId);
                    throw new ArgumentException("Invalid teamId or equipmentId");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking out item (Team: {TeamID}, Equipment: {EquipmentID}): {ErrorMessage}",
                    teamId, equipmentId, ex.Message);
                throw;
            }
        }

        // Update AutoBlacklistOverdueAsync method in CheckoutService.cs
        public async Task AutoBlacklistOverdueAsync(int systemUserId = 1)
        {
            try
            {
                _logger.LogInformation("Starting automatic blacklisting process for overdue equipment");

                // Ensure we have a valid user ID - never use 0
                int userIdToUse = systemUserId > 0 ? systemUserId : 1;

                // Get overdue checkouts
                var overdueThreshold = TimeSpan.Zero; // Just a placeholder, using ExpectedReturnDate for comparison
                _logger.LogInformation("Requesting overdue records from repository");
                var overdueCheckouts = await _checkoutRepository.GetOverdueAsync(overdueThreshold);
                _logger.LogInformation("Found {Count} overdue records", overdueCheckouts.Count);

                if (overdueCheckouts.Count == 0)
                {
                    _logger.LogInformation("No overdue equipment to process");
                    return;
                }

                // Group by team to handle each team once
                _logger.LogInformation("Grouping overdue items by team");
                var overdueTeams = overdueCheckouts
                    .GroupBy(c => c.TeamId)
                    .Select(g => new {
                        TeamId = g.Key,
                        ItemCount = g.Count(),
                        OldestCheckout = g.OrderBy(c => c.CheckedOutAt).First()
                    })
                    .ToList();

                _logger.LogInformation("Found {Count} teams with overdue equipment", overdueTeams.Count);

                // Log team details
                foreach (var teamData in overdueTeams)
                {
                    _logger.LogInformation("Team ID {TeamId} has {Count} overdue item(s). Oldest checkout date: {OldestDate}",
                        teamData.TeamId, teamData.ItemCount, teamData.OldestCheckout.CheckedOutAt);
                }

                int blacklistedCount = 0;
                foreach (var teamData in overdueTeams)
                {
                    _logger.LogInformation("Processing team ID {TeamId}", teamData.TeamId);

                    var team = await _teamRepository.GetByIntIdAsync(teamData.TeamId);
                    if (team == null)
                    {
                        _logger.LogWarning("Team with ID {TeamId} not found in database", teamData.TeamId);
                        continue;
                    }

                    _logger.LogInformation("Team {TeamName} (ID: {TeamId}) found. Current blacklist status: {IsBlacklisted}",
                        team.TeamName, team.TeamID, team.IsBlacklisted);

                    if (!team.IsBlacklisted)
                    {
                        _logger.LogInformation("Creating blacklist record for team {TeamName} (ID: {TeamId})",
                            team.TeamName, team.TeamID);

                        try
                        {
                            // Create a shorter reason string that won't exceed the database column length
                            string reason = "Automatic blacklist: Equipment overdue";
                            string notes = $"Equipment ID: {teamData.OldestCheckout.EquipmentId}, Expected Return: {teamData.OldestCheckout.CheckedOutAt.AddDays(7):yyyy-MM-dd}";

                            // Create blacklist record
                            var blacklist = new Blacklist
                            {
                                TeamID = team.TeamID,
                                BlacklistedBy = userIdToUse, // Using valid user ID
                                ReasonForBlacklisting = reason, // Shorter reason
                                Notes = notes, // Additional details in notes
                                BlacklistDate = DateTime.UtcNow
                            };

                            _logger.LogInformation("Creating blacklist record with: TeamID={TeamId}, BlacklistedBy={UserId}, Reason={Reason}",
                                blacklist.TeamID, blacklist.BlacklistedBy, blacklist.ReasonForBlacklisting);

                            await _blacklistRepository.AddAsync(blacklist);
                            _logger.LogInformation("Blacklist record created for team {TeamName} (ID: {TeamId})",
                                team.TeamName, team.TeamID);

                            // Update team status
                            team.IsBlacklisted = true;
                            await _teamRepository.UpdateAsync(team);
                            _logger.LogInformation("Updated team {TeamName} (ID: {TeamId}) blacklist status to true",
                                team.TeamName, team.TeamID);

                            blacklistedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error creating blacklist entry for team {TeamId}: {ErrorMessage}",
                                team.TeamID, ex.Message);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Team {TeamName} (ID: {TeamId}) is already blacklisted, no action needed",
                            team.TeamName, team.TeamID);
                    }
                }

                _logger.LogInformation("Saving changes to database");
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Auto-blacklisting process completed. {Count} teams were blacklisted", blacklistedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-blacklisting overdue checkouts: {ErrorMessage}", ex.Message);
                throw;
            }
        }



        public async Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync()
        {
            try
            {
                var history = await _checkoutRepository.GetCheckoutHistoryAsync();
                return history ?? new List<CheckoutRecordDto>(); // Ensure we return an empty list, not null
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting checkout history: {ErrorMessage}", ex.Message);
                return new List<CheckoutRecordDto>(); // Return empty list on error
            }
        }


        /// <summary>
        /// Gets the quantity of equipment currently in use (checked out)
        /// </summary>
        /// <param name="equipmentId">The ID of the equipment to check</param>
        /// <returns>The quantity of items currently checked out</returns>
        public async Task<int> GetInUseQuantityForEquipmentAsync(int equipmentId)
        {
            _logger.LogInformation("Getting in-use quantity for equipment ID {EquipmentId}", equipmentId);

            try
            {
                var inUseQuantity = await _checkoutRepository.GetInUseQuantityForEquipmentAsync(equipmentId);
                _logger.LogDebug("Equipment ID {EquipmentId} has {Quantity} units currently in use", equipmentId, inUseQuantity);
                return inUseQuantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-use quantity for equipment {EquipmentId}: {ErrorMessage}",
                    equipmentId, ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Returns equipment that was previously checked out
        /// </summary>
        /// <param name="checkoutId">The ID of the checkout record to return</param>
        /// <returns>True if the return was successful, false otherwise</returns>
        public async Task<bool> ReturnEquipmentAsync(int checkoutId)
        {
            _logger.LogInformation("Processing return request for checkout ID {CheckoutID}", checkoutId);

            try
            {
                // Get the checkout record
                var checkout = await _checkoutRepository.GetCheckoutByIdAsync(checkoutId);
                if (checkout == null)
                {
                    _logger.LogWarning("Return failed: Checkout ID {CheckoutID} not found", checkoutId);
                    return false;
                }

                _logger.LogDebug("Found checkout record: ID={CheckoutID}, EquipmentId={EquipmentId}, TeamId={TeamId}, Quantity={Quantity}, Status={Status}",
                    checkout.CheckoutID, checkout.EquipmentId, checkout.TeamID, checkout.Quantity, checkout.Status);

                // Check if it's already returned
                if (checkout.Status == "Returned")
                {
                    _logger.LogWarning("Checkout ID {CheckoutID} is already marked as returned", checkoutId);
                    return true; // Already returned, consider the operation successful
                }

                // Capture team ID for later blacklist check
                int teamId = checkout.TeamID;

                // Handle quantity - even if quantity is 0, process the return
                int returnQuantity = Math.Max(0, checkout.Quantity); // Ensure non-negative quantity

                // Update checkout status
                checkout.ActualReturnDate = DateTime.UtcNow;
                checkout.ReturnDate = DateTime.UtcNow;
                checkout.Status = "Returned";

                _logger.LogDebug("Updating checkout record with return date {ReturnDate}", checkout.ReturnDate);

                // Update the database
                await _checkoutRepository.UpdateCheckoutAsync(checkout);

                // Update equipment quantity in database
                var equipment = await _equipmentRepository.GetEquipmentByIdAsync(checkout.EquipmentId);
                if (equipment != null && returnQuantity > 0) // Only update equipment if there's something to return
                {
                    _logger.LogDebug("Updating equipment quantity. Current: {CurrentQuantity}, Returning: {ReturningQuantity}",
                        equipment.Quantity, returnQuantity);

                    equipment.Quantity += returnQuantity;

                    // If equipment was unavailable and now has items, mark as available
                    if (equipment.Status == "Unavailable" && equipment.Quantity > 0)
                    {
                        _logger.LogDebug("Changing equipment status from 'Unavailable' to 'Available'");
                        equipment.Status = "Available";
                    }

                    await _equipmentRepository.UpdateAsync(equipment);
                }
                else if (returnQuantity == 0)
                {
                    _logger.LogInformation("Equipment quantity is 0, no inventory adjustment needed");
                }

                // Equipment return is now complete, check if the team is blacklisted
                var team = await _teamRepository.GetByIntIdAsync(teamId);

                if (team != null && team.IsBlacklisted)
                {
                    _logger.LogInformation("Team {TeamId} is blacklisted, checking if it should be un-blacklisted", teamId);

                    // Get all overdue items for this team
                    var overdueItems = await GetOverdueItemsByTeamAsync(teamId);

                    // Check if the team has any overdue items at all (no time threshold)
                    var now = DateTime.UtcNow;
                    bool hasOverdueItems = overdueItems.Any(item =>
                        item.ExpectedReturnDate < now &&
                        item.Status != "Returned");

                    // If no items are overdue, un-blacklist the team
                    if (!hasOverdueItems)
                    {
                        _logger.LogInformation("Team {TeamId} has no overdue equipment, un-blacklisting", teamId);

                        // Use the BlacklistService to properly remove from blacklist
                        try
                        {
                            await _blacklistService.RemoveFromBlacklistAsync(
                                teamId, 
                                checkout.UserID, 
                                $"Auto-removed from blacklist after returning equipment ID {checkout.EquipmentId} (Checkout ID: {checkout.CheckoutID})"
                            );

                            // Update team status
                            team.IsBlacklisted = false;
                            await _teamRepository.UpdateAsync(team);

                            _logger.LogInformation("Team {TeamId} has been un-blacklisted", teamId);

                            // Log message that would have been returned - now we just log it
                            _logger.LogInformation("Team un-blacklisted message: {Message}",
                                $"צוות {team.TeamName} הוסר מהרשימה השחורה וכעת רשאי לשאול ציוד נוסף");
                        }
                        catch (KeyNotFoundException)
                        {
                            // Team wasn't in blacklist table, just update team status
                            _logger.LogWarning("Team {TeamId} not found in blacklist table, only updating team status", teamId);
                            team.IsBlacklisted = false;
                            await _teamRepository.UpdateAsync(team);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Team {TeamId} still has {Count} overdue items, remaining blacklisted",
                            teamId, overdueItems.Count(item =>
                                item.ExpectedReturnDate < now &&
                                item.Status != "Returned"));
                    }
                }

                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Successfully returned equipment for checkout ID {CheckoutID}. Equipment ID: {EquipmentId}, Quantity: {Quantity}",
                    checkoutId, checkout.EquipmentId, returnQuantity);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning equipment (ID: {CheckoutID}): {ErrorMessage}",
                    checkoutId, ex.Message);
                return false;
            }
        }



        /// <summary>
        /// Gets the available quantity for a specific equipment item
        /// </summary>
        /// <param name="equipmentId">The ID of the equipment</param>
        /// <param name="totalQuantity">The total quantity of the equipment</param>
        /// <returns>The available quantity that can be checked out</returns>
        public async Task<int> GetAvailableQuantityForEquipmentAsync(int equipmentId, int totalQuantity)
        {
            _logger.LogInformation("Calculating available quantity for equipment ID {EquipmentId}", equipmentId);

            try
            {
                // Get the in-use quantity
                var inUseQuantity = await GetInUseQuantityForEquipmentAsync(equipmentId);

                // Calculate available quantity
                var availableQuantity = Math.Max(0, totalQuantity - inUseQuantity);

                _logger.LogDebug("Equipment ID {EquipmentId}: Total={Total}, InUse={InUse}, Available={Available}",
                    equipmentId, totalQuantity, inUseQuantity, availableQuantity);

                return availableQuantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating available quantity for equipment {EquipmentId}: {ErrorMessage}",
                    equipmentId, ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Adds a checkout record from admin history
        /// </summary>
        /// <param name="record">The checkout record data</param>
        /// <returns>True if the record was added successfully, false otherwise</returns>
        public async Task<bool> AddAdminHistoryRecordAsync(CheckoutRecordDto record)
        {
            _logger.LogInformation("Adding admin history record for equipment ID {EquipmentId}, team ID {TeamId}",
                record.EquipmentId, record.TeamId);

            try
            {
                // Validate the equipment ID
                if (!int.TryParse(record.EquipmentId, out int equipmentId))
                {
                    _logger.LogWarning("Invalid equipment ID format: {EquipmentId}", record.EquipmentId);
                    return false;
                }

                // Create the checkout record
                var checkout = new EquipmentCheckout
                {
                    EquipmentId = equipmentId,
                    TeamID = record.TeamId,
                    UserID = record.UserId,
                    Quantity = record.Quantity,
                    CheckoutDate = record.CheckedOutAt ?? DateTime.UtcNow,
                    ReturnDate = record.ReturnedAt,
                    ExpectedReturnDate = record.ReturnedAt?.AddDays(-1) ?? DateTime.UtcNow.AddDays(7),
                    Status = record.ReturnedAt.HasValue ? "Returned" : "CheckedOut",
                    Notes = record.ItemNotes ?? "Admin record",
                    ReturnCondition = record.ItemCondition ?? "Good"
                };

                _logger.LogDebug("Creating checkout record with status {Status}, checkout date {CheckoutDate}, return date {ReturnDate}",
                    checkout.Status, checkout.CheckoutDate, checkout.ReturnDate);

                // Save to database
                await _checkoutRepository.AddCheckoutAsync(checkout);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Successfully added admin history record for equipment ID {EquipmentId}, team ID {TeamId}",
                    record.EquipmentId, record.TeamId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding admin history record for equipment {EquipmentId}: {ErrorMessage}",
                    record.EquipmentId, ex.Message);
                return false;
            }
        }
        /// <summary>
        /// Retrieves a team by its ID
        /// </summary>
        /// <param name="teamId">The ID of the team to retrieve</param>
        /// <returns>The team if found, null otherwise</returns>
        public async Task<Team> GetTeamByIdAsync(int teamId)
        {
            try
            {
                _logger.LogInformation("Getting team by ID {TeamId}", teamId);
                var team = await _teamRepository.GetByIntIdAsync(teamId);

                if (team == null)
                {
                    _logger.LogWarning("Team with ID {TeamId} not found", teamId);
                }
                else
                {
                    _logger.LogDebug("Retrieved team: ID={TeamId}, Name={TeamName}, IsBlacklisted={IsBlacklisted}",
                        teamId, team.TeamName, team.IsBlacklisted);
                }

                return team;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team by ID {TeamId}: {ErrorMessage}", teamId, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets all overdue items for a specific team
        /// </summary>
        /// <param name="teamId">The ID of the team</param>
        /// <returns>A list of overdue equipment checkouts</returns>
        public async Task<List<EquipmentCheckout>> GetOverdueItemsByTeamAsync(int teamId)
        {
            try
            {
                _logger.LogInformation("Getting overdue items for team ID {TeamId}", teamId);

                // Get all overdue checkouts
                var allOverdueCheckouts = await GetOverdueCheckoutsAsync();

                // Filter to just this team's overdue items
                var teamOverdueItems = allOverdueCheckouts
                    .Where(c => c.TeamID == teamId)
                    .ToList();

                _logger.LogDebug("Found {Count} overdue items for team ID {TeamId}",
                    teamOverdueItems.Count, teamId);

                // Load equipment details for each checkout if not already loaded
                foreach (var item in teamOverdueItems)
                {
                    if (item.Equipment == null)
                    {
                        try
                        {
                            var equipment = await _equipmentRepository.GetEquipmentByIdAsync(item.EquipmentId);
                            if (equipment != null)
                            {
                                item.Equipment = equipment;
                                _logger.LogDebug("Loaded equipment details for checkout ID {CheckoutId}: {EquipmentName}",
                                    item.CheckoutID, equipment.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to load equipment details for checkout ID {CheckoutId}",
                                item.CheckoutID);
                        }
                    }
                }

                return teamOverdueItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue items for team {TeamId}: {ErrorMessage}",
                    teamId, ex.Message);
                return new List<EquipmentCheckout>();
            }
        }

        public async Task ProcessEquipmentReturn(int teamId)
        {
            // 1. Mark the equipment as returned (your existing logic)

            // 2. Check if the team has any equipment still checked out
            bool hasOutstanding = await _checkoutRepository.GetInUseQuantityForEquipmentAsync(teamId) > 0;

            if (!hasOutstanding)
            {
                // 3. Un-blacklist the team
                var team = await _teamRepository.GetByIntIdAsync(teamId);
                if (team != null && team.IsBlacklisted)
                {
                    team.IsBlacklisted = false;
                    await _teamRepository.UpdateAsync(team);
                }
            }
        }

        public async Task<List<BlacklistedTeamDto>> GetBlacklistedTeamsAsync()
        {
            try
            {
                _logger.LogInformation("Getting all blacklisted teams");
                var blacklistedTeams = await _teamRepository.GetBlacklistedTeamsAsync();
                var result = new List<BlacklistedTeamDto>();

                foreach (var team in blacklistedTeams)
                {
                    var blacklistEntry = await _blacklistRepository.GetByTeamIdAsync(team.TeamID);
                    var overdueItems = await GetOverdueItemsByTeamAsync(team.TeamID);

                    var blacklistedTeam = new BlacklistedTeamDto
                    {
                        TeamId = team.TeamID,
                        TeamName = team.TeamName,
                        BlacklistReason = blacklistEntry?.ReasonForBlacklisting ?? "Unknown",
                        BlacklistDate = blacklistEntry?.BlacklistDate ?? DateTime.UtcNow,
                        OverdueItems = overdueItems.Select(item => new OverdueItemDto
                        {
                            EquipmentId = item.EquipmentId,
                            EquipmentName = item.Equipment?.Name ?? "Unknown",
                            CheckoutId = item.CheckoutID,
                            CheckoutDate = item.CheckoutDate,
                            ExpectedReturnDate = item.ExpectedReturnDate,
                            DaysOverdue = (DateTime.UtcNow - item.ExpectedReturnDate).TotalDays
                        }).ToList()
                    };

                    result.Add(blacklistedTeam);
                }

                _logger.LogInformation("Found {Count} blacklisted teams", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blacklisted teams: {ErrorMessage}", ex.Message);
                return new List<BlacklistedTeamDto>();
            }
        }

    }
}
