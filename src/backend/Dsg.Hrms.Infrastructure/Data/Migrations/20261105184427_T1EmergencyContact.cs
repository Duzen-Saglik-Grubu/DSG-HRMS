using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class T1EmergencyContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "emergency_contact",
                schema: "personnel",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<long>(type: "bigint", nullable: false),
                    relationship = table.Column<string>(type: "text", maxLength: 50, nullable: false),
                    first_name = table.Column<string>(type: "text", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "text", maxLength: 100, nullable: false),
                    mobile_phone = table.Column<string>(type: "text", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_emergency_contact", x => x.id);
                    table.ForeignKey(
                        name: "fk_emergency_contact_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "personnel",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_emergency_contact_deleted_at",
                schema: "personnel",
                table: "emergency_contact",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_contact_person_id",
                schema: "personnel",
                table: "emergency_contact",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_contact_public_id",
                schema: "personnel",
                table: "emergency_contact",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emergency_contact",
                schema: "personnel");
        }
    }
}
