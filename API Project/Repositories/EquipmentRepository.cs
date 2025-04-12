using API_Project.Data;
using Domain_Project.Interfaces;
using Domain_Project.Models;

namespace API_Project.Repositories
{
    public class EquipmentRepository : GenericRepository<Equipment>, IEquipmentRepository
    {
        public EquipmentRepository(EquipmentManagementContext context) : base(context)
        {
        }

        public override async Task<List<Equipment>> GetAllAsync()
        {
            var equipmentList = await base.GetAllAsync();
            return equipmentList.ToList(); // Convert IReadOnlyList to List
        }

        public async Task<Equipment> GetByIdAsync(string id)
        {
            if (!int.TryParse(id, out var intId))
            {
                throw new ArgumentException("Invalid ID format. ID must be an integer.", nameof(id));
            }

            return await base.GetByIdAsync(intId);
        }
    }
}
