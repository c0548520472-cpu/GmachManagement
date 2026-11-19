using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace GmachManagement.Data.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261119221100_EmailReminderDeliveriesSqlServer")]
public sealed class EmailReminderDeliveriesSqlServer : Migration
{
 protected override void Up(MigrationBuilder m)
 {
  m.CreateTable(name:"EmailReminderDeliveries",columns:t=>new {
   Id=t.Column<int>(type:"int",nullable:false).Annotation("SqlServer:Identity", "1, 1"),
   Kind=t.Column<string>(type:"nvarchar(20)",maxLength:20,nullable:false),
   ReferenceId=t.Column<int>(type:"int",nullable:false),
   ReminderDay=t.Column<DateTime>(type:"datetime2",nullable:false),
   SentAt=t.Column<DateTime>(type:"datetime2",nullable:false)
  },constraints:t=>t.PrimaryKey("PK_EmailReminderDeliveries",x=>x.Id));
  m.CreateIndex(name:"IX_EmailReminderDeliveries_Kind_ReferenceId_ReminderDay",table:"EmailReminderDeliveries",columns:new[]{"Kind","ReferenceId","ReminderDay"},unique:true);
 }
 protected override void Down(MigrationBuilder m)=>m.DropTable("EmailReminderDeliveries");
}
