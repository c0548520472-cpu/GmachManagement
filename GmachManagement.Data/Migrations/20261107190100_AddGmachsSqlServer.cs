using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using GmachManagement.Data.Context;
#nullable disable
namespace GmachManagement.Data.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261107190100_AddGmachsSqlServer")]
public class AddGmachsSqlServer : Migration {
 protected override void Up(MigrationBuilder m) {
  m.CreateTable(name:"Gmachs",columns:t=>new {Id=t.Column<int>(type:"int",nullable:false).Annotation("SqlServer:Identity","1, 1"),Name=t.Column<string>(type:"nvarchar(150)",maxLength:150,nullable:false),City=t.Column<string>(type:"nvarchar(100)",maxLength:100,nullable:false),Description=t.Column<string>(type:"nvarchar(max)",nullable:true),OwnerId=t.Column<int>(type:"int",nullable:false)},constraints:t=>{t.PrimaryKey("PK_Gmachs",x=>x.Id);t.ForeignKey("FK_Gmachs_Users_OwnerId",x=>x.OwnerId,"Users","Id",onDelete:ReferentialAction.Restrict);});
  m.CreateIndex(name:"IX_Gmachs_OwnerId",table:"Gmachs",column:"OwnerId",unique:true);
  m.AddColumn<int>(name:"GmachId",table:"Items",type:"int",nullable:true);
  m.CreateIndex(name:"IX_Items_GmachId",table:"Items",column:"GmachId");
  m.AddForeignKey(name:"FK_Items_Gmachs_GmachId",table:"Items",column:"GmachId",principalTable:"Gmachs",principalColumn:"Id",onDelete:ReferentialAction.Restrict);
 }
 protected override void Down(MigrationBuilder m){m.DropForeignKey("FK_Items_Gmachs_GmachId","Items");m.DropIndex("IX_Items_GmachId","Items");m.DropColumn("GmachId","Items");m.DropTable("Gmachs");}
}
