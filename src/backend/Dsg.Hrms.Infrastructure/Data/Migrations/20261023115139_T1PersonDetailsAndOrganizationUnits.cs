using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class T1PersonDetailsAndOrganizationUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "education_name",
                schema: "personnel",
                table: "person",
                type: "text",
                nullable: true,
                collation: "tr-TR-x-icu");

            migrationBuilder.AddColumn<int>(
                name: "gender",
                schema: "personnel",
                table: "person",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "marital_status",
                schema: "personnel",
                table: "person",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "previous_surname",
                schema: "personnel",
                table: "person",
                type: "text",
                nullable: true,
                collation: "tr-TR-x-icu");

            migrationBuilder.CreateTable(
                name: "branch",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    logo_number = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false, collation: "tr-TR-x-icu"),
                    city = table.Column<string>(type: "text", nullable: true, collation: "tr-TR-x-icu"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branch", x => x.id);
                    table.ForeignKey(
                        name: "fk_branch_company_company_id",
                        column: x => x.company_id,
                        principalSchema: "organization",
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "department",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    logo_number = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false, collation: "tr-TR-x-icu"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_department", x => x.id);
                    table.ForeignKey(
                        name: "fk_department_company_company_id",
                        column: x => x.company_id,
                        principalSchema: "organization",
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "person_sensitive",
                schema: "personnel",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<long>(type: "bigint", nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_person_sensitive", x => x.id);
                    table.ForeignKey(
                        name: "fk_person_sensitive_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "personnel",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_person_gender",
                schema: "personnel",
                table: "person",
                sql: "gender IS NULL OR gender IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_person_marital_status",
                schema: "personnel",
                table: "person",
                sql: "marital_status IS NULL OR marital_status IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_person_previous_surname_female",
                schema: "personnel",
                table: "person",
                sql: "previous_surname IS NULL OR gender = 2");

            migrationBuilder.CreateIndex(
                name: "ix_branch_company_id_logo_number",
                schema: "organization",
                table: "branch",
                columns: new[] { "company_id", "logo_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_branch_public_id",
                schema: "organization",
                table: "branch",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_department_company_id_logo_number",
                schema: "organization",
                table: "department",
                columns: new[] { "company_id", "logo_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_department_public_id",
                schema: "organization",
                table: "department",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_person_sensitive_person_id",
                schema: "personnel",
                table: "person_sensitive",
                column: "person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_person_sensitive_public_id",
                schema: "personnel",
                table: "person_sensitive",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "department",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "person_sensitive",
                schema: "personnel");

            migrationBuilder.DropCheckConstraint(
                name: "ck_person_gender",
                schema: "personnel",
                table: "person");

            migrationBuilder.DropCheckConstraint(
                name: "ck_person_marital_status",
                schema: "personnel",
                table: "person");

            migrationBuilder.DropCheckConstraint(
                name: "ck_person_previous_surname_female",
                schema: "personnel",
                table: "person");

            migrationBuilder.DropColumn(
                name: "education_name",
                schema: "personnel",
                table: "person");

            migrationBuilder.DropColumn(
                name: "gender",
                schema: "personnel",
                table: "person");

            migrationBuilder.DropColumn(
                name: "marital_status",
                schema: "personnel",
                table: "person");

            migrationBuilder.DropColumn(
                name: "previous_surname",
                schema: "personnel",
                table: "person");
        }
    }
}
