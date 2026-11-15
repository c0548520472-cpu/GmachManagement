using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GmachManagement.Data.Migrations;
[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261110215000_AddManagedLoanReturnReminders")]
public sealed class AddManagedLoanReturnReminders : Migration {
 protected override void Up(MigrationBuilder migrationBuilder) {
  migrationBuilder.AddColumn<DateTime>(name:"ExpectedReturnDate",table:"ManagedLoans",type:"timestamp with time zone",nullable:true);
  migrationBuilder.AddColumn<DateTime>(name:"LastReminderSentDate",table:"ManagedLoans",type:"timestamp with time zone",nullable:true);
 }
 protected override void Down(MigrationBuilder migrationBuilder) {
  migrationBuilder.DropColumn(name:"ExpectedReturnDate",table:"ManagedLoans");
  migrationBuilder.DropColumn(name:"LastReminderSentDate",table:"ManagedLoans");
 }
}