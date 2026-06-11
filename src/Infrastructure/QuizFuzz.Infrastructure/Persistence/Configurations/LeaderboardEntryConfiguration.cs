using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class LeaderboardEntryConfiguration : IEntityTypeConfiguration<LeaderboardEntry>
{
    public void Configure(EntityTypeBuilder<LeaderboardEntry> builder)
    {
        builder.ToTable("leaderboard_entries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Period)
            .HasColumnName("period")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Metric)
            .HasColumnName("metric")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.PeriodStart)
            .HasColumnName("period_start");

        builder.Property(x => x.PeriodEnd)
            .HasColumnName("period_end");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Rank)
            .HasColumnName("rank")
            .IsRequired();

        builder.Property(x => x.TotalScore)
            .HasColumnName("total_score")
            .IsRequired();

        builder.Property(x => x.GamesPlayed)
            .HasColumnName("games_played")
            .IsRequired();

        builder.Property(x => x.GamesWon)
            .HasColumnName("games_won")
            .IsRequired();

        builder.Property(x => x.AnsweredRounds)
            .HasColumnName("answered_rounds")
            .IsRequired();

        builder.Property(x => x.CorrectRounds)
            .HasColumnName("correct_rounds")
            .IsRequired();

        builder.Property(x => x.Accuracy)
            .HasColumnName("accuracy")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(x => x.CalculatedAt)
            .HasColumnName("calculated_at")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.Period, x.Metric, x.Rank })
            .HasDatabaseName("ix_leaderboard_entries_period_metric_rank");

        builder.HasIndex(x => new { x.Period, x.Metric, x.UserId })
            .IsUnique()
            .HasDatabaseName("ux_leaderboard_entries_period_metric_user");
    }
}
