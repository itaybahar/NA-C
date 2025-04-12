using System.Threading.Tasks;
using Blazor_WebAssembly.Models.Auth;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task<List<AuditLogModel>> GetUserAuditLogsAsync(int userId);
        Task<List<AuditLogModel>> FilterAuditLogsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? action = null); // Fixed by making 'action' nullable with '?'
    }
}