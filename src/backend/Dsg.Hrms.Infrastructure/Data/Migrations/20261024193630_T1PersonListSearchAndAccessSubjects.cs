using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class T1PersonListSearchAndAccessSubjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "search_key",
                schema: "personnel",
                table: "person",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid[]>(
                name: "subject_ids",
                schema: "audit",
                table: "access_log",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.CreateIndex(
                name: "ix_access_log_subject_ids",
                schema: "audit",
                table: "access_log",
                column: "subject_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            // Mevcut kisilerin arama anahtari (ADR-0021 §7). Kural Domain.Personnel.SearchText ile
            // aynidir: Turkce harfler ASCII karsiliklarina, sonra kucuk harfe. Onceki soyadi yalnizca
            // kadin personelde tutuldugu icin (ck_person_previous_surname_female) ayrica kosul gerekmez.
            migrationBuilder.Sql("""
                UPDATE personnel.person
                   SET search_key = lower(translate(
                       concat_ws(' ', NULLIF(btrim(first_name), ''), NULLIF(btrim(last_name), ''), NULLIF(btrim(previous_surname), '')),
                       'ÇĞIİÖŞÜçğıöşü', 'CGIIOSUcgiosu'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_access_log_subject_ids",
                schema: "audit",
                table: "access_log");

            migrationBuilder.DropColumn(
                name: "search_key",
                schema: "personnel",
                table: "person");

            migrationBuilder.DropColumn(
                name: "subject_ids",
                schema: "audit",
                table: "access_log");
        }
    }
}
