using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class T1ReferenceDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reference");

            migrationBuilder.CreateTable(
                name: "definition",
                schema: "reference",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "text", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "text", maxLength: 200, nullable: false, collation: "tr-TR-x-icu"),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_definition", x => x.id);
                    table.CheckConstraint("ck_definition_type", "type IN (1, 2, 3)");
                });

            migrationBuilder.InsertData(
                schema: "reference",
                table: "definition",
                columns: new[] { "id", "code", "created_at", "created_by", "is_active", "is_system", "name", "public_id", "sort_order", "type", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { 1L, "1", new DateTimeOffset(new DateTime(2026, 10, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, "Birim Operatörü", new Guid("0192a3b4-1001-7000-8000-000000000001"), 1, 1, null, null },
                    { 2L, "2", new DateTimeOffset(new DateTime(2026, 10, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, "Birim Teknik Sorumlusu", new Guid("0192a3b4-1001-7000-8000-000000000002"), 2, 1, null, null },
                    { 3L, "3", new DateTimeOffset(new DateTime(2026, 10, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, "Birim Sorumlusu", new Guid("0192a3b4-1001-7000-8000-000000000003"), 3, 1, null, null },
                    { 4L, "4", new DateTimeOffset(new DateTime(2026, 10, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, "Yardımcı Hizmetler", new Guid("0192a3b4-1001-7000-8000-000000000004"), 4, 1, null, null },
                    { 11L, "1", new DateTimeOffset(new DateTime(2026, 10, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "0 Rh+", new Guid("0192a3b4-1003-7000-8000-000000000001"), 1, 3, null, null },
                    { 12L, "2", new DateTimeOffset(new DateTime(2026, 10, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "0 Rh-", new Guid("0192a3b4-1003-7000-8000-000000000002"), 2, 3, null, null },
                    { 13L, "3", new DateTimeOffset(new DateTime(2026, 10, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "A Rh+", new Guid("0192a3b4-1003-7000-8000-000000000003"), 3, 3, null, null },
                    { 14L, "4", new DateTimeOffset(new DateTime(2026, 10, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "A Rh-", new Guid("0192a3b4-1003-7000-8000-000000000004"), 4, 3, null, null },
                    { 15L, "5", new DateTimeOffset(new DateTime(2026, 10, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "B Rh+", new Guid("0192a3b4-1003-7000-8000-000000000005"), 5, 3, null, null },
                    { 16L, "6", new DateTimeOffset(new DateTime(2026, 10, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "B Rh-", new Guid("0192a3b4-1003-7000-8000-000000000006"), 6, 3, null, null },
                    { 17L, "7", new DateTimeOffset(new DateTime(2026, 10, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "AB Rh+", new Guid("0192a3b4-1003-7000-8000-000000000007"), 7, 3, null, null },
                    { 18L, "8", new DateTimeOffset(new DateTime(2026, 10, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "AB Rh-", new Guid("0192a3b4-1003-7000-8000-000000000008"), 8, 3, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_definition_public_id",
                schema: "reference",
                table: "definition",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_definition_type_code",
                schema: "reference",
                table: "definition",
                columns: new[] { "type", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "definition",
                schema: "reference");
        }
    }
}
