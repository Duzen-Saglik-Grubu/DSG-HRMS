using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class VerificationCodesAndDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.CreateTable(
                name: "delivery",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    purpose = table.Column<int>(type: "integer", nullable: false),
                    person_id = table.Column<long>(type: "bigint", nullable: true),
                    recipient_masked = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    result_code = table.Column<string>(type: "text", nullable: true),
                    external_id = table.Column<string>(type: "text", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    queued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verification_code",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<long>(type: "bigint", nullable: false),
                    purpose = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    max_failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verification_code", x => x.id);
                    table.CheckConstraint("ck_verification_code_attempts", "failed_attempts >= 0 AND max_failed_attempts > 0");
                    table.CheckConstraint("ck_verification_code_hash_length", "length(code_hash) = 44");
                    table.ForeignKey(
                        name: "fk_verification_code_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "personnel",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_delivery_person_id_queued_at",
                schema: "notification",
                table: "delivery",
                columns: new[] { "person_id", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "ix_delivery_public_id",
                schema: "notification",
                table: "delivery",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delivery_status_queued_at",
                schema: "notification",
                table: "delivery",
                columns: new[] { "status", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "ix_verification_code_person_id_created_at",
                schema: "identity",
                table: "verification_code",
                columns: new[] { "person_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_verification_code_person_id_purpose_status",
                schema: "identity",
                table: "verification_code",
                columns: new[] { "person_id", "purpose", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_verification_code_public_id",
                schema: "identity",
                table: "verification_code",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delivery",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "verification_code",
                schema: "identity");
        }
    }
}
