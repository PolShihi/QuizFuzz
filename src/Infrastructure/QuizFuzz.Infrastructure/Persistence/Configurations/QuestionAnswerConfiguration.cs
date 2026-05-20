using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

/// <summary>
/// Конфигурация сущности QuestionAnswer
/// </summary>
public class QuestionAnswerConfiguration : IEntityTypeConfiguration<QuestionAnswer>
{
    public void Configure(EntityTypeBuilder<QuestionAnswer> builder)
    {
        builder.ToTable("question_answers");

        builder.HasKey(qa => qa.Id);

        builder.Property(qa => qa.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(qa => qa.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.Property(qa => qa.AnswerText)
            .HasColumnName("answer_text")
            .IsRequired();

        builder.Property(qa => qa.NormalizedAnswer)
            .HasColumnName("normalized_answer")
            .IsRequired();

        builder.Property(qa => qa.IsPrimary)
            .HasColumnName("is_primary")
            .HasDefaultValue(false);

        builder.Property(qa => qa.LanguageCode)
            .HasColumnName("language_code")
            .HasMaxLength(5)
            .HasDefaultValue("ru");

        builder.Property(qa => qa.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(qa => qa.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Navigation(qa => qa.Aliases)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Relationships
        builder.HasMany(qa => qa.Aliases)
            .WithOne(a => a.QuestionAnswer)
            .HasForeignKey(a => a.QuestionAnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(qa => qa.QuestionId)
            .HasDatabaseName("ix_question_answers_question");

        builder.HasIndex(qa => new { qa.QuestionId, qa.IsPrimary })
            .HasDatabaseName("ix_question_answers_question_primary")
            .HasFilter("is_primary = true");

        builder.HasIndex(qa => new { qa.QuestionId, qa.NormalizedAnswer })
            .IsUnique()
            .HasDatabaseName("ix_question_answers_question_normalized");
    }
}
