using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GmachManagement.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261119220100_ManagedLoanDueDatesSqlServer")]
public sealed class ManagedLoanDueDatesSqlServer : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<DateTime>(
            name: "ExpectedReturnDate", table: "ManagedLoans",
            type: "datetime2", nullable: true);
    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn(name: "ExpectedReturnDate", table: "ManagedLoans");
}
