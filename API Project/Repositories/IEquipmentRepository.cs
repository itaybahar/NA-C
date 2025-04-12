using Domain_Project.Models;
public interface IEquipmentRepository
{
    Task<Equipment> AddAsync(Equipment equipment); // Updated to match GenericRepository
    Task<List<Equipment>> GetAllAsync();
    Task<Equipment> GetByIdAsync(string id);
    Task UpdateAsync(Equipment equipment);
}
