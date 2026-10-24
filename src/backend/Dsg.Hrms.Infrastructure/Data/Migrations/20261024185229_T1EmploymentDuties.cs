using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class T1EmploymentDuties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employment_duty",
                schema: "personnel",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employment_id = table.Column<long>(type: "bigint", nullable: false),
                    duty_definition_id = table.Column<long>(type: "bigint", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("pk_employment_duty", x => x.id);
                    table.CheckConstraint("ck_employment_duty_range", "valid_to IS NULL OR valid_to > valid_from");
                    table.ForeignKey(
                        name: "fk_employment_duty_employment_employment_id",
                        column: x => x.employment_id,
                        principalSchema: "personnel",
                        principalTable: "employment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employment_duty_reference_definition_duty_definition_id",
                        column: x => x.duty_definition_id,
                        principalSchema: "reference",
                        principalTable: "definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employment_duty_deleted_at",
                schema: "personnel",
                table: "employment_duty",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employment_duty_duty_definition_id",
                schema: "personnel",
                table: "employment_duty",
                column: "duty_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_employment_duty_employment_id_valid_from",
                schema: "personnel",
                table: "employment_duty",
                columns: new[] { "employment_id", "valid_from" });

            migrationBuilder.CreateIndex(
                name: "ix_employment_duty_public_id",
                schema: "personnel",
                table: "employment_duty",
                column: "public_id",
                unique: true);

            // Ayni istihdamda silinmemis gorev donemleri cakisamaz (ADR-0021 §2, SYG-PERS-019). Silinmis
            // gorev, yerine girilen gorevle cakismaz. btree_gist T1EmploymentAssignments'ta kuruldu.
            migrationBuilder.Sql(
                "ALTER TABLE personnel.employment_duty ADD CONSTRAINT ex_employment_duty_no_overlap " +
                "EXCLUDE USING gist (employment_id WITH =, daterange(valid_from, valid_to, '[)') WITH &&) WHERE (deleted_at IS NULL);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employment_duty",
                schema: "personnel");
        }
    }
}
