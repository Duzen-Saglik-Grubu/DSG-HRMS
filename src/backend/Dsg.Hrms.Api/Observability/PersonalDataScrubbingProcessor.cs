using System.Diagnostics;
using OpenTelemetry;

namespace Dsg.Hrms.Api.Observability;

/// <summary>
/// Izleme kayitlarindan kisisel veri tasima ihtimali olan etiketleri siler
/// (ADR-0009 §4).
/// </summary>
/// <remarks>
/// <para>
/// Izleme verisi cogu kurumda "teknik veri" sayilir ve gozden kacar; oysa iki etiket
/// duzenli olarak kisisel veri tasir:
/// </para>
/// <list type="bullet">
/// <item>
/// <c>db.statement</c> / <c>db.query.text</c> — SQL metni. Parametreler ayri gitse de
/// metnin kendisi tablo ve kolon adlarini, bazen de gomulu degerleri tasir.
/// </item>
/// <item>
/// <c>url.query</c> — sorgu dizesi. Arama kutusuna girilen bir T.C. kimlik numarasi
/// dogrudan buraya duser (ayni gerekceyle istek gunlugune de yazilmiyor).
/// </item>
/// </list>
/// <para>
/// Etiketler <b>silinir, maskelenmez</b>: bir SQL metnini kismen gostermenin tani
/// degeri yoktur; islemin suresi ve turu zaten ayri etiketlerde durur.
/// </para>
/// </remarks>
public sealed class PersonalDataScrubbingProcessor : BaseProcessor<Activity>
{
    /// <summary>Izden tamamen silinen etiketler.</summary>
    private static readonly string[] RemovedTags =
    [
        "db.statement",
        "db.query.text",
        "url.query",
        "url.full",
        "http.url",
    ];

    /// <inheritdoc />
    public override void OnEnd(Activity data)
    {
        ArgumentNullException.ThrowIfNull(data);

        foreach (var tag in RemovedTags)
        {
            // SetTag(name, null) etiketi kaldirir.
            if (data.GetTagItem(tag) is not null)
            {
                data.SetTag(tag, null);
            }
        }

        base.OnEnd(data);
    }
}
