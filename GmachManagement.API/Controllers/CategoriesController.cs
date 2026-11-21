

using GmachManagement.Core.DTOs;
using AutoMapper;
using GmachManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using GmachManagement.Core.Exceptions;


namespace GmachManagement.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;
    private readonly IMemoryCache _cache;

    private readonly IMapper _mapper;

    public CategoriesController(ICategoryService service, IMemoryCache cache, IMapper mapper)
    {
        _service = service;
        _cache = cache;
        _mapper = mapper;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll(
        CancellationToken ct)
    {
        var categories = await _cache.GetOrCreateAsync("public-categories", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
            return _mapper.Map<List<CategoryDto>>(await _service.GetAllAsync(ct));
        });
        return Ok(categories);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var category = await _service.GetByIdAsync(id, ct);

        if (category == null)
            return NotFound();

        return Ok(_mapper.Map<CategoryDto>(category));
    }

    [HttpPost]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] SaveCategoryDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var category = await _service.CreateAsync(dto.Name, ct);

            _cache.Remove("public-categories");
            return CreatedAtAction(
                nameof(GetById),
                new { id = category.Id },
                _mapper.Map<CategoryDto>(category));
        }
        catch (DuplicateCategoryNameException ex)
        {
            return Conflict(ex.Message);
        }

    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] SaveCategoryDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Category name is required.");


        try
        {
            var updated = await _service.UpdateAsync(id, dto.Name, ct);
            if (updated) _cache.Remove("public-categories");
            return updated ? NoContent() : NotFound();
        }
        catch (DuplicateCategoryNameException ex)
        {
            return Conflict(ex.Message);
        }

    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);

        if (deleted) _cache.Remove("public-categories");
        return deleted ? NoContent() : NotFound();
    }

}
