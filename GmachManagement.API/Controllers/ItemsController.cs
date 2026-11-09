using System.Security.Claims;
﻿using Microsoft.AspNetCore.Mvc;


using GmachManagement.Core.DTOs;
using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;
using GmachManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GmachManagement.API.Controllers;

[ApiController]
[Route("api/items")]
[AllowAnonymous]
public class ItemsController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemsController(IItemService itemService)
    {
        
        _itemService = itemService;
    }



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

        var items = await _itemService.GetAllAsync(
            search,
            availableOnly,
            categoryId,
            page,
            pageSize,
            ct);

        return Ok(items.Select(ToDto).ToList());
    }


    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var item = await _itemService.GetByIdAsync(id, cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(ToDto(item));
    }

    [HttpPost]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<ActionResult<ItemDto>> Create(
        [FromBody] CreateItemDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId)) return Unauthorized();
        var item = await _itemService.AddAsync(
            dto.Name, dto.Description, ownerId, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = item.Id },
            ToDto(item));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CreateItemDto dto,
        CancellationToken cancellationToken)
    {
        if (!await OwnsItem(id, cancellationToken)) return NotFound();
        if (!TryGetOwnerId(out var ownerId)) return Unauthorized();
        var updated = await _itemService.UpdateAsync(
            id, dto.Name, dto.Description, ownerId, cancellationToken);

        if (!updated)
            return NotFound();

        return NoContent();
    }


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

        return Ok(copies.Select(ToCopyDto).ToList());
    }



    [HttpPost("{id:int}/copies")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<ActionResult<ItemCopyDto>> AddCopy(
        int id,
        CancellationToken cancellationToken)
    {
        if (!await OwnsItem(id, cancellationToken)) return NotFound();
        var copy = await _itemService.AddCopyAsync(
            id, cancellationToken);

        if (copy == null)
            return NotFound();

        return StatusCode(201, ToCopyDto(copy));
    }



    private bool TryGetOwnerId(out int ownerId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out ownerId) && ownerId > 0;

    private async Task<bool> OwnsItem(int itemId, CancellationToken ct) =>
        TryGetOwnerId(out var ownerId) && await _itemService.IsOwnedByAsync(itemId, ownerId, ct);

    private static ItemDto ToDto(Item item)
    {
        return new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            IsActive = item.IsActive,
            TotalCopies = item.Copies.Count,
            AvailableCopies = item.IsActive
                ? item.Copies.Count(c => c.Status == ItemStatus.Available)
                : 0
        };
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
            var copyOwner = await _itemService.GetCopyOwnerItemIdAsync(copyId, ct);
            if (copyOwner == null || !await OwnsItem(copyOwner.Value, ct)) return NotFound();
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


    private static ItemCopyDto ToCopyDto(ItemCopy copy)
    {
        return new ItemCopyDto
        {
            Id = copy.Id,
            ItemId = copy.ItemId,
            Status = copy.Status
        };
    }

    [HttpPut("{id:int}/active")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> SetActive(
        int id,
        [FromQuery] bool isActive,
        CancellationToken ct)
    {
        if (!await OwnsItem(id, ct)) return NotFound();
        var updated = await _itemService.SetActiveAsync(
            id, isActive, ct);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    [HttpGet("{id:int}/categories")]
    public async Task<IActionResult> GetCategories(
        int id,
        CancellationToken ct)
    {
        var categories = await _itemService.GetCategoriesAsync(id, ct);

        if (categories == null)
            return NotFound();

        return Ok(categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name
        }));
    }

    [HttpPost("{id:int}/categories/{categoryId:int}")]
    [Authorize(Roles = "GmachAdmin")]
    public async Task<IActionResult> AddCategory(
        int id,
        int categoryId,
        CancellationToken ct)
    {
        if (!await OwnsItem(id, ct)) return NotFound();
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
        if (!await OwnsItem(id, ct)) return NotFound();
        var result = await _itemService.RemoveCategoryAsync(
            id, categoryId, ct);

        return result ? NoContent() : NotFound();
    }


}

