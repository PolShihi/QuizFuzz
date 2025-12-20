using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class GameRoundConfiguration : IEntityTypeConfiguration<GameRound>
{
    public void Configure(EntityTypeBuilder<GameRound> builder)
    {
        builder.ToTable("game_rounds");

        builder.HasKey(gr => gr.Id);

        builder.Property(gr => gr.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(gr => gr.SessionId)
            .HasColumnName("session_id")
            .IsRequired();

        builder.Property(gr => gr.RoundIndex)
            .HasColumnName("round_index")
            .IsRequired();

        builder.Property(gr => gr.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.Property(gr => gr.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RoundStatus.Pending);

        builder.Property(gr => gr.StartedAt)
            .HasColumnName("started_at");

        builder.Property(gr => gr.EndedAt)
            .HasColumnName("ended_at");

        builder.Property(gr => gr.TimeLimitSec)
            .HasColumnName("time_limit_sec")
            .HasDefaultValue(60);

        builder.Property(gr => gr.WinnerUserId)
            .HasColumnName("winner_user_id");

        builder.Property(gr => gr.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(gr => gr.Question)
            .WithMany()
            .HasForeignKey(gr => gr.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(gr => gr.Winner)
            .WithMany()
            .HasForeignKey(gr => gr.WinnerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(gr => gr.Answers)
            .WithOne(a => a.Round)
            .HasForeignKey(a => a.RoundId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(gr => new { gr.SessionId, gr.RoundIndex })
            .IsUnique()
            .HasDatabaseName("ix_game_rounds_session_index");
    }
}
