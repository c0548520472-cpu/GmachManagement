using System.Data;
using GmachManagement.Core.DTOs;
using GmachManagement.Core.Enums;
using GmachManagement.Core.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GmachManagement.Data.Repositories;

public sealed partial class SiteAdminRepository
{
    public async Task<List<OpenLoanDto>> OpenLoansAsync(CancellationToken ct)
    {
        var standard = db.Loans
            .Where(loan => loan.ActualReturnDate == null)
            .Select(loan => new
            {
                Kind = "standard",
                loan.Id,
                ItemName = loan.ItemCopy.Item.Name,
                BorrowerName = loan.User.Name,
                BorrowedAt = loan.LoanDate
            });

        var managed = from loan in db.ManagedLoans
                      where loan.ReturnedAt == null
                      join copy in db.ItemCopies on loan.ItemCopyId equals copy.Id into copies
                      from copy in copies.DefaultIfEmpty()
                      select new
                      {
                          Kind = "managed",
                          loan.Id,
                          ItemName = copy == null ? "פריט לא נמצא" : copy.Item.Name,
                          loan.BorrowerName,
                          loan.BorrowedAt
                      };

        var loans = await standard.Concat(managed)
            .OrderByDescending(loan => loan.BorrowedAt)
            .ThenBy(loan => loan.Kind)
            .ThenBy(loan => loan.Id)
            .ToListAsync(ct);

        return loans.Select(loan => new OpenLoanDto(
            loan.Kind, loan.Id, loan.ItemName, loan.BorrowerName, loan.BorrowedAt)).ToList();
    }

    public async Task<int> DeleteOpenLoansAsync(IReadOnlyCollection<OpenLoanKey> keys, bool all, CancellationToken ct)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var standardIds = keys.Where(key => key.Kind == "standard").Select(key => key.Id).ToArray();
            var managedIds = keys.Where(key => key.Kind == "managed").Select(key => key.Id).ToArray();
            var standard = await db.Loans.Where(loan => loan.ActualReturnDate == null &&
                (all || standardIds.Contains(loan.Id))).ToListAsync(ct);
            var managed = await db.ManagedLoans.Where(loan => loan.ReturnedAt == null &&
                (all || managedIds.Contains(loan.Id))).ToListAsync(ct);

            // Delete only the set the administrator reviewed, even when deleting all open loans.
            var current = standard.Select(loan => new OpenLoanKey("standard", loan.Id))
                .Concat(managed.Select(loan => new OpenLoanKey("managed", loan.Id))).ToHashSet();
            if (!current.SetEquals(keys))
                throw new ConcurrencyConflictException();

            db.Loans.RemoveRange(standard);
            db.ManagedLoans.RemoveRange(managed);
            await db.SaveChangesAsync(ct);

            var copyIds = standard.Select(loan => loan.ItemCopyId)
                .Union(managed.Select(loan => loan.ItemCopyId)).ToArray();
            var copies = await db.ItemCopies
                .Where(copy => copyIds.Contains(copy.Id) && copy.Status == ItemStatus.Borrowed &&
                    !db.Loans.Any(loan => loan.ItemCopyId == copy.Id && loan.ActualReturnDate == null) &&
                    !db.ManagedLoans.Any(loan => loan.ItemCopyId == copy.Id && loan.ReturnedAt == null))
                .ToListAsync(ct);

            foreach (var copy in copies)
                copy.Status = ItemStatus.Available;

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return current.Count;
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException ||
                                   IsTransactionConflict(ex) ||
                                   ex is DbUpdateException { InnerException: { } cause } && IsTransactionConflict(cause))
        {
            throw new ConcurrencyConflictException();
        }
    }

    private static bool IsTransactionConflict(Exception exception) =>
        exception is PostgresException { SqlState: "40001" or "40P01" } ||
        exception is SqlException { Number: 1205 };
}
