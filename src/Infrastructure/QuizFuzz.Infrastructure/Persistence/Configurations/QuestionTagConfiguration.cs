using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class QuestionTagConfiguration : IEntityTypeConfiguration<QuestionTag>
{
    public void Configure(EntityTypeBuilder<QuestionTag> builder)
    {
        builder.ToTable("question_tags");

        builder.HasKey(qt => new { qt.QuestionId, qt.TagId });

        builder.Property(qt => qt.QuestionId)
            .HasColumnName("question_id");

        builder.Property(qt => qt.TagId)
            .HasColumnName("tag_id");

        // Relationships
        builder.HasOne(qt => qt.Question)
            .WithMany(q => q.Tags)
            .HasForeignKey(qt => qt.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(qt => qt.Tag)
            .WithMany(t => t.QuestionTags)
            .HasForeignKey(qt => qt.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(qt => qt.TagId)
            .HasDatabaseName("ix_question_tags_tag");
    }
}
