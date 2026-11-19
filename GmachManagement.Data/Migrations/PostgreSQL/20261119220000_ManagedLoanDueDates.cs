using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GmachManagement.Data.Migrations.PostgreSQL;

[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261119220000_ManagedLoanDueDates")]
public sealed class ManagedLoanDueDates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<DateTime>(
            name: "ExpectedReturnDate", table: "ManagedLoans",
            type: "timestamp with time zone", nullable: true);
    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn(name: "ExpectedReturnDate", table: "ManagedLoans");
}
