using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GmachManagement.Data.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261107213100_ManagedLoansSqlServer")]
public class ManagedLoansSqlServer : Migration {
 protected override void Up(MigrationBuilder m){m.CreateTable(name:"ManagedLoans",columns:t=>new {Id=t.Column<int>(type:"int",nullable:false).Annotation("SqlServer:Identity","1, 1"),GmachId=t.Column<int>(type:"int",nullable:false),ItemCopyId=t.Column<int>(type:"int",nullable:false),BorrowerName=t.Column<string>(type:"nvarchar(150)",maxLength:150,nullable:false),BorrowerPhone=t.Column<string>(type:"nvarchar(30)",maxLength:30,nullable:true),BorrowerEmail=t.Column<string>(type:"nvarchar(254)",maxLength:254,nullable:true),BorrowedAt=t.Column<DateTime>(type:"datetime2",nullable:false),ReturnedAt=t.Column<DateTime>(type:"datetime2",nullable:true)},constraints:t=>t.PrimaryKey("PK_ManagedLoans",x=>x.Id));m.CreateIndex(name:"IX_ManagedLoans_GmachId",table:"ManagedLoans",column:"GmachId");m.CreateIndex(name:"IX_ManagedLoans_ItemCopyId",table:"ManagedLoans",column:"ItemCopyId");}
 protected override void Down(MigrationBuilder m)=>m.DropTable(name:"ManagedLoans");
}
