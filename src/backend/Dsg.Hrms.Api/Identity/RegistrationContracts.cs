using System.Text.Json.Serialization;
using Dsg.Hrms.Application.Common.Security;
using Dsg.Hrms.Domain.Personnel;
using FluentValidation;

namespace Dsg.Hrms.Api.Identity;

/// <summary>Uyelik baslatma istegi (SYG-KMLK-013).</summary>
/// <param name="NationalId">T.C. Kimlik Numarasi.</param>
/// <param name="BirthDate">Dogum tarihi.</param>
/// <param name="Email">Kurumsal e-posta.</param>
public sealed record StartRegistrationRequest(
    [property: PersonalData(PersonalDataKind.NationalId)] string NationalId,
    [property: PersonalData(PersonalDataKind.Unspecified)] DateOnly BirthDate,
    [property: PersonalData(PersonalDataKind.Email)] string Email)
{
    /// <inheritdoc />
    public override string ToString() => nameof(StartRegistrationRequest);
}

/// <summary>Kod istegi (SYG-KMLK-019).</summary>
/// <param name="Channel">Secilen kanal.</param>
public sealed record RequestCodeRequest(VerificationChannelKind Channel);

/// <summary>Kod dogrulama istegi.</summary>
/// <param name="Code">Girilen kod.</param>
public sealed record VerifyCodeRequest([property: Secret] string Code)
{
    /// <inheritdoc />
    public override string ToString() => nameof(VerifyCodeRequest);
}

/// <summary>Hesap olusturma istegi.</summary>
/// <param name="Password">Parola.</param>
public sealed record CompleteRegistrationRequest([property: Secret] string Password)
{
    /// <inheritdoc />
    public override string ToString() => nameof(CompleteRegistrationRequest);
}

/// <summary>Uyelik baslatildi.</summary>
/// <param name="RegistrationId">Sonraki adimlarda kullanilacak kimlik.</param>
/// <param name="Channels">Sunulan kanallar. Kodun gidecegi hedef HICBIR bicimde donmez (SYG-KMLK-018).</param>
/// <param name="ExpiresAt">Uyelik isleminin gecerlilik sonu.</param>
public sealed record RegistrationStartedResponse(Guid RegistrationId, IReadOnlyList<VerificationChannelKind> Channels, DateTimeOffset ExpiresAt);

/// <summary>Kod istendi.</summary>
/// <param name="CodeExpiresAt">Kodun gecerlilik sonu (geri sayim, SYG-KMLK-028).</param>
public sealed record CodeRequestedResponse(DateTimeOffset CodeExpiresAt);

/// <summary>Kod dogrulama sonucu.</summary>
/// <param name="Result">Sonuc.</param>
/// <param name="AccountExists">Kisinin zaten hesabi var mi; yalnizca dogrulamadan sonra true olabilir; kullanici parola sifirlamaya yonlendirilir (SYG-KMLK-020).</param>
public sealed record VerificationResponse(VerificationOutcome Result, bool AccountExists);

/// <summary>Dogrulama kanali.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<VerificationChannelKind>))]
public enum VerificationChannelKind
{
    /// <summary>Kurumsal e-posta.</summary>
    [JsonStringEnumMemberName("email")]
    Email = 1,

    /// <summary>SMS.</summary>
    [JsonStringEnumMemberName("sms")]
    Sms = 2,
}

/// <summary>Kod dogrulama sonucu.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<VerificationOutcome>))]
public enum VerificationOutcome
{
    /// <summary>Kod dogru; parola belirlenebilir.</summary>
    [JsonStringEnumMemberName("verified")]
    Verified = 1,

    /// <summary>Kod yanlis; deneme hakki suruyor.</summary>
    [JsonStringEnumMemberName("mismatch")]
    Mismatch = 2,

    /// <summary>Kodun suresi doldu; yeni kod istenmeli (SYG-KMLK-023).</summary>
    [JsonStringEnumMemberName("expired")]
    Expired = 3,

    /// <summary>Deneme siniri asildi; yeni kod istenmeli (SYG-KMLK-024).</summary>
    [JsonStringEnumMemberName("attemptsExceeded")]
    AttemptsExceeded = 4,

    /// <summary>Kod kullanilamaz (istenmemis veya gecersiz kilinmis); yeni kod istenmeli.</summary>
    [JsonStringEnumMemberName("notUsable")]
    NotUsable = 5,
}

/// <summary>Uyelik baslatma istegi dogrulamasi (SYG-KMLK-014).</summary>
public sealed class StartRegistrationRequestValidator : AbstractValidator<StartRegistrationRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public StartRegistrationRequestValidator(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        // Gecersiz TCKN eslestirmeye GONDERILMEZ; ileti kisinin varligi hakkinda bilgi vermez.
        RuleFor(r => r.NationalId)
            .Must(NationalId.IsValid)
            .WithMessage("Gecerli bir T.C. Kimlik Numarasi girin.");

        RuleFor(r => r.BirthDate)
            .Must(date => date >= new DateOnly(1900, 1, 1) && date <= DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("Gecerli bir dogum tarihi girin.");

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Kurumsal e-posta adresinizi girin.")
            .MaximumLength(254).WithMessage("Gecerli bir e-posta adresi girin.")
            .EmailAddress().WithMessage("Gecerli bir e-posta adresi girin.");
    }
}

/// <summary>Kod istegi dogrulamasi.</summary>
public sealed class RequestCodeRequestValidator : AbstractValidator<RequestCodeRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public RequestCodeRequestValidator()
    {
        RuleFor(r => r.Channel).IsInEnum().WithMessage("Bir dogrulama kanali secin.");
    }
}

/// <summary>Kod dogrulama istegi dogrulamasi.</summary>
public sealed class VerifyCodeRequestValidator : AbstractValidator<VerifyCodeRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public VerifyCodeRequestValidator()
    {
        RuleFor(r => r.Code)
            .NotEmpty().WithMessage("Dogrulama kodunu girin.")
            .MaximumLength(12).WithMessage("Dogrulama kodu gecerli degil.");
    }
}

/// <summary>Hesap olusturma istegi dogrulamasi. Parola kurallari serviste denetlenir.</summary>
public sealed class CompleteRegistrationRequestValidator : AbstractValidator<CompleteRegistrationRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public CompleteRegistrationRequestValidator()
    {
        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("Parolanizi girin.")
            .MaximumLength(512).WithMessage("Parola en fazla 128 karakter olabilir.");
    }
}
