using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditOccurredAtIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_security_event_occurred_at",
                schema: "audit",
                table: "security_event",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_change_log_occurred_at",
                schema: "audit",
                table: "change_log",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_access_log_occurred_at",
                schema: "audit",
                table: "access_log",
                column: "occurred_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_security_event_occurred_at",
                schema: "audit",
                table: "security_event");

            migrationBuilder.DropIndex(
                name: "ix_change_log_occurred_at",
                schema: "audit",
                table: "change_log");

            migrationBuilder.DropIndex(
                name: "ix_access_log_occurred_at",
                schema: "audit",
                table: "access_log");
        }
    }
}
