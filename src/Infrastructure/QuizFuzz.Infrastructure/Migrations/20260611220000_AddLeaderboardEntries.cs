using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizFuzz.Infrastructure.Migrations
{
    public partial class AddLeaderboardEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "leaderboard_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    metric = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    total_score = table.Column<int>(type: "integer", nullable: false),
                    games_played = table.Column<int>(type: "integer", nullable: false),
                    games_won = table.Column<int>(type: "integer", nullable: false),
                    answered_rounds = table.Column<int>(type: "integer", nullable: false),
                    correct_rounds = table.Column<int>(type: "integer", nullable: false),
                    accuracy = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leaderboard_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_leaderboard_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_entries_period_metric_rank",
                table: "leaderboard_entries",
                columns: new[] { "period", "metric", "rank" });

            migrationBuilder.CreateIndex(
                name: "ux_leaderboard_entries_period_metric_user",
                table: "leaderboard_entries",
                columns: new[] { "period", "metric", "user_id" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "leaderboard_entries");
        }
    }
}
