using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
using GmachManagement.Core.Enums;
using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore;
using GmachManagement.Core.Exceptions;
namespace GmachManagement.Data.Repositories
{
    public class LoanRepository : ILoanRepository
    {
        public async Task<bool> ReturnAsAdminAsync(int loanId, CancellationToken ct)
        {
            var loan = await _context.Loans.AsNoTracking().FirstOrDefaultAsync(l => l.Id == loanId, ct);
            if (loan is null) return false;
            if (loan.ActualReturnDate is not null) return true;
            return await ReturnItemAsync(loanId, loan.UserId, ct);
        }

        public async Task<bool> ChangeReturnDateAsync(int loanId, DateTime date, CancellationToken ct)
        {
            var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == loanId, ct);
            if (loan is null || loan.ActualReturnDate is not null) return false;
            loan.ExpectedReturnDate = date;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private readonly AppDbContext _context;

        public LoanRepository(AppDbContext context)
        {
            _context = context;
        }


        public async Task<List<Loan>> GetActiveLoansAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.Loans
                .AsNoTracking()
                .Where(l => l.ActualReturnDate == null)
                .OrderBy(l => l.ExpectedReturnDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Loan>> GetOverdueLoansAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _context.Loans
                .AsNoTracking()
                .Where(l =>
                    l.ActualReturnDate == null &&
                    l.ExpectedReturnDate < now)
                .OrderBy(l => l.ExpectedReturnDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Loan>> GetActiveLoansByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Loans
                .AsNoTracking()
                .Where(l =>
                    l.UserId == userId &&
                    l.ActualReturnDate == null)
                .OrderBy(l => l.ExpectedReturnDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Loan>> GetLoansByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Loans
                .AsNoTracking()
                .Where(loan => loan.UserId == userId)
                .Include(loan => loan.ItemCopy)
                    .ThenInclude(copy => copy.Item)
                        .ThenInclude(item => item.Gmach)
                .OrderByDescending(loan => loan.LoanDate)
                .ToListAsync(cancellationToken);
        }


        public async Task<List<Loan>> GetAllLoansAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.Loans
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }



        public async Task<Loan?> BorrowItemAsync(
            int userId,
            int itemCopyId,
            DateTime expectedReturnDate,
            CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var itemCopy = await _context.ItemCopies
                    .Include(c => c.Item)
                    .FirstOrDefaultAsync(
                        c => c.Id == itemCopyId,
                        cancellationToken);

                if (itemCopy == null)
                {
                    return null;
                }

                // מניעת השאלה של פריט מושבת
                if (!itemCopy.Item.IsActive)
                {
                    throw new ConcurrencyConflictException();
                }

                // מניעת השאלה של עותק שאינו זמין
                if (itemCopy.Status != ItemStatus.Available)
                {
                    throw new ConcurrencyConflictException();
                }

                itemCopy.Status = ItemStatus.Borrowed;

                var loan = new Loan
                {
                    UserId = userId,
                    ItemCopyId = itemCopyId,
                    LoanDate = DateTime.UtcNow,
                    ExpectedReturnDate = expectedReturnDate
                };

                _context.Loans.Add(loan);

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return loan;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new ConcurrencyConflictException();
            }
        }


        public async Task<bool> ReturnItemAsync(
            int loanId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var loan = await _context.Loans
                    .Include(l => l.ItemCopy)
                    .FirstOrDefaultAsync(
                        l => l.Id == loanId && l.UserId == userId,
                        cancellationToken);

                if (loan == null)
                {
                    return false;
                }

                if (loan.ActualReturnDate != null ||
                    loan.ItemCopy.Status != ItemStatus.Borrowed)
                {
                    throw new ConcurrencyConflictException();
                }

                loan.ActualReturnDate = DateTime.UtcNow;
                loan.ItemCopy.Status = ItemStatus.Available;

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new ConcurrencyConflictException();
            }
        }


    }
}

