using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dsg.Hrms.Infrastructure.Data.Interceptors;

/// <summary>
/// Kapanan her oturum icin kimlik olayi yazar (SYG-KMLK-060) ve kimlik olayi kayitlarinin
/// degistirilmesini engeller.
/// </summary>
/// <remarks>
/// <para>
/// Oturum cok sayida yerde kapanir: cikis, hareketsizlik, toplam sure, baska cihazdan giris,
/// parola degisikligi ve sifirlama, davet, hesabin pasiflesmesi, jeton tekrari. Her birine ayri
/// kayit cagrisi eklemek birini unutmaya acikti. Oturumun kapandigi an
/// (<see cref="UserSession.EndedAt"/> bos iken dolar) tek noktada yakalanir.
/// </para>
/// <para>
/// Olay, oturumun kapanisiyla ayni islemde yazilir.
/// </para>
/// </remarks>
public sealed class SessionEndAuditInterceptor(
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Write(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Write(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Write(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        if (context.ChangeTracker.Entries<SecurityEventEntry>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "Kimlik olayi kayitlari degistirilemez ve silinemez (KR-060). Bu tabloya yalnizca ekleme yapilabilir.");
        }

        var now = dateTimeProvider.UtcNow;

        // Once toplanir, sonra eklenir: gezinirken degisiklik izleyiciye ekleme yapilmaz.
        var ended = context.ChangeTracker
            .Entries<UserSession>()
            .Where(e => e.State == EntityState.Modified
                && e.Property(s => s.EndedAt).OriginalValue is null
                && e.Entity.EndedAt is not null)
            .Select(e => e.Entity)
            .ToList();

        foreach (var session in ended)
        {
            context.Set<SecurityEventEntry>().Add(SecurityEventLog.Create(
                SecurityEventType.SessionEnded, session.UserAccountId, null, EndReasonCode(session.EndReason), currentUser, now));
        }
    }

    /// <summary>Kapanma nedeninin, istemciye donen hata turuyla ayni kodu (SYG-KMLK-042).</summary>
    public static string EndReasonCode(SessionEndReason? reason) => reason switch
    {
        SessionEndReason.LoggedOut => "logged-out",
        SessionEndReason.SignedInElsewhere => "signed-in-elsewhere",
        SessionEndReason.TokenReuse => "token-reuse",
        SessionEndReason.IdleTimeout => "idle-timeout",
        SessionEndReason.Expired => "expired",
        SessionEndReason.AccountChanged => "account-changed",
        SessionEndReason.PasswordChanged => "password-changed",
        _ => "unknown",
    };
}
