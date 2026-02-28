# Gsoft – Template de aplicação .NET

Este repositório contém o **template Gsoft**: um template .NET que gera uma solução com Aspire, UI.Api, Workers (Jobs, Executor, Dashboard, Scheduler) e Migrations. O código **Shared** não é incluído no template; o projeto gerado usa um **submodule Git** em `src/.Shared` apontando para **este repositório**.

---

## Como instalar o template

1. Clone este repositório (ou baixe e extraia).
2. Instale o template a partir da **raiz** do repositório:

```bash
cd Gsoft.Dotnet.Template
dotnet new install .
```

Ou usando o caminho absoluto:

```bash
dotnet new install "C:\caminho\para\Gsoft.Dotnet.Template"
```

Para desinstalar:

```bash
dotnet new uninstall "C:\caminho\para\Gsoft.Dotnet.Template"
```

---

## Como usar o template

1. Crie um novo projeto com o nome desejado (ex.: `MeuProjeto`):

```bash
dotnet new gsoftapp -n MeuProjeto -o MeuProjeto
```

- **`-n MeuProjeto`** — nome da aplicação (substitui "ToDoSystem" em pastas, projetos e nomes dos .slnx).
- **`-o MeuProjeto`** — pasta de saída.

2. Inicialize o Git e o submodule **Shared**:

```bash
cd MeuProjeto
git init
git submodule update --init
```

Ou execute o script gerado: `init-submodule.cmd` (Windows) / `./init-submodule.sh` (Linux/macOS). O template tenta rodar o script automaticamente após a criação (quando possível).

3. Build:

```bash
cd src
dotnet build -f MeuProjeto.slnx
```

Ou abra `MeuProjeto\src\MeuProjeto.slnx` (só app) ou `MeuProjetoAll.slnx` (app + Shared) no Visual Studio / Rider.

---

## Estrutura gerada

| Item | Descrição |
|------|-----------|
| **`.github/`** | Pasta para workflows (vazia no template). |
| **`src/Shared/`** | Código Shared (Core, Infra, UI). |
| **`src/Apps/`** | Aspire + projeto da aplicação (ex.: MeuProjeto). |
| **`src/<Nome>.slnx`** | Solution só com Apps. |
| **`src/<Nome>All.slnx`** | Solution com Apps + .Shared. |
| **`src/Directory.Build.props`** | `SharedRoot = Shared`. |
| **`README.md`, `.gitignore`, `version.json`, `.gitmodules`** | Na raiz do projeto. |

Os arquivos `ToDoSystem.slnx` (só app) e `ToDoSystemAll.slnx` (app + Shared) ficam em `src/`.

---

## Estrutura deste repositório

| Pasta | Conteúdo |
|-------|----------|
| **Raiz do repo** | `.template.config/`, `.github/`, `src/`, `README.md`, `.gitignore`, `version.json`, `.gitmodules`. Instale com `dotnet new install .` na raiz. |
| **`src/`** | `Apps/` (Aspire + app + Databases), `Shared/` (Core, Infra, UI), `.Build/`, `ToDoSystem.slnx`, `ToDoSystemAll.slnx`, `Solution.slnx`, build props/targets. |
