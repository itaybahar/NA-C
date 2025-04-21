using Domain_Project.Models;

public interface IEquipmentRepository
{
    Task<Equipment> AddAsync(Equipment equipment); // Updated to match GenericRepository
    Task<Equipment> AddEquipmentAsync(Equipment equipment);
    Task DeleteEquipmentAsync(int id);
    Task<List<Equipment>> GetAllAsync();
    Task<IEnumerable<Equipment>> GetAllEquipmentAsync();
    Task<IEnumerable<Equipment>> GetAvailableEquipmentAsync();
    Task<Equipment> GetByIdAsync(string id);
    Task<IEnumerable<Equipment>> GetEquipmentByCategoryAsync(int categoryId);
    Task<Equipment?> GetEquipmentByIdAsync(int id);
    Task UpdateAsync(Equipment equipment);
    Task UpdateEquipmentAsync(Equipment existingEquipment);
}
