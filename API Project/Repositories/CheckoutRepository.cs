using API_Project.Data;
using Domain_Project.DTOs;
using Domain_Project.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace API_Project.Repositories
{
    public class CheckoutRepository : ICheckoutRepository
    {
        private readonly EquipmentManagementContext _dbContext;

        public CheckoutRepository(EquipmentManagementContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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
            // Convert the Checkout model to EquipmentCheckout entity
            var equipmentCheckout = new EquipmentCheckout
            {
                EquipmentID = checkout.EquipmentId,
                TeamID = checkout.TeamId,
                UserID = checkout.UserId, // Using the UserID property from EquipmentCheckout
                CheckoutDate = checkout.CheckoutDate,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7), // Default to 7 days
                Status = "CheckedOut",
                Notes = "Created from Checkout object"
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
                            EquipmentId = ec.EquipmentID,
                            TeamId = ec.TeamID,
                            Team = t, // Set the required Team property
                            UserId = ec.UserID,
                            CheckoutDate = ec.CheckoutDate,
                            ReturnDate = ec.ActualReturnDate,
                            IsReturned = ec.Status == "Returned"
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
                             ec.EquipmentID == equipmentId &&
                             ec.Status == "CheckedOut")
                .OrderByDescending(ec => ec.CheckoutDate)
                .FirstOrDefaultAsync();

            return checkout?.CheckoutID;
        }



        public async Task MarkAsReturnedAsync(int checkoutId)
        {
            var checkout = await _dbContext.EquipmentCheckouts.FindAsync(checkoutId);
            if (checkout != null)
            {
                checkout.Status = "Returned";
                checkout.ActualReturnDate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task AddAsync(CheckoutRecord record)
        {
            // Convert the CheckoutRecord to an EquipmentCheckout
            var equipmentCheckout = new EquipmentCheckout
            {
                EquipmentID = record.EquipmentId,
                TeamID = record.TeamId,
                CheckoutDate = record.CheckedOutAt,
                ExpectedReturnDate = record.CheckedOutAt.AddDays(7), // Default to 7 days
                ActualReturnDate = record.ReturnedAt,
                Status = record.ReturnedAt.HasValue ? "Returned" : "CheckedOut"
            };

            await _dbContext.EquipmentCheckouts.AddAsync(equipmentCheckout);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<CheckoutRecord>> GetByTeamIdAsync(string teamId)
        {
            if (int.TryParse(teamId, out int numericTeamId))
            {
                // Query EquipmentCheckouts and map to CheckoutRecord objects
                var records = await _dbContext.EquipmentCheckouts
                    .Where(ec => ec.TeamID == numericTeamId)
                    .Join(
                        _dbContext.Teams,
                        ec => ec.TeamID,
                        t => t.TeamID,
                        (ec, t) => new { Checkout = ec, Team = t }
                    )
                    .Join(
                        _dbContext.Equipment,
                        joined => joined.Checkout.EquipmentID,
                        e => e.Id,
                        (joined, e) => new CheckoutRecord
                        {
                            Id = joined.Checkout.CheckoutID.ToString(),
                            EquipmentId = joined.Checkout.EquipmentID,
                            Equipment = e, // Set the required Equipment property
                            TeamId = joined.Checkout.TeamID,
                            Team = joined.Team, // Set the required Team property
                            UserId = joined.Checkout.UserID,
                            CheckedOutAt = joined.Checkout.CheckoutDate,
                            ReturnedAt = joined.Checkout.ActualReturnDate
                        }
                    )
                    .ToListAsync();

                return records;
            }
            return new List<CheckoutRecord>();
        }

        public async Task<List<CheckoutRecord>> GetOverdueAsync(TimeSpan overdueTime)
        {
            var now = DateTime.UtcNow;
            var overdueDate = now.Subtract(overdueTime);

            // Query EquipmentCheckouts and map to CheckoutRecord objects
            var overdueRecords = await _dbContext.EquipmentCheckouts
                .Where(ec => ec.Status == "CheckedOut" && ec.ExpectedReturnDate < now)
                .Join(
                    _dbContext.Teams,
                    ec => ec.TeamID,
                    t => t.TeamID,
                    (ec, t) => new { Checkout = ec, Team = t }
                )
                .Join(
                    _dbContext.Equipment,
                    joined => joined.Checkout.EquipmentID,
                    e => e.Id,
                    (joined, e) => new CheckoutRecord
                    {
                        Id = joined.Checkout.CheckoutID.ToString(),
                        EquipmentId = joined.Checkout.EquipmentID,
                        Equipment = e, // Set the required Equipment property
                        TeamId = joined.Checkout.TeamID,
                        Team = joined.Team, // Set the required Team property
                        UserId = joined.Checkout.UserID, // Add the required UserId property
                        CheckedOutAt = joined.Checkout.CheckoutDate,
                        ReturnedAt = null
                    }
                )
                .ToListAsync();

            return overdueRecords;
        }

        public async Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync()
        {
            try
            {
                var checkoutHistory = await _dbContext.EquipmentCheckouts
                    .Join(
                        _dbContext.Equipment,
                        ec => ec.EquipmentID,
                        e => e.Id,
                        (ec, e) => new { Checkout = ec, Equipment = e }
                    )
                    .Join(
                        _dbContext.Teams,
                        joined => joined.Checkout.TeamID,
                        t => t.TeamID,
                        (joined, t) => new { joined.Checkout, joined.Equipment, Team = t }
                    )
                    .Join(
                        _dbContext.Users,
                        joined => joined.Checkout.UserID,
                        u => u.UserID,
                        (joined, u) => new CheckoutRecordDto
                        {
                            Id = joined.Checkout.CheckoutID.ToString(),
                            EquipmentId = joined.Checkout.EquipmentID.ToString(),
                            TeamId = joined.Checkout.TeamID,
                            UserId = joined.Checkout.UserID,
                            CheckedOutAt = joined.Checkout.CheckoutDate,
                            ReturnedAt = joined.Checkout.ActualReturnDate,
                            EquipmentName = joined.Equipment.Name,
                            TeamName = joined.Team.TeamName,
                            UserName = $"{u.FirstName} {u.LastName}",
                            UserRole = u.Role
                        }
                    )
                    .ToListAsync();

                return checkoutHistory;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCheckoutHistoryAsync: {ex.Message}");
                return new List<CheckoutRecordDto>();
            }
        }

    }
}
