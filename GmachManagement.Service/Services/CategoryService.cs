using System;
using System.Collections.Generic;
using System.Text;

using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
using GmachManagement.Core.Exceptions;

namespace GmachManagement.Service.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Category>> GetAllAsync(CancellationToken ct)
        => _repository.GetAllAsync(ct);

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct)
        => _repository.GetByIdAsync(id, ct);


    public async Task<Category> CreateAsync(
        string name,
        CancellationToken ct)
    {
        name = name.Trim();

        if (await _repository.NameExistsAsync(name, null, ct))
            throw new DuplicateCategoryNameException();

        var category = new Category { Name = name };
        return await _repository.AddAsync(category, ct);
    }

    public async Task<bool> UpdateAsync(
        int id,
        string name,
        CancellationToken ct)
    {
        name = name.Trim();

        if (await _repository.NameExistsAsync(name, id, ct))
            throw new DuplicateCategoryNameException();

        return await _repository.UpdateAsync(id, name, ct);
    }

    public Task<bool> DeleteAsync(
        int id,
        CancellationToken ct)
    {
        return _repository.DeleteAsync(id, ct);
    }


}

