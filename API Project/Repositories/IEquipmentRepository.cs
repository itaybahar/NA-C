using Domain_Project.Models.Domain_Project.Models;

public interface IEquipmentRepository
{
    Task AddAsync(Equipment equipment);
    Task<List<Equipment>> GetAllAsync();
    Task<Equipment> GetByIdAsync(string id);
    Task UpdateAsync(Equipment equipment);
}
