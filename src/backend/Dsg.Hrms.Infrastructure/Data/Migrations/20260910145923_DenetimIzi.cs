using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DenetimIzi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "change_log",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    user_account_id = table.Column<long>(type: "bigint", nullable: true),
                    entity_name = table.Column<string>(type: "text", maxLength: 200, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changes = table.Column<string>(type: "jsonb", nullable: false),
                    trace_id = table.Column<string>(type: "text", maxLength: 64, nullable: true),
                    ip_address = table.Column<string>(type: "text", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_change_log", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_change_log_entity_name_entity_id",
                schema: "audit",
                table: "change_log",
                columns: new[] { "entity_name", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_change_log_trace_id",
                schema: "audit",
                table: "change_log",
                column: "trace_id");

            migrationBuilder.CreateIndex(
                name: "ix_change_log_user_account_id_occurred_at",
                schema: "audit",
                table: "change_log",
                columns: new[] { "user_account_id", "occurred_at" });

            // Denetim izi DEGISTIRILEMEZ (ADR-0009 bolum 2).
            //
            // Uygulama katmanindaki denetim (AuditTrailInterceptor) yalnizca uygulama
            // uzerinden gelen islemleri kapsar. Veritabanina dogrudan baglanan bir
            // istemci veya uygulamadaki bir hata, denetim izini sessizce bozabilirdi.
            // Bu tetikleyici, kaydin degistirilemezligini veritabaninin kendisinde
            // zorunlu kilar.
            migrationBuilder.Sql("""
                CREATE FUNCTION audit.reject_change_log_modification()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION
                        'Denetim izi kayitlari degistirilemez ve silinemez (ADR-0009 bolum 2).'
                        USING ERRCODE = 'check_violation';
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER change_log_row_immutable
                BEFORE UPDATE OR DELETE ON audit.change_log
                FOR EACH ROW EXECUTE FUNCTION audit.reject_change_log_modification();
                """);

            // TRUNCATE satir bazli tetikleyiciyi CALISTIRMAZ; ayri bir ifade bazli
            // tetikleyici gerekir. Aksi hâlde tek komutla tum denetim izi silinebilirdi.
            migrationBuilder.Sql("""
                CREATE TRIGGER change_log_truncate_immutable
                BEFORE TRUNCATE ON audit.change_log
                FOR EACH STATEMENT EXECUTE FUNCTION audit.reject_change_log_modification();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS change_log_truncate_immutable ON audit.change_log;");
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS change_log_row_immutable ON audit.change_log;");

            migrationBuilder.DropTable(
                name: "change_log",
                schema: "audit");

            migrationBuilder.Sql(
                "DROP FUNCTION IF EXISTS audit.reject_change_log_modification();");
        }
    }
}
