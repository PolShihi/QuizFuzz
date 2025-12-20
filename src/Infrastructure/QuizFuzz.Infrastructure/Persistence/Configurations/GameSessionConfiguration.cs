using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Infrastructure.Persistence.Configurations;

public class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> builder)
    {
        builder.ToTable("game_sessions");

        builder.HasKey(gs => gs.Id);

        builder.Property(gs => gs.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(gs => gs.RoomId)
            .HasColumnName("room_id")
            .IsRequired();

        builder.Property(gs => gs.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(GameSessionStatus.Pending);

        builder.Property(gs => gs.StartedAt)
            .HasColumnName("started_at");

        builder.Property(gs => gs.EndedAt)
            .HasColumnName("ended_at");

        builder.Property(gs => gs.TotalRoundsPlanned)
            .HasColumnName("total_rounds_planned")
            .IsRequired();

        builder.Property(gs => gs.TotalRoundsPlayed)
            .HasColumnName("total_rounds_played")
            .HasDefaultValue(0);

        builder.Property(gs => gs.CurrentRoundId)
            .HasColumnName("current_round_id");

        builder.Property(gs => gs.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasMany(gs => gs.Rounds)
            .WithOne(r => r.Session)
            .HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(gs => gs.Players)
            .WithOne(p => p.Session)
            .HasForeignKey(p => p.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(gs => gs.Scoreboards)
            .WithOne(s => s.Session)
            .HasForeignKey(s => s.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(gs => gs.RoomId)
            .HasDatabaseName("ix_game_sessions_room");

        builder.HasIndex(gs => gs.Status)
            .HasDatabaseName("ix_game_sessions_status");
    }
}
