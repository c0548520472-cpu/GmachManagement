
using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;

namespace GmachManagement.Core.Interfaces;

public interface IItemService
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
        string name,
        string description,
        int ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        int id,
        string name,
        string description,
        CancellationToken cancellationToken = default);

    Task<List<ItemCopy>> GetCopiesAsync(
        int itemId,
        CancellationToken cancellationToken = default);

    Task<ItemCopy?> AddCopyAsync(
        int itemId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateCopyStatusAsync(
        int copyId,
        ItemStatus status,
        CancellationToken ct);

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
