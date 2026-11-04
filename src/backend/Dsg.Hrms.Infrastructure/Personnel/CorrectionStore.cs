using System.Globalization;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Personnel.Corrections;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Reference;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Data.Configurations;
using Dsg.Hrms.Infrastructure.Personnel.Protection;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>
/// Duzeltme talebi deposu (SYG-PERS-045…049). Onerilen kan grubu yazilirken sifrelenir, okunurken
/// cozulur; uygulama katmani duz kodu, veritabani yalnizca sifreli degeri gorur (ADR-0020 §1).
/// </summary>
public sealed class CorrectionStore(HrmsDbContext context, PersonalDataProtector protector) : ICorrectionStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CorrectionField>> ListOpenFieldsAsync(long personId, CancellationToken cancellationToken) =>
        await context.Set<CorrectionRequest>()
            .AsNoTracking()
            .Where(r => r.PersonId == personId && r.Status == CorrectionStatus.New)
            .Select(r => r.Field)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<long>> ListHrRecipientsAsync(CancellationToken cancellationToken) =>
        await (from userRole in context.Set<UserRole>()
               join role in context.Set<Role>() on userRole.RoleId equals role.Id
               join account in context.Set<UserAccount>() on userRole.UserAccountId equals account.Id
               where role.Code == Role.HrIdentityOperationsCode && account.Status == AccountStatus.Active
               select account.Id)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CorrectionOption>> ListBloodGroupsAsync(CancellationToken cancellationToken) =>
        await context.Set<ReferenceDefinition>()
            .AsNoTracking()
            .Where(d => d.Type == ReferenceType.BloodGroup && d.IsActive)
            .OrderBy(d => d.SortOrder)
            .Select(d => new CorrectionOption(d.Code, d.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void Add(CorrectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Field == CorrectionField.BloodGroup)
        {
            request.ProtectValue(code => protector.Protect(code, ProtectedField.CorrectionValue(request.PublicId)));
        }

        context.Add(request);
    }

    /// <inheritdoc />
    public void Add(IEnumerable<InboxItem> items) => context.AddRange(items);

    /// <inheritdoc />
    public Task<CorrectionRequest?> FindAsync(Guid requestId, CancellationToken cancellationToken) =>
        context.Set<CorrectionRequest>().SingleOrDefaultAsync(r => r.PublicId == requestId, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResult<CorrectionRecord>> ListAsync(CorrectionStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var requests = context.Set<CorrectionRequest>().AsNoTracking();
        if (status is { } wanted)
        {
            requests = requests.Where(r => r.Status == wanted);
        }

        var total = await requests.CountAsync(cancellationToken).ConfigureAwait(false);
        var selected = requests
            .OrderByDescending(r => r.SubmittedAt)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        var rows = await Rows(selected).ToListAsync(cancellationToken).ConfigureAwait(false);
        rows = [.. rows.OrderByDescending(row => row.Request.SubmittedAt).ThenByDescending(row => row.Request.Id)];

        var bloodGroups = await BloodGroupNamesAsync(cancellationToken).ConfigureAwait(false);
        return new PagedResult<CorrectionRecord>([.. rows.Select(row => ToRecord(row, bloodGroups))], page, pageSize, total);
    }

    /// <inheritdoc />
    public async Task<CorrectionRecord?> GetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var row = await Rows(context.Set<CorrectionRequest>().AsNoTracking().Where(r => r.PublicId == requestId))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return row is null ? null : ToRecord(row, await BloodGroupNamesAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: CorrectionRequestConfiguration.OneOpenPerFieldIndex,
        })
        {
            throw new ConflictException("Bu alan için gönderdiğiniz bir talep zaten İK'da bekliyor.");
        }
    }

    // Suzme ve siralama talep sorgusunda yapilir; kayit turune yansitilmis sorgu EF tarafindan cevrilemez.
    private IQueryable<Row> Rows(IQueryable<CorrectionRequest> requests) =>
        from request in requests
        join person in context.Set<Person>().AsNoTracking() on request.PersonId equals person.Id
        select new Row(request, person.PublicId, person.FirstName, person.LastName);

    private Task<Dictionary<string, string>> BloodGroupNamesAsync(CancellationToken cancellationToken) =>
        context.Set<ReferenceDefinition>()
            .AsNoTracking()
            .Where(d => d.Type == ReferenceType.BloodGroup)
            .ToDictionaryAsync(d => d.Code, d => d.Name, StringComparer.Ordinal, cancellationToken);

    private CorrectionRecord ToRecord(Row row, IReadOnlyDictionary<string, string> bloodGroups) =>
        new(
            row.Request.PublicId,
            row.PersonPublicId,
            row.FirstName,
            row.LastName,
            row.Request.Field,
            ProposedValue(row.Request, bloodGroups),
            row.Request.Status,
            row.Request.SubmittedAt,
            row.Request.CompletedAt);

    private string? ProposedValue(CorrectionRequest request, IReadOnlyDictionary<string, string> bloodGroups)
    {
        if (request.ProposedValue is not { } stored || request.Field != CorrectionField.BloodGroup)
        {
            return request.ProposedValue;
        }

        // Cozulemeyen deger (bilinmeyen anahtar, kopyalanmis satir) gosterilmez; ayrinti disari verilmez.
        var code = protector.TryUnprotect(stored, ProtectedField.CorrectionValue(request.PublicId));
        return code is null ? null : bloodGroups.GetValueOrDefault(code, code.ToString(CultureInfo.InvariantCulture));
    }

    private sealed record Row(CorrectionRequest Request, Guid PersonPublicId, string FirstName, string LastName);
}
