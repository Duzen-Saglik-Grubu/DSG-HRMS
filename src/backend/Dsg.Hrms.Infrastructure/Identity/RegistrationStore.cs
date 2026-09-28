using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Identity.Registration;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dsg.Hrms.Infrastructure.Identity;

/// <summary><see cref="IRegistrationStore"/> uygulamasi (PostgreSQL).</summary>
public sealed class RegistrationStore : IRegistrationStore
{
    /// <summary>Kisiye tek hesap kisitinin adi (SYG-KMLK-021).</summary>
    public const string SingleAccountConstraint = "ix_user_account_person_id";

    private readonly HrmsDbContext _context;

    /// <summary>Yeni ornek olusturur.</summary>
    public RegistrationStore(HrmsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<int> CountAttemptsSinceAsync(string nationalIdHash, DateTimeOffset since, CancellationToken cancellationToken) =>
        _context.Set<RegistrationAttempt>().CountAsync(a => a.NationalIdHash == nationalIdHash && a.CreatedAt >= since, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountAttemptsFromIpSinceAsync(string ipAddress, DateTimeOffset since, CancellationToken cancellationToken) =>
        _context.Set<RegistrationAttempt>().CountAsync(a => a.IpAddress == ipAddress && a.CreatedAt >= since, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountCodeRequestsSinceAsync(string nationalIdHash, DateTimeOffset since, CancellationToken cancellationToken) =>
        _context.Set<RegistrationCodeRequest>().CountAsync(r => r.NationalIdHash == nationalIdHash && r.RequestedAt >= since, cancellationToken);

    /// <inheritdoc />
    public async Task<RegistrationCandidate?> FindCandidateAsync(string nationalId, CancellationToken cancellationToken)
    {
        var person = await _context.Set<Person>().AsNoTracking()
            .SingleOrDefaultAsync(p => p.NationalId == nationalId, cancellationToken).ConfigureAwait(false);

        return person is null ? null : await ToCandidateAsync(person, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RegistrationCandidate?> FindCandidateAsync(long personId, CancellationToken cancellationToken)
    {
        var person = await _context.Set<Person>().AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == personId, cancellationToken).ConfigureAwait(false);

        return person is null ? null : await ToCandidateAsync(person, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<RegistrationAttempt?> FindAttemptAsync(Guid publicId, CancellationToken cancellationToken) =>
        _context.Set<RegistrationAttempt>().SingleOrDefaultAsync(a => a.PublicId == publicId, cancellationToken);

    /// <inheritdoc />
    public void Add(RegistrationAttempt attempt) => _context.Add(attempt);

    /// <inheritdoc />
    public void Add(RegistrationCodeRequest request) => _context.Add(request);

    /// <inheritdoc />
    public void Add(UserAccount account) => _context.Add(account);

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { ConstraintName: SingleAccountConstraint })
        {
            throw new ConflictException("Adınıza açılmış bir hesap zaten var. Parolanızı unuttuysanız parola sıfırlamayı kullanın.");
        }
    }

    private async Task<RegistrationCandidate> ToCandidateAsync(Person person, CancellationToken cancellationToken)
    {
        var hasActiveEmployment = await _context.Set<Employment>()
            .AnyAsync(e => e.PersonId == person.Id && e.IsActive, cancellationToken).ConfigureAwait(false);
        var account = await _context.Set<UserAccount>().AsNoTracking()
            .SingleOrDefaultAsync(a => a.PersonId == person.Id, cancellationToken).ConfigureAwait(false);

        return new RegistrationCandidate(person, hasActiveEmployment, account);
    }
}
