using System.Collections.Generic;
using System.Threading.Tasks;
using Domain_Project.Models;
using Domain_Project.Models.Domain_Project.Models;

namespace Domain_Project.Interfaces
{
    public interface IEquipmentService
    {
        Task<IEnumerable<Equipment>> GetAvailableEquipmentAsync();
        Task<IEnumerable<Equipment>> GetEquipmentByCategoryAsync(int categoryId);
        Task UpdateEquipmentStatusAsync(int id, string newStatus);
    }
}
