using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.Entities;

namespace GmachManagement.Core.Interfaces;

public interface ICategoryService
{
    Task<List<Category>> GetAllAsync(CancellationToken ct);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct);
    Task<Category> CreateAsync(string name, CancellationToken ct);
    Task<bool> UpdateAsync(int id, string name, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);

}

