using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class T1CorrectionRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "correction_request",
                schema: "personnel",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<long>(type: "bigint", nullable: false),
                    field = table.Column<int>(type: "integer", nullable: false),
                    proposed_value = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    completed_by = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_correction_request", x => x.id);
                    table.CheckConstraint("ck_correction_request_closed_without_value", "status = 1 OR proposed_value IS NULL");
                    table.ForeignKey(
                        name: "fk_correction_request_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "personnel",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inbox_item",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recipient_user_id = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_inbox_item_user_account_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalSchema: "identity",
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_correction_request_public_id",
                schema: "personnel",
                table: "correction_request",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_correction_request_status_submitted_at",
                schema: "personnel",
                table: "correction_request",
                columns: new[] { "status", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "ux_correction_request_open_field",
                schema: "personnel",
                table: "correction_request",
                columns: new[] { "person_id", "field" },
                unique: true,
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "ix_inbox_item_public_id",
                schema: "notification",
                table: "inbox_item",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inbox_item_recipient_user_id_created_at",
                schema: "notification",
                table: "inbox_item",
                columns: new[] { "recipient_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_inbox_item_unread",
                schema: "notification",
                table: "inbox_item",
                column: "recipient_user_id",
                filter: "read_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "correction_request",
                schema: "personnel");

            migrationBuilder.DropTable(
                name: "inbox_item",
                schema: "notification");
        }
    }
}
