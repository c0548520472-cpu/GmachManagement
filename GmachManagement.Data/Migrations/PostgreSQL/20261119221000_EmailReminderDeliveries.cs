using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace GmachManagement.Data.Migrations.PostgreSQL;
[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261119221000_EmailReminderDeliveries")]
public sealed class EmailReminderDeliveries : Migration
{
 protected override void Up(MigrationBuilder m)
 {
  m.CreateTable(name:"EmailReminderDeliveries",columns:t=>new {
   Id=t.Column<int>(type:"integer",nullable:false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
   Kind=t.Column<string>(type:"character varying(20)",maxLength:20,nullable:false),
   ReferenceId=t.Column<int>(type:"integer",nullable:false),
   ReminderDay=t.Column<DateTime>(type:"timestamp with time zone",nullable:false),
   SentAt=t.Column<DateTime>(type:"timestamp with time zone",nullable:false)
  },constraints:t=>t.PrimaryKey("PK_EmailReminderDeliveries",x=>x.Id));
  m.CreateIndex(name:"IX_EmailReminderDeliveries_Kind_ReferenceId_ReminderDay",table:"EmailReminderDeliveries",columns:new[]{"Kind","ReferenceId","ReminderDay"},unique:true);
 }
 protected override void Down(MigrationBuilder m)=>m.DropTable("EmailReminderDeliveries");
}
