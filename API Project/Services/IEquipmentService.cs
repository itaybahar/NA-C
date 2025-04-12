using Domain_Project.DTOs.Domain_Project.DTOs;
using Domain_Project.DTOs.Domain_Project.DTOs.Domain_Project.Models;
using Domain_Project.Models;

public interface IEquipmentService
{
    Task AddEquipmentAsync(EquipmentDto dto);
    Task<List<Equipment>> GetAllAsync();
    Task<List<Equipment>> GetAvailableAsync();
}
