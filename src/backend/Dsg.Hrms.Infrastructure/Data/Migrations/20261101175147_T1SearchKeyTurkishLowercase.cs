using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Arama anahtarlarini yeniden uretir (#306). T1PersonListSearchAndAccessSubjects'teki doldurma,
    /// Turkce siralamali ad sutunlarinda <c>lower('I')</c> noktasiz "ı" verdigi icin "I" ve "İ"
    /// iceren adlarda "ısmaıl" gibi anahtarlar yazdi; uygulama ise aranan terimi "ismail" yapar.
    /// Kural <c>Domain.Personnel.SearchText</c> ile ayni: Turkce harfler once KUCUK ASCII
    /// karsiliklarina, sonra kucuk harfe.
    /// </summary>
    public partial class T1SearchKeyTurkishLowercase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE personnel.person
                   SET search_key = lower(translate(
                       concat_ws(' ', NULLIF(btrim(first_name), ''), NULLIF(btrim(last_name), ''), NULLIF(btrim(previous_surname), '')),
                       'ÇĞIİÖŞÜçğıöşü', 'cgiiosucgiosu'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alinacak bir sey yok: eski anahtarlar hataliydi ve uygulama kisi her
            // guncellendiginde dogru anahtari yazar.
        }
    }
}
