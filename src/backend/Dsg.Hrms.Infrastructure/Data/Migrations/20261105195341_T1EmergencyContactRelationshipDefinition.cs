using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class T1EmergencyContactRelationshipDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            // 1. Yeni tur ve ilk yakinlik tanimlari (KR-135).
            migrationBuilder.DropCheckConstraint(
                name: "ck_definition_type",
                schema: "reference",
                table: "definition");

            migrationBuilder.InsertData(
                schema: "reference",
                table: "definition",
                columns: new[] { "id", "code", "created_at", "created_by", "is_active", "is_system", "name", "public_id", "sort_order", "type", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { 21L, "Y001", new DateTimeOffset(new DateTime(2026, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, "Eşi", new Guid("0192a3b4-1004-7000-8000-000000000001"), 1, 4, null, null },
                    { 22L, "Y002", new DateTimeOffset(new DateTime(2026, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, "Annesi", new Guid("0192a3b4-1004-7000-8000-000000000002"), 2, 4, null, null },
                    { 23L, "Y003", new DateTimeOffset(new DateTime(2026, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, "Babası", new Guid("0192a3b4-1004-7000-8000-000000000003"), 3, 4, null, null },
                    { 24L, "Y004", new DateTimeOffset(new DateTime(2026, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, "Kardeşi", new Guid("0192a3b4-1004-7000-8000-000000000004"), 4, 4, null, null }
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_definition_type",
                schema: "reference",
                table: "definition",
                sql: "type IN (1, 2, 3, 4)");

            // 2. Kolon once bos birakilabilir acilir; mevcut kayitlar serbest metinden tanima eslenir.
            migrationBuilder.AddColumn<long>(
                name: "relationship_definition_id",
                schema: "personnel",
                table: "emergency_contact",
                type: "bigint",
                nullable: true);

            // Ad eslesmesi buyuk/kucuk harften bagimsizdir (Turkce harmanlama). Eslesmeyen kayit
            // varsa migration durur: yakinlik bilgisi sessizce kaybolmamali. Ozellik #347 ile
            // geldi; kurulu bir ortamda kayit yoktur.
            migrationBuilder.Sql("""
                UPDATE personnel.emergency_contact c
                SET relationship_definition_id = d.id
                FROM reference.definition d
                WHERE d.type = 4 AND lower(d.name) = lower(btrim(c.relationship));

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM personnel.emergency_contact WHERE relationship_definition_id IS NULL) THEN
                        RAISE EXCEPTION 'emergency_contact: yakinligi tanimla eslesmeyen kayit var; once yakinlik tanimi eklenmeli (KR-135)';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "relationship_definition_id",
                schema: "personnel",
                table: "emergency_contact",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "relationship",
                schema: "personnel",
                table: "emergency_contact");

            // 3. Dizin ve yabanci anahtar.
            migrationBuilder.CreateIndex(
                name: "ix_emergency_contact_relationship_definition_id",
                schema: "personnel",
                table: "emergency_contact",
                column: "relationship_definition_id");

            migrationBuilder.AddForeignKey(
                name: "fk_emergency_contact_reference_definition_relationship_definit",
                schema: "personnel",
                table: "emergency_contact",
                column: "relationship_definition_id",
                principalSchema: "reference",
                principalTable: "definition",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_emergency_contact_reference_definition_relationship_definit",
                schema: "personnel",
                table: "emergency_contact");

            migrationBuilder.DropIndex(
                name: "ix_emergency_contact_relationship_definition_id",
                schema: "personnel",
                table: "emergency_contact");

            migrationBuilder.AddColumn<string>(
                name: "relationship",
                schema: "personnel",
                table: "emergency_contact",
                type: "text",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Geri donuste yakinlik tanimin adiyla serbest metne yazilir.
            migrationBuilder.Sql("""
                UPDATE personnel.emergency_contact c
                SET relationship = d.name
                FROM reference.definition d
                WHERE d.id = c.relationship_definition_id;
                """);

            migrationBuilder.DropColumn(
                name: "relationship_definition_id",
                schema: "personnel",
                table: "emergency_contact");

            migrationBuilder.DropCheckConstraint(
                name: "ck_definition_type",
                schema: "reference",
                table: "definition");

            migrationBuilder.DeleteData(
                schema: "reference",
                table: "definition",
                keyColumn: "id",
                keyValue: 21L);

            migrationBuilder.DeleteData(
                schema: "reference",
                table: "definition",
                keyColumn: "id",
                keyValue: 22L);

            migrationBuilder.DeleteData(
                schema: "reference",
                table: "definition",
                keyColumn: "id",
                keyValue: 23L);

            migrationBuilder.DeleteData(
                schema: "reference",
                table: "definition",
                keyColumn: "id",
                keyValue: 24L);

            // Sonradan eklenen yakinlik tanimlari da kaldirilir; aksi halde eski tur kisiti eklenemez.
            migrationBuilder.Sql("DELETE FROM reference.definition WHERE type = 4;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_definition_type",
                schema: "reference",
                table: "definition",
                sql: "type IN (1, 2, 3)");
        }
    }
}
