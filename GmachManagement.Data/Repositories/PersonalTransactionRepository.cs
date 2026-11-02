using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GmachManagement.Data.Repositories;

public class PersonalTransactionRepository : IPersonalTransactionRepository
{
    private readonly AppDbContext _context;

    public PersonalTransactionRepository(AppDbContext context) => _context = context;

    public Task<List<PersonalTransaction>> GetByUserAsync(int userId, CancellationToken cancellationToken) =>
        _context.PersonalTransactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

    public async Task<PersonalTransaction> AddAsync(PersonalTransaction transaction, CancellationToken cancellationToken)
    {
        _context.PersonalTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<bool> CompleteAsync(int userId, int transactionId, CancellationToken cancellationToken)
    {
        var transaction = await _context.PersonalTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId, cancellationToken);
        if (transaction is null) return false;
        if (!transaction.IsCompleted)
        {
            transaction.IsCompleted = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
        return true;
    }
}
