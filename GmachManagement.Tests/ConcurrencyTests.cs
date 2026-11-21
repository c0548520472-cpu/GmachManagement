using System;
using System.Collections.Generic;
using System.Text;



using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;
using GmachManagement.Data.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GmachManagement.Tests;

public class ConcurrencyTests
{
    [Fact]
    public async Task TwoContexts_UpdatingSameCopy_SecondUpdateFails()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setup = new AppDbContext(options))
        {
            // יצירת טבלת בדיקה בלבד, ללא Seed Data
            await setup.Database.ExecuteSqlRawAsync("""
                CREATE TABLE ItemCopies (
                    Id INTEGER PRIMARY KEY,
                    ItemId INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    RowVersion BLOB NOT NULL
                );
                """);

            await setup.Database.ExecuteSqlRawAsync("""
                INSERT INTO ItemCopies
                    (Id, ItemId, Status, RowVersion)
                VALUES
                    (100, 1, 1, X'0102030405060708');
                """);
        }

        await using var context1 = new AppDbContext(options);
        await using var context2 = new AppDbContext(options);

        var copy1 = await context1.ItemCopies
            .SingleAsync(c => c.Id == 100);

        var copy2 = await context2.ItemCopies
            .SingleAsync(c => c.Id == 100);

        // שני התהליכים קראו את אותה גרסה
        Assert.Equal(copy1.RowVersion, copy2.RowVersion);

        // SQLite does not provide SQL Server rowversion automatically.
        // Simulate database-managed rowversion with a trigger, not a manual
        // update between the two SaveChanges calls.
        // The trigger is installed before either context saves.
        await context1.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER ItemCopies_UpdateVersion
            AFTER UPDATE OF Status ON ItemCopies
            BEGIN
                UPDATE ItemCopies
                SET RowVersion = randomblob(8)
                WHERE Id = NEW.Id;
            END;
            """);

        copy1.Status = ItemStatus.Borrowed;
        await context1.SaveChangesAsync();

        // Confirm the database actually changed the concurrency token.
        await using (var verification = new AppDbContext(options))
        {
            var saved = await verification.ItemCopies.AsNoTracking()
                .SingleAsync(c => c.Id == 100);
            Assert.Equal(ItemStatus.Borrowed, saved.Status);
            Assert.NotEqual(Convert.ToHexString(copy2.RowVersion),
                Convert.ToHexString(saved.RowVersion));
        }

        // התהליך השני עדיין מחזיק בגרסה הישנה
        copy2.Status = ItemStatus.Reserved;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => context2.SaveChangesAsync());
    }
}
