using Dsg.Hrms.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>Oturum tablosunun eslemesi (SYG-KMLK-037…043).</summary>
public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_session", UserAccountConfiguration.IdentitySchema, table =>
            table.HasCheckConstraint("ck_user_session_ended", "(ended_at IS NULL) = (end_reason IS NULL)"));

        builder.HasKey(session => session.Id);
        builder.HasIndex(session => session.PublicId).IsUnique();

        // Tek aktif oturum ve yeniden kullanimda "tum oturumlari kapat" sorgusu.
        builder.HasIndex(session => session.UserAccountId).HasFilter("ended_at IS NULL");

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(session => session.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Yenileme jetonu tablosunun eslemesi.</summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("refresh_token", UserAccountConfiguration.IdentitySchema, table =>
            // SHA-256 ozeti (Base64, 44 karakter): jetonun kendisi yazilamaz.
            table.HasCheckConstraint("ck_refresh_token_hash_length", "length(token_hash) = 44"));

        builder.HasKey(token => token.Id);
        builder.Property(token => token.TokenHash).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();

        builder.HasOne<UserSession>()
            .WithMany()
            .HasForeignKey(token => token.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Hatali giris sayaci tablosunun eslemesi (SYG-KMLK-033).</summary>
public sealed class LoginThrottleConfiguration : IEntityTypeConfiguration<LoginThrottle>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LoginThrottle> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("login_throttle", UserAccountConfiguration.IdentitySchema, table =>
            table.HasCheckConstraint("ck_login_throttle_email_hash_length", "length(email_hash) = 44"));

        builder.HasKey(throttle => throttle.Id);
        builder.Property(throttle => throttle.EmailHash).IsRequired();
        builder.HasIndex(throttle => throttle.EmailHash).IsUnique();
    }
}

/// <summary>Bekleyen iki adimli giris tablosunun eslemesi (SYG-KMLK-034).</summary>
public sealed class LoginChallengeConfiguration : IEntityTypeConfiguration<LoginChallenge>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LoginChallenge> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("login_challenge", UserAccountConfiguration.IdentitySchema);

        builder.HasKey(challenge => challenge.Id);
        builder.HasIndex(challenge => challenge.PublicId).IsUnique();

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(challenge => challenge.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
