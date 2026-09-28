using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dsg.Hrms.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SessionsAndSignIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_until",
                schema: "identity",
                table: "user_account",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "login_challenge",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<long>(type: "bigint", nullable: false),
                    offered_channels = table.Column<int>(type: "integer", nullable: false),
                    verification_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed = table.Column<bool>(type: "boolean", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_challenge", x => x.id);
                    table.ForeignKey(
                        name: "fk_login_challenge_user_account_user_account_id",
                        column: x => x.user_account_id,
                        principalSchema: "identity",
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "login_throttle",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email_hash = table.Column<string>(type: "text", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_throttle", x => x.id);
                    table.CheckConstraint("ck_login_throttle_email_hash_length", "length(email_hash) = 44");
                });

            migrationBuilder.CreateTable(
                name: "user_session",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<long>(type: "bigint", nullable: false),
                    security_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_activity_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    activity_window_started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    activity_window_count = table.Column<int>(type: "integer", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    end_reason = table.Column<int>(type: "integer", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_session", x => x.id);
                    table.CheckConstraint("ck_user_session_ended", "(ended_at IS NULL) = (end_reason IS NULL)");
                    table.ForeignKey(
                        name: "fk_user_session_user_account_user_account_id",
                        column: x => x.user_account_id,
                        principalSchema: "identity",
                        principalTable: "user_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token", x => x.id);
                    table.CheckConstraint("ck_refresh_token_hash_length", "length(token_hash) = 44");
                    table.ForeignKey(
                        name: "fk_refresh_token_user_session_session_id",
                        column: x => x.session_id,
                        principalSchema: "identity",
                        principalTable: "user_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_login_challenge_public_id",
                schema: "identity",
                table: "login_challenge",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_login_challenge_user_account_id",
                schema: "identity",
                table: "login_challenge",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_login_throttle_email_hash",
                schema: "identity",
                table: "login_throttle",
                column: "email_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_session_id",
                schema: "identity",
                table: "refresh_token",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_token_hash",
                schema: "identity",
                table: "refresh_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_session_public_id",
                schema: "identity",
                table: "user_session",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_session_user_account_id",
                schema: "identity",
                table: "user_session",
                column: "user_account_id",
                filter: "ended_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "login_challenge",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "login_throttle",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "refresh_token",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_session",
                schema: "identity");

            migrationBuilder.DropColumn(
                name: "locked_until",
                schema: "identity",
                table: "user_account");
        }
    }
}
