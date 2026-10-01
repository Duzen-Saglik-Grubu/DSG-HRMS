using System.Reflection;

namespace Dsg.Hrms.Application.Common.Security;

/// <summary>Bir alana uygulanacak islem.</summary>
public enum MaskAction
{
    /// <summary>Deger oldugu gibi yazilir.</summary>
    None = 0,

    /// <summary>Deger <see cref="MaskDecision.Kind"/> kuralina gore maskelenir.</summary>
    Mask = 1,

    /// <summary>Deger hicbir bicimde yazilmaz.</summary>
    Exclude = 2,
}

/// <summary>Bir alan icin verilen maskeleme karari.</summary>
/// <param name="Action">Uygulanacak islem.</param>
/// <param name="Kind">
/// <see cref="MaskAction.Mask"/> durumunda kullanilacak maskeleme kurali.
/// </param>
public readonly record struct MaskDecision(MaskAction Action, PersonalDataKind Kind)
{
    /// <summary>Islem gerektirmeyen karar.</summary>
    public static MaskDecision None => new(MaskAction.None, PersonalDataKind.Unspecified);
}

/// <summary>
/// Bir alanin maskelenip maskelenmeyecegine karar verir (ADR-0009 §4).
/// </summary>
/// <remarks>
/// <para>
/// Karar iki katmanlidir ve bu bilincli bir tekrardir:
/// </para>
/// <list type="number">
/// <item>
/// <b>Oznitelik</b> (<see cref="PersonalDataAttribute"/>, <see cref="SecretAttribute"/>):
/// birincil ve kesin kaynaktir.
/// </item>
/// <item>
/// <b>Ad benzerligi</b>: oznitelik unutulmus olabilir. Bilinen hassas alan adlari
/// oznitelik olmasa da maskelenir. Bu, tek bir gozden kacmanin KVKK ihlaline
/// donusmesini engelleyen ikinci savunma hattidir.
/// </item>
/// </list>
/// <para>
/// Ikinci katman yanlis pozitif uretebilir: hassas olmayan bir alan gereksiz
/// maskelenebilir. Bu, tersine gore kabul edilebilir bir maliyettir.
/// </para>
/// </remarks>
public static class MaskRules
{
    // Ad -> kural. Karsilastirma buyuk/kucuk harf duyarsiz ve KULTUR BAGIMSIZDIR:
    // Turkce kulturde "I".ToLower() = "i" olmadigi icin kultur duyarli karsilastirma
    // "IBAN" gibi adlari kacirabilirdi.
    private static readonly Dictionary<string, MaskDecision> ByName =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // --- Kisisel veri: kismen gorunur ---
            ["NationalId"] = new(MaskAction.Mask, PersonalDataKind.NationalId),
            ["IdentityNumber"] = new(MaskAction.Mask, PersonalDataKind.NationalId),
            ["Tckn"] = new(MaskAction.Mask, PersonalDataKind.NationalId),

            // TCKN'nin anahtarli ozeti (RegistrationAttempt). Ozet de yazilmaz: anahtar ele
            // gecerse TCKN'nin dar araligi denenerek ozetten TCKN bulunabilir.
            ["NationalIdHash"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),

            ["Phone"] = new(MaskAction.Mask, PersonalDataKind.Phone),
            ["PhoneNumber"] = new(MaskAction.Mask, PersonalDataKind.Phone),
            ["MobilePhone"] = new(MaskAction.Mask, PersonalDataKind.Phone),
            ["Email"] = new(MaskAction.Mask, PersonalDataKind.Email),
            ["EmailAddress"] = new(MaskAction.Mask, PersonalDataKind.Email),
            ["Iban"] = new(MaskAction.Mask, PersonalDataKind.Iban),

            // Dogum tarihi uyelikte TCKN ile birlikte KIMLIK DOGRULAMA unsurudur
            // (SYG-KMLK-013). Ikisi ayni kayitta acik gorunurse denetim izini okuyan
            // biri baskasi adina uyelik baslatabilir. Kismi gosterimin anlamli bir
            // bicimi olmadigi icin tamamen maskelenir.
            ["BirthDate"] = new(MaskAction.Mask, PersonalDataKind.Unspecified),

            // --- Sir: hic yazilmaz ---
            ["Password"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["PasswordHash"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["NewPassword"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["CurrentPassword"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["Token"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["AccessToken"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["RefreshToken"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["VerificationCode"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),

            // Dogrulama kodunun anahtarli ozeti (VerificationCode). Ozet de yazilmaz: anahtar
            // ele gecerse 6 haneli kodun tum olasiliklari saniyeler icinde denenebilir.
            ["CodeHash"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),

            // Yenileme jetonu ozeti ve giris sayacinin e-posta ozeti (oturum kayitlari).
            ["TokenHash"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["EmailHash"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),

            ["ApiKey"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["Secret"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["ClientSecret"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["ConnectionString"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),

            // Sifreli sir parametresi (SystemParameter). Sifreli bile olsa yazilmaz:
            // denetim izinde yalnizca "degisti" gorunur (KR-071).
            ["ProtectedValue"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),

            // Kurumsal logonun dosya icerigi (BrandLogo). Denetim izine dosya degil, ozeti ve boyutu
            // yazilir; izin satiri yuzlerce KB buyumez.
            ["Content"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),

            // Hesap guvenlik damgasi (UserAccount). Oturum jetonlari buna baglanir;
            // denetim izini okuyan biri jeton uretimine yardimci bir deger gormemelidir.
            ["SecurityStamp"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),

            // Bildirim govdesi: icinde dogrulama kodu ve kisisel veri tasir (ADR-0009 §4).
            ["MessageBody"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["SmsBody"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
            ["MailBody"] = new(MaskAction.Exclude, PersonalDataKind.Unspecified),
        };

    /// <summary>
    /// Bir uye icin karar uretir: once oznitelikler, sonra ad benzerligi.
    /// </summary>
    public static MaskDecision For(MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (member.GetCustomAttribute<SecretAttribute>() is not null)
        {
            return new MaskDecision(MaskAction.Exclude, PersonalDataKind.Unspecified);
        }

        var personalData = member.GetCustomAttribute<PersonalDataAttribute>();
        if (personalData is not null)
        {
            return new MaskDecision(MaskAction.Mask, personalData.Kind);
        }

        return ForName(member.Name);
    }

    /// <summary>
    /// Yalnizca ada bakarak karar uretir (ikinci savunma hatti).
    /// </summary>
    public static MaskDecision ForName(string? name) =>
        name is not null && ByName.TryGetValue(name, out var decision)
            ? decision
            : MaskDecision.None;

    /// <summary>
    /// Karari bir metin degere uygular.
    /// </summary>
    public static string? Apply(MaskDecision decision, string? value) => decision.Action switch
    {
        MaskAction.Exclude => Mask.SecretPlaceholder,
        MaskAction.Mask => Mask.ByKind(decision.Kind, value),
        _ => value,
    };
}
