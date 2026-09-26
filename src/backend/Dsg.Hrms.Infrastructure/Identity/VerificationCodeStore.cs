using Dsg.Hrms.Application.Identity.Verification;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Identity;

/// <summary>
/// <see cref="IVerificationCodeStore"/> uygulamasi (PostgreSQL).
/// </summary>
/// <remarks>
/// Eszamanlilik, her varliga uygulanan <c>xmin</c> satir surumuyle saglanir (ADR-0004 §4):
/// ayni kodu ayni anda dogrulayan iki istekten ikincisinin kaydi reddedilir
/// (SYG-KMLK-027).
/// </remarks>
public sealed class VerificationCodeStore : IVerificationCodeStore
{
    private readonly HrmsDbContext _context;

    /// <summary>Yeni ornek olusturur.</summary>
    public VerificationCodeStore(HrmsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<int> CountIssuedSinceAsync(long personId, DateTimeOffset since, CancellationToken cancellationToken) =>
        _context.Set<VerificationCode>()
            .CountAsync(c => c.PersonId == personId && c.CreatedAt >= since, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<VerificationCode>> GetOpenAsync(
        long personId,
        VerificationPurpose purpose,
        CancellationToken cancellationToken) =>
        await _context.Set<VerificationCode>()
            .Where(c => c.PersonId == personId && c.Purpose == purpose && c.Status == VerificationCodeStatus.Issued)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<VerificationCode?> FindAsync(Guid publicId, CancellationToken cancellationToken) =>
        _context.Set<VerificationCode>().SingleOrDefaultAsync(c => c.PublicId == publicId, cancellationToken);

    /// <inheritdoc />
    public void Add(VerificationCode code) => _context.Add(code);

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new VerificationConflictException("Dogrulama kodu eszamanli bir istekle degistirildi.", ex);
        }
    }
}
