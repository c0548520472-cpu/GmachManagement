using GmachManagement.Core.DTOs;
using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;
using GmachManagement.Core.Interfaces;

namespace GmachManagement.Service.Services;

public class PersonalTransactionService : IPersonalTransactionService
{
    private readonly IPersonalTransactionRepository _repository;

    public PersonalTransactionService(IPersonalTransactionRepository repository) => _repository = repository;

    private static PersonalTransactionDto Map(PersonalTransaction t) => new()
    {
        Id = t.Id,
        OtherPartyName = t.OtherPartyName,
        ItemDescription = t.ItemDescription,
        Type = t.Type,
        TransactionDate = t.TransactionDate,
        ReminderDate = t.ReminderDate,
        IsCompleted = t.IsCompleted
    };

    public async Task<List<PersonalTransactionDto>> GetMineAsync(int userId, CancellationToken cancellationToken) =>
        (await _repository.GetByUserAsync(userId, cancellationToken)).Select(Map).ToList();

    public async Task<PersonalTransactionDto> AddAsync(int userId, CreatePersonalTransactionDto request, CancellationToken cancellationToken)
    {
        if (request.Type != TransactionType.Borrowed && request.Type != TransactionType.Lent)
            throw new ArgumentException("Invalid transaction type.");
        if (string.IsNullOrWhiteSpace(request.OtherPartyName) || string.IsNullOrWhiteSpace(request.ItemDescription))
            throw new ArgumentException("Name and item description are required.");
        if (request.TransactionDate == default || request.TransactionDate > DateTime.UtcNow)
            throw new ArgumentException("Transaction date must not be in the future.");
        var transaction = new PersonalTransaction
        {
            UserId = userId,
            OtherPartyName = request.OtherPartyName.Trim(),
            ItemDescription = request.ItemDescription.Trim(),
            Type = request.Type,
            TransactionDate = request.TransactionDate,
            ReminderDate = request.ReminderDate,
            IsCompleted = false
        };
        return Map(await _repository.AddAsync(transaction, cancellationToken));
    }

    public Task<bool> CompleteAsync(int userId, int transactionId, CancellationToken cancellationToken) =>
        _repository.CompleteAsync(userId, transactionId, cancellationToken);
}
