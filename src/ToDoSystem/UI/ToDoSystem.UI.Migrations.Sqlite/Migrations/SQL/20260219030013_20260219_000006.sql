CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "TestEnum" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_TestEnum" PRIMARY KEY,
    "CreatedBy" TEXT NULL,
    "CreatedDate" TEXT NOT NULL,
    "Name" TEXT NOT NULL
);

CREATE TABLE "todo_items" (
    -- Identificador único do item
    "id" INTEGER NOT NULL CONSTRAINT "PK_todo_items" PRIMARY KEY AUTOINCREMENT,

    -- Título/tarefa do item
    "Title" TEXT NOT NULL,

    -- Campo de teste para demonstração
    "Teste" INTEGER NOT NULL,

    "Teste2" INTEGER NOT NULL,

    -- Descrição opcional do item
    "Description" TEXT NULL,

    -- Indica se o item está concluído
    "IsCompleted" INTEGER NOT NULL DEFAULT 0,

    -- Data e hora de criação do item
    "CreatedAt" TEXT NOT NULL,

    -- Data e hora da última atualização do item
    "UpdatedAt" TEXT NULL
);

CREATE TABLE "TypeEnum" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_TypeEnum" PRIMARY KEY,
    "CreatedBy" TEXT NULL,
    "CreatedDate" TEXT NOT NULL,
    "Name" TEXT NOT NULL
);

INSERT INTO "TestEnum" ("Id", "CreatedBy", "CreatedDate", "Name")
VALUES (0, 'Seed', '2000-01-01 00:00:00', 'Test');
SELECT changes();


INSERT INTO "TypeEnum" ("Id", "CreatedBy", "CreatedDate", "Name")
VALUES (0, 'Seed', '2000-01-01 00:00:00', 'Valor para testes');
SELECT changes();


CREATE INDEX "IX_todo_items_CreatedAt" ON "todo_items" ("CreatedAt");

CREATE INDEX "IX_todo_items_IsCompleted" ON "todo_items" ("IsCompleted");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260219030013_20260219_000006', '10.0.3');

COMMIT;

