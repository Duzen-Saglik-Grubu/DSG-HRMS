using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersonnelAndOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.EnsureSchema(
                name: "personnel");

            migrationBuilder.CreateTable(
                name: "company",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    logo_firm_number = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "person",
                schema: "personnel",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    national_id = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    is_email_shared = table.Column<bool>(type: "boolean", nullable: false),
                    mobile_phone = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_person", x => x.id);
                    table.CheckConstraint("ck_person_email_lowercase", "email IS NULL OR email = lower(email)");
                    table.CheckConstraint("ck_person_mobile_phone_format", "mobile_phone IS NULL OR mobile_phone ~ '^5[0-9]{9}$'");
                    table.CheckConstraint("ck_person_national_id_format", "national_id ~ '^[1-9][0-9]{10}$'");
                });

            migrationBuilder.CreateTable(
                name: "sync_run",
                schema: "personnel",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    records_read = table.Column<int>(type: "integer", nullable: false),
                    records_skipped = table.Column<int>(type: "integer", nullable: false),
                    persons_created = table.Column<int>(type: "integer", nullable: false),
                    persons_updated = table.Column<int>(type: "integer", nullable: false),
                    employments_created = table.Column<int>(type: "integer", nullable: false),
                    employments_updated = table.Column<int>(type: "integer", nullable: false),
                    employments_deactivated = table.Column<int>(type: "integer", nullable: false),
                    companies_changed = table.Column<int>(type: "integer", nullable: false),
                    warning_count = table.Column<int>(type: "integer", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_run", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employment",
                schema: "personnel",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<long>(type: "bigint", nullable: false),
                    registry_code = table.Column<string>(type: "text", nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    hire_date = table.Column<DateOnly>(type: "date", nullable: false),
                    termination_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    logo_ref = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employment", x => x.id);
                    table.CheckConstraint("ck_employment_termination_after_hire", "termination_date IS NULL OR termination_date >= hire_date");
                    table.ForeignKey(
                        name: "fk_employment_company_company_id",
                        column: x => x.company_id,
                        principalSchema: "organization",
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employment_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "personnel",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sync_warning",
                schema: "personnel",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    run_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    registry_code = table.Column<string>(type: "text", nullable: false),
                    detail = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_warning", x => x.id);
                    table.ForeignKey(
                        name: "fk_sync_warning_sync_run_run_id",
                        column: x => x.run_id,
                        principalSchema: "personnel",
                        principalTable: "sync_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_company_logo_firm_number",
                schema: "organization",
                table: "company",
                column: "logo_firm_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_company_public_id",
                schema: "organization",
                table: "company",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employment_company_id",
                schema: "personnel",
                table: "employment",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_employment_person_id_is_active",
                schema: "personnel",
                table: "employment",
                columns: new[] { "person_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_employment_public_id",
                schema: "personnel",
                table: "employment",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employment_registry_code",
                schema: "personnel",
                table: "employment",
                column: "registry_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_person_email",
                schema: "personnel",
                table: "person",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_person_national_id",
                schema: "personnel",
                table: "person",
                column: "national_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_person_public_id",
                schema: "personnel",
                table: "person",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sync_run_public_id",
                schema: "personnel",
                table: "sync_run",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sync_run_started_at",
                schema: "personnel",
                table: "sync_run",
                column: "started_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_sync_warning_run_id_registry_code",
                schema: "personnel",
                table: "sync_warning",
                columns: new[] { "run_id", "registry_code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employment",
                schema: "personnel");

            migrationBuilder.DropTable(
                name: "sync_warning",
                schema: "personnel");

            migrationBuilder.DropTable(
                name: "company",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "person",
                schema: "personnel");

            migrationBuilder.DropTable(
                name: "sync_run",
                schema: "personnel");
        }
    }
}
