@echo off
setlocal
echo Inicializando repositorio e submodule...
if not exist .git git init 2>&1
git submodule update --init 2>&1
if errorlevel 1 (
  echo Aviso: git submodule falhou. Execute manualmente: git init && git submodule update --init
  exit /b 0
)
echo Submodule OK. Executando build...
cd src
dotnet build -f ToDoSystem.slnx
endlocal
