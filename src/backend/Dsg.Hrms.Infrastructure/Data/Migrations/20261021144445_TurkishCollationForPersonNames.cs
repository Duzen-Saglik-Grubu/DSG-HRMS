using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TurkishCollationForPersonNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                schema: "personnel",
                table: "person",
                type: "text",
                nullable: false,
                collation: "tr-TR-x-icu",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                schema: "personnel",
                table: "person",
                type: "text",
                nullable: false,
                collation: "tr-TR-x-icu",
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                schema: "personnel",
                table: "person",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "tr-TR-x-icu");

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                schema: "personnel",
                table: "person",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "tr-TR-x-icu");
        }
    }
}
