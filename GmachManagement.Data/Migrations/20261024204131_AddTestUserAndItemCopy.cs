using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GmachManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTestUserAndItemCopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ItemCopies",
                columns: new[] { "Id", "ItemId", "Status" },
                values: new object[] { 1, 1, 1 });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "Name", "PasswordHash", "Role" },
                values: new object[] { 1, "test@example.com", "משתמש בדיקה", "TEST_USER_NO_LOGIN", 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ItemCopies",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
