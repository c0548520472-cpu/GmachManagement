using GmachManagement.Core.DTOs;

namespace GmachManagement.Core.Interfaces;

public interface IPersonalTransactionService
{
    Task<PersonalTransactionDto?> GetByIdAsync(int userId, int transactionId, CancellationToken cancellationToken);
    Task<List<PersonalTransactionDto>> GetMineAsync(int userId, CancellationToken cancellationToken);
    Task<PersonalTransactionDto> AddAsync(int userId, CreatePersonalTransactionDto request, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(int userId, int transactionId, CancellationToken cancellationToken);
}
