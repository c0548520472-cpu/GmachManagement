using System;
using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GmachManagement.Data.Migrations;

[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261109223000_AddAnonymousBorrowRequestsPostgreSql")]
public class AddAnonymousBorrowRequestsPostgreSql : Migration {
 protected override void Up(MigrationBuilder migrationBuilder) {
  migrationBuilder.CreateTable(
   name:"BorrowRequests",
   columns:table=>new {
    Id=table.Column<int>(type:"integer",nullable:false).Annotation("Npgsql:ValueGenerationStrategy",Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
    ItemId=table.Column<int>(type:"integer",nullable:false),
    ApplicantName=table.Column<string>(type:"character varying(150)",maxLength:150,nullable:false),
    Phone=table.Column<string>(type:"character varying(30)",maxLength:30,nullable:true),
    Email=table.Column<string>(type:"character varying(254)",maxLength:254,nullable:true),
    CreatedAt=table.Column<DateTime>(type:"timestamp with time zone",nullable:false),
    FulfilledAt=table.Column<DateTime>(type:"timestamp with time zone",nullable:true)
   },
   constraints:table=>{
    table.PrimaryKey("PK_BorrowRequests",x=>x.Id);
    table.ForeignKey("FK_BorrowRequests_Items_ItemId",x=>x.ItemId,"Items","Id",onDelete:ReferentialAction.Restrict);
   });
  migrationBuilder.CreateIndex(name:"IX_BorrowRequests_ItemId",table:"BorrowRequests",column:"ItemId");
 }
 protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name:"BorrowRequests");
}
