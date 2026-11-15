using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GmachManagement.Data.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261115234001_DeletedAccountsSqlServer")]
public class DeletedAccountsSqlServer : Migration {
 protected override void Up(MigrationBuilder m){
  m.CreateTable(name:"DeletedAccounts",columns:t=>new {
   Id=t.Column<int>(type:"int",nullable:false).Annotation("SqlServer:Identity","1, 1"),
   Email=t.Column<string>(type:"nvarchar(254)",maxLength:254,nullable:false),
   Approved=t.Column<bool>(type:"bit",nullable:false),
   Requested=t.Column<bool>(type:"bit",nullable:false),
   DeletedAtUtc=t.Column<DateTime>(type:"datetime2",nullable:false)
  },constraints:t=>t.PrimaryKey("PK_DeletedAccounts",x=>x.Id));
  m.CreateIndex(name:"IX_DeletedAccounts_Email",table:"DeletedAccounts",column:"Email",unique:true);
 }
 protected override void Down(MigrationBuilder m)=>m.DropTable(name:"DeletedAccounts");
}