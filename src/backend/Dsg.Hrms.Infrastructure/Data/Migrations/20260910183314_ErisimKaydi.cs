using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ErisimKaydi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_log",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    user_account_id = table.Column<long>(type: "bigint", nullable: true),
                    access_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    entity_name = table.Column<string>(type: "text", maxLength: 200, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    record_count = table.Column<int>(type: "integer", nullable: false),
                    filters = table.Column<string>(type: "jsonb", nullable: false),
                    trace_id = table.Column<string>(type: "text", maxLength: 64, nullable: true),
                    ip_address = table.Column<string>(type: "text", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_log", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_log_access_type_occurred_at",
                schema: "audit",
                table: "access_log",
                columns: new[] { "access_type", "occurred_at" },
                filter: "access_type IN ('Export', 'Report')");

            migrationBuilder.CreateIndex(
                name: "ix_access_log_entity_name_entity_id",
                schema: "audit",
                table: "access_log",
                columns: new[] { "entity_name", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_access_log_trace_id",
                schema: "audit",
                table: "access_log",
                column: "trace_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_log_user_account_id_occurred_at",
                schema: "audit",
                table: "access_log",
                columns: new[] { "user_account_id", "occurred_at" });

            // Erisim kaydi da DEGISTIRILEMEZ (KR-060). Degisiklik kaydiyla ayni
            // fonksiyon kullanilir; kural tek yerde tanimlidir.
            migrationBuilder.Sql("""
                CREATE TRIGGER access_log_row_immutable
                BEFORE UPDATE OR DELETE ON audit.access_log
                FOR EACH ROW EXECUTE FUNCTION audit.reject_change_log_modification();
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER access_log_truncate_immutable
                BEFORE TRUNCATE ON audit.access_log
                FOR EACH STATEMENT EXECUTE FUNCTION audit.reject_change_log_modification();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS access_log_truncate_immutable ON audit.access_log;");
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS access_log_row_immutable ON audit.access_log;");

            migrationBuilder.DropTable(
                name: "access_log",
                schema: "audit");
        }
    }
}
