using Domain_Project.DTOs.Domain_Project.DTOs;
using Domain_Project.Models.Domain_Project.Models;

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
            StorageLocation = dto.StorageLocation
        };
        await _repo.AddAsync(item);
    }

    public async Task<List<Equipment>> GetAllAsync() => await _repo.GetAllAsync();

    public async Task<List<Equipment>> GetAvailableAsync()
    {
        var all = await _repo.GetAllAsync();
        return all.Where(i => i.Quantity > 0).ToList();
    }
}
