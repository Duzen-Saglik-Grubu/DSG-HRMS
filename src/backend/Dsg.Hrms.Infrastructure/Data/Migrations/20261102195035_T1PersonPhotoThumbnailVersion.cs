using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class T1PersonPhotoThumbnailVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut fotograflar ilk kuralla (surum 1, EXIF yonu uygulanarak) kucultulmustu; bir
            // sonraki senkronizasyon onlari yeni kuralla yeniden kucultur (#315).
            migrationBuilder.AddColumn<int>(
                name: "thumbnail_version",
                schema: "personnel",
                table: "person_photo",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "thumbnail_version",
                schema: "personnel",
                table: "person_photo");
        }
    }
}
