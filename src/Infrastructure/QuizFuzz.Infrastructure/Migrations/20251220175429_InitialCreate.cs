using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizFuzz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
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
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
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

            migrationBuilder.CreateTable(
                name: "game_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_rounds_planned = table.Column<int>(type: "integer", nullable: false),
                    total_rounds_played = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    current_round_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_game_sessions_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_invitations_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invitations_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_tag_selections",
                columns: table => new
                {
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_tag_selections", x => new { x.room_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_room_tag_selections_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_tag_selections_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                        name: "FK_hints_media_assets_media_asset_id",
                        column: x => x.media_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_hints_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "game_rounds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_index = table.Column<int>(type: "integer", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    time_limit_sec = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    winner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_rounds", x => x.id);
                    table.ForeignKey(
                        name: "FK_game_rounds_game_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "game_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_game_rounds_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_rounds_users_winner_user_id",
                        column: x => x.winner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "players_in_room",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_owner_snapshot = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players_in_room", x => x.id);
                    table.ForeignKey(
                        name: "FK_players_in_room_game_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "game_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_players_in_room_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scoreboards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score_total = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    correct_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    unique_correct_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scoreboards", x => x.id);
                    table.ForeignKey(
                        name: "FK_scoreboards_game_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "game_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scoreboards_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_text = table.Column<string>(type: "text", nullable: false),
                    answer_time_ms = table.Column<int>(type: "integer", nullable: false),
                    client_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_answers_game_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "game_rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_player_answers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "answer_evaluations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    match_strategy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    score_awarded = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    confidence = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    normalized_answer = table.Column<string>(type: "text", nullable: false),
                    matched_question_answer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    matched_alias_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evaluated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_answer_evaluations", x => x.id);
                    table.ForeignKey(
                        name: "FK_answer_evaluations_fuzzy_aliases_matched_alias_id",
                        column: x => x.matched_alias_id,
                        principalTable: "fuzzy_aliases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_answer_evaluations_player_answers_player_answer_id",
                        column: x => x.player_answer_id,
                        principalTable: "player_answers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_answer_evaluations_question_answers_matched_question_answer~",
                        column: x => x.matched_question_answer_id,
                        principalTable: "question_answers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_answer_evaluations_matched_alias_id",
                table: "answer_evaluations",
                column: "matched_alias_id");

            migrationBuilder.CreateIndex(
                name: "IX_answer_evaluations_matched_question_answer_id",
                table: "answer_evaluations",
                column: "matched_question_answer_id");

            migrationBuilder.CreateIndex(
                name: "ix_answer_evaluations_player_answer",
                table: "answer_evaluations",
                column: "player_answer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fuzzy_aliases_answer_normalized",
                table: "fuzzy_aliases",
                columns: new[] { "question_answer_id", "normalized_alias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fuzzy_aliases_normalized",
                table: "fuzzy_aliases",
                column: "normalized_alias");

            migrationBuilder.CreateIndex(
                name: "ix_fuzzy_aliases_question_answer",
                table: "fuzzy_aliases",
                column: "question_answer_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_rounds_question_id",
                table: "game_rounds",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_rounds_session_index",
                table: "game_rounds",
                columns: new[] { "session_id", "round_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_rounds_winner_user_id",
                table: "game_rounds",
                column: "winner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_sessions_room",
                table: "game_sessions",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_sessions_status",
                table: "game_sessions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_hints_media_asset_id",
                table: "hints",
                column: "media_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_hints_question_order",
                table: "hints",
                columns: new[] { "question_id", "order_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitations_code",
                table: "invitations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invitations_created_by_user_id",
                table: "invitations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_expires_at",
                table: "invitations",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_invitations_room_id",
                table: "invitations",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_question",
                table: "media_assets",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_answers_round_created",
                table: "player_answers",
                columns: new[] { "round_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_player_answers_round_user",
                table: "player_answers",
                columns: new[] { "round_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_player_answers_user_id",
                table: "player_answers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_players_in_room_session",
                table: "players_in_room",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_players_in_room_session_user",
                table: "players_in_room",
                columns: new[] { "session_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_players_in_room_user_id",
                table: "players_in_room",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_question_answers_question",
                table: "question_answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_question_answers_question_normalized",
                table: "question_answers",
                columns: new[] { "question_id", "normalized_answer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_question_answers_question_primary",
                table: "question_answers",
                columns: new[] { "question_id", "is_primary" },
                filter: "is_primary = true");

            migrationBuilder.CreateIndex(
                name: "ix_question_tags_tag",
                table: "question_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_questions_author",
                table: "questions",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_questions_created_at",
                table: "questions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_questions_status_type",
                table: "questions",
                columns: new[] { "status", "type" });

            migrationBuilder.CreateIndex(
                name: "IX_room_tag_selections_tag_id",
                table: "room_tag_selections",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_rooms_created_at",
                table: "rooms",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_rooms_owner",
                table: "rooms",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_rooms_status_visibility",
                table: "rooms",
                columns: new[] { "status", "visibility" });

            migrationBuilder.CreateIndex(
                name: "ix_scoreboards_session_score",
                table: "scoreboards",
                columns: new[] { "session_id", "score_total" });

            migrationBuilder.CreateIndex(
                name: "ix_scoreboards_session_user",
                table: "scoreboards",
                columns: new[] { "session_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scoreboards_user_id",
                table: "scoreboards",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tags_is_active",
                table: "tags",
                column: "is_active",
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_tags_name",
                table: "tags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_created_at",
                table: "users",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_is_banned",
                table: "users",
                column: "is_banned",
                filter: "is_banned = true");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "answer_evaluations");

            migrationBuilder.DropTable(
                name: "hints");

            migrationBuilder.DropTable(
                name: "invitations");

            migrationBuilder.DropTable(
                name: "players_in_room");

            migrationBuilder.DropTable(
                name: "question_tags");

            migrationBuilder.DropTable(
                name: "room_tag_selections");

            migrationBuilder.DropTable(
                name: "scoreboards");

            migrationBuilder.DropTable(
                name: "fuzzy_aliases");

            migrationBuilder.DropTable(
                name: "player_answers");

            migrationBuilder.DropTable(
                name: "media_assets");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "question_answers");

            migrationBuilder.DropTable(
                name: "game_rounds");

            migrationBuilder.DropTable(
                name: "game_sessions");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "rooms");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
