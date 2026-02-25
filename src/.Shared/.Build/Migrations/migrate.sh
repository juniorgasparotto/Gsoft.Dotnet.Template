#!/usr/bin/env bash
set -euo pipefail

# ANSI colors
R='\033[1;31m'   # Red
G='\033[1;32m'   # Green
Y='\033[1;33m'   # Yellow
B='\033[1;34m'   # Blue
C='\033[1;36m'   # Cyan
M='\033[1;35m'   # Magenta
W='\033[1;37m'   # White
X='\033[0m'      # Reset

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
MIGRATIONS_DIR="$SCRIPT_DIR"
ROOT_DIR="$(cd "$SCRIPT_DIR/../../.." && pwd)"

# When called with project path: ./migrate.sh <project_path>
if [[ -n "${1:-}" ]]; then
  PROJECT_DIR_FOR_SCRIPTS="$1"
  echo ""
  echo -e "${C}📁 Project:${X} ${Y}$1${X}"
  echo ""
  echo -e "${W}What do you want to do?${X}"
  echo -e "  ${G}1)${X} Full Migration"
  echo -e "  ${G}2)${X} Empty Migration"
  echo ""
  read -p "$(echo -e "${B}Choose:${X} ")" ACTION_CHOICE
else
  echo -e "${R}❌ Inform the project: ./migrate.sh <project_path>${X}"
  exit 1
fi

case "$ACTION_CHOICE" in
  1)
    "$MIGRATIONS_DIR/db-update.sh" "$PROJECT_DIR_FOR_SCRIPTS"
    ;;
  2)
    "$MIGRATIONS_DIR/create-empty-migration.sh" "$PROJECT_DIR_FOR_SCRIPTS"
    ;;
  *)
    echo -e "${R}❌ Invalid option.${X}"
    exit 1
    ;;
esac
