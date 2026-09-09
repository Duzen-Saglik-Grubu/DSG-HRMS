using System.Security.Claims;
using Dsg.Hrms.Application.Ortak.Soyutlamalar;

namespace Dsg.Hrms.Api.Kimlik;

/// <summary>
/// Oturum acmis kullaniciyi HTTP baglamindan okur.
/// </summary>
/// <remarks>
/// <para>
/// Kimlik dogrulama altyapisi henuz kurulmadigi icin su an daima <c>null</c> doner;
/// bu, islemlerin "sistem" adina yapildigi anlamina gelir. Kimlik Yonetimi modulu
/// (T3) tamamlandiginda talep (claim) okuma devreye girecektir.
/// </para>
/// <para>
/// Arka plan islerinde HTTP baglami bulunmaz; bu durumda da <c>null</c> doner.
/// </para>
/// </remarks>
public sealed class HttpBaglamMevcutKullanici(IHttpContextAccessor baglamErisimi) : IMevcutKullanici
{
    /// <summary>Kullanici kimliginin tasindigi talep (claim) adi.</summary>
    public const string KullaniciIdTalebi = "hrms:kullanici_id";

    /// <inheritdoc />
    public long? KullaniciId
    {
        get
        {
            var deger = baglamErisimi.HttpContext?.User?.FindFirstValue(KullaniciIdTalebi);

            return long.TryParse(deger, System.Globalization.CultureInfo.InvariantCulture, out var id)
                ? id
                : null;
        }
    }
}
