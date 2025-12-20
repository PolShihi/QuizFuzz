using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets");

        builder.HasKey(ma => ma.Id);

        builder.Property(ma => ma.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(ma => ma.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.Property(ma => ma.MediaType)
            .HasColumnName("media_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ma => ma.Url)
            .HasColumnName("url")
            .IsRequired();

        builder.Property(ma => ma.StorageProvider)
            .HasColumnName("storage_provider")
            .HasMaxLength(50)
            .HasDefaultValue("S3");

        builder.Property(ma => ma.Checksum)
            .HasColumnName("checksum")
            .HasMaxLength(64);

        builder.Property(ma => ma.DurationSec)
            .HasColumnName("duration_sec");

        builder.Property(ma => ma.Width)
            .HasColumnName("width");

        builder.Property(ma => ma.Height)
            .HasColumnName("height");

        builder.Property(ma => ma.IsSensitive)
            .HasColumnName("is_sensitive")
            .HasDefaultValue(false);

        builder.Property(ma => ma.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(ma => ma.QuestionId)
            .HasDatabaseName("ix_media_assets_question");
    }
}
