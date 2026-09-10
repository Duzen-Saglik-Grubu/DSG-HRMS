namespace Dsg.Hrms.Application.Common.Abstractions;

/// <summary>
/// Suanki zamani saglar.
/// </summary>
/// <remarks>
/// <c>DateTimeOffset.UtcNow</c> dogrudan kullanilmaz; boylece zamana bagli is
/// kurallari (izin hakedisi, kidem, istisna tarih araliklari) testte sabit bir
/// zaman verilerek dogrulanabilir.
/// </remarks>
public interface IDateTimeProvider
{
    /// <summary>Suanki an. DAIMA UTC (ADR-0004 §3).</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Bugunun tarihi (Europe/Istanbul takvimine gore).</summary>
    DateOnly Today { get; }
}
