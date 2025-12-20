using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class PlayerInRoomConfiguration : IEntityTypeConfiguration<PlayerInRoom>
{
    public void Configure(EntityTypeBuilder<PlayerInRoom> builder)
    {
        builder.ToTable("players_in_room");

        builder.HasKey(pir => pir.Id);

        builder.Property(pir => pir.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(pir => pir.SessionId)
            .HasColumnName("session_id")
            .IsRequired();

        builder.Property(pir => pir.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(pir => pir.JoinedAt)
            .HasColumnName("joined_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(pir => pir.LeftAt)
            .HasColumnName("left_at");

        builder.Property(pir => pir.IsOwnerSnapshot)
            .HasColumnName("is_owner_snapshot")
            .HasDefaultValue(false);

        builder.Property(pir => pir.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(pir => pir.User)
            .WithMany()
            .HasForeignKey(pir => pir.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(pir => pir.SessionId)
            .HasDatabaseName("ix_players_in_room_session");

        builder.HasIndex(pir => new { pir.SessionId, pir.UserId })
            .IsUnique()
            .HasDatabaseName("ix_players_in_room_session_user");
    }
}
