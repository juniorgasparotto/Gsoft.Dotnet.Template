using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToDoSystem.UI.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class _20260219_000006 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestEnum",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestEnum", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "todo_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Identificador único do item")
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false, comment: "Título/tarefa do item"),
                    Teste = table.Column<int>(type: "INTEGER", nullable: false, comment: "Campo de teste para demonstração"),
                    Teste2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true, comment: "Descrição opcional do item"),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false, comment: "Indica se o item está concluído"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Data e hora de criação do item"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "Data e hora da última atualização do item")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todo_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "TypeEnum",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeEnum", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TestEnum",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Name" },
                values: new object[] { 0, "Seed", new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Test" });

            migrationBuilder.InsertData(
                table: "TypeEnum",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Name" },
                values: new object[] { 0, "Seed", new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Valor para testes" });

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
                name: "TestEnum");

            migrationBuilder.DropTable(
                name: "todo_items");

            migrationBuilder.DropTable(
                name: "TypeEnum");
        }
    }
}
