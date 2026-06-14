using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizFuzz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundTimeLimitToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "round_time_limit_sec",
                table: "rooms",
                type: "integer",
                nullable: false,
                defaultValue: 60);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "round_time_limit_sec",
                table: "rooms");
        }
    }
}
