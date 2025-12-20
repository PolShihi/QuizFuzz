using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuizFuzz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Users table
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_banned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    banned_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    roles = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_created_at",
                table: "users",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_users_is_banned",
                table: "users",
                column: "is_banned",
                filter: "is_banned = true");

            // Tags table
            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tags_name",
                table: "tags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tags_is_active",
                table: "tags",
                column: "is_active",
                filter: "is_active = true");

            // Questions table
            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    prompt_text = table.Column<string>(type: "text", nullable: false),
                    difficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    language_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "ru"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_questions_users_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_questions_status_type",
                table: "questions",
                columns: new[] { "status", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_questions_author",
                table: "questions",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_questions_created_at",
                table: "questions",
                column: "created_at");

            // Question Answers table
            migrationBuilder.CreateTable(
                name: "question_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_text = table.Column<string>(type: "text", nullable: false),
                    normalized_answer = table.Column<string>(type: "text", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    language_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "ru"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_question_answers_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_question_answers_question",
                table: "question_answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_question_answers_question_primary",
                table: "question_answers",
                columns: new[] { "question_id", "is_primary" },
                filter: "is_primary = true");

            migrationBuilder.CreateIndex(
                name: "ix_question_answers_question_normalized",
                table: "question_answers",
                columns: new[] { "question_id", "normalized_answer" },
                unique: true);

            // Fuzzy Aliases table
            migrationBuilder.CreateTable(
                name: "fuzzy_aliases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alias_text = table.Column<string>(type: "text", nullable: false),
                    normalized_alias = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fuzzy_aliases", x => x.id);
                    table.ForeignKey(
                        name: "FK_fuzzy_aliases_question_answers_question_answer_id",
                        column: x => x.question_answer_id,
                        principalTable: "question_answers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fuzzy_aliases_question_answer",
                table: "fuzzy_aliases",
                column: "question_answer_id");

            migrationBuilder.CreateIndex(
                name: "ix_fuzzy_aliases_normalized",
                table: "fuzzy_aliases",
                column: "normalized_alias");

            migrationBuilder.CreateIndex(
                name: "ix_fuzzy_aliases_answer_normalized",
                table: "fuzzy_aliases",
                columns: new[] { "question_answer_id", "normalized_alias" },
                unique: true);

            // Question Tags (many-to-many)
            migrationBuilder.CreateTable(
                name: "question_tags",
                columns: table => new
                {
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_tags", x => new { x.question_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_question_tags_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_question_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_question_tags_tag",
                table: "question_tags",
                column: "tag_id");

            // Hints table
            migrationBuilder.CreateTable(
                name: "hints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    hint_text = table.Column<string>(type: "text", nullable: true),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reveal_time_sec = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hints", x => x.id);
                    table.ForeignKey(
                        name: "FK_hints_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_hints_question_order",
                table: "hints",
                columns: new[] { "question_id", "order_index" },
                unique: true);

            // Media Assets table
            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    storage_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "S3"),
                    checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    duration_sec = table.Column<int>(type: "integer", nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    is_sensitive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.id);
                    table.ForeignKey(
                        name: "FK_media_assets_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_question",
                table: "media_assets",
                column: "question_id");

            // Add FK from hints to media_assets
            migrationBuilder.AddForeignKey(
                name: "FK_hints_media_assets_media_asset_id",
                table: "hints",
                column: "media_asset_id",
                principalTable: "media_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Rooms table
            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    access_code_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    max_players = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    victory_condition_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Points"),
                    victory_value = table.Column<int>(type: "integer", nullable: false),
                    tag_selection_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Any"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Lobby"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.id);
                    table.ForeignKey(
                        name: "FK_rooms_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rooms_status_visibility",
                table: "rooms",
                columns: new[] { "status", "visibility" });

            migrationBuilder.CreateIndex(
                name: "ix_rooms_owner",
                table: "rooms",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_rooms_created_at",
                table: "rooms",
                column: "created_at");

            // NOTE: Continuing in next part due to length...
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "fuzzy_aliases");
            migrationBuilder.DropTable(name: "question_tags");
            migrationBuilder.DropTable(name: "hints");
            migrationBuilder.DropTable(name: "media_assets");
            migrationBuilder.DropTable(name: "question_answers");
            migrationBuilder.DropTable(name: "questions");
            migrationBuilder.DropTable(name: "tags");
            migrationBuilder.DropTable(name: "users");
            // NOTE: More tables to drop...
        }
    }
}
