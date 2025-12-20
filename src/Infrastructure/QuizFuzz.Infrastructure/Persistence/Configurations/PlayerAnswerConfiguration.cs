using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class PlayerAnswerConfiguration : IEntityTypeConfiguration<PlayerAnswer>
{
    public void Configure(EntityTypeBuilder<PlayerAnswer> builder)
    {
        builder.ToTable("player_answers");

        builder.HasKey(pa => pa.Id);

        builder.Property(pa => pa.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(pa => pa.RoundId)
            .HasColumnName("round_id")
            .IsRequired();

        builder.Property(pa => pa.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(pa => pa.AnswerText)
            .HasColumnName("answer_text")
            .IsRequired();

        builder.Property(pa => pa.AnswerTimeMs)
            .HasColumnName("answer_time_ms")
            .IsRequired();

        builder.Property(pa => pa.ClientTimestamp)
            .HasColumnName("client_timestamp");

        builder.Property(pa => pa.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(pa => pa.User)
            .WithMany()
            .HasForeignKey(pa => pa.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pa => pa.Evaluation)
            .WithOne(e => e.PlayerAnswer)
            .HasForeignKey<AnswerEvaluation>(e => e.PlayerAnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(pa => new { pa.RoundId, pa.UserId })
            .HasDatabaseName("ix_player_answers_round_user");

        builder.HasIndex(pa => new { pa.RoundId, pa.CreatedAt })
            .HasDatabaseName("ix_player_answers_round_created");
    }
}
