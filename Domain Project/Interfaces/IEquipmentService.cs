using Domain_Project.DTOs;
using Domain_Project.DTOs.Domain_Project.DTOs.Domain_Project.Models;
using Domain_Project.Models;

namespace Domain_Project.Interfaces
{
    public interface IEquipmentService
    {
        Task<IEnumerable<Equipment>> GetAllEquipmentAsync();
        Task<Equipment?> GetEquipmentByIdAsync(int id);
        Task<Equipment> AddEquipmentAsync(EquipmentDto equipmentDto);
        Task<bool> UpdateEquipmentAsync(EquipmentDto equipmentDto);
        Task<bool> DeleteEquipmentAsync(int id);
        Task<IEnumerable<Equipment>> GetAvailableEquipmentAsync();
        Task<IEnumerable<Equipment>> GetEquipmentByCategoryAsync(int categoryId);
    }
}
