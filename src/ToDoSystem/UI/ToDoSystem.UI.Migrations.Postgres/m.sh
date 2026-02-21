#!/usr/bin/env bash
# Calls migrate.sh in .Build for this migrations project
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
exec "$(cd "$SCRIPT_DIR/../../../.." && pwd)/src/.Build/Migrations/migrate.sh" "$SCRIPT_DIR"
