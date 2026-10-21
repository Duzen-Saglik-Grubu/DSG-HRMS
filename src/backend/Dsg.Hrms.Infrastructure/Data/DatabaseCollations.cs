namespace Dsg.Hrms.Infrastructure.Data;

/// <summary>
/// Veritabani karsilastirma kurallari (ADR-0004 §10).
/// </summary>
/// <remarks>
/// Veritabani tr_TR yerel ayariyla kurulsa da Alpine tabanli PostgreSQL imajinda bu ayar
/// Turkce kurallari uygulamaz: siralama harf kodu sirasidir (C, I, O, S, U, Z, Ç, Ö, Ü, İ,
/// Ş) ve <c>lower('IŞIK')</c> "işik" verir. Kural bu yuzden kolon duzeyinde ICU ile verilir
/// (#244, tasarim gozden gecirmesi B-05).
/// </remarks>
public static class DatabaseCollations
{
    /// <summary>
    /// Turkce ICU kurali: Ç/Ğ/İ/Ö/Ş/Ü alfabedeki yerinde siralanir; I↔ı ve İ↔i esler.
    /// PostgreSQL'in ICU ile derlenmis olmasini gerektirir (resmi imajlar oyledir).
    /// </summary>
    public const string Turkish = "tr-TR-x-icu";
}
