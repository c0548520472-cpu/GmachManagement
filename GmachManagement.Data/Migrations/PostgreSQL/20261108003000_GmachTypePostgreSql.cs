using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GmachManagement.Data.Context;
#nullable disable
namespace GmachManagement.Data.Migrations.PostgreSQL;
[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261108003000_GmachTypePostgreSql")]
public class GmachTypePostgreSql:Migration {
 protected override void Up(MigrationBuilder m)=>m.AddColumn<string>(name:"Type",table:"Gmachs",type:"character varying(100)",maxLength:100,nullable:false,defaultValue:"");
 protected override void Down(MigrationBuilder m)=>m.DropColumn(name:"Type",table:"Gmachs");
}
