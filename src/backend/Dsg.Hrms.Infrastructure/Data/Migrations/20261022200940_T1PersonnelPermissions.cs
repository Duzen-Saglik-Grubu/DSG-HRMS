using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class T1PersonnelPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "identity.sync.create", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "identity.sync.view", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "identity.sync.view", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "identity.sync.create");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "identity.sync.view");

            migrationBuilder.InsertData(
                schema: "identity",
                table: "permission",
                columns: new[] { "code", "description" },
                values: new object[,]
                {
                    { "audit.log.view", "Denetim kayıtlarını görme" },
                    { "personnel.correction.update", "Düzeltme taleplerini kapatma" },
                    { "personnel.correction.view", "Düzeltme taleplerini görme" },
                    { "personnel.person.export", "Personel listesini dışa aktarma" },
                    { "personnel.person.update", "Görev, evlilik bilgisi ve acil durum kişisini girme" },
                    { "personnel.person.view", "Tüm personeli görme" },
                    { "personnel.person.view_national_id", "Tam T.C. kimlik numarasını görme" },
                    { "personnel.person.view_sensitive", "Adres, kan grubu, acil durum kişileri ve evlilik bilgisini görme" },
                    { "personnel.quality.export", "Veri kalitesi raporunu dışa aktarma" },
                    { "personnel.quality.view", "Veri kalitesi raporunu görme" },
                    { "personnel.sync.create", "Senkronizasyonu elle başlatma" },
                    { "personnel.sync.view", "Senkronizasyon durumunu görme" },
                    { "reference.definition.update", "Ünvan ve görev tanımlarını değiştirme" },
                    { "reference.definition.view", "Ünvan ve görev tanımlarını görme" }
                });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role",
                keyColumn: "id",
                keyValue: 2L,
                column: "name",
                value: "İnsan Kaynakları");

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_permission",
                columns: new[] { "permission", "role_id" },
                values: new object[,]
                {
                    { "audit.log.view", 1L },
                    { "personnel.correction.update", 1L },
                    { "personnel.correction.view", 1L },
                    { "personnel.person.export", 1L },
                    { "personnel.person.update", 1L },
                    { "personnel.person.view", 1L },
                    { "personnel.person.view_national_id", 1L },
                    { "personnel.person.view_sensitive", 1L },
                    { "personnel.quality.export", 1L },
                    { "personnel.quality.view", 1L },
                    { "personnel.sync.create", 1L },
                    { "personnel.sync.view", 1L },
                    { "reference.definition.update", 1L },
                    { "reference.definition.view", 1L },
                    { "personnel.correction.update", 2L },
                    { "personnel.correction.view", 2L },
                    { "personnel.person.export", 2L },
                    { "personnel.person.update", 2L },
                    { "personnel.person.view", 2L },
                    { "personnel.person.view_national_id", 2L },
                    { "personnel.person.view_sensitive", 2L },
                    { "personnel.quality.export", 2L },
                    { "personnel.quality.view", 2L },
                    { "personnel.sync.create", 2L },
                    { "personnel.sync.view", 2L },
                    { "reference.definition.update", 2L },
                    { "reference.definition.view", 2L }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "audit.log.view", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.correction.update", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.correction.view", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.person.export", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.person.update", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.person.view", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.person.view_national_id", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.person.view_sensitive", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.quality.export", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.quality.view", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.sync.create", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.sync.view", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "reference.definition.update", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "reference.definition.view", 1L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.correction.update", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.correction.view", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.person.export", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.person.update", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.person.view", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.person.view_national_id", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.person.view_sensitive", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.quality.export", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.quality.view", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.sync.create", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "personnel.sync.view", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "reference.definition.update", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permission",
                keyColumns: new[] { "permission", "role_id" },
                keyValues: new object[] { "reference.definition.view", 2L });

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "audit.log.view");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "personnel.correction.update");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "personnel.correction.view");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "personnel.person.export");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "personnel.person.update");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "personnel.person.view");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "personnel.person.view_national_id");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "personnel.person.view_sensitive");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "personnel.quality.export");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "personnel.quality.view");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "personnel.sync.create");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "personnel.sync.view");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "reference.definition.update");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permission",
                keyColumn: "code",
                keyValue: "reference.definition.view");

            migrationBuilder.InsertData(
                schema: "identity",
                table: "permission",
                columns: new[] { "code", "description" },
                values: new object[,]
                {
                    { "identity.sync.create", "Senkronizasyonu elle başlatma" },
                    { "identity.sync.view", "Senkronizasyon durumunu görme" }
                });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role",
                keyColumn: "id",
                keyValue: 2L,
                column: "name",
                value: "İK Kimlik İşlemleri");

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_permission",
                columns: new[] { "permission", "role_id" },
                values: new object[,]
                {
                    { "identity.sync.create", 1L },
                    { "identity.sync.view", 1L },
                    { "identity.sync.view", 2L }
                });
        }
    }
}
