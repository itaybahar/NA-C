using Domain_Project.DTOs;
using Domain_Project.DTOs.Domain_Project.DTOs;
using Domain_Project.DTOs.Domain_Project.DTOs.Domain_Project.Models;
using Domain_Project.Models;
namespace API_Project.Services;

public class EquipmentService : IEquipmentService
{
    private readonly IEquipmentRepository _repo;

    public EquipmentService(IEquipmentRepository repo)
    {
        _repo = repo;
    }

    public async Task AddEquipmentAsync(EquipmentDto dto)
    {
        var item = new Equipment
        {
            Name = dto.Name,
            Quantity = dto.Quantity,
            StorageLocation = dto.StorageLocation,
            Status = dto.Status,
            CheckoutRecords = new List<CheckoutRecord>() // Initialize the required CheckoutRecords property
        };
        await _repo.AddAsync(item);
    }

    public async Task<List<Equipment>> GetAllAsync()
    {
        var equipmentList = await _repo.GetAllAsync();
        return equipmentList.ToList(); // Convert IReadOnlyList to List
    }

    public async Task<List<Equipment>> GetAvailableAsync()
    {
        var all = await _repo.GetAllAsync();
        return all.Where(i => i.Quantity > 0).ToList();
    }
}
