using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GmachManagement.Data.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261107220100_OfflineRecoverySqlServer")]
public class OfflineRecoverySqlServer : Migration {
 protected override void Up(MigrationBuilder m)=>m.AddColumn<string>(name:"RecoveryCodeHash",table:"Users",type:"nvarchar(max)",nullable:true);
 protected override void Down(MigrationBuilder m)=>m.DropColumn(name:"RecoveryCodeHash",table:"Users");
}
