using GmachManagement.Core.DTOs;
using GmachManagement.Core.Enums;
using GmachManagement.Core.Exceptions;
using GmachManagement.Data.Context;
using GmachManagement.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GmachManagement.Tests;

public class SiteAdminOpenLoansTests
{
    [Fact]
    public async Task DeleteOpenLoansPreservesHistoryAndReleasesOnlyCopiesWithoutOpenLoans()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE Users (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);
            CREATE TABLE Items (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);
            CREATE TABLE ItemCopies (
                Id INTEGER PRIMARY KEY, ItemId INTEGER NOT NULL, Status INTEGER NOT NULL,
                RowVersion BLOB NOT NULL DEFAULT X'0102030405060708');
            CREATE TABLE Loans (
                Id INTEGER PRIMARY KEY, UserId INTEGER NOT NULL, ItemCopyId INTEGER NOT NULL,
                LoanDate TEXT NOT NULL, ExpectedReturnDate TEXT NOT NULL, ActualReturnDate TEXT);
            CREATE TABLE ManagedLoans (
                Id INTEGER PRIMARY KEY, GmachId INTEGER NOT NULL, ItemCopyId INTEGER NOT NULL,
                BorrowerName TEXT NOT NULL, BorrowerPhone TEXT, BorrowerEmail TEXT,
                BorrowedAt TEXT NOT NULL, ReturnedAt TEXT);
            INSERT INTO Users VALUES (1, 'Borrower');
            INSERT INTO Items VALUES (1, 'Item');
            INSERT INTO ItemCopies (Id, ItemId, Status) VALUES (1, 1, 2), (2, 1, 2), (3, 1, 3);
            INSERT INTO Loans VALUES
                (1, 1, 1, '2026-11-01', '2026-12-01', NULL),
                (2, 1, 2, '2026-11-02', '2026-12-01', NULL),
                (3, 1, 1, '2026-10-01', '2026-10-02', '2026-10-02'),
                (4, 1, 3, '2026-11-03', '2026-12-01', NULL);
            INSERT INTO ManagedLoans VALUES
                (1, 1, 2, 'Managed borrower', NULL, NULL, '2026-11-04', NULL),
                (2, 1, 1, 'Managed borrower', NULL, NULL, '2026-10-03', '2026-10-04');
            """);

        var repository = new SiteAdminRepository(db);
        var open = await repository.OpenLoansAsync(default);
        Assert.Equal(4, open.Count);
        Assert.Equal("Item", open[0].ItemName);
        Assert.Equal("managed", open[0].Kind);

        OpenLoanKey[] selected = [new("standard", 1), new("standard", 2)];
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            repository.DeleteOpenLoansAsync(selected, all: true, default));
        Assert.Equal(4, (await repository.OpenLoansAsync(default)).Count);

        Assert.Equal(2, await repository.DeleteOpenLoansAsync(selected, all: false, default));
        Assert.Equal(
            [ItemStatus.Available, ItemStatus.Borrowed, ItemStatus.Reserved],
            await db.ItemCopies.AsNoTracking().OrderBy(copy => copy.Id).Select(copy => copy.Status).ToArrayAsync());

        var remaining = (await repository.OpenLoansAsync(default))
            .Select(loan => new OpenLoanKey(loan.Kind, loan.Id)).ToArray();
        Assert.Equal(2, await repository.DeleteOpenLoansAsync(remaining, all: true, default));
        Assert.Empty(await repository.OpenLoansAsync(default));
        Assert.Equal(3, (await db.Loans.SingleAsync()).Id);
        Assert.Equal(2, (await db.ManagedLoans.SingleAsync()).Id);
        Assert.Equal(
            [ItemStatus.Available, ItemStatus.Available, ItemStatus.Reserved],
            await db.ItemCopies.AsNoTracking().OrderBy(copy => copy.Id).Select(copy => copy.Status).ToArrayAsync());
    }
}
