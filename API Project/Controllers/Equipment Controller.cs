using Domain_Project.DTOs.Domain_Project.DTOs;
using Domain_Project.DTOs.Domain_Project.DTOs.Domain_Project.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentService _service;

    public EquipmentController(IEquipmentService service)
    {
        _service = service;
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] EquipmentDto dto)
    {
        await _service.AddEquipmentAsync(dto);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable() =>
        Ok(await _service.GetAvailableAsync());
}
