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

        // התהליך הראשון מעדכן את העותק
        copy1.Status = ItemStatus.Borrowed;

        await context1.Database.ExecuteSqlRawAsync("""
            UPDATE ItemCopies
            SET RowVersion = X'1112131415161718'
            WHERE Id = 100;
            """);

        // מגדירים את הגרסה החדשה כגרסה המקורית
        // לצורך שמירת העדכון הראשון
        context1.Entry(copy1)
            .Property(c => c.RowVersion)
            .OriginalValue =
                Convert.FromHexString("1112131415161718");

        await context1.SaveChangesAsync();

        // התהליך השני עדיין מחזיק בגרסה הישנה
        copy2.Status = ItemStatus.Reserved;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => context2.SaveChangesAsync());
    }
}
