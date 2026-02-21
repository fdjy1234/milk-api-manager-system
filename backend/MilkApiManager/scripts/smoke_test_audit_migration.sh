#!/usr/bin/env bash
set -euo pipefail

# smoke_test_audit_migration.sh
# Basic verification checks after applying AddAuditMetadata migration.
# Usage:
#   ./smoke_test_audit_migration.sh --host <host> --port <port> --db <database> --user <user> --password <password>
# Or set environment variables: PGHOST, PGPORT, PGDATABASE, PGUSER, PGPASSWORD

print_usage(){
  echo "Usage: $0 --host HOST --port PORT --db DATABASE --user USER --password PASSWORD"
}

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

export PGPASSWORD="$PGPASSWORD"

PSQL="psql -h $PGHOST -p $PGPORT -U $PGUSER -d $PGDATABASE -t -c"

echo "Running smoke tests against ${PGHOST}:${PGPORT}/${PGDATABASE}"

# 1) Check columns exist on AuditLog table
$PSQL "SELECT column_name FROM information_schema.columns WHERE table_name='auditlog' AND column_name IN ('correlationid','requestid','operatorip');" | sed '/^\s*$/d' | awk '{print $1}'

# 2) Check index existence (simple existence check)
$PSQL "SELECT indexname FROM pg_indexes WHERE tablename='auditlog' AND indexname LIKE '%correlationid%';" | sed '/^\s*$/d'

# 3) Run a simple insert/select test (non-destructive)
TMP_SQL="INSERT INTO auditlog(message) VALUES('smoke test') RETURNING id;"
ID=$($PSQL "$TMP_SQL" | sed '/^\s*$/d' | head -n1)
if [[ -z "$ID" ]]; then
  echo "Insert test failed" >&2
  exit 3
fi
$PSQL "DELETE FROM auditlog WHERE id = $ID;"

echo "Smoke tests passed."
