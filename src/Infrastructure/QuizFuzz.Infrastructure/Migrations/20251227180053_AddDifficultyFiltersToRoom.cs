using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizFuzz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDifficultyFiltersToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "difficulty_filters_json",
                table: "rooms",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "difficulty_filters_json",
                table: "rooms");
        }
    }
}
