using System;
using Dim.Abstractions.Messages;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dim");
            for (var i = 0; i < 16; i++)
            {
                migrationBuilder.CreateTable(
                name: $"messages_{i}",
                schema: "dim",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    app_id = table.Column<long>(type: "bigint", nullable: false),
                    sender = table.Column<string>(type: "text", nullable: false),
                    receiver = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<MessageContent>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => new { x.id, x.created_at });
                });

                migrationBuilder.CreateIndex(
                    name: "IX_messages_id_created_at",
                    schema: "dim",
                    table: "messages",
                    columns: new[] { "id", "created_at" });

                migrationBuilder.Sql($"""
                SELECT extensions.create_hypertable(
                    'dim.messages_{i}',
                    'created_at',
                    chunk_time_interval => INTERVAL '7 days',
                    if_not_exists => TRUE
                );
            """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (var i = 0; i < 16; i++)
            {
                migrationBuilder.DropTable(
                    name: $"messages_{i}",
                    schema: "dim");
            }
        }
    }
}
