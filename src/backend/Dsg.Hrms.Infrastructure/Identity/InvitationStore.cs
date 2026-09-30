using Dsg.Hrms.Application.Identity.Invitations;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Identity;

/// <summary>Davetlerin veritabani tarafi (SYG-KMLK-051…053).</summary>
public sealed class InvitationStore : IInvitationStore
{
    private readonly HrmsDbContext _context;

    /// <summary>Yeni ornek olusturur.</summary>
    public InvitationStore(HrmsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccountInvitation>> GetUsableAsync(long personId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await _context.Set<AccountInvitation>()
            .Where(i => i.PersonId == personId && i.UsedAt == null && i.RevokedAt == null && i.ExpiresAt > now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<AccountInvitation?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        _context.Set<AccountInvitation>().SingleOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

    /// <inheritdoc />
    public void Add(object entity) => _context.Add(entity);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}

/// <summary>Davet tablosunun eslemesi.</summary>
public sealed class AccountInvitationConfiguration : IEntityTypeConfiguration<AccountInvitation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AccountInvitation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("account_invitation", UserAccountConfiguration.IdentitySchema, table =>
        {
            // SHA-256 ozeti (Base64, 44 karakter). Jetonun kendisi yazilamaz.
            table.HasCheckConstraint("ck_account_invitation_token_hash_length", "length(token_hash) = 44");
            table.HasCheckConstraint("ck_account_invitation_reason", "btrim(reason) <> ''");
        });

        builder.HasKey(invitation => invitation.Id);
        builder.Property(invitation => invitation.TokenHash).IsRequired().HasMaxLength(44);
        builder.Property(invitation => invitation.Reason).IsRequired().HasMaxLength(AccountInvitation.MaxReasonLength);
        builder.HasIndex(invitation => invitation.TokenHash).IsUnique();
        builder.HasIndex(invitation => invitation.PersonId);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(invitation => invitation.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
    }
}
