using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GmachManagement.Data.Migrations.PostgreSQL;
[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261107220000_OfflineRecovery")]
public class OfflineRecovery : Migration {
 protected override void Up(MigrationBuilder m)=>m.AddColumn<string>(name:"RecoveryCodeHash",table:"Users",type:"text",nullable:true);
 protected override void Down(MigrationBuilder m)=>m.DropColumn(name:"RecoveryCodeHash",table:"Users");
}
