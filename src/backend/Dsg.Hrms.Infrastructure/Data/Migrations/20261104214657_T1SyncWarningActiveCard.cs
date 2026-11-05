using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class T1SyncWarningActiveCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active_card",
                schema: "personnel",
                table: "sync_warning",
                type: "boolean",
                nullable: false,
                // Onceki calismalarin uyarilari aktif sayilir; bir sonraki calisma dogru degeri yazar.
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active_card",
                schema: "personnel",
                table: "sync_warning");
        }
    }
}
