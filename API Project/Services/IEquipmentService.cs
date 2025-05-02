using Domain_Project.DTOs;
using Domain_Project.Models;

public interface IEquipmentService
{
    Task AddEquipmentAsync(EquipmentDto dto); // Keep this method
    Task<List<Equipment>> GetAllAsync(); // Keep this method
    Task<IEnumerable<Equipment>> GetAllEquipmentAsync(); // Keep this method
    Task<List<Equipment>> GetAvailableAsync(); // Keep this method
    Task<IEnumerable<Equipment>> GetAvailableEquipmentAsync(); // Keep this method
    Task<IEnumerable<Equipment>> GetEquipmentByCategoryAsync(int categoryId); // Keep this method
    Task<Equipment?> GetEquipmentByIdAsync(int id); // Keep this method
    Task<bool> UpdateEquipmentAsync(EquipmentDto equipmentDto); // Keep this method
    Task<bool> DeleteEquipmentAsync(int id); // Add this method

}
