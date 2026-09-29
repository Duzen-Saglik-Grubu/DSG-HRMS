using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PasswordResetPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "purpose",
                schema: "identity",
                table: "registration_attempt",
                type: "integer",
                nullable: false,
                // Mevcut denemelerin tamami uyeliktir (VerificationPurpose.Registration = 1).
                // 0 verilseydi yeni kisit mevcut satirlarda ihlal edilirdi.
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "ck_registration_attempt_purpose",
                schema: "identity",
                table: "registration_attempt",
                sql: "purpose IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_registration_attempt_purpose",
                schema: "identity",
                table: "registration_attempt");

            migrationBuilder.DropColumn(
                name: "purpose",
                schema: "identity",
                table: "registration_attempt");
        }
    }
}
