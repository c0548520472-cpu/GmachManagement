using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GmachManagement.Data.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261107202100_UniqueUserEmailSqlServer")]
public class UniqueUserEmailSqlServer : Migration {
 protected override void Up(MigrationBuilder m){m.CreateIndex(name:"IX_Users_Email",table:"Users",column:"Email",unique:true);}
 protected override void Down(MigrationBuilder m){m.DropIndex(name:"IX_Users_Email",table:"Users");}
}
