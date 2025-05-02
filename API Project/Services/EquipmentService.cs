using API_Project.Data;
using Domain_Project.DTOs;
using Domain_Project.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace API_Project.Services
{
    public class EquipmentService : IEquipmentService
    {
        private readonly EquipmentManagementContext? _dbContext;
        private readonly ILogger<EquipmentService> _logger;
        private readonly IEquipmentRepository? _equipmentRepository;
        private readonly HttpClient? _httpClient;
        private JsonSerializerOptions? _jsonOptions;
        private string? _apiEndpoint;

        public JsonSerializerOptions JsonOptions { get; internal set; }

        // Constructor for direct database access
        public EquipmentService(EquipmentManagementContext dbContext, ILogger<EquipmentService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Constructor for repository and API access
        public EquipmentService(
            IEquipmentRepository equipmentRepository,
            EquipmentManagementContext dbContext,
            HttpClient httpClient,
            ILogger<EquipmentService> logger)
        {
            _equipmentRepository = equipmentRepository ?? throw new ArgumentNullException(nameof(equipmentRepository));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Equipment>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all equipment");

                if (_dbContext == null)
                {
                    throw new InvalidOperationException("Database context is not initialized");
                }

                // Only select columns that exist in the database
                return await _dbContext.Equipment
                    .Select(e => new Equipment
                    {
                        Id = e.Id,
                        Name = e.Name,
                        Description = e.Description,
                        SerialNumber = e.SerialNumber,
                        PurchaseDate = e.PurchaseDate,
                        Value = e.Value,
                        Status = e.Status,
                        Notes = e.Notes,
                        LastUpdatedDate = e.LastUpdatedDate,
                        Quantity = e.Quantity,
                        StorageLocation = e.StorageLocation,
                        CategoryId = e.CategoryId,
                        // Don't include ModelNumber in the projection
                        CheckoutRecords = new List<CheckoutRecord>(), // Empty list to satisfy the required property
                        ModelNumber = "Unknown" // Hardcode a value since we can't get it from DB
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all equipment");
                throw;
            }
        }


        public async Task AddEquipmentAsync(EquipmentDto equipmentDto)
        {
            if (equipmentDto == null)
            {
                throw new ArgumentNullException(nameof(equipmentDto));
            }

            try
            {
                _logger.LogInformation($"Adding new equipment: {equipmentDto.Name}");

                if (_dbContext == null)
                {
                    throw new InvalidOperationException("Database context is not initialized");
                }

                var equipment = new Equipment
                {
                    Name = equipmentDto.Name,
                    Description = equipmentDto.Description,
                    SerialNumber = equipmentDto.SerialNumber,
                    Status = equipmentDto.Status,
                    Value = equipmentDto.Value,
                    Quantity = equipmentDto.Quantity,
                    StorageLocation = equipmentDto.StorageLocation,
                    CheckoutRecords = new List<CheckoutRecord>(),
                    ModelNumber = equipmentDto.SerialNumber ?? "Unknown"
                };

                _dbContext.Equipment.Add(equipment);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding equipment: {equipmentDto.Name}");
                throw;
            }
        }

        public async Task<bool> DeleteEquipmentAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Deleting equipment with ID: {id}");

                if (_dbContext == null)
                {
                    throw new InvalidOperationException("Database context is not initialized");
                }

                var equipment = await _dbContext.Equipment.FindAsync(id);
                if (equipment == null)
                {
                    _logger.LogWarning($"Equipment with ID: {id} not found for deletion");
                    return false;
                }

                _dbContext.Equipment.Remove(equipment);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting equipment with ID: {id}");
                throw;
            }
        }

        public async Task<IEnumerable<Equipment>> GetAllEquipmentAsync()
        {
            try
            {
                if (_httpClient == null || _apiEndpoint == null)
                {
                    throw new InvalidOperationException("HttpClient or API endpoint is not initialized");
                }

                _logger.LogInformation($"Fetching from: {_httpClient.BaseAddress}{_apiEndpoint}");

                var response = await _httpClient.GetAsync(_apiEndpoint, HttpCompletionOption.ResponseHeadersRead);
                _logger.LogInformation($"Response status: {response.StatusCode}");

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Response content: {content}");

                if (response.IsSuccessStatusCode)
                {
                    var equipment = JsonSerializer.Deserialize<List<Equipment>>(content, _jsonOptions);
                    return equipment ?? new List<Equipment>();
                }
                else
                {
                    _logger.LogWarning($"API request failed with status: {response.StatusCode}");
                    return new List<Equipment>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetAllEquipmentAsync");
                throw;
            }
        }

        public async Task<List<Equipment>> GetAvailableAsync()
        {
            try
            {
                _logger.LogInformation("Fetching available equipment");

                if (_dbContext == null)
                {
                    throw new InvalidOperationException("Database context is not initialized");
                }

                return await _dbContext.Equipment
                    .Where(e => e.Status == "Available" || e.Status == "זמין")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available equipment");
                throw;
            }
        }

        public async Task<IEnumerable<Equipment>> GetAvailableEquipmentAsync()
        {
            try
            {
                _logger.LogInformation("Fetching available equipment (IEnumerable)");

                if (_dbContext == null)
                {
                    throw new InvalidOperationException("Database context is not initialized");
                }

                return await _dbContext.Equipment
                    .Where(e => e.Status == "Available" || e.Status == "זמין")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available equipment (IEnumerable)");
                throw;
            }
        }

        public async Task<IEnumerable<Equipment>> GetEquipmentByCategoryAsync(int categoryId)
        {
            try
            {
                _logger.LogInformation($"Fetching equipment in category: {categoryId}");

                if (_dbContext == null)
                {
                    throw new InvalidOperationException("Database context is not initialized");
                }

                return await _dbContext.Equipment
                    .Where(e => e.CategoryId == categoryId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching equipment in category: {categoryId}");
                throw;
            }
        }

        public async Task<Equipment?> GetEquipmentByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Fetching equipment with ID: {id}");

                if (_dbContext == null)
                {
                    throw new InvalidOperationException("Database context is not initialized");
                }

                return await _dbContext.Equipment.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching equipment with ID: {id}");
                throw;
            }
        }

        public async Task<bool> UpdateEquipmentAsync(EquipmentDto equipmentDto)
        {
            if (equipmentDto == null)
            {
                throw new ArgumentNullException(nameof(equipmentDto));
            }

            try
            {
                _logger.LogInformation($"Updating equipment with ID: {equipmentDto.Id}");

                if (_dbContext == null)
                {
                    throw new InvalidOperationException("Database context is not initialized");
                }

                var existingEquipment = await _dbContext.Equipment.FindAsync(equipmentDto.Id);
                if (existingEquipment == null)
                {
                    _logger.LogWarning($"Equipment with ID: {equipmentDto.Id} not found");
                    return false;
                }

                existingEquipment.Name = equipmentDto.Name;
                existingEquipment.Description = equipmentDto.Description;
                existingEquipment.SerialNumber = equipmentDto.SerialNumber;
                existingEquipment.Status = equipmentDto.Status;
                existingEquipment.Value = equipmentDto.Value;
                existingEquipment.Quantity = equipmentDto.Quantity;
                existingEquipment.StorageLocation = equipmentDto.StorageLocation;

                _dbContext.Equipment.Update(existingEquipment);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating equipment with ID: {equipmentDto.Id}");
                throw;
            }
        }
    }
}
