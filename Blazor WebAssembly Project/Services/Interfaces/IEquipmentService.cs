using Blazor_WebAssembly.Models.Equipment;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IEquipmentService
    {
        Task<List<EquipmentModel>> GetAllEquipmentAsync();
        Task<EquipmentModel?> GetEquipmentByIdAsync(int id); // Changed from Equipment? to EquipmentModel?
        Task<bool> AddEquipmentAsync(EquipmentModel equipment); // Changed from EquipmentDto to EquipmentModel
        Task<bool> UpdateEquipmentAsync(EquipmentModel equipment); // Changed from EquipmentDto to EquipmentModel
        Task<bool> DeleteEquipmentAsync(int id);
        Task<List<EquipmentModel>> GetAvailableEquipmentAsync(); // Changed return type
        Task<List<EquipmentModel>> FilterEquipmentByCategoryAsync(int categoryId); // Renamed and updated return type
    }
}
