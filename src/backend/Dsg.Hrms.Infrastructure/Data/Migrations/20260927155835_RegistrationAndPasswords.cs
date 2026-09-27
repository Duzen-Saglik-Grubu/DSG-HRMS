using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RegistrationAndPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_changed_at",
                schema: "identity",
                table: "user_account",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                schema: "identity",
                table: "user_account",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "registration_attempt",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    national_id_hash = table.Column<string>(type: "text", nullable: false),
                    person_id = table.Column<long>(type: "bigint", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    offered_channels = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    verification_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decoy_code_expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    decoy_failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    decoy_max_attempts = table.Column<int>(type: "integer", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_attempt", x => x.id);
                    table.CheckConstraint("ck_registration_attempt_channels", "offered_channels BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_registration_attempt_national_id_hash_length", "length(national_id_hash) = 44");
                    table.ForeignKey(
                        name: "fk_registration_attempt_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "personnel",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registration_code_request",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    national_id_hash = table.Column<string>(type: "text", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_code_request", x => x.id);
                    table.CheckConstraint("ck_registration_code_request_hash_length", "length(national_id_hash) = 44");
                });

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempt_ip_address_created_at",
                schema: "identity",
                table: "registration_attempt",
                columns: new[] { "ip_address", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempt_national_id_hash_created_at",
                schema: "identity",
                table: "registration_attempt",
                columns: new[] { "national_id_hash", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempt_person_id",
                schema: "identity",
                table: "registration_attempt",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempt_public_id",
                schema: "identity",
                table: "registration_attempt",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_code_request_national_id_hash_requested_at",
                schema: "identity",
                table: "registration_code_request",
                columns: new[] { "national_id_hash", "requested_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registration_attempt",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "registration_code_request",
                schema: "identity");

            migrationBuilder.DropColumn(
                name: "password_changed_at",
                schema: "identity",
                table: "user_account");

            migrationBuilder.DropColumn(
                name: "password_hash",
                schema: "identity",
                table: "user_account");
        }
    }
}
