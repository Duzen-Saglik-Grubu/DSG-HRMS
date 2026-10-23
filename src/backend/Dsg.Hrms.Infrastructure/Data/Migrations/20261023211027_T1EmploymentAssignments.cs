using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class T1EmploymentAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ayni istihdamda cakisan aralik engellenir (ADR-0021 §2, SYG-PERS-019). EXCLUDE kisitinda
            // bigint esitligi icin btree_gist gerekir. Eklenti PostgreSQL 13+ ile guvenilir (trusted)
            // sayilir; migration'i uygulayan sahip rol kurar, calisma rolu hrms_app kurmaz (B-19).
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.CreateTable(
                name: "employment_assignment",
                schema: "personnel",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employment_id = table.Column<long>(type: "bigint", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    department_id = table.Column<long>(type: "bigint", nullable: true),
                    title_code = table.Column<string>(type: "text", nullable: true),
                    status_code = table.Column<string>(type: "text", nullable: true),
                    occupation_code = table.Column<string>(type: "text", nullable: true),
                    occupation_name = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employment_assignment", x => x.id);
                    table.CheckConstraint("ck_employment_assignment_range", "valid_to IS NULL OR valid_to > valid_from");
                    table.ForeignKey(
                        name: "fk_employment_assignment_branch_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "organization",
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employment_assignment_company_company_id",
                        column: x => x.company_id,
                        principalSchema: "organization",
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employment_assignment_department_department_id",
                        column: x => x.department_id,
                        principalSchema: "organization",
                        principalTable: "department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employment_assignment_employment_employment_id",
                        column: x => x.employment_id,
                        principalSchema: "personnel",
                        principalTable: "employment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employment_assignment_branch_id",
                schema: "personnel",
                table: "employment_assignment",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_employment_assignment_company_id",
                schema: "personnel",
                table: "employment_assignment",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_employment_assignment_department_id",
                schema: "personnel",
                table: "employment_assignment",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_employment_assignment_employment_id_valid_from",
                schema: "personnel",
                table: "employment_assignment",
                columns: new[] { "employment_id", "valid_from" });

            migrationBuilder.CreateIndex(
                name: "ix_employment_assignment_public_id",
                schema: "personnel",
                table: "employment_assignment",
                column: "public_id",
                unique: true);

            // Atamalar LOGO kaynaklidir ve silinmez; kisit butun kayitlara uygulanir.
            migrationBuilder.Sql(
                "ALTER TABLE personnel.employment_assignment ADD CONSTRAINT ex_employment_assignment_no_overlap " +
                "EXCLUDE USING gist (employment_id WITH =, daterange(valid_from, valid_to, '[)') WITH &&);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employment_assignment",
                schema: "personnel");
        }
    }
}
