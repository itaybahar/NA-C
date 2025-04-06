using System.Threading.Tasks;
using Blazor_WebAssembly.Models.Equipment;

namespace Blazor_WebAssembly.Services.Interfaces
{
    public interface IEquipmentService
    {
        Task<List<EquipmentModel>> GetAllEquipmentAsync();
        Task<EquipmentModel> GetEquipmentByIdAsync(int id);
        Task<bool> AddEquipmentAsync(EquipmentModel equipment);
        Task<bool> UpdateEquipmentAsync(EquipmentModel equipment);
        Task<bool> DeleteEquipmentAsync(int id);
        Task<List<EquipmentModel>> GetAvailableEquipmentAsync();
        Task<List<EquipmentModel>> FilterEquipmentByCategoryAsync(int categoryId);
    }
}