using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;

namespace GmachManagement.Service.Services;

public class BorrowRequestService : IBorrowRequestService
{
    private readonly IBorrowRequestRepository _repository;
    public BorrowRequestService(IBorrowRequestRepository repository) => _repository = repository;

    public Task<BorrowRequest?> CreateAsync(int itemCopyId, string name, string? phone, string? email, CancellationToken ct) =>
        _repository.CreateAsync(itemCopyId, name, phone, email, ct);

    public Task<List<BorrowRequest>> GetPendingForOwnerAsync(int ownerId, CancellationToken ct) =>
        _repository.GetPendingForOwnerAsync(ownerId, ct);

    public Task<BorrowRequest?> FulfillAsync(int requestId, int ownerId, CancellationToken ct) =>
        _repository.FulfillAsync(requestId, ownerId, ct);
}
