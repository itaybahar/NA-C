using Microsoft.EntityFrameworkCore;
using API_Project.Data;
using Domain_Project.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_Project.Repositories
{
    public class RoleChangeRequestRepository : GenericRepository<RoleChangeRequest>, IRoleChangeRequestRepository
    {
        public RoleChangeRequestRepository(EquipmentManagementContext context) : base(context)
        {
        }

        public async Task<IEnumerable<RoleChangeRequest>> GetPendingRequestsAsync()
        {
            return await _dbSet
                .Where(r => r.Status == RequestStatus.Pending)
                .Include(r => r.User)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<RoleChangeRequest>> GetUserRequestsAsync(int userId)
        {
            return await _dbSet
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<RoleChangeRequest>> GetRequestsByStatusAsync(RequestStatus status)
        {
            return await _dbSet
                .Where(r => r.Status == status)
                .Include(r => r.User)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        public override async Task<RoleChangeRequest> AddAsync(RoleChangeRequest request)
        {
            // Set default values if not already set
            if (request.RequestDate == default)
            {
                request.RequestDate = DateTime.UtcNow;
            }

            if (request.Status == default)
            {
                request.Status = RequestStatus.Pending;
            }

            return await base.AddAsync(request);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
