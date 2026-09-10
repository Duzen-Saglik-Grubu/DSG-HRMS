namespace Dsg.Hrms.Application.Common.Abstractions;

/// <summary>
/// Istegi yapan kullaniciya ve istek baglamina erisim saglar.
/// </summary>
/// <remarks>
/// Denetim alanlarinin doldurulmasi, denetim izi ve satir bazli yetki kapsami bu
/// soyutlamayi kullanir (ADR-0007 §3, ADR-0009 §2). Uygulamasi Api katmanindadir;
/// Application katmani HTTP baglamini bilmez.
/// </remarks>
public interface ICurrentUser
{
    /// <summary>
    /// Oturum acmis kullanici hesabinin kimligi.
    /// Arka plan islerinde ve kimlik dogrulanmamis isteklerde <c>null</c> doner.
    /// </summary>
    long? UserId { get; }

    /// <summary>
    /// Istegin geldigi IP adresi. HTTP baglami yoksa <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Denetim izinde tutulur: "bu islem nereden yapildi" sorusu, bir olay
    /// incelemesinin ilk sorularindandir.
    /// </remarks>
    string? IpAddress { get; }

    /// <summary>
    /// Istegin izleme kimligi. HTTP baglami yoksa <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Denetim izi ile uygulama gunlugunu iliskilendirir: bir denetim kaydindan
    /// ayni istegin teknik gunluk satirlarina ulasilabilir (ADR-0009 §2).
    /// </remarks>
    string? TraceId { get; }
}
