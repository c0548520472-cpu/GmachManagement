using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GmachManagement.Data.Migrations.PostgreSQL;
[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261107202000_UniqueUserEmail")]
public class UniqueUserEmail : Migration {
 protected override void Up(MigrationBuilder m){m.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Users_Email\" ON \"Users\" (lower(\"Email\"));");}
 protected override void Down(MigrationBuilder m){m.Sql("DROP INDEX IF EXISTS \"IX_Users_Email\";");}
}
