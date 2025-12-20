using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class HintConfiguration : IEntityTypeConfiguration<Hint>
{
    public void Configure(EntityTypeBuilder<Hint> builder)
    {
        builder.ToTable("hints");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(h => h.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.Property(h => h.OrderIndex)
            .HasColumnName("order_index")
            .IsRequired();

        builder.Property(h => h.HintText)
            .HasColumnName("hint_text");

        builder.Property(h => h.MediaAssetId)
            .HasColumnName("media_asset_id");

        builder.Property(h => h.RevealTimeSec)
            .HasColumnName("reveal_time_sec")
            .IsRequired();

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(h => h.MediaAsset)
            .WithMany()
            .HasForeignKey(h => h.MediaAssetId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(h => new { h.QuestionId, h.OrderIndex })
            .IsUnique()
            .HasDatabaseName("ix_hints_question_order");
    }
}
