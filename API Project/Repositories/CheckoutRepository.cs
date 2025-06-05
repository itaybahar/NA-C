using API_Project.Data;
using Domain_Project.DTOs;
using Domain_Project.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_Project.Repositories
{
    public class CheckoutRepository : ICheckoutRepository
    {
        private readonly EquipmentManagementContext _dbContext;

        private readonly ILogger<CheckoutRepository> _logger;

        public CheckoutRepository(
            EquipmentManagementContext dbContext,
            ILogger<CheckoutRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> HasUnreturnedItemsAsync(string teamId)
        {
            if (int.TryParse(teamId, out int numericTeamId))
            {
                // Use the EquipmentCheckouts table to check for unreturned items
                return await _dbContext.EquipmentCheckouts
                    .AnyAsync(ec => ec.TeamID == numericTeamId && ec.Status == "CheckedOut");
            }
            return false;
        }

        public async Task AddCheckoutAsync(Checkout checkout)
        {
            // Convert the Checkout model to EquipmentCheckout entity
            var equipmentCheckout = new EquipmentCheckout
            {
                EquipmentId = checkout.EquipmentId, 
                TeamID = checkout.TeamId,
                UserID = checkout.UserId,
                CheckoutDate = checkout.CheckoutDate,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7), // Default to 7 days
                Status = "CheckedOut",
                Notes = "Created from Checkout object",
                Quantity = 1 // Default to 1 if not specified
            };

            await _dbContext.EquipmentCheckouts.AddAsync(equipmentCheckout);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<Checkout>> GetCheckoutsByTeamIdAsync(string teamId)
        {
            if (int.TryParse(teamId, out int numericTeamId))
            {
                // Query EquipmentCheckouts and map to Checkout objects
                var checkouts = await _dbContext.EquipmentCheckouts
                    .Where(ec => ec.TeamID == numericTeamId)
                    .Join(
                        _dbContext.Teams,
                        ec => ec.TeamID,
                        t => t.TeamID,
                        (ec, t) => new Checkout
                        {
                            Id = ec.CheckoutID,
                            EquipmentId = ec.EquipmentId, // Changed from EquipmentID to EquipmentId
                            TeamId = ec.TeamID,
                            Team = t, // Set the required Team property
                            UserId = ec.UserID,
                            CheckoutDate = ec.CheckoutDate, // Removed null coalescing operator since CheckoutDate is required
                            ReturnDate = ec.ActualReturnDate,
                            IsReturned = ec.Status == "Returned",
                            Quantity = ec.Quantity // Include quantity information
                        }
                    )
                    .ToListAsync();

                return checkouts;
            }
            return Enumerable.Empty<Checkout>();
        }

        public async Task<int?> GetCheckoutIdByTeamAndEquipmentAsync(int teamId, int equipmentId)
        {
            var checkout = await _dbContext.EquipmentCheckouts
                .Where(ec => ec.TeamID == teamId &&
                             ec.EquipmentId == equipmentId && // Changed from EquipmentID to EquipmentId
                             ec.Status == "CheckedOut")
                .OrderByDescending(ec => ec.CheckoutDate)
                .FirstOrDefaultAsync();

            return checkout?.CheckoutID;
        }

        public async Task<EquipmentCheckout?> GetCheckoutByIdAsync(int checkoutId)
        {
            return await _dbContext.EquipmentCheckouts.FindAsync(checkoutId);
        }

        public async Task MarkAsReturnedAsync(int checkoutId, int quantity = 0, string condition = "Good", string notes = "")
        {
            var checkout = await _dbContext.EquipmentCheckouts.FindAsync(checkoutId);
            if (checkout != null)
            {
                // Get the equipment
                var equipment = await _dbContext.Equipment.FindAsync(checkout.EquipmentId); // Changed from EquipmentID to EquipmentId
                if (equipment == null)
                {
                    throw new InvalidOperationException($"Equipment with ID {checkout.EquipmentId} not found."); // Changed from EquipmentID to EquipmentId
                }

                // Determine the quantity to return
                int returnQuantity = quantity > 0 ? quantity : checkout.Quantity;

                // Ensure return quantity doesn't exceed the checked out quantity
                if (returnQuantity > checkout.Quantity)
                {
                    returnQuantity = checkout.Quantity;
                }

                if (returnQuantity < checkout.Quantity)
                {
                    // Partial return
                    checkout.Quantity -= returnQuantity;

                    // Create a new record for the returned items
                    var returnedCheckout = new EquipmentCheckout
                    {
                        EquipmentId = checkout.EquipmentId, // Changed from EquipmentID to EquipmentId
                        TeamID = checkout.TeamID,
                        UserID = checkout.UserID,
                        CheckoutDate = checkout.CheckoutDate,
                        ExpectedReturnDate = checkout.ExpectedReturnDate,
                        ActualReturnDate = DateTime.UtcNow,
                        Status = "Returned",
                        Notes = string.IsNullOrEmpty(notes) ? "Partial return" : notes,
                        ReturnCondition = condition,
                        Quantity = returnQuantity
                    };

                    await _dbContext.EquipmentCheckouts.AddAsync(returnedCheckout);
                }
                else
                {
                    // Full return
                    checkout.Status = "Returned";
                    checkout.ActualReturnDate = DateTime.UtcNow;
                    checkout.ReturnCondition = condition;

                    if (!string.IsNullOrEmpty(notes))
                    {
                        checkout.Notes = notes;
                    }
                }

                // Update equipment quantity
                equipment.Quantity += returnQuantity;
                if (equipment.Quantity > 0 && equipment.Status == "Unavailable")
                {
                    equipment.Status = "Available";
                }

                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task AddAsync(CheckoutRecord record)
        {
            // Convert the CheckoutRecord to an EquipmentCheckout
            var equipmentCheckout = new EquipmentCheckout
            {
                EquipmentId = record.EquipmentId,
                TeamID = record.TeamId,
                UserID = record.UserId,
                CheckoutDate = record.CheckedOutAt,
                ExpectedReturnDate = record.CheckedOutAt.AddDays(7), // Default to 7 days from checkout
                ActualReturnDate = record.ReturnedAt,
                Status = record.ReturnedAt.HasValue ? "Returned" : "CheckedOut",
                Quantity = record.Quantity,
                Notes = "Created from CheckoutRecord"
            };

            await _dbContext.EquipmentCheckouts.AddAsync(equipmentCheckout);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<CheckoutRecordDto>> GetByTeamIdAsync(string teamId)
        {
            try
            {
                // Parse the team ID
                if (!int.TryParse(teamId, out int numericTeamId))
                {
                    throw new ArgumentException($"Invalid team ID: {teamId}");
                }

                // Use raw SQL to avoid EF Core issues
                var query = @"
            SELECT 
                CAST(ec.CheckoutID AS CHAR) AS Id,
                ec.EquipmentID AS EquipmentId,
                e.Name AS EquipmentName, 
                ec.TeamID AS TeamId,
                t.TeamName,
                ec.UserID AS UserId,
                ec.CheckoutDate AS CheckedOutAt,
                ec.ActualReturnDate AS ReturnedAt,
                ec.ExpectedReturnDate,
                COALESCE(ec.Quantity, 1) AS Quantity
            FROM EquipmentCheckouts ec
            INNER JOIN Equipment e ON ec.EquipmentID = e.Id
            INNER JOIN Teams t ON ec.TeamID = t.TeamID
            WHERE ec.TeamID = @teamId";

                // Execute the query directly
                var connection = _dbContext.Database.GetDbConnection();
                await connection.OpenAsync();

                var result = new List<CheckoutRecordDto>();

                using var command = connection.CreateCommand();
                command.CommandText = query;

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@teamId";
                parameter.Value = numericTeamId;
                command.Parameters.Add(parameter);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new CheckoutRecordDto
                    {
                        Id = reader["Id"].ToString(),
                        EquipmentId = reader["EquipmentId"].ToString(),
                        EquipmentName = reader["EquipmentName"].ToString(),
                        TeamId = Convert.ToInt32(reader["TeamId"]),
                        TeamName = reader["TeamName"].ToString(),
                        UserId = Convert.ToInt32(reader["UserId"]),
                        CheckedOutAt = reader["CheckedOutAt"] != DBNull.Value ?
                            Convert.ToDateTime(reader["CheckedOutAt"]) : null,
                        ReturnedAt = reader["ReturnedAt"] != DBNull.Value ?
                            Convert.ToDateTime(reader["ReturnedAt"]) : null,
                        ExpectedReturnDate = reader["ExpectedReturnDate"] != DBNull.Value ?
                            Convert.ToDateTime(reader["ExpectedReturnDate"]) : null,
                        Quantity = reader["Quantity"] != DBNull.Value ?
                            Convert.ToInt32(reader["Quantity"]) : 1
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetByTeamIdAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex, "Inner exception: {ex.InnerException.Message}");
                }
                throw; // Let the controller handle it
            }
        }



        public async Task<List<CheckoutRecord>> GetOverdueAsync(TimeSpan overdueTime)
        {
            var now = DateTime.UtcNow;
            _logger.LogInformation("Executing GetOverdueAsync...");


            // Use ExpectedReturnDate for overdue check instead of threshold
            var overdueRecords = await _dbContext.EquipmentCheckouts
                .Where(ec => ec.Status == "CheckedOut" && (ec.ExpectedReturnDate < now ||  now - ec.CheckoutDate > overdueTime))
                .Join(
                    _dbContext.Teams,
                    ec => ec.TeamID,
                    t => t.TeamID,
                    (ec, t) => new { Checkout = ec, Team = t }
                )
                .Join(
                    _dbContext.Equipment,
                    joined => joined.Checkout.EquipmentId,
                    e => e.Id,
                    (joined, e) => new CheckoutRecord
                    {
                        Id = joined.Checkout.CheckoutID.ToString(),
                        EquipmentId = joined.Checkout.EquipmentId,
                        Equipment = e,
                        TeamId = joined.Checkout.TeamID,
                        Team = joined.Team,
                        UserId = joined.Checkout.UserID,
                        CheckedOutAt = joined.Checkout.CheckoutDate,
                        ReturnedAt = null,
                        Quantity = joined.Checkout.Quantity
                    }
                )
                .ToListAsync();

            _logger.LogInformation($"Found {overdueRecords.Count} overdue records based on ExpectedReturnDate");
            return overdueRecords;
        }


        public async Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync()
        {
            try
            {
                _logger.LogInformation("Executing GetCheckoutHistoryAsync...");

                // Use raw SQL with proper null handling for all nullable columns
                var query = @"
            SELECT 
                CAST(ec.CheckoutID AS CHAR) AS Id,
                CAST(ec.EquipmentID AS CHAR) AS EquipmentId,
                ec.TeamID,
                ec.UserID,
                ec.CheckoutDate AS CheckedOutAt,
                ec.ActualReturnDate AS ReturnedAt,
                ec.ExpectedReturnDate,
                e.Name AS EquipmentName,
                t.TeamName,
                u.FirstName,
                u.LastName,
                u.Role AS UserRole,
                COALESCE(ec.Quantity, 1) AS Quantity,
                COALESCE(ec.ReturnCondition, 'Good') AS ItemCondition,
                COALESCE(ec.Notes, '') AS ItemNotes
            FROM EquipmentCheckouts ec
            INNER JOIN Equipment e ON ec.EquipmentID = e.Id
            INNER JOIN Teams t ON ec.TeamID = t.TeamID
            INNER JOIN Users u ON ec.UserID = u.UserID
            ORDER BY ec.CheckoutDate DESC";

                // Execute raw SQL safely
                var connection = _dbContext.Database.GetDbConnection();
                await connection.OpenAsync();

                var result = new List<CheckoutRecordDto>();

                using var command = connection.CreateCommand();
                command.CommandText = query;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new CheckoutRecordDto
                    {
                        Id = reader["Id"].ToString(),
                        EquipmentId = reader["EquipmentId"].ToString(),
                        TeamId = Convert.ToInt32(reader["TeamID"]),
                        UserId = Convert.ToInt32(reader["UserID"]),
                        CheckedOutAt = reader["CheckedOutAt"] != DBNull.Value ? Convert.ToDateTime(reader["CheckedOutAt"]) : null,
                        ReturnedAt = reader["ReturnedAt"] != DBNull.Value ? Convert.ToDateTime(reader["ReturnedAt"]) : null,
                        ExpectedReturnDate = reader["ExpectedReturnDate"] != DBNull.Value ? Convert.ToDateTime(reader["ExpectedReturnDate"]) : null,
                        EquipmentName = reader["EquipmentName"] != DBNull.Value ? reader["EquipmentName"].ToString() : null,
                        TeamName = reader["TeamName"] != DBNull.Value ? reader["TeamName"].ToString() : null,
                        UserName = $"{reader["FirstName"]} {reader["LastName"]}",
                        UserRole = reader["UserRole"] != DBNull.Value ? reader["UserRole"].ToString() : null,
                        Quantity = reader["Quantity"] != DBNull.Value ? Convert.ToInt32(reader["Quantity"]) : 1,
                        ItemCondition = reader["ItemCondition"].ToString(),
                        ItemNotes = reader["ItemNotes"].ToString()
                    });
                }

                _logger.LogInformation($"Successfully retrieved {result.Count} checkout records");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCheckoutHistoryAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex, "Inner exception: {ex.InnerException.Message}");
                }
                _logger.LogError(ex, "Stack trace: {ex.StackTrace}");
                throw; // Rethrow so the controller can handle it
            }
        }



        // Method to get in-use quantity for a specific equipment
        public async Task<int> GetInUseQuantityForEquipmentAsync(int equipmentId)
        {
            return await _dbContext.EquipmentCheckouts
                .Where(ec => ec.EquipmentId == equipmentId && ec.Status == "CheckedOut") // Changed from EquipmentID to EquipmentId
                .SumAsync(ec => ec.Quantity);
        }

        // Method to get available quantity for a specific equipment
        public async Task<int> GetAvailableQuantityForEquipmentAsync(int equipmentId)
        {
            var equipment = await _dbContext.Equipment.FindAsync(equipmentId);
            if (equipment == null)
            {
                return 0;
            }

            var inUseQuantity = await GetInUseQuantityForEquipmentAsync(equipmentId);
            return equipment.Quantity;
        }

        // Method to update team checkout amounts
        public async Task UpdateTeamCheckoutQuantityAsync(int teamId, int equipmentId, int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
            }

            // Find existing checkout record
            var checkout = await _dbContext.EquipmentCheckouts
                .Where(ec => ec.TeamID == teamId &&
                       ec.EquipmentId == equipmentId && // Changed from EquipmentID to EquipmentId
                       ec.Status == "CheckedOut")
                .OrderByDescending(ec => ec.CheckoutDate)
                .FirstOrDefaultAsync();

            if (checkout != null)
            {
                // Update existing checkout quantity
                checkout.Quantity += quantity;
            }
            else
            {
                // Create new checkout record
                checkout = new EquipmentCheckout
                {
                    EquipmentId = equipmentId, // Changed from EquipmentID to EquipmentId
                    TeamID = teamId,
                    UserID = 1, // Default system user
                    CheckoutDate = DateTime.UtcNow,
                    ExpectedReturnDate = DateTime.UtcNow.AddDays(7),
                    Status = "CheckedOut",
                    Quantity = quantity,
                    Notes = "Created via quantity update"
                };

                await _dbContext.EquipmentCheckouts.AddAsync(checkout);
            }

            // Update equipment quantity
            var equipment = await _dbContext.Equipment.FindAsync(equipmentId);
            if (equipment != null)
            {
                equipment.Quantity -= quantity;
                if (equipment.Quantity <= 0)
                {
                    equipment.Status = "Unavailable";
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        // Implementation of ProcessPartialReturnAsync
        public async Task<bool> ProcessPartialReturnAsync(int checkoutId, int returnQuantity, string condition = "Good", string notes = "")
        {
            if (returnQuantity <= 0)
            {
                throw new ArgumentException("Return quantity must be greater than zero", nameof(returnQuantity));
            }

            var checkout = await _dbContext.EquipmentCheckouts.FindAsync(checkoutId);
            if (checkout == null)
            {
                return false;
            }

            // Ensure the return quantity doesn't exceed the checked out quantity
            if (returnQuantity > checkout.Quantity)
            {
                returnQuantity = checkout.Quantity;
            }

            // Get the equipment
            var equipment = await _dbContext.Equipment.FindAsync(checkout.EquipmentId); // Changed from EquipmentID to EquipmentId
            if (equipment == null)
            {
                return false;
            }

            try
            {
                if (returnQuantity == checkout.Quantity)
                {
                    // Full return
                    checkout.Status = "Returned";
                    checkout.ActualReturnDate = DateTime.UtcNow;
                    checkout.ReturnCondition = condition;
                    checkout.Notes = !string.IsNullOrEmpty(notes) ? notes : checkout.Notes;
                }
                else
                {
                    // Partial return - reduce the quantity on the original checkout
                    checkout.Quantity -= returnQuantity;

                    // Create a new record for the returned portion
                    var returnedCheckout = new EquipmentCheckout
                    {
                        EquipmentId = checkout.EquipmentId, // Changed from EquipmentID to EquipmentId
                        TeamID = checkout.TeamID,
                        UserID = checkout.UserID,
                        CheckoutDate = checkout.CheckoutDate,
                        ExpectedReturnDate = checkout.ExpectedReturnDate,
                        ActualReturnDate = DateTime.UtcNow,
                        Status = "Returned",
                        ReturnCondition = condition,
                        Notes = !string.IsNullOrEmpty(notes) ? notes : "Partial return",
                        Quantity = returnQuantity
                    };

                    await _dbContext.EquipmentCheckouts.AddAsync(returnedCheckout);
                }

                // Update equipment quantity
                equipment.Quantity += returnQuantity;
                if (equipment.Quantity > 0 && equipment.Status == "Unavailable")
                {
                    equipment.Status = "Available";
                }

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessPartialReturnAsync: {ex.Message}");
                return false;
            }
        }

        // Implementation of BulkReturnEquipmentAsync
        public async Task<bool> BulkReturnEquipmentAsync(int teamId, IEnumerable<(int equipmentId, int quantity)> equipmentReturns,
            int userId, string condition = "Good", string notes = "")
        {
            if (teamId <= 0 || !equipmentReturns.Any())
            {
                return false;
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                int successCount = 0;

                foreach (var (equipmentId, quantity) in equipmentReturns)
                {
                    if (quantity <= 0)
                        continue;

                    // Find the checkout record for this equipment and team
                    var checkout = await _dbContext.EquipmentCheckouts
                        .Where(ec => ec.TeamID == teamId &&
                                     ec.EquipmentId == equipmentId && // Changed from EquipmentID to EquipmentId
                                     ec.Status == "CheckedOut")
                        .OrderByDescending(ec => ec.CheckoutDate)
                        .FirstOrDefaultAsync();

                    if (checkout == null)
                        continue;

                    // Get the equipment
                    var equipment = await _dbContext.Equipment.FindAsync(equipmentId);
                    if (equipment == null)
                        continue;

                    // Determine the return quantity (don't exceed what was checked out)
                    int returnQuantity = Math.Min(quantity, checkout.Quantity);

                    if (returnQuantity == checkout.Quantity)
                    {
                        // Full return
                        checkout.Status = "Returned";
                        checkout.ActualReturnDate = DateTime.UtcNow;
                        checkout.ReturnCondition = condition;

                        if (!string.IsNullOrEmpty(notes))
                            checkout.Notes = notes;
                    }
                    else
                    {
                        // Partial return
                        checkout.Quantity -= returnQuantity;

                        // Create a new record for the returned portion
                        var returnedCheckout = new EquipmentCheckout
                        {
                            EquipmentId = equipmentId, // Changed from EquipmentID to EquipmentId
                            TeamID = teamId,
                            UserID = userId,
                            CheckoutDate = checkout.CheckoutDate,
                            ExpectedReturnDate = checkout.ExpectedReturnDate,
                            ActualReturnDate = DateTime.UtcNow,
                            Status = "Returned",
                            ReturnCondition = condition,
                            Notes = string.IsNullOrEmpty(notes) ? "Bulk partial return" : notes,
                            Quantity = returnQuantity
                        };

                        await _dbContext.EquipmentCheckouts.AddAsync(returnedCheckout);
                    }

                    // Update equipment quantity
                    equipment.Quantity += returnQuantity;
                    if (equipment.Quantity > 0 && equipment.Status == "Unavailable")
                    {
                        equipment.Status = "Available";
                    }

                    successCount++;
                }

                if (successCount > 0)
                {
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                else
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in BulkReturnEquipmentAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Returns a checked out equipment item and checks for blacklist status changes
        /// </summary>
        /// <param name="checkoutId">The ID of the checkout to return</param>
        /// <returns>A tuple with success status and any un-blacklist message</returns>
        public async Task<(bool Success, string? UnBlacklistMessage)> ReturnEquipmentAsync(int checkoutId)
        {
            try
            {
                // Get the checkout record
                var checkout = await GetCheckoutByIdAsync(checkoutId);
                if (checkout == null)
                {
                    return (false, null);
                }

                // Check if it's already returned
                if (checkout.Status == "Returned")
                {
                    return (true, null); // Already returned, consider the operation successful
                }

                // Update checkout status
                checkout.ReturnDate = DateTime.UtcNow;
                checkout.ActualReturnDate = DateTime.UtcNow;
                checkout.Status = "Returned";

                // Update the database
                await UpdateCheckoutAsync(checkout);

                // The blacklist message handling is done at the service level, 
                // not the repository level, so we just return a success with no message
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error returning equipment (ID: {checkoutId}): {ex.Message}");
                return (false, null);
            }
        }


        // Implementation of GetAllCheckoutsAsync
        public async Task<List<EquipmentCheckout>> GetAllCheckoutsAsync()
        {
            return await _dbContext.EquipmentCheckouts.ToListAsync();
        }

        // Implementation of UpdateCheckoutAsync
        public async Task UpdateCheckoutAsync(EquipmentCheckout checkout)
        {
            _dbContext.EquipmentCheckouts.Update(checkout);
            await _dbContext.SaveChangesAsync();
        }

        // Implementation of GetUnreturnedItemsForTeamAsync
        public async Task<List<CheckoutRecord>> GetUnreturnedItemsForTeamAsync(string teamId)
        {
            if (int.TryParse(teamId, out int numericTeamId))
            {
                var unreturned = await _dbContext.EquipmentCheckouts
                    .Where(ec => ec.TeamID == numericTeamId && ec.Status == "CheckedOut")
                    .Join(
                        _dbContext.Teams,
                        ec => ec.TeamID,
                        t => t.TeamID,
                        (ec, t) => new { Checkout = ec, Team = t }
                    )
                    .Join(
                        _dbContext.Equipment,
                        joined => joined.Checkout.EquipmentId,
                        e => e.Id,
                        (joined, e) => new CheckoutRecord
                        {
                            Id = joined.Checkout.CheckoutID.ToString(),
                            EquipmentId = joined.Checkout.EquipmentId,
                            Equipment = e,
                            TeamId = joined.Checkout.TeamID,
                            Team = joined.Team,
                            UserId = joined.Checkout.UserID,
                            CheckedOutAt = joined.Checkout.CheckoutDate,
                            ReturnedAt = null,
                            Quantity = joined.Checkout.Quantity
                        }
                    )
                    .ToListAsync();

                return unreturned;
            }
            return new List<CheckoutRecord>();
        }

        // Implementation of GetActiveCheckoutsAsync
        public async Task<List<EquipmentCheckout>> GetActiveCheckoutsAsync()
        {
            return await _dbContext.EquipmentCheckouts
                .Where(ec => ec.Status == "CheckedOut")
                .ToListAsync();
        }

        // Overload for AddCheckoutAsync to support EquipmentCheckout
        public async Task AddCheckoutAsync(EquipmentCheckout checkout)
        {
            await _dbContext.EquipmentCheckouts.AddAsync(checkout);
            await _dbContext.SaveChangesAsync();
        }

    }
}
