using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class RoomTagSelectionConfiguration : IEntityTypeConfiguration<RoomTagSelection>
{
    public void Configure(EntityTypeBuilder<RoomTagSelection> builder)
    {
        builder.ToTable("room_tag_selections");

        builder.HasKey(rts => new { rts.RoomId, rts.TagId });

        builder.Property(rts => rts.RoomId)
            .HasColumnName("room_id");

        builder.Property(rts => rts.TagId)
            .HasColumnName("tag_id");

        builder.Property(rts => rts.Weight)
            .HasColumnName("weight")
            .HasDefaultValue(1);

        // Relationships
        builder.HasOne(rts => rts.Room)
            .WithMany(r => r.TagSelections)
            .HasForeignKey(rts => rts.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rts => rts.Tag)
            .WithMany()
            .HasForeignKey(rts => rts.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
