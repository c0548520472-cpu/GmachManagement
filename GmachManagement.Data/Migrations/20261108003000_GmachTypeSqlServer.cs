using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GmachManagement.Data.Context;
#nullable disable
namespace GmachManagement.Data.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261108003000_GmachTypeSqlServer")]
public class GmachTypeSqlServer:Migration {
 protected override void Up(MigrationBuilder m)=>m.AddColumn<string>(name:"Type",table:"Gmachs",type:"nvarchar(100)",maxLength:100,nullable:false,defaultValue:"");
 protected override void Down(MigrationBuilder m)=>m.DropColumn(name:"Type",table:"Gmachs");
}
