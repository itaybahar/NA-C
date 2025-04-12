using Domain_Project.Models;

namespace Domain_Project.Interfaces
{
    public interface IEquipmentRepository
    {
        Task<Equipment> AddAsync(Equipment equipment);
        Task<List<Equipment>> GetAllAsync(); // Updated to match GenericRepository
        Task<Equipment> GetByIdAsync(string id);
        Task UpdateAsync(Equipment equipment);
    }

}
