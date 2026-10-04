using System.Globalization;
using System.Text.RegularExpressions;

namespace Dsg.Hrms.Application.Settings;

/// <summary>Parametre turu (Y4 katalogundaki "Tur" sutunu).</summary>
public enum ParameterType
{
    /// <summary>Tam sayi; <see cref="ParameterDefinition.Min"/>–<see cref="ParameterDefinition.Max"/> araliginda.</summary>
    Number = 1,

    /// <summary>Acik/Kapali.</summary>
    Toggle = 2,

    /// <summary>Serbest metin.</summary>
    Text = 3,

    /// <summary>Virgulle ayrilmis liste.</summary>
    List = 4,

    /// <summary>Sir (parola, API anahtari). Sifreli saklanir, geri okunamaz (<c>KR-071</c>).</summary>
    Secret = 5,
}

/// <summary>
/// Bir parametrenin katalog tanimi (SYG-KMLK-075).
/// </summary>
/// <param name="Key">Katalog kimligi (orn. <c>PRM-KML-03</c>).</param>
/// <param name="ConfigurationKey">
/// Veritabaninda deger yoksa bakilan yapilandirma anahtari (orn. <c>Parameters:MaxFailedLogins</c>).
/// Ilk kurulumun ekransiz yapilabilmesi ve sirlarin ortam degiskeninden gelebilmesi icindir (ADR-0008).
/// </param>
/// <param name="Type">Tur.</param>
/// <param name="Description">Turkce aciklama (parametre ekraninda gosterilir).</param>
/// <param name="DefaultValue">Kanonik varsayilan deger; yoksa <c>null</c> (kurulumda girilir).</param>
/// <param name="Min">Tam sayi icin alt sinir.</param>
/// <param name="Max">Tam sayi icin ust sinir.</param>
/// <param name="Pattern">Metnin veya her liste ogesinin uymasi gereken ifade.</param>
/// <param name="AllowedItems">Liste ogesi icin izin verilen degerler.</param>
/// <param name="FormatHint">Bicime uymayan degerde kullaniciya soylenen bicim, orn. "sunucu:port biçiminde olmalıdır (örneğin mail.duzen.com.tr:587)" (SYG-KMLK-064, B-09).</param>
public sealed record ParameterDefinition(
    string Key,
    string ConfigurationKey,
    ParameterType Type,
    string Description,
    string? DefaultValue = null,
    int? Min = null,
    int? Max = null,
    Regex? Pattern = null,
    IReadOnlyList<string>? AllowedItems = null,
    string? FormatHint = null)
{
    /// <summary>Sir parametre mi.</summary>
    public bool IsSecret => Type == ParameterType.Secret;

    /// <summary>
    /// Girilen degeri dogrular ve kanonik bicime getirir.
    /// </summary>
    /// <remarks>
    /// Kanonik bicim, saklanan ve karsilastirilan bicimdir: tam sayi kultur bagimsiz,
    /// Acik/Kapali <c>true</c>/<c>false</c>, liste kucuk harfli, bosluksuz, tekrarsiz ve
    /// virgulle ayrilmis. Boylece ayni degerin iki yazimi iki farkli deger sayilmaz.
    /// </remarks>
    public ParameterValidation Validate(string? input)
    {
        var value = input?.Trim() ?? string.Empty;

        if (value.Length == 0)
        {
            return ParameterValidation.Invalid("Değer boş olamaz.");
        }

        return Type switch
        {
            ParameterType.Number => ValidateInteger(value),
            ParameterType.Toggle => ValidateBoolean(value),
            ParameterType.List => ValidateList(value),
            ParameterType.Text when Pattern is not null && !Pattern.IsMatch(value) =>
                ParameterValidation.Invalid(FormatHint is null ? "Değer beklenen biçimde değil." : $"Değer {FormatHint}."),
            _ => ParameterValidation.Valid(value),
        };
    }

    private ParameterValidation ValidateInteger(string value)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            return ParameterValidation.Invalid("Değer bir tam sayı olmalıdır.");
        }

        if ((Min is { } min && number < min) || (Max is { } max && number > max))
        {
            return ParameterValidation.Invalid(
                string.Create(CultureInfo.InvariantCulture, $"Değer {Min}–{Max} aralığında olmalıdır."));
        }

        return ParameterValidation.Valid(number.ToString(CultureInfo.InvariantCulture));
    }

    private static ParameterValidation ValidateBoolean(string value) => value.ToUpperInvariant() switch
    {
        "TRUE" => ParameterValidation.Valid("true"),
        "FALSE" => ParameterValidation.Valid("false"),
        _ => ParameterValidation.Invalid("Değer açık veya kapalı olmalıdır."),
    };

    private ParameterValidation ValidateList(string value)
    {
        var items = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (items.Count == 0)
        {
            return ParameterValidation.Invalid("Liste en az bir öğe içermelidir.");
        }

        foreach (var item in items)
        {
            if (AllowedItems is not null && !AllowedItems.Contains(item, StringComparer.Ordinal))
            {
                return ParameterValidation.Invalid(
                    $"Geçersiz öğe: '{item}'. İzin verilenler: {string.Join(", ", AllowedItems)}.");
            }

            if (Pattern is not null && !Pattern.IsMatch(item))
            {
                return ParameterValidation.Invalid(FormatHint is null ? $"Geçersiz öğe: '{item}'." : $"Geçersiz öğe: '{item}'. Her öğe {FormatHint}.");
            }
        }

        return ParameterValidation.Valid(string.Join(',', items));
    }
}

/// <summary>Dogrulama sonucu.</summary>
/// <param name="IsValid">Gecerli mi.</param>
/// <param name="CanonicalValue">Gecerliyse kanonik deger.</param>
/// <param name="Error">Gecersizse Turkce hata iletisi.</param>
public sealed record ParameterValidation(bool IsValid, string? CanonicalValue, string? Error)
{
    /// <summary>Gecerli sonuc.</summary>
    public static ParameterValidation Valid(string canonicalValue) => new(true, canonicalValue, null);

    /// <summary>Gecersiz sonuc.</summary>
    public static ParameterValidation Invalid(string error) => new(false, null, error);
}
