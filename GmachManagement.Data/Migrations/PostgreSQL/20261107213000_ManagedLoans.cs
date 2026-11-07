using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GmachManagement.Data.Migrations.PostgreSQL;
[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261107213000_ManagedLoans")]
public class ManagedLoans : Migration {
 protected override void Up(MigrationBuilder m){m.CreateTable(name:"ManagedLoans",columns:t=>new {Id=t.Column<int>(type:"integer",nullable:false).Annotation("Npgsql:ValueGenerationStrategy",Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),GmachId=t.Column<int>(type:"integer",nullable:false),ItemCopyId=t.Column<int>(type:"integer",nullable:false),BorrowerName=t.Column<string>(type:"character varying(150)",maxLength:150,nullable:false),BorrowerPhone=t.Column<string>(type:"character varying(30)",maxLength:30,nullable:true),BorrowerEmail=t.Column<string>(type:"character varying(254)",maxLength:254,nullable:true),BorrowedAt=t.Column<DateTime>(type:"timestamp with time zone",nullable:false),ReturnedAt=t.Column<DateTime>(type:"timestamp with time zone",nullable:true)},constraints:t=>t.PrimaryKey("PK_ManagedLoans",x=>x.Id));m.CreateIndex(name:"IX_ManagedLoans_GmachId",table:"ManagedLoans",column:"GmachId");m.CreateIndex(name:"IX_ManagedLoans_ItemCopyId",table:"ManagedLoans",column:"ItemCopyId");}
 protected override void Down(MigrationBuilder m)=>m.DropTable(name:"ManagedLoans");
}
