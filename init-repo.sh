#!/usr/bin/env sh
set -e
echo "Inicializando repositorio e submodule..."
[ -d .git ] || git init 2>&1
git submodule update --init 2>&1 || { echo "Aviso: git submodule falhou. Execute manualmente: git init && git submodule update --init"; exit 0; }
echo "Submodule OK. Executando build..."
cd src
dotnet build -f ToDoSystem.slnx
