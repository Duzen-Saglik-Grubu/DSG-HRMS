using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Identity.Accounts;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Identity;

/// <summary>Hesap islemlerinin veritabani tarafi (SYG-KMLK-073).</summary>
public sealed class AccountAdministrationStore : IAccountAdministrationStore
{
    private readonly HrmsDbContext _context;

    /// <summary>Yeni ornek olusturur.</summary>
    public AccountAdministrationStore(HrmsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PagedResult<AccountSummary>> SearchAsync(
        string? term,
        AccountSort sort,
        bool descending,
        PageRequest page,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var persons = _context.Set<Person>().AsNoTracking();

        if (term is not null)
        {
            if (term.All(char.IsAsciiDigit))
            {
                // Terim yalnizca rakamdir; LIKE icin kacis gerekmez.
                persons = persons.Where(p => p.Employments.Any(e => EF.Functions.Like(e.RegistryCode, term + "%")));
            }
            else
            {
                // ILIKE: buyuk/kucuk harf duyarsiz; veritabani Turkce dil ayariyla kurulur (ADR-0004).
                var pattern = $"%{EscapeLike(term)}%";
                persons = persons.Where(p =>
                    EF.Functions.ILike(p.FirstName, pattern, "\\")
                    || EF.Functions.ILike(p.LastName, pattern, "\\")
                    || EF.Functions.ILike(p.FirstName + " " + p.LastName, pattern, "\\"));
            }
        }

        var total = await persons.CountAsync(cancellationToken).ConfigureAwait(false);

        var ordered = (sort, descending) switch
        {
            (AccountSort.FirstName, false) => persons.OrderBy(p => p.FirstName).ThenBy(p => p.LastName),
            (AccountSort.FirstName, true) => persons.OrderByDescending(p => p.FirstName).ThenByDescending(p => p.LastName),
            (_, true) => persons.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName),
            _ => persons.OrderBy(p => p.LastName).ThenBy(p => p.FirstName),
        };

        var rows = await ordered
            .ThenBy(p => p.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(p => new { p.Id, p.PublicId, p.FirstName, p.LastName })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ids = rows.Select(r => r.Id).ToList();

        var employments = await (
            from employment in _context.Set<Employment>().AsNoTracking()
            join company in _context.Set<Company>().AsNoTracking() on employment.CompanyId equals company.Id
            where ids.Contains(employment.PersonId)
            orderby employment.IsActive descending, employment.RegistryCode
            select new { employment.PersonId, employment.RegistryCode, CompanyName = company.Name, employment.IsActive })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var accounts = await _context.Set<UserAccount>().AsNoTracking()
            .Where(a => ids.Contains(a.PersonId))
            .Select(a => new { a.PersonId, a.Status, a.StatusReason, a.LockedUntil })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Select(row =>
        {
            var account = accounts.SingleOrDefault(a => a.PersonId == row.Id);
            var state = account is null ? AccountState.None
                : account.Status == AccountStatus.Passive ? AccountState.Passive
                : account.LockedUntil > now ? AccountState.Locked
                : AccountState.Active;

            return new AccountSummary(
                row.PublicId,
                row.FirstName,
                row.LastName,
                [.. employments.Where(e => e.PersonId == row.Id).Select(e => new AccountEmployment(e.RegistryCode, e.CompanyName, e.IsActive))],
                state,
                account?.StatusReason);
        }).ToList();

        return new PagedResult<AccountSummary>(items, page.Page, page.PageSize, total);
    }

    /// <inheritdoc />
    public async Task<(Person Person, UserAccount? Account, bool HasActiveEmployment)?> FindAsync(Guid personId, CancellationToken cancellationToken)
    {
        var person = await _context.Set<Person>().SingleOrDefaultAsync(p => p.PublicId == personId, cancellationToken).ConfigureAwait(false);
        if (person is null)
        {
            return null;
        }

        var account = await _context.Set<UserAccount>().SingleOrDefaultAsync(a => a.PersonId == person.Id, cancellationToken).ConfigureAwait(false);
        var hasActiveEmployment = await _context.Set<Employment>().AnyAsync(e => e.PersonId == person.Id && e.IsActive, cancellationToken).ConfigureAwait(false);

        return (person, account, hasActiveEmployment);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
