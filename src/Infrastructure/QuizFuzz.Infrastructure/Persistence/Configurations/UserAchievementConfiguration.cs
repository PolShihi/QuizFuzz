using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("user_achievements");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(a => a.AchievementCode)
            .HasColumnName("achievement_code")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(a => a.UnlockedAt)
            .HasColumnName("unlocked_at")
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("ix_user_achievements_user");

        builder.HasIndex(a => new { a.UserId, a.AchievementCode })
            .IsUnique()
            .HasDatabaseName("ix_user_achievements_user_code");

        builder.HasIndex(a => a.UnlockedAt)
            .HasDatabaseName("ix_user_achievements_unlocked_at");
    }
}
