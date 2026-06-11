using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizFuzz.Infrastructure.Migrations;

public partial class AddEmailVerificationCodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "email_verification_codes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                attempts_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                resend_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                last_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_email_verification_codes", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_email_verification_codes_email",
            table: "email_verification_codes",
            column: "email");

        migrationBuilder.CreateIndex(
            name: "ix_email_verification_codes_username",
            table: "email_verification_codes",
            column: "username");

        migrationBuilder.CreateIndex(
            name: "ix_email_verification_codes_email_active_lookup",
            table: "email_verification_codes",
            columns: new[] { "email", "confirmed_at", "expires_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "email_verification_codes");
    }
}
