using GmachManagement.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GmachManagement.Data.Migrations.PostgreSQL;
[DbContext(typeof(PostgreSqlMigrationsDbContext))]
[Migration("20261107202000_UniqueUserEmail")]
public class UniqueUserEmail : Migration {
 protected override void Up(MigrationBuilder m){m.Sql("""
CREATE OR REPLACE FUNCTION prevent_duplicate_user_email() RETURNS trigger AS $function$
BEGIN
    -- Serialize concurrent registrations of the same normalized email.
    PERFORM pg_advisory_xact_lock(hashtext(lower(btrim(NEW."Email"))));
    IF EXISTS (SELECT 1 FROM "Users" u WHERE lower(btrim(u."Email")) = lower(btrim(NEW."Email")) AND u."Id" <> NEW."Id") THEN
        RAISE EXCEPTION 'Email already registered' USING ERRCODE = '23505', CONSTRAINT = 'UX_Users_Email_Registration';
    END IF;
    RETURN NEW;
END;
$function$ LANGUAGE plpgsql;
CREATE TRIGGER "TR_Users_UniqueEmail" BEFORE INSERT OR UPDATE OF "Email" ON "Users"
FOR EACH ROW EXECUTE FUNCTION prevent_duplicate_user_email();
""");}
 protected override void Down(MigrationBuilder m){m.Sql("""DROP TRIGGER IF EXISTS "TR_Users_UniqueEmail" ON "Users"; DROP FUNCTION IF EXISTS prevent_duplicate_user_email();""");}
}
