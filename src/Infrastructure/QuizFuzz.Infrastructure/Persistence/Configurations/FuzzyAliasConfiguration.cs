using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class FuzzyAliasConfiguration : IEntityTypeConfiguration<FuzzyAlias>
{
    public void Configure(EntityTypeBuilder<FuzzyAlias> builder)
    {
        builder.ToTable("fuzzy_aliases");

        builder.HasKey(fa => fa.Id);

        builder.Property(fa => fa.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(fa => fa.QuestionAnswerId)
            .HasColumnName("question_answer_id")
            .IsRequired();

        builder.Property(fa => fa.AliasText)
            .HasColumnName("alias_text")
            .IsRequired();

        builder.Property(fa => fa.NormalizedAlias)
            .HasColumnName("normalized_alias")
            .IsRequired();

        builder.Property(fa => fa.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(fa => fa.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(fa => fa.QuestionAnswerId)
            .HasDatabaseName("ix_fuzzy_aliases_question_answer");

        builder.HasIndex(fa => fa.NormalizedAlias)
            .HasDatabaseName("ix_fuzzy_aliases_normalized");

        builder.HasIndex(fa => new { fa.QuestionAnswerId, fa.NormalizedAlias })
            .IsUnique()
            .HasDatabaseName("ix_fuzzy_aliases_answer_normalized");
    }
}
