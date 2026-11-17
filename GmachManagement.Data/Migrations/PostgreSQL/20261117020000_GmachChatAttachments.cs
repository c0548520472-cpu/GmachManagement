using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GmachManagement.Data.Migrations.PostgreSQL;

[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261117020000_GmachChatAttachments")]
public sealed class GmachChatAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("AttachmentName", "GmachChatMessages", "character varying(150)", nullable: true);
        migrationBuilder.AddColumn<byte[]>("AttachmentContent", "GmachChatMessages", "bytea", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("AttachmentName", "GmachChatMessages");
        migrationBuilder.DropColumn("AttachmentContent", "GmachChatMessages");
    }
}
