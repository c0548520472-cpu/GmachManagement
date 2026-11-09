using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;
using GmachManagement.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace GmachManagement.Service.Services;

public class ItemService : IItemService
{
    private readonly IItemRepository _itemRepository;

    public ItemService(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }


    public Task<List<Item>> GetAllAsync(
        string? search,
        bool availableOnly,
        int? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return _itemRepository.GetAllAsync(
            search,
            availableOnly,
            categoryId,
            page,
            pageSize,
            cancellationToken);
    }


    public Task<int?> GetCopyOwnerItemIdAsync(int copyId, CancellationToken ct) =>
        _itemRepository.GetCopyOwnerItemIdAsync(copyId, ct);

    public Task<bool> IsOwnedByAsync(int itemId, int ownerId, CancellationToken ct) =>
        _itemRepository.IsOwnedByAsync(itemId, ownerId, ct);

    public Task<Item?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _itemRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<Item> AddAsync(
        string name,
        string description,
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        var item = new Item
        {
            Name = name.Trim(),
            Description = description.Trim(),
            OwnerId = ownerId
        };

        return _itemRepository.AddAsync(item, cancellationToken);
    }

    public Task<bool> UpdateAsync(
        int id,
        string name,
        string description,
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        var item = new Item
        {
            Id = id,
            Name = name.Trim(),
            Description = description.Trim(),
            OwnerId = ownerId
        };

        return _itemRepository.UpdateAsync(item, cancellationToken);
    }

    public Task<List<ItemCopy>> GetCopiesAsync(
        int itemId,
        CancellationToken cancellationToken = default)
    {
        return _itemRepository.GetCopiesAsync(itemId, cancellationToken);
    }

    public Task<ItemCopy?> AddCopyAsync(
        int itemId,
        CancellationToken cancellationToken = default)
    {
        return _itemRepository.AddCopyAsync(itemId, cancellationToken);
    }

    public async Task<bool> UpdateCopyStatusAsync(
        int copyId,
        ItemStatus status,
        CancellationToken ct)
    {
        var copy = await _itemRepository.GetCopyByIdAsync(copyId, ct);

        if (copy == null)
            return false;

        if (copy.Loans.Any(l => l.ActualReturnDate == null))
            throw new InvalidOperationException(
                "Cannot change the status of a copy with an active loan.");

        if (status == ItemStatus.Borrowed)
            throw new InvalidOperationException(
                "Borrowed status must be set through the loan process.");

        copy.Status = status;

        await _itemRepository.SaveChangesAsync(ct);
        return true;

    }

    public Task<bool> SetActiveAsync(
        int itemId,
        bool isActive,
        CancellationToken ct)
    {
        return _itemRepository.SetActiveAsync(
            itemId, isActive, ct);
    }

    public Task<bool> AddCategoryAsync(
        int itemId,
        int categoryId,
        CancellationToken ct)
    {
        return _itemRepository.AddCategoryAsync(itemId, categoryId, ct);
    }

    public Task<bool> RemoveCategoryAsync(
        int itemId,
        int categoryId,
        CancellationToken ct)
    {
        return _itemRepository.RemoveCategoryAsync(itemId, categoryId, ct);
    }

    public Task<List<Category>?> GetCategoriesAsync(
        int itemId,
        CancellationToken ct)
    {
        return _itemRepository.GetCategoriesAsync(itemId, ct);
    }



}

