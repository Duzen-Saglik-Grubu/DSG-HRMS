using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>
/// Kullanicinin personel bakisi (ADR-0019 §1): hesabin kisi kaydi ve izinleri.
/// </summary>
/// <remarks>
/// Istek basina bir kez okunur ve saklanir. Izinler eylem yetkisiyle ayni kaynaktan gelir;
/// erisim jetonunda tasinmaz.
/// </remarks>
public sealed class CurrentUserScope(HrmsDbContext context, ICurrentUser currentUser, AccessControlService access) : ICurrentUserScope
{
    private PersonnelViewer? _viewer;

    /// <inheritdoc />
    public async Task<PersonnelViewer> GetAsync(CancellationToken cancellationToken)
    {
        if (_viewer is not null)
        {
            return _viewer;
        }

        if (currentUser.UserId is not { } accountId)
        {
            return _viewer = PersonnelViewer.From(null, new HashSet<string>(StringComparer.Ordinal));
        }

        var personId = await context.Set<UserAccount>()
            .Where(account => account.Id == accountId)
            .Select(account => (long?)account.PersonId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var permissions = await access.GetPermissionsAsync(accountId, cancellationToken).ConfigureAwait(false);

        return _viewer = PersonnelViewer.From(personId, permissions);
    }
}
