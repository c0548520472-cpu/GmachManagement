using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GmachManagement.Data.Migrations.PostgreSQL;

[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261117013000_GoogleIdentities")]
public sealed class GoogleIdentities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE "GoogleIdentities" (
                "UserId" integer PRIMARY KEY REFERENCES "Users" ("Id") ON DELETE CASCADE,
                "Subject" varchar(255) NOT NULL UNIQUE
            )
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "GoogleIdentities");
}
