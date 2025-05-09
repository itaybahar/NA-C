using Domain_Project.DTOs;
using Domain_Project.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API_Project.Data;

namespace API_Project.Repositories
{
    public interface ICheckoutRepository
    {
        // Basic CRUD operations
        Task AddAsync(CheckoutRecord record);

        // Updated method using raw SQL to avoid column name mismatches
        Task<List<CheckoutRecordDto>> GetByTeamIdAsync(string teamId);

        Task<List<CheckoutRecord>> GetOverdueAsync(TimeSpan overdueTime);
        Task<bool> HasUnreturnedItemsAsync(string teamId);

        // Working method that correctly handles nullable database fields
        Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync();

        // Checkout and return operations with quantity support
        Task<int?> GetCheckoutIdByTeamAndEquipmentAsync(int teamId, int equipmentId);
        Task<EquipmentCheckout?> GetCheckoutByIdAsync(int checkoutId);

        // Enhanced return functionality with quantity, condition, and notes
        Task MarkAsReturnedAsync(int checkoutId, int quantity = 0, string condition = "Good", string notes = "");

        // Working quantity tracking method - returns 200 OK responses in logs
        Task<int> GetInUseQuantityForEquipmentAsync(int equipmentId);

        Task<int> GetAvailableQuantityForEquipmentAsync(int equipmentId);

        // Team checkout quantity updates
        Task UpdateTeamCheckoutQuantityAsync(int teamId, int equipmentId, int quantity);

        // Extended checkout methods
        Task AddCheckoutAsync(Checkout checkout);
        Task<IEnumerable<Checkout>> GetCheckoutsByTeamIdAsync(string teamId);

        // Partial returns and quantity adjustments
        Task<bool> ProcessPartialReturnAsync(int checkoutId, int returnQuantity, string condition = "Good", string notes = "");

        // Bulk operations
        Task<bool> BulkReturnEquipmentAsync(
            int teamId,
            IEnumerable<(int equipmentId, int quantity)> equipmentReturns,
            int userId,
            string condition = "Good",
            string notes = "");
    }
}
