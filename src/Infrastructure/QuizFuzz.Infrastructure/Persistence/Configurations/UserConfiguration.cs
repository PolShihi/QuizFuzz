using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

/// <summary>
/// Конфигурация сущности User
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(u => u.IsBanned)
            .HasColumnName("is_banned")
            .HasDefaultValue(false);

        builder.Property(u => u.BannedUntil)
            .HasColumnName("banned_until");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Email value object
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();
            
            email.HasIndex(e => e.Value)
                .IsUnique()
                .HasDatabaseName("ix_users_email");
        });

        // Roles are stored in a single string column and backed by the private _roles field.
        // The ValueComparer is important: without it EF Core does not reliably detect
        // in-place changes inside the role collection, so role updates can appear to
        // succeed but are not written to the database.
        var rolesComparer = new ValueComparer<IReadOnlyCollection<UserRole>>(
            (left, right) => (left ?? Array.Empty<UserRole>()).SequenceEqual(right ?? Array.Empty<UserRole>()),
            roles => roles == null
                ? 0
                : roles.Aggregate(0, (hash, role) => HashCode.Combine(hash, role.GetHashCode())),
            roles => roles == null ? Array.Empty<UserRole>() : roles.ToList());

        builder.Property(u => u.Roles)
            .HasField("_roles")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("roles")
            .HasConversion(
                roles => string.Join(',', roles.Select(role => role.ToString())),
                value => value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(role => Enum.Parse<UserRole>(role))
                    .ToList())
            .Metadata.SetValueComparer(rolesComparer);

        // Indexes
        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("ix_users_username");

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("ix_users_created_at");

        builder.HasIndex(u => u.IsBanned)
            .HasDatabaseName("ix_users_is_banned")
            .HasFilter("is_banned = true");
    }
}
