using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

/// <summary>
/// Конфигурация сущности ModerationAction для EF Core
/// </summary>
public class ModerationActionConfiguration : IEntityTypeConfiguration<ModerationAction>
{
    public void Configure(EntityTypeBuilder<ModerationAction> builder)
    {
        builder.ToTable("ModerationActions");

        builder.HasKey(ma => ma.Id);

        builder.Property(ma => ma.ActionType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(ma => ma.Comment)
            .HasMaxLength(2000);

        builder.Property(ma => ma.Reason)
            .HasMaxLength(1000);

        builder.Property(ma => ma.PreviousStatus)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(ma => ma.NewStatus)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(ma => ma.ActionDate)
            .IsRequired();

        // Relationships
        builder.HasOne(ma => ma.Question)
            .WithMany()
            .HasForeignKey(ma => ma.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ma => ma.Moderator)
            .WithMany()
            .HasForeignKey(ma => ma.ModeratorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(ma => ma.QuestionId);
        builder.HasIndex(ma => ma.ModeratorUserId);
        builder.HasIndex(ma => ma.ActionType);
        builder.HasIndex(ma => ma.ActionDate);
    }
}
