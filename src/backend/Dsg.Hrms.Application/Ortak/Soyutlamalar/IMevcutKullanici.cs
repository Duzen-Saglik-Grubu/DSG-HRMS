namespace Dsg.Hrms.Application.Ortak.Soyutlamalar;

/// <summary>
/// Istegi yapan kullaniciya erisim saglar.
/// </summary>
/// <remarks>
/// Denetim alanlarinin doldurulmasi ve satir bazli yetki kapsami bu soyutlamayi
/// kullanir (ADR-0007 §3). Uygulamasi Api katmanindadir; Application katmani
/// HTTP baglamini bilmez.
/// </remarks>
public interface IMevcutKullanici
{
    /// <summary>
    /// Oturum acmis kullanici hesabinin kimligi.
    /// Arka plan islerinde ve kimlik dogrulanmamis isteklerde <c>null</c> doner.
    /// </summary>
    long? KullaniciId { get; }
}
