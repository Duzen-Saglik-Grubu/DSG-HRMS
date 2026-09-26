using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SystemParametersAndUserAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "settings");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.AddColumn<int>(
                name: "accounts_deactivated",
                schema: "personnel",
                table: "sync_run",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "accounts_reactivated",
                schema: "personnel",
                table: "sync_run",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "system_parameter",
                schema: "settings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    protected_value = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_parameter", x => x.id);
                    table.CheckConstraint("ck_system_parameter_key_format", "key ~ '^PRM-[A-Z]{3}-[0-9]{2}$'");
                    table.CheckConstraint("ck_system_parameter_single_value", "value IS NULL OR protected_value IS NULL");
                });

            migrationBuilder.CreateTable(
                name: "user_account",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    status_reason = table.Column<int>(type: "integer", nullable: true),
                    status_note = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_account", x => x.id);
                    table.CheckConstraint("ck_user_account_manual_note", "status_reason IS DISTINCT FROM 3 OR (status_note IS NOT NULL AND btrim(status_note) <> '')");
                    table.ForeignKey(
                        name: "fk_user_account_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "personnel",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_system_parameter_key",
                schema: "settings",
                table: "system_parameter",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_system_parameter_public_id",
                schema: "settings",
                table: "system_parameter",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_account_person_id",
                schema: "identity",
                table: "user_account",
                column: "person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_account_public_id",
                schema: "identity",
                table: "user_account",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_parameter",
                schema: "settings");

            migrationBuilder.DropTable(
                name: "user_account",
                schema: "identity");

            migrationBuilder.DropColumn(
                name: "accounts_deactivated",
                schema: "personnel",
                table: "sync_run");

            migrationBuilder.DropColumn(
                name: "accounts_reactivated",
                schema: "personnel",
                table: "sync_run");
        }
    }
}
