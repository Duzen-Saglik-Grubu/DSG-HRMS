namespace Dsg.Hrms.Application.Ortak.Soyutlamalar;

/// <summary>
/// Suanki zamani saglar.
/// </summary>
/// <remarks>
/// <c>DateTimeOffset.UtcNow</c> dogrudan kullanilmaz; boylece zamana bagli is
/// kurallari (izin hakedisi, kidem, istisna tarih araliklari) testte sabit bir
/// zaman verilerek dogrulanabilir.
/// Doner deger DAIMA UTC'dir (ADR-0004 §3).
/// </remarks>
public interface IZamanSaglayici
{
    /// <summary>Suanki an (UTC).</summary>
    DateTimeOffset SuAn { get; }

    /// <summary>Bugunun tarihi (Europe/Istanbul takvimine gore).</summary>
    DateOnly Bugun { get; }
}
