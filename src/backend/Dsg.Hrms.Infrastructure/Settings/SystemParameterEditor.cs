using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Settings;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Settings;

/// <summary>
/// <see cref="ISystemParameterEditor"/> uygulamasi.
/// </summary>
/// <remarks>
/// Degisiklik, istegin baglamiyla kaydedilir: denetim izine degistiren kullanici ve
/// eski/yeni deger duser (SYG-KMLK-075). Sir parametrede deger yerine yalnizca
/// "degisti" gorunur (<c>KR-071</c>, ad tabanli maskeleme: <c>ProtectedValue</c>).
/// </remarks>
public sealed class SystemParameterEditor : ISystemParameterEditor
{
    private readonly HrmsDbContext _context;
    private readonly SystemParameters _parameters;
    private readonly ISecretProtector _protector;

    /// <summary>Yeni ornek olusturur.</summary>
    public SystemParameterEditor(HrmsDbContext context, SystemParameters parameters, ISecretProtector protector)
    {
        _context = context;
        _parameters = parameters;
        _protector = protector;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ParameterView>> ListAsync(CancellationToken cancellationToken)
    {
        // Liste onbellekten DEGIL, dogrudan veritabanindan okunur: yonetici az once
        // yaptigi degisikligi gormelidir.
        var rows = await SystemParameters.LoadRowsAsync(_context, cancellationToken).ConfigureAwait(false);

        return ParameterCatalog.All
            .Select(p =>
            {
                var resolved = _parameters.Resolve(p, rows, revealSecret: false);
                return new ParameterView(
                    p.Key,
                    p.Description,
                    p.Type,
                    p.Min,
                    p.Max,
                    p.IsSecret ? null : resolved.Value,
                    resolved.Source != ParameterSource.None,
                    resolved.Source);
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(string key, string value, CancellationToken cancellationToken)
    {
        var parameter = ParameterCatalog.Find(key) ?? throw new NotFoundException("Parametre bulunamadı.");

        var row = await _context.Set<SystemParameter>()
            .SingleOrDefaultAsync(p => p.Key == parameter.Key, cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            row = SystemParameter.Create(parameter.Key);
            _context.Add(row);
        }

        if (parameter.IsSecret)
        {
            // Sir KIRPILMAZ: parolanin basindaki/sonundaki bosluk parolanin parcasi olabilir.
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new BusinessRuleException("Değer boş olamaz.");
            }

            if (!_protector.IsConfigured)
            {
                throw new BusinessRuleException(
                    "Gizli değer kaydedilemiyor: şifreleme anahtarı (ParameterProtection:Key) tanımlı değil. Bilgi İşlem birimine başvurun.");
            }

            row.SetProtectedValue(_protector.Protect(value, parameter.Key));
        }
        else
        {
            var validation = parameter.Validate(value);
            if (!validation.IsValid)
            {
                throw new BusinessRuleException(validation.Error!);
            }

            if (!row.SetValue(validation.CanonicalValue!))
            {
                return;
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _parameters.Invalidate();
    }
}
