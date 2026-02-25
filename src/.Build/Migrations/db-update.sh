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

# Usage: ./db-update.sh <migrations-project-path>
# Accepts absolute or relative path to repo root (e.g. src/ToDoSystem/UI/...)
PROJECT_DIR="${1:?Provide the migrations project path}"
if [[ "$PROJECT_DIR" == /* ]]; then
  cd "$PROJECT_DIR"
else
  ROOT_DIR="$(cd "$(dirname "$0")/../../.." && pwd)"
  cd "$ROOT_DIR/$PROJECT_DIR"
fi

# Output folder
OUT_DIR="Migrations/SQL"
mkdir -p "$OUT_DIR"

# Migration name (timestamp)
TS="$(date +"%Y%m%d_%H%M%S")"

echo -e "${B}🔧 Creating migration${X} ${Y}${TS}${X}"
dotnet ef migrations add "${TS}"

# Capture ONLY migration lines (format: 14 digits + underscore)
mapfile -t MIGS < <(dotnet ef migrations list --no-color \
  | grep -E '^[0-9]{14}_.+' \
  | awk '{print $1}')

COUNT=${#MIGS[@]}
if (( COUNT == 0 )); then
  echo -e "${R}❌ No migration found. Something went wrong creating ${TS}.${X}"
  exit 1
fi

if (( COUNT == 1 )); then
  # First migration: generate everything from 0 -> FIRST
  PREV="0"
  LAST="${MIGS[0]}"
else
  PREV="${MIGS[$COUNT-2]}"
  LAST="${MIGS[$COUNT-1]}"
fi

OUT_FILE="${OUT_DIR}/${LAST}.sql"

echo -e "${B}🔧 Generating DELTA script:${X} ${C}${PREV}${X} ${M}->${X} ${C}${LAST}${X}"
dotnet ef migrations script "${PREV}" "${LAST}" -o "${OUT_FILE}"

echo -e "${G}✅ Script saved to${X} ${Y}${OUT_FILE}${X}"
echo ""
# Separator: slightly smaller than terminal width
COLS=$(tput cols 2>/dev/null || echo 80)
SEP_LEN=$((COLS > 30 ? COLS - 4 : 60))
mksep() { printf '━%.0s' $(seq 1 "${1:-40}" 2>/dev/null) 2>/dev/null || true; }
LEFT=$(( (SEP_LEN - 13) / 2 ))
RIGHT=$(( SEP_LEN - 13 - LEFT ))
echo -e "${C}$(mksep $LEFT)${X} ${M}SQL Preview${X} ${C}$(mksep $RIGHT)${X}"
echo -e "${Y}"
cat "$OUT_FILE"
echo -e "${X}"
echo -e "${C}$(mksep $SEP_LEN)${X}"

read -p "$(echo -e "${B}👉 Apply script to database now? (y/N):${X} ")" CONFIRM

if [[ "$CONFIRM" =~ ^[Yy]$ ]]; then
  echo -e "${B}🚀 Applying migration to database...${X}"
  echo -e "${C}ℹ️  Note:${X} If 'Failed executing DbCommand' appears when reading __EFMigrationsHistory, this is normal on the first migration."
  dotnet ef database update
  echo -e "${G}✅ Migration applied successfully.${X}"
else
  echo -e "${Y}⏭️  Update cancelled, reverting everything.${X}"

  echo -e "${M}🗑️  Removing generated SQL${X} (${Y}${OUT_FILE}${X})..."
  rm -f "${OUT_FILE}"
  echo -e "${M}🗑️  Removing last created migration${X} (${Y}${LAST}${X})..."
  dotnet ef migrations remove
  echo -e "${G}✅ Migration ${LAST} and SQL removed.${X}"
fi
