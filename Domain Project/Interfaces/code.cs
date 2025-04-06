using Domain_Project.DTOs;
using Domain_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Domain_Project.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
    }

    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User> GetUserByUsernameAsync(string username);
        Task<bool> ValidateUserCredentialsAsync(string username, string password);
        Task<IEnumerable<UserRole>> GetUserRolesAsync(int userId);
    }

    public interface IEquipmentRepository : IGenericRepository<Equipment>
    {
        Task<IEnumerable<Equipment>> GetAvailableEquipmentAsync();
        Task<IEnumerable<Equipment>> GetEquipmentByCategoryAsync(int categoryId);
    }

    public interface IEquipmentCheckoutRepository : IGenericRepository<EquipmentCheckout>
    {
        Task<IEnumerable<EquipmentCheckout>> GetActiveCheckoutsByTeamAsync(int teamId);
        Task<IEnumerable<EquipmentCheckout>> GetOverdueCheckoutsAsync();
    }

    public interface ITeamRepository : IGenericRepository<Team>
    {
        Task<bool> IsTeamBlacklistedAsync(int teamId);
        Task<IEnumerable<User>> GetTeamMembersAsync(int teamId);
    }

    public interface IEquipmentRequestRepository : IGenericRepository<EquipmentRequest>
    {
        Task<IEnumerable<EquipmentRequest>> GetPendingRequestsAsync();
        Task<IEnumerable<EquipmentRequest>> GetRequestsByStatusAsync(string status);
    }

    public interface IBlacklistRepository : IGenericRepository<Blacklist>
    {
        Task<Blacklist> GetActiveBlacklistForTeamAsync(int teamId);
        Task<IEnumerable<Blacklist>> GetAllActiveBlacklistsAsync();
    }
    public interface IAuthenticationService
    {
        Task<AuthenticationResponseDto> AuthenticateAsync(UserLoginDto loginDto);
        Task<UserDto> RegisterUserAsync(UserDto userDto, string password);
    }
}
