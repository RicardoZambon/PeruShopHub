#!/usr/bin/env bash
# PeruShopHub — Backup Restore Verification
# Restores the latest backup into a temporary database and verifies integrity.
# Intended to run weekly (e.g. Sundays after backup).
set -euo pipefail

DB_HOST="${PGHOST:-db}"
DB_PORT="${PGPORT:-5432}"
DB_USER="${PGUSER:-perushophub}"
DB_NAME="${PGDATABASE:-perushophub}"
BACKUP_DIR="${BACKUP_DIR:-/backups}"
VERIFY_DB="${DB_NAME}_verify_$(date +%s)"
ALERT_WEBHOOK="${ALERT_WEBHOOK:-}"

log() { echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"; }

send_alert() {
  local message="$1"
  log "ALERT: ${message}"
  if [[ -n "${ALERT_WEBHOOK}" ]]; then
    curl -sf -X POST -H 'Content-Type: application/json' \
      -d "{\"text\":\"🚨 PeruShopHub Restore Verify: ${message}\"}" \
      "${ALERT_WEBHOOK}" || log "WARNING: Failed to send alert webhook"
  fi
}

cleanup() {
  log "Dropping verification database ${VERIFY_DB} ..."
  psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d postgres \
    -c "DROP DATABASE IF EXISTS \"${VERIFY_DB}\";" 2>/dev/null || true
}
trap cleanup EXIT

# Find latest daily backup
LATEST_BACKUP=$(find "${BACKUP_DIR}/daily" -name "*.sql.gz" -type f -printf '%T@ %p\n' \
  | sort -rn | head -1 | awk '{print $2}')

if [[ -z "${LATEST_BACKUP}" ]]; then
  send_alert "No backup files found in ${BACKUP_DIR}/daily"
  exit 1
fi

log "Verifying backup: ${LATEST_BACKUP}"

# Create temporary verification database
log "Creating verification database: ${VERIFY_DB}"
psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d postgres \
  -c "CREATE DATABASE \"${VERIFY_DB}\";"

# Restore backup
log "Restoring backup into ${VERIFY_DB} ..."
if gunzip -c "${LATEST_BACKUP}" | psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${VERIFY_DB}" > /dev/null 2>&1; then
  log "Restore completed successfully"
else
  send_alert "Restore FAILED for ${LATEST_BACKUP}"
  exit 1
fi

# Verify integrity — check key tables exist and have data
log "Running integrity checks ..."
ERRORS=0

check_table() {
  local table="$1"
  local count
  count=$(psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${VERIFY_DB}" \
    -t -A -c "SELECT COUNT(*) FROM \"${table}\" LIMIT 1;" 2>/dev/null || echo "ERROR")
  if [[ "${count}" == "ERROR" ]]; then
    log "  FAIL: Table '${table}' — query failed"
    ERRORS=$((ERRORS + 1))
  else
    log "  OK: Table '${table}' — ${count} row(s)"
  fi
}

# Check core tables (these should exist in any PeruShopHub database)
for table in Tenants Users Products Categories; do
  check_table "${table}"
done

# Check table count matches source
SOURCE_TABLES=$(psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" \
  -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE';")
VERIFY_TABLES=$(psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${VERIFY_DB}" \
  -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE';")

if [[ "${SOURCE_TABLES}" == "${VERIFY_TABLES}" ]]; then
  log "  OK: Table count matches (${SOURCE_TABLES} tables)"
else
  log "  WARN: Table count mismatch — source=${SOURCE_TABLES}, restored=${VERIFY_TABLES}"
  ERRORS=$((ERRORS + 1))
fi

if (( ERRORS > 0 )); then
  send_alert "Restore verification found ${ERRORS} issue(s) — check logs"
  exit 1
fi

# Record successful verification
date +%s > "${BACKUP_DIR}/.last_verify_timestamp"
log "Restore verification PASSED"
