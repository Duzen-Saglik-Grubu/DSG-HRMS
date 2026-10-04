using System.Text.Json.Serialization;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Identity.Accounts;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Domain.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// IK hesap islemleri (SYG-KMLK-057, 073).
/// </summary>
/// <remarks>
/// Yanit kisisel veri olarak YALNIZCA ad, soyad, sicil ve firma tasir; TCKN, dogum tarihi,
/// e-posta ve telefon hic donmez (SYG-KMLK-073). Listeleme erisim kaydina yazilir.
/// </remarks>
[ApiController]
[Route("api/v1/identity/accounts")]
[Produces("application/json")]
public sealed class AccountsController : ControllerBase
{
    private readonly AccountAdministrationService _accounts;

    /// <summary>Yeni ornek olusturur.</summary>
    public AccountsController(AccountAdministrationService accounts)
    {
        _accounts = accounts;
    }

    /// <summary>Kisileri sicil numarasi, ad veya soyadla arar.</summary>
    /// <response code="200">Sayfalanmis liste.</response>
    /// <response code="400">Gecersiz sayfa veya siralama.</response>
    /// <response code="403">Yetki yok (<c>identity.account.view</c>).</response>
    [HttpGet]
    [HasPermission(Permissions.AccountView)]
    [ProducesResponseType<PagedResponse<AccountSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<PagedResponse<AccountSummaryResponse>> SearchAsync([FromQuery] AccountSearchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _accounts.SearchAsync(
            request.Q,
            request.Sort == AccountSortField.FirstName ? AccountSort.FirstName : AccountSort.LastName,
            request.Order == SortOrder.Desc,
            new PageRequest(request.Page, request.PageSize),
            cancellationToken);

        return PagedResponse.From(result, ToResponse);
    }

    /// <summary>Hesabi gerekceyle elle pasife alir; acik oturumlar hemen kapanir.</summary>
    /// <response code="204">Hesap pasif.</response>
    /// <response code="400">Gerekce girilmedi.</response>
    /// <response code="403">Yetki yok (<c>identity.account.update</c>).</response>
    /// <response code="404">Kisi bulunamadi.</response>
    /// <response code="422">Kisinin hesabi yok veya hesap zaten pasif.</response>
    [HttpPost("{personId:guid}/deactivation")]
    [HasPermission(Permissions.AccountUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeactivateAsync(Guid personId, AccountStatusChangeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _accounts.DeactivateAsync(personId, request.Reason, cancellationToken);
        return NoContent();
    }

    /// <summary>Hesabi gerekceyle elle yeniden aktiflestirir.</summary>
    /// <response code="204">Hesap aktif.</response>
    /// <response code="400">Gerekce girilmedi.</response>
    /// <response code="403">Yetki yok (<c>identity.account.update</c>).</response>
    /// <response code="404">Kisi bulunamadi.</response>
    /// <response code="422">Kisinin hesabi yok, hesap zaten aktif veya kisinin aktif calisma kaydi yok.</response>
    [HttpPost("{personId:guid}/activation")]
    [HasPermission(Permissions.AccountUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ActivateAsync(Guid personId, AccountStatusChangeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _accounts.ActivateAsync(personId, request.Reason, cancellationToken);
        return NoContent();
    }

    private static AccountSummaryResponse ToResponse(AccountSummary summary) => new(
        summary.PersonId,
        summary.FirstName,
        summary.LastName,
        [.. summary.Employments.Select(e => new AccountEmploymentResponse(e.RegistryCode, e.CompanyName, e.IsActive))],
        summary.State switch
        {
            AccountState.Active => AccountStateKind.Active,
            AccountState.Passive => AccountStateKind.Passive,
            AccountState.Locked => AccountStateKind.Locked,
            _ => AccountStateKind.None,
        },
        summary.StatusReason switch
        {
            AccountStatusReason.EmploymentEnded => AccountStatusReasonKind.EmploymentEnded,
            AccountStatusReason.Manual => AccountStatusReasonKind.Manual,
            AccountStatusReason.NewEmployment => AccountStatusReasonKind.NewEmployment,
            _ => null,
        },
        summary.IsCurrentUser);
}

/// <summary>Sayfalanmis yanit (ADR-0010 §4).</summary>
/// <param name="Items">Bu sayfadaki kayitlar.</param>
/// <param name="Page">Sayfa numarasi.</param>
/// <param name="PageSize">Sayfa boyutu.</param>
/// <param name="TotalCount">Toplam kayit sayisi.</param>
/// <param name="TotalPages">Toplam sayfa sayisi.</param>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

/// <summary>Sayfalanmis yanit ureticisi.</summary>
public static class PagedResponse
{
    /// <summary>Uygulama katmaninin sonucunu yanita cevirir.</summary>
    public static PagedResponse<T> From<TSource, T>(PagedResult<TSource> result, Func<TSource, T> map)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);

        return new PagedResponse<T>([.. result.Items.Select(map)], result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }
}

/// <summary>Arama istegi.</summary>
public sealed class AccountSearchRequest
{
    /// <summary>Sicil numarasi (basi), ad veya soyad (icerik).</summary>
    [FromQuery(Name = "q")]
    public string? Q { get; init; }

    /// <summary>Sayfa (1'den baslar).</summary>
    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    /// <summary>Sayfa boyutu (1–100).</summary>
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = 25;

    /// <summary>Siralama alani.</summary>
    [FromQuery(Name = "sort")]
    public AccountSortField Sort { get; init; } = AccountSortField.LastName;

    /// <summary>Siralama yonu.</summary>
    [FromQuery(Name = "order")]
    public SortOrder Order { get; init; } = SortOrder.Asc;

    /// <inheritdoc />
    public override string ToString() => nameof(AccountSearchRequest);
}

/// <summary>Siralama alani.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AccountSortField>))]
public enum AccountSortField
{
    /// <summary>Soyad.</summary>
    [JsonStringEnumMemberName("lastName")]
    LastName = 1,

    /// <summary>Ad.</summary>
    [JsonStringEnumMemberName("firstName")]
    FirstName = 2,
}

/// <summary>Siralama yonu.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SortOrder>))]
public enum SortOrder
{
    /// <summary>Artan.</summary>
    [JsonStringEnumMemberName("asc")]
    Asc = 1,

    /// <summary>Azalan.</summary>
    [JsonStringEnumMemberName("desc")]
    Desc = 2,
}

/// <summary>Listedeki kisi. Yalnizca ad, soyad, sicil ve firma (SYG-KMLK-073).</summary>
/// <param name="PersonId">Kisinin dis kimligi.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="Employments">Istihdamlar (sicil ve firma).</param>
/// <param name="State">Hesap durumu.</param>
/// <param name="StatusReason">Durumun nedeni.</param>
/// <param name="IsCurrentUser">Satir oturumdaki kullanicinin kendisi mi; kendi hesabi pasife alinamaz (#138).</param>
public sealed record AccountSummaryResponse(
    Guid PersonId,
    string FirstName,
    string LastName,
    IReadOnlyList<AccountEmploymentResponse> Employments,
    AccountStateKind State,
    AccountStatusReasonKind? StatusReason,
    bool IsCurrentUser);

/// <summary>Istihdam.</summary>
/// <param name="RegistryCode">Sicil numarasi.</param>
/// <param name="CompanyName">Firma.</param>
/// <param name="IsActive">Istihdam suruyor mu.</param>
public sealed record AccountEmploymentResponse(string RegistryCode, string CompanyName, bool IsActive);

/// <summary>Hesap durumu.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AccountStateKind>))]
public enum AccountStateKind
{
    /// <summary>Hesap yok (uye olmamis).</summary>
    [JsonStringEnumMemberName("none")]
    None = 1,

    /// <summary>Aktif.</summary>
    [JsonStringEnumMemberName("active")]
    Active = 2,

    /// <summary>Pasif.</summary>
    [JsonStringEnumMemberName("passive")]
    Passive = 3,

    /// <summary>Hatali giris siniri asildigi icin gecici olarak kilitli.</summary>
    [JsonStringEnumMemberName("locked")]
    Locked = 4,
}

/// <summary>Durumun nedeni.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AccountStatusReasonKind>))]
public enum AccountStatusReasonKind
{
    /// <summary>Tum istihdamlar sona erdi (senkronizasyon).</summary>
    [JsonStringEnumMemberName("employmentEnded")]
    EmploymentEnded = 1,

    /// <summary>IK tarafindan elle.</summary>
    [JsonStringEnumMemberName("manual")]
    Manual = 2,

    /// <summary>Yeni istihdamla yeniden aktiflesti.</summary>
    [JsonStringEnumMemberName("newEmployment")]
    NewEmployment = 3,
}

/// <summary>Hesap durumu degisikligi istegi.</summary>
/// <param name="Reason">Gerekce (zorunlu; denetim izine yazilir).</param>
public sealed record AccountStatusChangeRequest(string Reason);

/// <summary>Arama istegi dogrulamasi.</summary>
public sealed class AccountSearchRequestValidator : AbstractValidator<AccountSearchRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public AccountSearchRequestValidator()
    {
        RuleFor(r => r.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");
        RuleFor(r => r.PageSize)
            .InclusiveBetween(1, PagedResult<object>.MaxPageSize)
            .WithMessage($"Sayfa boyutu 1 ile {PagedResult<object>.MaxPageSize} arasında olmalıdır.");
        RuleFor(r => r.Q).MaximumLength(100).WithMessage("Arama metni en fazla 100 karakter olabilir.");
        RuleFor(r => r.Sort).IsInEnum().WithMessage("Geçersiz sıralama alanı.");
        RuleFor(r => r.Order).IsInEnum().WithMessage("Geçersiz sıralama yönü.");
    }
}

/// <summary>Durum degisikligi istegi dogrulamasi.</summary>
public sealed class AccountStatusChangeRequestValidator : AbstractValidator<AccountStatusChangeRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public AccountStatusChangeRequestValidator()
    {
        RuleFor(r => r.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason)).WithMessage("Gerekçe girin.")
            .MaximumLength(500).WithMessage("Gerekçe en fazla 500 karakter olabilir.");
    }
}
