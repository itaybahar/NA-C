using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Domain_Project.DTOs;
using System.Linq.Expressions;

namespace API_Project.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "CentralManager")]
    public class AuditLogController : BaseController<AuditLog, IGenericRepository<AuditLog>>
    {
        public AuditLogController(IGenericRepository<AuditLog> repository) : base(repository) { }

        protected override int GetEntityId(AuditLog entity) => entity.LogID;

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserAuditLogs(int userId)
        {
            var userLogs = await _repository.FindAsync(log => log.UserID == userId);
            return Ok(userLogs);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> FilterAuditLogs(
            [FromQuery] AuditLogFilterDto filterDto)
        {
            Expression<Func<AuditLog, bool>> query = log => true;

            if (filterDto.StartDate.HasValue)
                query = query.And(log => log.LogDate >= filterDto.StartDate.Value);

            if (filterDto.EndDate.HasValue)
                query = query.And(log => log.LogDate <= filterDto.EndDate.Value);

            if (!string.IsNullOrEmpty(filterDto.Action))
                query = query.And(log => log.Action == filterDto.Action);

            if (!string.IsNullOrEmpty(filterDto.LogLevel))
                query = query.And(log => log.LogLevel == filterDto.LogLevel);

            var filteredLogs = await _repository.FindAsync(query);
            return Ok(filteredLogs);
        }
    }

    // Extension method for building complex predicates
    public static class PredicateExtensions
    {
        public static Expression<Func<T, bool>> And<T>(
            this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
        {
            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.AndAlso(
                Expression.Invoke(left, parameter),
                Expression.Invoke(right, parameter)
            );

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }

    // DTO for Audit Log Filtering
    public class AuditLogFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Action { get; set; }
        public string LogLevel { get; set; }
    }
}