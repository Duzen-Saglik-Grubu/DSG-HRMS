using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class T1ReferencePermissionDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "reference.definition.update",
                column: "description",
                value: "Ünvan, görev ve yakınlık tanımlarını değiştirme");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "reference.definition.view",
                column: "description",
                value: "Ünvan, görev, kan grubu ve yakınlık tanımlarını görme");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "reference.definition.update",
                column: "description",
                value: "Ünvan ve görev tanımlarını değiştirme");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "reference.definition.view",
                column: "description",
                value: "Ünvan ve görev tanımlarını görme");
        }
    }
}
