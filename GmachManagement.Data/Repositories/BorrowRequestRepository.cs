using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;
using GmachManagement.Core.Exceptions;
using GmachManagement.Core.Interfaces;
using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GmachManagement.Data.Repositories;

public class BorrowRequestRepository : IBorrowRequestRepository
{
    private readonly AppDbContext _db;
    public BorrowRequestRepository(AppDbContext db) => _db = db;

    public async Task<BorrowRequest?> CreateAsync(int itemCopyId, string name, string? phone, string? email, CancellationToken ct)
    {
        var copy = await _db.ItemCopies.AsNoTracking()
            .Include(c => c.Item)
            .FirstOrDefaultAsync(c => c.Id == itemCopyId, ct);
        if (copy == null || !copy.Item.IsActive || copy.Status != ItemStatus.Available || copy.Item.OwnerId == null)
            return null;
        var request = new BorrowRequest
        {
            ItemCopyId = itemCopyId,
            ApplicantName = name.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _db.BorrowRequests.Add(request);
        await _db.SaveChangesAsync(ct);
        return request;
    }

    public Task<List<BorrowRequest>> GetPendingForOwnerAsync(int ownerId, CancellationToken ct) =>
        _db.BorrowRequests.AsNoTracking()
            .Include(r => r.ItemCopy).ThenInclude(c => c.Item)
            .Where(r => r.FulfilledAt == null && r.ItemCopy.Item.OwnerId == ownerId)
            .OrderBy(r => r.CreatedAt).ToListAsync(ct);

    public async Task<BorrowRequest?> FulfillAsync(int requestId, int ownerId, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var request = await _db.BorrowRequests
            .Include(r => r.ItemCopy).ThenInclude(c => c.Item)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ItemCopy.Item.OwnerId == ownerId, ct);
        if (request == null) return null;
        if (request.FulfilledAt != null || !request.ItemCopy.Item.IsActive ||
            request.ItemCopy.Status != ItemStatus.Available)
            throw new ConcurrencyConflictException();
        request.ItemCopy.Status = ItemStatus.Borrowed;
        request.FulfilledAt = DateTime.UtcNow;
        try
        {
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw new ConcurrencyConflictException();
        }
        return request;
    }
}
