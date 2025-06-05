using API_Project.Data;
using Domain_Project.DTOs;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EquipmentController : ControllerBase
    {
        private readonly IEquipmentService _equipmentService;
        private readonly ILogger<EquipmentController> _logger;
        private readonly EquipmentManagementContext _dbContext;

        public EquipmentController(
            IEquipmentService equipmentService,
            ILogger<EquipmentController> logger,
            EquipmentManagementContext dbContext)
        {
            _equipmentService = equipmentService;
            _logger = logger;
            _dbContext = dbContext;
        }

        // GET: api/equipment
        // In EquipmentController.cs
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<object>>> GetEquipment()
        {
            try
            {
                _logger.LogInformation("Getting all equipment");
                var equipment = await _equipmentService.GetAllAsync();
                if (equipment == null || !equipment.Any())
                {
                    return NotFound("No equipment found.");
                }

                // Convert to anonymous objects to break circular references
                var result = equipment.Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Description,
                    e.SerialNumber,
                    e.PurchaseDate,
                    e.Value,
                    e.Status,
                    e.Quantity,
                    e.StorageLocation,
                    e.CategoryId,
                    e.ModelNumber,
                    // Don't include CheckoutRecords to avoid circular reference
                });

                _logger.LogInformation($"Retrieved {equipment.Count} equipment items");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving equipment: {Message}, Stack: {StackTrace}",
                    ex.Message, ex.StackTrace);

                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException.Message);
                }

                return StatusCode(500, "Internal server error. Please check server logs for details.");
            }
        }


        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetEquipment(int id)
        {
            try
            {
                _logger.LogInformation($"Getting equipment with ID: {id}");

                // First check if equipment exists in database to provide better error messages
                var equipmentExists = await _dbContext.Equipment.AnyAsync(e => e.Id == id);
                if (!equipmentExists)
                {
                    _logger.LogWarning($"Equipment with ID {id} not found in database");
                    return NotFound($"Equipment with ID {id} not found");
                }

                // Use the service to get equipment data
                var equipment = await _equipmentService.GetEquipmentByIdAsync(id);

                if (equipment == null)
                {
                    _logger.LogWarning($"Equipment with ID {id} returned null from service");
                    return NotFound($"Equipment with ID {id} not found");
                }

                if (equipment.CheckoutRecords == null)
                {
                    equipment.CheckoutRecords = new List<CheckoutRecord>();
                }

                // Create a properly formatted response that includes checkout records
                var result = new
                {
                    id = equipment.Id,                   // Use lowercase to match JSON naming conventions
                    equipmentID = equipment.Id,          // Include for backward compatibility 
                    name = equipment.Name ?? "Unknown",
                    description = equipment.Description ?? string.Empty,
                    serialNumber = equipment.SerialNumber ?? string.Empty,
                    purchaseDate = equipment.PurchaseDate,
                    value = equipment.Value,
                    status = equipment.Status ?? "Unknown",
                    quantity = equipment.Quantity,
                    storageLocation = equipment.StorageLocation ?? string.Empty,
                    categoryId = equipment.CategoryId,
                    modelNumber = equipment.ModelNumber ?? string.Empty,
                    // Include the actual checkout records
                    checkoutRecords = equipment.CheckoutRecords.Select(cr => new {
                        id = cr.Id,
                        equipmentId = cr.EquipmentId,
                        teamId = cr.TeamId,
                        userId = cr.UserId,
                        checkedOutAt = cr.CheckedOutAt,
                        returnedAt = cr.ReturnedAt,
                        quantity = cr.Quantity
                    }).ToList()
                };

                _logger.LogInformation($"Successfully retrieved equipment with ID: {id} with {equipment.CheckoutRecords.Count} checkout records");
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the full exception details for better diagnostics
                _logger.LogError(ex, "Error retrieving equipment with ID: {ID}. Message: {Message}, Stack: {StackTrace}",
                    id, ex.Message, ex.StackTrace);

                // Include inner exception details if available
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException.Message);
                }

                return StatusCode(500, $"Internal server error retrieving equipment {id}: {ex.Message}");
            }
        }


        // Add this new endpoint for basic equipment data without related entities
        [HttpGet("{id}/basic")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetBasicEquipment(int id)
        {
            try
            {
                _logger.LogInformation($"Getting basic equipment with ID: {id}");

                var equipment = await _dbContext.Equipment.FindAsync(id);

                if (equipment == null)
                {
                    _logger.LogWarning($"Equipment with ID {id} not found");
                    return NotFound($"Equipment with ID {id} not found");
                }

                // Return simplified projection with no related entities
                var result = new
                {
                    Id = equipment.Id,
                    Name = equipment.Name ?? "Unknown",
                    Description = equipment.Description ?? string.Empty,
                    SerialNumber = equipment.SerialNumber ?? string.Empty,
                    Value = equipment.Value,
                    Status = equipment.Status ?? "Unknown",
                    Quantity = equipment.Quantity,
                    StorageLocation = equipment.StorageLocation ?? string.Empty,
                    CategoryId = equipment.CategoryId,
                    ModelNumber = equipment.ModelNumber ?? string.Empty
                };

                _logger.LogInformation($"Successfully retrieved basic equipment with ID: {id}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving basic equipment with ID: {ID}", id);
                return StatusCode(500, $"Internal server error retrieving basic equipment {id}: {ex.Message}");
            }
        }



        // GET: api/equipment/available
        [HttpGet("available")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Equipment>>> GetAvailableEquipment()
        {
            try
            {
                _logger.LogInformation("Getting all available equipment");

                // Since GetAvailableEquipmentAsync doesn't exist, get it directly from the context
                var equipment = await _dbContext.Equipment
                    .Where(e => e.Status == "Available")
                    .ToListAsync();

                if (equipment == null || !equipment.Any())
                {
                    return NotFound("No available equipment found.");
                }
                _logger.LogInformation($"Retrieved {equipment.Count} available equipment items");
                return Ok(equipment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available equipment");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/equipment/category/5
        [HttpGet("category/{categoryId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Equipment>>> GetEquipmentByCategory(int categoryId)
        {
            try
            {
                _logger.LogInformation($"Getting equipment in category: {categoryId}");

                await _equipmentService.GetEquipmentByCategoryAsync(categoryId);

                // Since the service method doesn't return data, get it directly from the context
                var equipment = await _dbContext.Equipment
                    .Where(e => e.CategoryId == categoryId)
                    .ToListAsync();

                if (equipment == null || !equipment.Any())
                {
                    return NotFound($"No equipment found in category {categoryId}");
                }
                _logger.LogInformation($"Retrieved {equipment.Count} equipment items in category {categoryId}");
                return Ok(equipment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving equipment in category: {categoryId}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("add")]
        //[Authorize(Roles = "Admin,WarehouseManager")]
        [AllowAnonymous]
        public async Task<ActionResult<Equipment>> AddEquipment([FromBody] EquipmentDto equipmentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Adding new equipment: {Name}", equipmentDto.Name);

                await _equipmentService.AddEquipmentAsync(equipmentDto);

                // Get the newly created equipment from the database
                var newEquipment = await _dbContext.Equipment
                    .OrderByDescending(e => e.Id)
                    .FirstOrDefaultAsync(e => e.Name == equipmentDto.Name);

                if (newEquipment == null)
                {
                    return BadRequest("Failed to add equipment.");
                }
                _logger.LogInformation("Added new equipment with ID: {Id}", newEquipment.Id);

                return CreatedAtAction(nameof(GetEquipment), new { id = newEquipment.Id }, newEquipment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding equipment: {ErrorMessage}", ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        //[Authorize(Roles = "Admin,WarehouseManager")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateEquipment(int id, [FromBody] EquipmentDto equipmentDto)
        {
            try
            {
                if (id != equipmentDto.Id)
                {
                    return BadRequest("ID in URL doesn't match ID in request body");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Updating equipment with ID: {Id}", id);

                var success = await _equipmentService.UpdateEquipmentAsync(equipmentDto);
                if (success)
                {
                    _logger.LogInformation("Updated equipment with ID: {Id}", id);
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("Equipment with ID: {Id} not found for update", id);
                    return NotFound($"Equipment with ID {id} not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating equipment with ID: {Id}", id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        // DELETE: api/equipment/5
        [HttpDelete("{id}")]
        //[Authorize(Roles = "Admin,WarehouseManager")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteEquipment(int id)
        {
            try
            {
                _logger.LogInformation($"Deleting equipment with ID: {id}");

                var success = await _equipmentService.DeleteEquipmentAsync(id);
                if (success)
                {
                    _logger.LogInformation($"Equipment with ID {id} deleted successfully");
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning($"Equipment with ID {id} not found for deletion");
                    return NotFound($"Equipment with ID {id} not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting equipment with ID: {id}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPatch("{id}")]
        //[Authorize(Roles = "Admin,WarehouseManager")]
        [AllowAnonymous]
        public async Task<IActionResult> PatchEquipment(int id, [FromBody] JsonPatchDocument<EquipmentDto> patchDoc)
        {
            try
            {
                if (patchDoc == null)
                {
                    return BadRequest("Invalid patch document");
                }

                _logger.LogInformation("Patching equipment with ID: {Id}", id);

                await _equipmentService.GetEquipmentByIdAsync(id);

                // Since the service method doesn't return data, get it directly from the context
                var equipment = await _dbContext.Equipment.FindAsync(id);

                if (equipment == null)
                {
                    _logger.LogWarning("Equipment with ID: {Id} not found for patching", id);
                    return NotFound($"Equipment with ID {id} not found");
                }

                var equipmentDto = new EquipmentDto
                {
                    Id = equipment.Id,
                    Name = equipment.Name,
                    Description = equipment.Description,
                    Value = equipment.Value,
                    Status = equipment.Status,
                    SerialNumber = equipment.SerialNumber,
                    Quantity = equipment.Quantity,
                    StorageLocation = equipment.StorageLocation
                };

                patchDoc.ApplyTo(equipmentDto, error =>
                {
                    ModelState.AddModelError(string.Empty, error.ErrorMessage);
                });

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var success = await _equipmentService.UpdateEquipmentAsync(equipmentDto);
                if (success)
                {
                    _logger.LogInformation("Patched equipment with ID: {Id}", id);
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("Failed to apply patch to equipment with ID: {Id}", id);
                    return BadRequest("Failed to apply patch");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching equipment with ID: {Id}", id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/equipment/5/quantity-info
        [HttpGet("{id}/quantity-info")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetEquipmentQuantityInfo(int id)
        {
            try
            {
                _logger.LogInformation($"Getting quantity information for equipment ID: {id}");

                // Check if equipment exists
                var equipment = await _dbContext.Equipment.FindAsync(id);
                if (equipment == null)
                {
                    return NotFound($"Equipment with ID {id} not found");
                }

                // Calculate in-use quantity (items currently checked out)
                var inUseQuantity = await _dbContext.EquipmentCheckouts
                    .Where(c => c.EquipmentId == id && c.Status != "Returned")
                    .SumAsync(c => c.Quantity);

                // Calculate available quantity
                var availableQuantity = Math.Max(0, equipment.Quantity - inUseQuantity);

                var result = new
                {
                    equipmentId = id,
                    equipmentName = equipment.Name,
                    totalQuantity = equipment.Quantity,
                    inUseQuantity = inUseQuantity,
                    availableQuantity = availableQuantity,
                    status = equipment.Status,
                    storageLocation = equipment.StorageLocation,
                    lastUpdated = equipment.LastUpdatedDate
                };

                _logger.LogInformation($"Equipment ID {id} quantity info: Total={equipment.Quantity}, In-Use={inUseQuantity}, Available={availableQuantity}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving quantity information for equipment ID: {id}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
