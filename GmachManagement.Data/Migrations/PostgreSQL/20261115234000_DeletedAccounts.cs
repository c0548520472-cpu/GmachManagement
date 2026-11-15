using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GmachManagement.Data.Migrations.PostgreSQL;
[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261115234000_DeletedAccounts")]
public class DeletedAccounts : Migration {
 protected override void Up(MigrationBuilder m){
  m.CreateTable(name:"DeletedAccounts",columns:t=>new {
   Id=t.Column<int>(type:"integer",nullable:false).Annotation("Npgsql:ValueGenerationStrategy",Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
   Email=t.Column<string>(type:"character varying(254)",maxLength:254,nullable:false),
   Approved=t.Column<bool>(type:"boolean",nullable:false),
   Requested=t.Column<bool>(type:"boolean",nullable:false),
   DeletedAtUtc=t.Column<DateTime>(type:"timestamp with time zone",nullable:false)
  },constraints:t=>t.PrimaryKey("PK_DeletedAccounts",x=>x.Id));
  m.CreateIndex(name:"IX_DeletedAccounts_Email",table:"DeletedAccounts",column:"Email",unique:true);
 }
 protected override void Down(MigrationBuilder m)=>m.DropTable(name:"DeletedAccounts");
}