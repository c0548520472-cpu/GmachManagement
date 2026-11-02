using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.Entities;

namespace GmachManagement.Core.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(CancellationToken ct);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct);
    Task<Category> AddAsync(Category category, CancellationToken ct);
    Task<bool> UpdateAsync(int id, string name, CancellationToken ct);

    Task<bool> NameExistsAsync(
        string name,
        int? excludeId,
        CancellationToken ct);

    Task<bool> HasItemsAsync(int id, CancellationToken ct);

    Task<bool> DeleteAsync(int id, CancellationToken ct);


}

