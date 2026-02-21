#!/usr/bin/env bash
set -euo pipefail

# rollback_audit_migration.sh
# Rolls back the AddAuditMetadata migration by restoring from a pg_dump file produced before applying the migration.
# Usage:
#   ./rollback_audit_migration.sh --host <host> --port <port> --db <database> --user <user> --dump-file <path>
# Or set environment variables: PGHOST, PGPORT, PGDATABASE, PGUSER, PGPASSWORD

print_usage(){
  echo "Usage: $0 --host HOST --port PORT --db DATABASE --user USER --dump-file /path/to/backup.sql"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --host) PGHOST="$2"; shift 2;;
    --port) PGPORT="$2"; shift 2;;
    --db) PGDATABASE="$2"; shift 2;;
    --user) PGUSER="$2"; shift 2;;
    --password) PGPASSWORD="$2"; shift 2;;
    --dump-file) DUMP_FILE="$2"; shift 2;;
    -h|--help) print_usage; exit 0;;
    *) echo "Unknown arg: $1"; print_usage; exit 1;;
  esac
done

: "${PGHOST:=}" || true
: "${PGPORT:=5432}" || true
: "${PGDATABASE:=}" || true
: "${PGUSER:=}" || true
: "${PGPASSWORD:=}" || true

if [[ -z "${DUMP_FILE:-}" ]]; then
  echo "Missing dump file." >&2
  print_usage
  exit 2
fi

if [[ ! -f "$DUMP_FILE" ]]; then
  echo "Dump file not found: $DUMP_FILE" >&2
  exit 3
fi

export PGPASSWORD="${PGPASSWORD}"

echo "Restoring database from dump: $DUMP_FILE to ${PGHOST}:${PGPORT}/${PGDATABASE} as ${PGUSER}"
psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -f "$DUMP_FILE"

echo "Restore completed."
