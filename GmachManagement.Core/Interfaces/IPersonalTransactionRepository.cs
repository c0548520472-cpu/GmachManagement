using GmachManagement.Core.Entities;

namespace GmachManagement.Core.Interfaces;

public interface IPersonalTransactionRepository
{
    Task<List<PersonalTransaction>> GetByUserAsync(int userId, CancellationToken cancellationToken);
    Task<PersonalTransaction> AddAsync(PersonalTransaction transaction, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(int userId, int transactionId, CancellationToken cancellationToken);
}
