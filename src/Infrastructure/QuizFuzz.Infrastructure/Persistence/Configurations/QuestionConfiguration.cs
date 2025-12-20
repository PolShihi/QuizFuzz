using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

/// <summary>
/// Конфигурация сущности Question
/// </summary>
public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(q => q.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(q => q.Title)
            .HasColumnName("title")
            .HasMaxLength(500);

        builder.Property(q => q.PromptText)
            .HasColumnName("prompt_text")
            .IsRequired();

        builder.Property(q => q.Difficulty)
            .HasColumnName("difficulty")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(q => q.LanguageCode)
            .HasColumnName("language_code")
            .HasMaxLength(5)
            .HasDefaultValue("ru");

        builder.Property(q => q.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(QuestionStatus.Draft);

        builder.Property(q => q.AuthorUserId)
            .HasColumnName("author_user_id");

        builder.Property(q => q.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(q => q.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(q => q.Author)
            .WithMany(u => u.Questions)
            .HasForeignKey(q => q.AuthorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(q => q.Answers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Hints)
            .WithOne(h => h.Question)
            .HasForeignKey(h => h.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.MediaAssets)
            .WithOne(m => m.Question)
            .HasForeignKey(m => m.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(q => new { q.Status, q.Type })
            .HasDatabaseName("ix_questions_status_type");

        builder.HasIndex(q => q.AuthorUserId)
            .HasDatabaseName("ix_questions_author");

        builder.HasIndex(q => q.CreatedAt)
            .HasDatabaseName("ix_questions_created_at");
    }
}
