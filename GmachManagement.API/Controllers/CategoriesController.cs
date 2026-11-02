

using GmachManagement.Core.DTOs;
using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GmachManagement.Core.Exceptions;


namespace GmachManagement.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;

    public CategoriesController(ICategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll(
        CancellationToken ct)
    {
        var categories = await _service.GetAllAsync(ct);
        return Ok(categories.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(
        int id,
        CancellationToken ct)
    {
        var category = await _service.GetByIdAsync(id, ct);

        if (category == null)
            return NotFound();

        return Ok(ToDto(category));
    }

    [HttpPost]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] SaveCategoryDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Category name is required.");


        try
        {
            var category = await _service.CreateAsync(dto.Name, ct);

            return CreatedAtAction(
                nameof(GetById),
                new { id = category.Id },
                ToDto(category));
        }
        catch (DuplicateCategoryNameException ex)
        {
            return Conflict(ex.Message);
        }

    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveCategoryDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Category name is required.");


        try
        {
            var updated = await _service.UpdateAsync(id, dto.Name, ct);
            return updated ? NoContent() : NotFound();
        }
        catch (DuplicateCategoryNameException ex)
        {
            return Conflict(ex.Message);
        }

    }

    private static CategoryDto ToDto(Category category)
        => new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);

        return deleted ? NoContent() : NotFound();
    }

}
