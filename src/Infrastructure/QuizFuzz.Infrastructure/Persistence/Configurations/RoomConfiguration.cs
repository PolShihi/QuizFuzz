using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Visibility)
            .HasColumnName("visibility")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.AccessCodeHash)
            .HasColumnName("access_code_hash")
            .HasMaxLength(255);

        builder.Property(r => r.MaxPlayers)
            .HasColumnName("max_players")
            .HasDefaultValue(10);

        builder.Property(r => r.VictoryConditionType)
            .HasColumnName("victory_condition_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(VictoryConditionType.Points);

        builder.Property(r => r.VictoryValue)
            .HasColumnName("victory_value")
            .IsRequired();

        builder.Property(r => r.TagSelectionMode)
            .HasColumnName("tag_selection_mode")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TagSelectionMode.Any);

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RoomStatus.Lobby);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(r => r.Owner)
            .WithMany()
            .HasForeignKey(r => r.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Sessions)
            .WithOne(s => s.Room)
            .HasForeignKey(s => s.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Invitations)
            .WithOne(i => i.Room)
            .HasForeignKey(i => i.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(r => new { r.Status, r.Visibility })
            .HasDatabaseName("ix_rooms_status_visibility");

        builder.HasIndex(r => r.OwnerUserId)
            .HasDatabaseName("ix_rooms_owner");

        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("ix_rooms_created_at");
    }
}
