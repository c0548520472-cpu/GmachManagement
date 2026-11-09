using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using GmachManagement.Data.Context;

#nullable disable

namespace GmachManagement.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261109170000_AddBorrowRequestsAndItemOwner")]
public partial class AddBorrowRequestsAndItemOwner : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "OwnerId", table: "Items", type: "int", nullable: true);
        migrationBuilder.Sql(@"UPDATE Items SET OwnerId = (SELECT MIN(Id) FROM Users WHERE Role = 2)
WHERE OwnerId IS NULL AND (SELECT COUNT(*) FROM Users WHERE Role = 2) = 1;");
        migrationBuilder.CreateIndex(name: "IX_Items_OwnerId", table: "Items", column: "OwnerId");
        migrationBuilder.AddForeignKey(name: "FK_Items_Users_OwnerId", table: "Items", column: "OwnerId",
            principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

        migrationBuilder.CreateTable(name: "BorrowRequests", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
            ItemCopyId = table.Column<int>(type: "int", nullable: false),
            ApplicantName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
            Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            FulfilledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_BorrowRequests", x => x.Id);
            table.ForeignKey("FK_BorrowRequests_ItemCopies_ItemCopyId", x => x.ItemCopyId,
                "ItemCopies", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex(name: "IX_BorrowRequests_ItemCopyId", table: "BorrowRequests", column: "ItemCopyId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BorrowRequests");
        migrationBuilder.DropForeignKey(name: "FK_Items_Users_OwnerId", table: "Items");
        migrationBuilder.DropIndex(name: "IX_Items_OwnerId", table: "Items");
        migrationBuilder.DropColumn(name: "OwnerId", table: "Items");
    }
}
