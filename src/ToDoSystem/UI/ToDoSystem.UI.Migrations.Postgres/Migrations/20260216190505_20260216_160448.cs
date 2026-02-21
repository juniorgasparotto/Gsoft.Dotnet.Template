using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ToDoSystem.UI.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class _20260216_160448 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "todo_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, comment: "Identificador único do item")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Título/tarefa do item"),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Descrição opcional do item"),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indica se o item está concluído"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Data e hora de criação do item"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data e hora da última atualização do item")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todo_items", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_todo_items_CreatedAt",
                table: "todo_items",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_todo_items_IsCompleted",
                table: "todo_items",
                column: "IsCompleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "todo_items");
        }
    }
}
