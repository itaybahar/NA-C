using Domain_Project.Interfaces;
using Domain_Project.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API_Project.Repositories
{
    public interface IRoleChangeRequestRepository : IGenericRepository<RoleChangeRequest>
    {
        Task<IEnumerable<RoleChangeRequest>> GetPendingRequestsAsync();
        Task<IEnumerable<RoleChangeRequest>> GetUserRequestsAsync(int userId);
        Task<IEnumerable<RoleChangeRequest>> GetRequestsByStatusAsync(RequestStatus status);
    }
}
