using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GmachManagement.Data.Migrations.PostgreSQL;

[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261117010000_GmachChatReadReceipts")]
public sealed class GmachChatReadReceipts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<DateTime>(
            name: "ReadByOwnerAt", table: "GmachChatMessages", type: "timestamp with time zone", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn(name: "ReadByOwnerAt", table: "GmachChatMessages");
}
