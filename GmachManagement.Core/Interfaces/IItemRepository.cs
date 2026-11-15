using System;
using System.Collections.Generic;
using System.Text;

using GmachManagement.Core.Entities;

namespace GmachManagement.Core.Interfaces;

public interface IItemRepository
{

    Task<List<Item>> GetAllAsync(
        string? search,
        bool availableOnly,
        int? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);


    Task<int?> GetCopyOwnerItemIdAsync(int copyId, CancellationToken ct);

    Task<bool> IsOwnedByAsync(int itemId, int ownerId, CancellationToken ct);

    Task<Item?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Item> AddAsync(
        Item item,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Item item,
        CancellationToken cancellationToken = default);

    Task<List<ItemCopy>> GetCopiesAsync(
        int itemId,
        CancellationToken cancellationToken = default);

    Task<ItemCopy?> AddCopyAsync(
        int itemId,
        CancellationToken cancellationToken = default);
    Task<ItemCopy?> GetCopyByIdAsync(int copyId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

    Task<bool> SetActiveAsync(
        int itemId,
        bool isActive,
        CancellationToken ct);

    Task<bool> AddCategoryAsync(
        int itemId,
        int categoryId,
        CancellationToken ct);

    Task<bool> RemoveCategoryAsync(
        int itemId,
        int categoryId,
        CancellationToken ct);

    Task<List<Category>?> GetCategoriesAsync(
        int itemId,
        CancellationToken ct);


}
