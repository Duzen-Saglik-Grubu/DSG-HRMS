using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permission",
                schema: "identity",
                columns: table => new
                {
                    code = table.Column<string>(type: "text", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "role",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "text", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("pk_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                schema: "identity",
                columns: table => new
                {
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    permission = table.Column<string>(type: "text", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permission", x => new { x.role_id, x.permission });
                    table.ForeignKey(
                        name: "fk_role_permission_permission_permission",
                        column: x => x.permission,
                        principalSchema: "identity",
                        principalTable: "permission",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_permission_role_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_role",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_account_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_role_role_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_role_user_account_user_account_id",
                        column: x => x.user_account_id,
                        principalSchema: "identity",
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "permission",
                columns: new[] { "code", "description" },
                values: new object[,]
                {
                    { "identity.account.update", "Hesabı pasife alma ve yeniden aktifleştirme" },
                    { "identity.account.view", "Hesap durumunu görme" },
                    { "identity.invite.create", "Parola oluşturma bağlantısı gönderme" },
                    { "identity.sync.create", "Senkronizasyonu elle başlatma" },
                    { "identity.sync.view", "Senkronizasyon durumunu görme" },
                    { "system.parameter.update", "Sistem parametrelerini değiştirme" },
                    { "system.parameter.view", "Sistem parametrelerini görme" }
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role",
                columns: new[] { "id", "code", "created_at", "created_by", "is_system", "name", "public_id", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { 1L, "system-administrator", new DateTimeOffset(new DateTime(2026, 9, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Sistem Yöneticisi", new Guid("0192a3b4-0001-7000-8000-000000000001"), null, null },
                    { 2L, "hr-identity-operations", new DateTimeOffset(new DateTime(2026, 9, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "İK Kimlik İşlemleri", new Guid("0192a3b4-0002-7000-8000-000000000002"), null, null }
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_permission",
                columns: new[] { "permission", "role_id" },
                values: new object[,]
                {
                    { "identity.account.update", 1L },
                    { "identity.account.view", 1L },
                    { "identity.invite.create", 1L },
                    { "identity.sync.create", 1L },
                    { "identity.sync.view", 1L },
                    { "system.parameter.update", 1L },
                    { "system.parameter.view", 1L },
                    { "identity.account.update", 2L },
                    { "identity.account.view", 2L },
                    { "identity.invite.create", 2L },
                    { "identity.sync.view", 2L }
                });

            // Hazir roller sabit kimlikle (1, 2) yuklendi; kimlik sayaci bunu bilmez. Ileride
            // eklenecek ilk rol (T4) 1 numarasini alip cakismasin diye sayac ileri alinir.
            //
            // SELECT setval(...) KULLANILMAZ: idempotent sema betigi (UAT dagitimi) her adimi bir
            // PL/pgSQL blogunun icine sarar ve orada sonucu kullanilmayan SELECT hata verir.
            // ALTER TABLE her iki baglamda da calisir.
            migrationBuilder.Sql("ALTER TABLE identity.role ALTER COLUMN id RESTART WITH 3;");

            migrationBuilder.CreateIndex(
                name: "ix_role_code",
                schema: "identity",
                table: "role",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_public_id",
                schema: "identity",
                table: "role",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_permission",
                schema: "identity",
                table: "role_permission",
                column: "permission");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_public_id",
                schema: "identity",
                table: "user_role",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_role_role_id",
                schema: "identity",
                table: "user_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_user_account_id_role_id",
                schema: "identity",
                table: "user_role",
                columns: new[] { "user_account_id", "role_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permission",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_role",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "permission",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "role",
                schema: "identity");
        }
    }
}
