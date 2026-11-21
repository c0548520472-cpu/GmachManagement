using Microsoft.AspNetCore.Mvc;
using AutoMapper;


using GmachManagement.Core.DTOs;
using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;
using GmachManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GmachManagement.Service.Services;

namespace GmachManagement.API.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IItemService _itemService;
    private readonly GmachService _gmachService;
    private int OwnerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public ItemsController(IItemService itemService, GmachService gmachService, IMapper mapper)
    {
        
        _mapper = mapper;
        _itemService = itemService;
        _gmachService = gmachService;
    }



    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetAll(
    [FromQuery] string? search,
    [FromQuery] bool availableOnly = false,
    [FromQuery] int? categoryId = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Page must be at least 1 and pageSize must be between 1 and 100.");
        }

        if ((page - 1L) * pageSize > int.MaxValue)
        {
            return BadRequest("The requested page offset is too large.");
        }

        var items = await _itemService.GetAllAsync(
            search,
            availableOnly,
            categoryId,
            page,
            pageSize,
            ct);

        return Ok(_mapper.Map<List<ItemDto>>(items));
    }


    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemDto>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var item = await _itemService.GetByIdAsync(id, cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(_mapper.Map<ItemDto>(item));
    }

    [HttpPost]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<ActionResult<ItemDto>> Create(
        [FromBody] CreateItemDto dto,
        CancellationToken cancellationToken)
    {
        var gmach = await _gmachService.GetMineAsync(OwnerId, cancellationToken);
        if (gmach == null) return Forbid();
        var item = await _gmachService.AddItemAsync(gmach.Id, dto.Name.Trim(), dto.Description?.Trim() ?? string.Empty, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = item.Id },
            _mapper.Map<ItemDto>(item));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] CreateItemDto dto,
        CancellationToken cancellationToken)
    {
        if (!await _gmachService.OwnsItemAsync(OwnerId,id,cancellationToken)) return Forbid();
        var updated = await _itemService.UpdateAsync(
            id, dto.Name, dto.Description, cancellationToken);

        if (!updated)
            return NotFound();

        return NoContent();
    }


    [Authorize]
    [HttpGet("{id:int}/copies")]
    public async Task<ActionResult<IEnumerable<ItemCopyDto>>> GetCopies(
        int id,
        CancellationToken cancellationToken)
    {
        var item = await _itemService.GetByIdAsync(
            id, cancellationToken);

        if (item == null)
            return NotFound();

        var copies = await _itemService.GetCopiesAsync(
            id, cancellationToken);

        return Ok(_mapper.Map<List<ItemCopyDto>>(copies));
    }



    [Authorize]
    [HttpGet("{id:int}/copies/{copyId:int}")]
    public async Task<ActionResult<ItemCopyDto>> GetCopyById([FromRoute] int id, [FromRoute] int copyId, CancellationToken ct)
    {
        var item = await _itemService.GetByIdAsync(id, ct);
        if (item == null) return NotFound();
        var copies = await _itemService.GetCopiesAsync(id, ct);
        var copy = copies.FirstOrDefault(c => c.Id == copyId);
        return copy == null ? NotFound() : Ok(_mapper.Map<ItemCopyDto>(copy));
    }

    [HttpPost("{id:int}/copies")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<ActionResult<ItemCopyDto>> AddCopy(
        int id,
        CancellationToken cancellationToken)
    {
        if (!await _gmachService.OwnsItemAsync(OwnerId,id,cancellationToken)) return Forbid();
        var copy = await _itemService.AddCopyAsync(
            id, cancellationToken);

        if (copy == null)
            return NotFound();

        return CreatedAtAction(nameof(GetCopyById), new { id, copyId = copy.Id }, _mapper.Map<ItemCopyDto>(copy));
    }



    [HttpPost("{id:int}/copies/batch")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> AddCopies(int id,[FromQuery] int quantity,CancellationToken ct)
    {
        if(quantity<1||quantity>1000)return BadRequest(new {message="יש לבחור כמות בין 1 ל־1000."});
        if(!await _gmachService.OwnsItemAsync(OwnerId,id,ct))return Forbid();
        var added=await _gmachService.AddCopiesAsync(id,quantity,ct);
        return added==0?NotFound():Ok(new {added});
    }

    [HttpPut("copies/{copyId:int}/status")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> UpdateCopyStatus(
        int copyId,
        [FromBody] UpdateItemCopyStatusDto dto,
        CancellationToken ct)
    {
        try
        {
            var itemId = await _gmachService.GetItemIdForCopyAsync(copyId, ct);
            if (itemId == null || !await _gmachService.OwnsItemAsync(OwnerId,itemId.Value,ct)) return Forbid();
            var updated = await _itemService.UpdateCopyStatusAsync(
                copyId, dto.Status, ct);

            if (!updated)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }


    [HttpPut("{id:int}/active")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> SetActive(
        int id,
        [FromQuery] bool isActive,
        CancellationToken ct)
    {
        if (!await _gmachService.OwnsItemAsync(OwnerId,id,ct)) return Forbid();
        var updated = await _itemService.SetActiveAsync(
            id, isActive, ct);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/categories")]
    public async Task<IActionResult> GetCategories(
        int id,
        CancellationToken ct)
    {
        var categories = await _itemService.GetCategoriesAsync(id, ct);

        if (categories == null)
            return NotFound();

        return Ok(_mapper.Map<List<CategoryDto>>(categories));
    }

    [HttpPost("{id:int}/categories/{categoryId:int}")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> AddCategory(
        int id,
        int categoryId,
        CancellationToken ct)
    {
        if (!await _gmachService.OwnsItemAsync(OwnerId,id,ct)) return Forbid();
        var result = await _itemService.AddCategoryAsync(
            id, categoryId, ct);

        return result ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}/categories/{categoryId:int}")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> RemoveCategory(
        int id,
        int categoryId,
        CancellationToken ct)
    {
        if (!await _gmachService.OwnsItemAsync(OwnerId,id,ct)) return Forbid();
        var result = await _itemService.RemoveCategoryAsync(
            id, categoryId, ct);

        return result ? NoContent() : NotFound();
    }


}

