using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PasswordChangeRequirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "password_change_required",
                schema: "identity",
                table: "user_session",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "first_password_change_pending",
                schema: "identity",
                table: "user_account",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "first_signed_in_at",
                schema: "identity",
                table: "user_account",
                type: "timestamptz",
                nullable: true);

            // Daha once giris yapmis hesaplarin ilk giris ani oturum kayitlarindan doldurulur
            // (SYG-KMLK-050): aksi halde PRM-KML-20 acildiginda bu hesaplar da "ilk giris"
            // sayilirdi. Yeniden calistirilmasi zararsizdir.
            migrationBuilder.Sql(
                """
                UPDATE identity.user_account AS a
                SET first_signed_in_at = s.first_started_at
                FROM (
                    SELECT user_account_id, min(started_at) AS first_started_at
                    FROM identity.user_session
                    GROUP BY user_account_id
                ) AS s
                WHERE s.user_account_id = a.id AND a.first_signed_in_at IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_change_required",
                schema: "identity",
                table: "user_session");

            migrationBuilder.DropColumn(
                name: "first_password_change_pending",
                schema: "identity",
                table: "user_account");

            migrationBuilder.DropColumn(
                name: "first_signed_in_at",
                schema: "identity",
                table: "user_account");
        }
    }
}
