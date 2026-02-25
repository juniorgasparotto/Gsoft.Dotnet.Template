#!/usr/bin/env bash
set -euo pipefail

# ANSI colors
R='\033[1;31m'   # Red
G='\033[1;32m'   # Green
Y='\033[1;33m'   # Yellow
B='\033[1;34m'   # Blue
C='\033[1;36m'   # Cyan
X='\033[0m'      # Reset

# Usage: ./create-empty-migration.sh <migrations-project-path>
# Accepts absolute or relative path to repo root
PROJECT_DIR="${1:?Provide the migrations project path}"
if [[ "$PROJECT_DIR" == /* ]]; then
  cd "$PROJECT_DIR"
else
  ROOT_DIR="$(cd "$(dirname "$0")/../../.." && pwd)"
  cd "$ROOT_DIR/$PROJECT_DIR"
fi

# Timestamp for migration name
TS="$(date +"%Y%m%d_%H%M%S")"

echo -e "${B}🔧 Creating empty migration${X} ${Y}${TS}${X}"
dotnet ef migrations add "${TS}"

echo -e "${G}✅ Empty migration created.${X} Edit Up/Down in Migrations/ to add custom SQL."
