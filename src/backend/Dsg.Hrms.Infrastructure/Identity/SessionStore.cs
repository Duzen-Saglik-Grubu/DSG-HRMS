using Dsg.Hrms.Application.Identity.Sessions;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Identity;

/// <summary><see cref="ISessionStore"/> uygulamasi (PostgreSQL).</summary>
public sealed class SessionStore : ISessionStore
{
    private readonly HrmsDbContext _context;

    /// <summary>Yeni ornek olusturur.</summary>
    public SessionStore(HrmsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SignInCandidate?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        // E-posta kucuk harfle saklanir (veritabani kisiti). Ortak adresle giris yapilamaz
        // (SYG-KMLK-008): birden fazla kisi eslesirse kimse secilmez.
        var persons = await _context.Set<Person>()
            .Where(p => p.Email == normalizedEmail && !p.IsEmailShared)
            .Take(2)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (persons.Count != 1)
        {
            return null;
        }

        var account = await _context.Set<UserAccount>()
            .SingleOrDefaultAsync(a => a.PersonId == persons[0].Id, cancellationToken)
            .ConfigureAwait(false);

        return account is null ? null : await ToCandidateAsync(account, persons[0], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SignInCandidate?> FindByAccountIdAsync(long userAccountId, CancellationToken cancellationToken)
    {
        var account = await _context.Set<UserAccount>()
            .SingleOrDefaultAsync(a => a.Id == userAccountId, cancellationToken)
            .ConfigureAwait(false);
        if (account is null)
        {
            return null;
        }

        var person = await _context.Set<Person>()
            .SingleAsync(p => p.Id == account.PersonId, cancellationToken)
            .ConfigureAwait(false);

        return await ToCandidateAsync(account, person, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<LoginThrottle?> FindThrottleAsync(string emailHash, CancellationToken cancellationToken) =>
        _context.Set<LoginThrottle>().SingleOrDefaultAsync(t => t.EmailHash == emailHash, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSession>> GetOpenSessionsAsync(long userAccountId, CancellationToken cancellationToken) =>
        await _context.Set<UserSession>()
            .Where(s => s.UserAccountId == userAccountId && s.EndedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<UserSession?> FindSessionAsync(Guid publicId, CancellationToken cancellationToken) =>
        _context.Set<UserSession>().SingleOrDefaultAsync(s => s.PublicId == publicId, cancellationToken);

    /// <inheritdoc />
    public async Task<(RefreshToken Token, UserSession Session)?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var token = await _context.Set<RefreshToken>()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        if (token is null)
        {
            return null;
        }

        var session = await _context.Set<UserSession>()
            .SingleAsync(s => s.Id == token.SessionId, cancellationToken)
            .ConfigureAwait(false);

        return (token, session);
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkUsedAsync(long refreshTokenId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Tek komutluk kosullu guncelleme: eszamanli iki istekten yalnizca biri satiri degistirir.
        var affected = await _context.Set<RefreshToken>()
            .Where(t => t.Id == refreshTokenId && t.UsedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.UsedAt, now), cancellationToken)
            .ConfigureAwait(false);

        return affected == 1;
    }

    /// <inheritdoc />
    public Task<LoginChallenge?> FindChallengeAsync(Guid publicId, CancellationToken cancellationToken) =>
        _context.Set<LoginChallenge>().SingleOrDefaultAsync(c => c.PublicId == publicId, cancellationToken);

    /// <inheritdoc />
    public void Add(object entity) => _context.Add(entity);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);

    private async Task<SignInCandidate> ToCandidateAsync(UserAccount account, Person person, CancellationToken cancellationToken)
    {
        var hasActiveEmployment = await _context.Set<Employment>()
            .AnyAsync(e => e.PersonId == person.Id && e.IsActive, cancellationToken)
            .ConfigureAwait(false);

        return new SignInCandidate(account, person, hasActiveEmployment);
    }
}
