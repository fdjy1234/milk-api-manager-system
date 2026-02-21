#!/usr/bin/env bash
set -euo pipefail

# apply_audit_migration.sh
# Applies the existing SQL migration file 20260214_AddAuditMetadata.sql to a PostgreSQL database.
# Usage:
#   ./apply_audit_migration.sh \
#       --host <host> --port <port> --db <database> --user <user> --password <password>
# Or set environment variables: PGHOST, PGPORT, PGDATABASE, PGUSER, PGPASSWORD

print_usage(){
  echo "Usage: $0 --host HOST --port PORT --db DATABASE --user USER --password PASSWORD"
  echo "Or set PGHOST/PGPORT/PGDATABASE/PGUSER/PGPASSWORD env vars."
}

# parse args
while [[ $# -gt 0 ]]; do
  case "$1" in
    --host) PGHOST="$2"; shift 2;;
    --port) PGPORT="$2"; shift 2;;
    --db) PGDATABASE="$2"; shift 2;;
    --user) PGUSER="$2"; shift 2;;
    --password) PGPASSWORD="$2"; shift 2;;
    -h|--help) print_usage; exit 0;;
    *) echo "Unknown arg: $1"; print_usage; exit 1;;
  esac
done

: "${PGHOST:=}" || true
: "${PGPORT:=5432}" || true
: "${PGDATABASE:=}" || true
: "${PGUSER:=}" || true
: "${PGPASSWORD:=}" || true

if [[ -z "$PGHOST" || -z "$PGDATABASE" || -z "$PGUSER" || -z "$PGPASSWORD" ]]; then
  echo "Missing required DB connection info." >&2
  print_usage
  exit 2
fi

SQL_FILE="$(dirname "$0")/../Migrations/20260214_AddAuditMetadata.sql"
if [[ ! -f "$SQL_FILE" ]]; then
  echo "SQL migration file not found: $SQL_FILE" >&2
  exit 3
fi

export PGPASSWORD="$PGPASSWORD"

echo "Applying SQL migration: $SQL_FILE to ${PGHOST}:${PGPORT}/${PGDATABASE} as ${PGUSER}"
psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -f "$SQL_FILE"

echo "Migration applied successfully."
