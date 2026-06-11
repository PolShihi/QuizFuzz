using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class EmailVerificationCodeConfiguration : IEntityTypeConfiguration<EmailVerificationCode>
{
    public void Configure(EntityTypeBuilder<EmailVerificationCode> builder)
    {
        builder.ToTable("email_verification_codes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.Username)
            .HasColumnName("username")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.CodeHash)
            .HasColumnName("code_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(x => x.ConfirmedAt)
            .HasColumnName("confirmed_at");

        builder.Property(x => x.AttemptsCount)
            .HasColumnName("attempts_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.ResendCount)
            .HasColumnName("resend_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.LastSentAt)
            .HasColumnName("last_sent_at")
            .IsRequired();

        builder.Property(x => x.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasMaxLength(64);

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(512);

        builder.HasIndex(x => x.Email)
            .HasDatabaseName("ix_email_verification_codes_email");

        builder.HasIndex(x => x.Username)
            .HasDatabaseName("ix_email_verification_codes_username");

        builder.HasIndex(x => new { x.Email, x.ConfirmedAt, x.ExpiresAt })
            .HasDatabaseName("ix_email_verification_codes_email_active_lookup");
    }
}
