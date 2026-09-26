using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Personnel.Sync;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>
/// <see cref="IPersonnelSyncStore"/> uygulamasi (PostgreSQL).
/// </summary>
/// <remarks>
/// <para>
/// <b>Es zamanli calisma kilidi (SYG-KMLK-004):</b> <c>pg_try_advisory_xact_lock</c>.
/// Kilit veritabani isleminin omrune baglidir: islem bittiginde — uygulama cokse
/// bile — kendiliginden birakilir. Uygulamanin birden fazla ornegi calissa da ayni
/// anda tek senkronizasyon yapilir.
/// </para>
/// <para>
/// <b>Yeniden deneme bu oturumda kapalidir.</b> HRMS baglami gecici hatalarda yeniden
/// deneme ile yapilandirilmistir; bu strateji, birden fazla cagriya yayilan
/// kullanici islemleriyle birlikte kullanilamaz. Gecici bir hata calismayi
/// basarisiz kilar ve bir sonraki periyotta (15 dk) yeniden denenir; yari
/// uygulanmis bir fark riski yerine bu tercih edildi.
/// </para>
/// </remarks>
public sealed class PersonnelSyncStore : IPersonnelSyncStore, IAsyncDisposable
{
    /// <summary>
    /// Danisma kilidinin anahtari. Uygulama genelinde bu amaca ayrilmistir; baska bir
    /// kilit ayni degeri KULLANMAMALIDIR.
    /// </summary>
    public const long LockKey = 0x4852_4D53_5359_4E43; // "HRMSSYNC"

    private readonly DbContextOptions<HrmsDbContext> _options;
    private readonly IDbContextFactory<HrmsDbContext> _factory;
    private HrmsDbContext? _runContext;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonnelSyncStore(DbContextOptions<HrmsDbContext> options, IDbContextFactory<HrmsDbContext> factory)
    {
        _options = options;
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task<IPersonnelSyncSession?> TryOpenSessionAsync(CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>(_options)
            .UseNpgsql(npgsql => npgsql.ExecutionStrategy(dependencies => new NonRetryingExecutionStrategy(dependencies)))
            .Options;

        HrmsDbContext? context = null;
        IDbContextTransaction? transaction = null;

        try
        {
#pragma warning disable CA2000 // Sahiplik Session'a devredilir; diger tum yollarda finally kapatir. Cozumleyici async finally'yi izleyemiyor.
            context = new HrmsDbContext(options);
#pragma warning restore CA2000
            transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var acquired = await context.Database
                .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({LockKey}) AS \"Value\"")
                .SingleAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!acquired)
            {
                return null;
            }

            // Sahiplik oturuma gecer; asagidaki finally blogu artik kapatmaz.
            var session = new Session(context, transaction);
            context = null;
            transaction = null;
            return session;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }

            if (context is not null)
            {
                await context.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task SaveRunAsync(PersonnelSyncRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);

        // Calisma kaydi, oturumdan AYRI ve calisma boyunca ACIK kalan bir baglamla
        // yazilir: ilk cagri ekler, sonraki cagri ayni izlenen nesneyi gunceller.
        if (_runContext is null)
        {
            _runContext = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            _runContext.Set<PersonnelSyncRun>().Add(run);
        }

        await _runContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_runContext is not null)
        {
            await _runContext.DisposeAsync().ConfigureAwait(false);
            _runContext = null;
        }
    }

    private sealed class Session(HrmsDbContext context, IDbContextTransaction transaction) : IPersonnelSyncSession
    {
        public async Task<PersonnelSnapshot> LoadAsync(CancellationToken cancellationToken)
        {
            // Siralama onemlidir: firma ve kisiler once izlemeye alinir, istihdamlar
            // yuklendiginde gezinme ozellikleri bunlara baglanir.
            var companies = await context.Set<Company>().ToListAsync(cancellationToken).ConfigureAwait(false);
            var persons = await context.Set<Person>().ToListAsync(cancellationToken).ConfigureAwait(false);
            var employments = await context.Set<Employment>().ToListAsync(cancellationToken).ConfigureAwait(false);
            var accounts = await context.Set<UserAccount>().ToListAsync(cancellationToken).ConfigureAwait(false);

            return new PersonnelSnapshot(companies, persons, employments) { Accounts = accounts };
        }

        public void Add(IEnumerable<Company> companies, IEnumerable<Person> persons, IEnumerable<Employment> employments)
        {
            context.Set<Company>().AddRange(companies);
            context.Set<Person>().AddRange(persons);
            context.Set<Employment>().AddRange(employments);
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException ex)
            {
                throw new SyncPersistenceException("Senkronizasyon sonucu kaydedilemedi.", ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Onaylanmamis islem burada geri alinir; kilit islemle birlikte birakilir.
            await transaction.DisposeAsync().ConfigureAwait(false);
            await context.DisposeAsync().ConfigureAwait(false);
        }
    }
}
