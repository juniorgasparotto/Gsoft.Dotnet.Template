# Gsoft – Template de aplicação .NET

Este repositório contém o **template Gsoft**: um template .NET que gera uma solução com Aspire, UI.Api, Workers (Jobs, Executor, Dashboard, Scheduler) e Migrations. O código **Shared** não é incluído no template; o projeto gerado usa um **submodule Git** apontando para **este repositório** para obter o Shared.

---

## Como instalar o template

1. Clone este repositório (ou baixe e extraia).
2. Na pasta do repositório, instale o template a partir da pasta `Template`:

```bash
cd Template
dotnet new install .
```

Ou usando o caminho absoluto:

```bash
dotnet new install "C:\caminho\para\Gsoft.Dotnet.Template\Template"
```

Para desinstalar depois:

```bash
dotnet new uninstall "C:\caminho\para\Gsoft.Dotnet.Template\Template"
```

---

## Como usar o template

1. Crie um novo projeto com o nome desejado (ex.: `MeuProjeto`):

```bash
dotnet new gsoftapp -n MeuProjeto -o MeuProjeto
```

- **`-n MeuProjeto`** — nome da aplicação (substitui "ToDoSystem" em pastas e projetos).
- **`-o MeuProjeto`** — pasta de saída.

2. Inicialize o Git e o submodule **Shared** (este repositório):

```bash
cd MeuProjeto
git init
git submodule update --init
```

3. Abra a solution e faça o build:

```bash
cd src
dotnet build
```

Ou abra `MeuProjeto\src\Solution.slnx` no Visual Studio / Rider.

O template já inclui o arquivo **`.gitmodules`** apontando para este repo; o submodule é clonado na pasta `Shared/`, e o código Shared fica em `Shared/src/Shared` (referenciado por `$(SharedRoot)` no MSBuild).

---

## Estrutura do repositório

| Pasta        | Conteúdo |
|-------------|----------|
| **`Template/`** | Conteúdo do template (`.template.config`, `src`, `.gitmodules`, etc.). Use com `dotnet new install` e `dotnet new gsoftapp`. |
| **`src/`**      | Solução para desenvolvimento de Shared e Databases (SqliteBrowser). Aspire e o app de exemplo (ToDoSystem) existem só no `Template/`. |

O template **não** inclui código Shared; o projeto gerado referencia Shared via submodule (este repositório). Mais detalhes em [Template/README.md](Template/README.md).
