# Template Gsoft App

Template .NET que gera uma solução com **Aspire**, **UI.Api**, **Workers** (Jobs, Executor, Dashboard, Scheduler) e **Migrations**. O código **Shared** não faz parte do template: é obtido via **submodule Git** apontando para este repositório.

## Uso

```bash
dotnet new install .   # na pasta Template (ou caminho absoluto)
dotnet new gsoftapp -n MeuProjeto -o MeuProjeto
cd MeuProjeto
```

## Estrutura após criar o projeto

O template gera a aplicação (ex.: `MeuProjeto`) com um arquivo **`.gitmodules`** que define o submodule **Shared** apontando para este repositório (Gsoft.Dotnet.Template). O código Shared fica em `Shared/src/Shared` dentro do clone do submodule.

```
MeuProjeto/
  .gitmodules          ← submodule "Shared" → este repo
  version.json
  README.md
  src/
    Aspire/
    MeuProjeto.UI.Api
    MeuProjeto.UI.Migrations.Sqlite
    MeuProjeto.UI.Migrations.Postgres
    MeuProjeto.Infra
    MeuProjeto.Worker.Scheduler
    ...
  Shared/              ← após git submodule update (clone deste repo)
    src/
      Shared/
        Core/
        Infra/
        UI/
    ...
```

## Inicializar o submodule Shared

Após criar o projeto, inicialize o repositório Git e o submodule para que o Shared fique disponível:

```bash
cd MeuProjeto
git init
git submodule update --init
cd src
dotnet build
```

Ou, se já estiver em um repo Git:

```bash
git submodule update --init
cd src
dotnet build
```

Assim, a pasta `src` do projeto usa `$(SharedRoot)` = `../Shared/src/Shared` (o submodule é este repositório; o código Shared está em `src/Shared` dentro dele).

## O que não entra no template

- Nenhum código **Shared** (referenciado via submodule; `$(SharedRoot)` = `../Shared/src/Shared`)
- Projeto de **Databases** (ex.: SqliteBrowser)
- Pastas **bin**, **obj**, **.vs**, **.git**
