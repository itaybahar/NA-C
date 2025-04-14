using Domain_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Domain_Project.Models.TeamMember;

namespace API_Project.Repositories
{
    public class CheckoutRepository : ICheckoutRepository
    {
        private readonly List<Checkout> _checkouts = new();
        private readonly List<CheckoutRecord> _checkoutRecords = new();

        public async Task<bool> HasUnreturnedItemsAsync(string teamId)
        {
            // Simulate async database operation
            return await Task.FromResult(_checkouts.Any(c => c.TeamId == teamId && !c.IsReturned));
        }

        public async Task AddCheckoutAsync(Checkout checkout)
        {
            // Simulate async database operation
            await Task.Run(() => _checkouts.Add(checkout));
        }

        public async Task<IEnumerable<Checkout>> GetCheckoutsByTeamIdAsync(string teamId)
        {
            // Simulate async database operation
            return await Task.FromResult(_checkouts.Where(c => c.TeamId == teamId));
        }

        public async Task MarkAsReturnedAsync(int checkoutId)
        {
            // Simulate async database operation
            var checkout = _checkouts.FirstOrDefault(c => c.Id == checkoutId);
            if (checkout != null)
            {
                await Task.Run(() => checkout.IsReturned = true);
            }
        }

        public async Task AddAsync(CheckoutRecord record)
        {
            // Simulate async database operation
            await Task.Run(() => _checkoutRecords.Add(record));
        }

        public async Task<List<CheckoutRecord>> GetByTeamIdAsync(int teamId)
        {
            // Simulate async database operation
            return await Task.FromResult(_checkoutRecords.Where(r => r.TeamId == teamId).ToList());
        }

        public async Task<List<CheckoutRecord>> GetOverdueAsync(TimeSpan overdueTime)
        {
            // Simulate async database operation
            var now = DateTime.UtcNow;
            return await Task.FromResult(
                _checkoutRecords.Where(r => r.ReturnedAt == null && (now - r.CheckedOutAt) > overdueTime).ToList()
            );
        }

        public Task<List<CheckoutRecord>> GetByTeamIdAsync(string teamId)
        {
            throw new NotImplementedException();
        }
    }
}
