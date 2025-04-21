using API_Project.Data;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_Project.Repositories
{
    public class EquipmentRepository : GenericRepository<Equipment>, IEquipmentRepository
    {
        // Add "new" keyword to fix CS0108 warning
        private new readonly EquipmentManagementContext _context;

        public EquipmentRepository(EquipmentManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Equipment> AddEquipmentAsync(Equipment equipment)
        {
            await _context.Equipment.AddAsync(equipment);
            await _context.SaveChangesAsync();
            return equipment;
        }

        public async Task DeleteEquipmentAsync(int id)
        {
            var equipment = await _context.Equipment.FindAsync(id);
            if (equipment != null)
            {
                _context.Equipment.Remove(equipment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> RemoveEquipmentAsync(int id)
        {
            try
            {
                // Find the equipment by ID
                var equipment = await _context.Equipment.FindAsync(id);
                if (equipment == null)
                {
                    // If the equipment doesn't exist, return false
                    return false;
                }

                // Remove the equipment from the database
                _context.Equipment.Remove(equipment);
                await _context.SaveChangesAsync();

                // Return true to indicate successful deletion
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception and rethrow it
                Console.WriteLine($"Error removing equipment with ID {id}: {ex.Message}");
                throw;
            }
        }

        public override async Task<List<Equipment>> GetAllAsync()
        {
            return await _context.Equipment.ToListAsync();
        }

        public async Task<IEnumerable<Equipment>> GetAllEquipmentAsync()
        {
            return await _context.Equipment.ToListAsync();
        }

        public async Task<IEnumerable<Equipment>> GetAvailableEquipmentAsync()
        {
            return await _context.Equipment
                .Where(e => e.Status == "Available" || e.Status == "זמין")
                .ToListAsync();
        }

        public async Task<Equipment> GetByIdAsync(string id)
        {
            if (!int.TryParse(id, out var intId))
            {
                throw new ArgumentException("Invalid ID format. ID must be an integer.", nameof(id));
            }

            return await base.GetByIdAsync(intId);
        }

        public async Task<IEnumerable<Equipment>> GetEquipmentByCategoryAsync(int categoryId)
        {
            // Temporary solution until database schema is updated
            // Return all equipment since we can't filter by CategoryId
            try
            {
                return await _context.Equipment
                    .Where(e => e.CategoryId == categoryId)
                    .ToListAsync();
            }
            catch (Exception)
            {
                // Fallback if CategoryId column doesn't exist yet
                return await _context.Equipment.ToListAsync();
            }
        }

        public async Task<Equipment?> GetEquipmentByIdAsync(int id)
        {
            return await _context.Equipment.FindAsync(id);
        }

        public async Task UpdateEquipmentAsync(Equipment existingEquipment)
        {
            _context.Equipment.Update(existingEquipment);
            await _context.SaveChangesAsync();
        }
    }
}
