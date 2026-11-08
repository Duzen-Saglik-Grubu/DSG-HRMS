using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SystemEncryptionPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "permission",
                columns: new[] { "code", "description" },
                values: new object[] { "system.encryption.update", "Özel nitelikli veri şifrelemesini yönetme (yeniden şifreleme)" });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_permission",
                columns: new[] { "permission", "role_id" },
                values: new object[] { "system.encryption.update", 1L });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "system.encryption.update", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "system.encryption.update");
        }
    }
}
