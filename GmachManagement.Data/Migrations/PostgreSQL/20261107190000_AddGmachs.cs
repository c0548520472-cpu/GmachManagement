using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GmachManagement.Data.Context;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
#nullable disable
namespace GmachManagement.Data.Migrations.PostgreSQL;
[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261107190000_AddGmachs")]
public class AddGmachs : Migration {
 protected override void Up(MigrationBuilder m) {
  m.CreateTable(name:"Gmachs",columns:t=>new { Id=t.Column<int>(type:"integer",nullable:false).Annotation("Npgsql:ValueGenerationStrategy",NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),Name=t.Column<string>(type:"character varying(150)",maxLength:150,nullable:false),City=t.Column<string>(type:"character varying(100)",maxLength:100,nullable:false),Description=t.Column<string>(type:"text",nullable:true),OwnerId=t.Column<int>(type:"integer",nullable:false)},constraints:t=>{t.PrimaryKey("PK_Gmachs",x=>x.Id);t.ForeignKey("FK_Gmachs_Users_OwnerId",x=>x.OwnerId,"Users","Id",onDelete:ReferentialAction.Restrict);});
  m.CreateIndex(name:"IX_Gmachs_OwnerId",table:"Gmachs",column:"OwnerId",unique:true);
  m.AddColumn<int>(name:"GmachId",table:"Items",type:"integer",nullable:true);
  m.CreateIndex(name:"IX_Items_GmachId",table:"Items",column:"GmachId");
  m.AddForeignKey(name:"FK_Items_Gmachs_GmachId",table:"Items",column:"GmachId",principalTable:"Gmachs",principalColumn:"Id",onDelete:ReferentialAction.Restrict);
 }
 protected override void Down(MigrationBuilder m){m.DropForeignKey("FK_Items_Gmachs_GmachId","Items");m.DropIndex("IX_Items_GmachId","Items");m.DropColumn("GmachId","Items");m.DropTable("Gmachs");}
}
