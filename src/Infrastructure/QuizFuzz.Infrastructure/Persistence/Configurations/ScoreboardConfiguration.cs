using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class ScoreboardConfiguration : IEntityTypeConfiguration<Scoreboard>
{
    public void Configure(EntityTypeBuilder<Scoreboard> builder)
    {
        builder.ToTable("scoreboards");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.SessionId)
            .HasColumnName("session_id")
            .IsRequired();

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.ScoreTotal)
            .HasColumnName("score_total")
            .HasDefaultValue(0);

        builder.Property(s => s.CorrectCount)
            .HasColumnName("correct_count")
            .HasDefaultValue(0);

        builder.Property(s => s.UniqueCorrectCount)
            .HasColumnName("unique_correct_count")
            .HasDefaultValue(0);

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => new { s.SessionId, s.UserId })
            .IsUnique()
            .HasDatabaseName("ix_scoreboards_session_user");

        builder.HasIndex(s => new { s.SessionId, s.ScoreTotal })
            .HasDatabaseName("ix_scoreboards_session_score");
    }
}
