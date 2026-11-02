using GmachManagement.Core.Entities;
using GmachManagement.Core.Exceptions;
using GmachManagement.Core.Interfaces;
using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GmachManagement.Data.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Category>> GetAllAsync(CancellationToken ct)
    {
        return _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct)
    {
        return _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Category> AddAsync(
        Category category,
        CancellationToken ct)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(ct);
        return category;
    }

    public async Task<bool> UpdateAsync(
        int id,
        string name,
        CancellationToken ct)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category == null)
            return false;

        category.Name = name;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> NameExistsAsync(
        string name,
        int? excludeId,
        CancellationToken ct)
    {
        return _context.Categories.AnyAsync(
            c => c.Name == name &&
                 (!excludeId.HasValue || c.Id != excludeId.Value),
            ct);
    }

    public Task<bool> HasItemsAsync(
        int id,
        CancellationToken ct)
    {
        return _context.Categories
            .Where(c => c.Id == id)
            .AnyAsync(c => c.Items.Any(), ct);
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken ct)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category == null)
            return false;

        if (await _context.Categories
            .Where(c => c.Id == id)
            .AnyAsync(c => c.Items.Any(), ct))
        {

            throw new ConcurrencyConflictException();

        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(ct);

        return true;
    }


}
