using System;
using System.Collections.Generic;
using System.Text;

using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using GmachManagement.Core.Enums;
using GmachManagement.Data.Context;

using GmachManagement.Core.Exceptions;

namespace GmachManagement.Data.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly AppDbContext _context;

    public ItemRepository(AppDbContext context)
    {
        _context = context;
    }



    public async Task<List<Item>> GetAllAsync(
        string? search,
        bool availableOnly,
        int? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Items.AsNoTracking().Where(i => i.IsActive).AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(i =>
                i.Categories.Any(c => c.Id == categoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(i =>

            EF.Functions.Like(i.Name, "%" + term + "%") ||
            (i.Description != null &&
             EF.Functions.Like(i.Description, "%" + term + "%")));

        }

        if (availableOnly)
        {
            query = query.Where(i =>
                i.IsActive &&
                i.Copies.Any(c => c.Status == ItemStatus.Available));
        }

        return await query
            .OrderBy(i => i.Name)
            .ThenBy(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(i => i.Copies)
            .Include(i => i.Categories)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }


    public async Task<Item?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Items
            .Include(i => i.Copies)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Item> AddAsync(
        Item item,
        CancellationToken cancellationToken = default)
    {
        _context.Items.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<bool> UpdateAsync(
        Item item,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == item.Id, cancellationToken);

        if (existing == null)
            return false;

        existing.Name = item.Name;
        existing.Description = item.Description;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<ItemCopy>> GetCopiesAsync(
        int itemId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ItemCopies
            .AsNoTracking()
            .Where(c => c.ItemId == itemId)
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ItemCopy?> AddCopyAsync(
        int itemId,
        CancellationToken cancellationToken = default)
    {
        var itemExists = await _context.Items
            .AnyAsync(i => i.Id == itemId, cancellationToken);

        if (!itemExists)
            return null;

        var copy = new ItemCopy
        {
            ItemId = itemId
        };

        _context.ItemCopies.Add(copy);
        await _context.SaveChangesAsync(cancellationToken);

        return copy;
    }

    public async Task<ItemCopy?> GetCopyByIdAsync(
        int copyId,
        CancellationToken ct)
    {
        return await _context.ItemCopies
            .Include(c => c.Loans)
            .FirstOrDefaultAsync(c => c.Id == copyId, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> SetActiveAsync(
        int itemId,
        bool isActive,
        CancellationToken ct)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        if (item == null)
            return false;

        item.IsActive = isActive;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AddCategoryAsync(
        int itemId,
        int categoryId,
        CancellationToken ct)
    {
        var item = await _context.Items
            .Include(i => i.Categories)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        if (item == null)
            return false;

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category == null)
            return false;

        if (!item.Categories.Any(c => c.Id == categoryId))
        {
            item.Categories.Add(category);
            await _context.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task<bool> RemoveCategoryAsync(
        int itemId,
        int categoryId,
        CancellationToken ct)
    {
        var item = await _context.Items
            .Include(i => i.Categories)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        if (item == null)
            return false;

        var category = item.Categories
            .FirstOrDefault(c => c.Id == categoryId);

        if (category == null)
            return false;

        item.Categories.Remove(category);
        await _context.SaveChangesAsync(ct);

        return true;
    }

    public async Task<List<Category>?> GetCategoriesAsync(
        int itemId,
        CancellationToken ct)
    {
        var item = await _context.Items
            .AsNoTracking()
            .Include(i => i.Categories)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        return item?.Categories
            .OrderBy(c => c.Name)
            .ToList();
    }



}

