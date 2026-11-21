using AutoMapper;
using GmachManagement.Core.DTOs;
using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;
using GmachManagement.Core.Interfaces;

namespace GmachManagement.Service.Services;

public class PersonalTransactionService : IPersonalTransactionService
{
    private readonly IPersonalTransactionRepository _repository;
    private readonly IMapper _mapper;

    public PersonalTransactionService(IPersonalTransactionRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<PersonalTransactionDto>> GetMineAsync(int userId, CancellationToken cancellationToken) =>
        _mapper.Map<List<PersonalTransactionDto>>(await _repository.GetByUserAsync(userId, cancellationToken));

    public async Task<PersonalTransactionDto?> GetByIdAsync(int userId, int transactionId, CancellationToken cancellationToken) =>
        _mapper.Map<PersonalTransactionDto?>(await _repository.GetByIdAsync(userId, transactionId, cancellationToken));

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
            Amount = request.Amount,
            TransactionDate = request.TransactionDate,
            ReminderDate = request.ReminderDate,
            IsCompleted = false
        };
        return _mapper.Map<PersonalTransactionDto>(await _repository.AddAsync(transaction, cancellationToken));
    }

    public Task<bool> CompleteAsync(int userId, int transactionId, CancellationToken cancellationToken) =>
        _repository.CompleteAsync(userId, transactionId, cancellationToken);
}
