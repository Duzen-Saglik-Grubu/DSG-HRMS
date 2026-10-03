using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SecurityEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "security_event",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    event_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    user_account_id = table.Column<long>(type: "bigint", nullable: true),
                    person_id = table.Column<long>(type: "bigint", nullable: true),
                    actor_user_account_id = table.Column<long>(type: "bigint", nullable: true),
                    detail = table.Column<string>(type: "text", maxLength: 100, nullable: true),
                    trace_id = table.Column<string>(type: "text", maxLength: 64, nullable: true),
                    ip_address = table.Column<string>(type: "text", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_event", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_security_event_event_type_occurred_at",
                schema: "audit",
                table: "security_event",
                columns: new[] { "event_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_security_event_ip_address_occurred_at",
                schema: "audit",
                table: "security_event",
                columns: new[] { "ip_address", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_security_event_trace_id",
                schema: "audit",
                table: "security_event",
                column: "trace_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_event_user_account_id_occurred_at",
                schema: "audit",
                table: "security_event",
                columns: new[] { "user_account_id", "occurred_at" });

            // Kimlik olaylari da DEGISTIRILEMEZ (KR-060). Degisiklik kaydiyla ayni
            // fonksiyon kullanilir; kural tek yerde tanimlidir.
            migrationBuilder.Sql("""
                CREATE TRIGGER security_event_row_immutable
                BEFORE UPDATE OR DELETE ON audit.security_event
                FOR EACH ROW EXECUTE FUNCTION audit.reject_change_log_modification();
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER security_event_truncate_immutable
                BEFORE TRUNCATE ON audit.security_event
                FOR EACH STATEMENT EXECUTE FUNCTION audit.reject_change_log_modification();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS security_event_truncate_immutable ON audit.security_event;");
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS security_event_row_immutable ON audit.security_event;");

            migrationBuilder.DropTable(
                name: "security_event",
                schema: "audit");
        }
    }
}
