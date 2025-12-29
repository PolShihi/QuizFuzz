using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizFuzz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFuzzyMatchingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowFuzzyMatch",
                table: "question_answers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxEditDistance",
                table: "question_answers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinConfidence",
                table: "question_answers",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowFuzzyMatch",
                table: "question_answers");

            migrationBuilder.DropColumn(
                name: "MaxEditDistance",
                table: "question_answers");

            migrationBuilder.DropColumn(
                name: "MinConfidence",
                table: "question_answers");
        }
    }
}
