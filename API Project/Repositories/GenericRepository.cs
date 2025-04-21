using API_Project.Data;
using Domain_Project.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema; // Add this for NotMapped attribute

namespace API_Project.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly EquipmentManagementContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(EquipmentManagementContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id) ?? throw new KeyNotFoundException($"Entity with id {id} not found");
        }
        public virtual async Task<List<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }
        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
        // Added new method for deleting by ID
        public virtual async Task<bool> DeleteByIdAsync(int id)
        {
            try
            {
                var entity = await _dbSet.FindAsync(id);
                if (entity == null)
                {
                    return false; // Entity not found
                }

                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
                return true; // Successfully deleted
            }
            catch (DbUpdateException ex)
            {
                // Log the exception
                Console.WriteLine($"Database error when deleting entity with ID {id}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw; // Re-throw to be handled by caller
            }
            catch (Exception ex)
            {
                // Log any other exceptions
                Console.WriteLine($"Error deleting entity with ID {id}: {ex.Message}");
                throw; // Re-throw to be handled by caller
            }
        }

        public virtual async Task SaveChangesAsync(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
    }
}
