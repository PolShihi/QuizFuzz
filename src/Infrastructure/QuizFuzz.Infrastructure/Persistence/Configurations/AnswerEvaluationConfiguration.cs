using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class AnswerEvaluationConfiguration : IEntityTypeConfiguration<AnswerEvaluation>
{
    public void Configure(EntityTypeBuilder<AnswerEvaluation> builder)
    {
        builder.ToTable("answer_evaluations");

        builder.HasKey(ae => ae.Id);

        builder.Property(ae => ae.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(ae => ae.PlayerAnswerId)
            .HasColumnName("player_answer_id")
            .IsRequired();

        builder.Property(ae => ae.IsCorrect)
            .HasColumnName("is_correct")
            .IsRequired();

        builder.Property(ae => ae.MatchStrategy)
            .HasColumnName("match_strategy")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ae => ae.ScoreAwarded)
            .HasColumnName("score_awarded")
            .HasDefaultValue(0);

        builder.Property(ae => ae.ConfidenceValue)
            .HasColumnName("confidence")
            .HasColumnType("decimal(3,2)")
            .IsRequired();

        builder.Property(ae => ae.NormalizedAnswer)
            .HasColumnName("normalized_answer")
            .IsRequired();

        builder.Property(ae => ae.MatchedQuestionAnswerId)
            .HasColumnName("matched_question_answer_id");

        builder.Property(ae => ae.MatchedAliasId)
            .HasColumnName("matched_alias_id");

        builder.Property(ae => ae.EvaluatedAt)
            .HasColumnName("evaluated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(ae => ae.MatchedQuestionAnswer)
            .WithMany()
            .HasForeignKey(ae => ae.MatchedQuestionAnswerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ae => ae.MatchedAlias)
            .WithMany()
            .HasForeignKey(ae => ae.MatchedAliasId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(ae => ae.PlayerAnswerId)
            .IsUnique()
            .HasDatabaseName("ix_answer_evaluations_player_answer");
    }
}
