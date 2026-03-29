#!/usr/bin/env bash
# PeruShopHub — Automated PostgreSQL Backup
# Performs pg_dump with compression, applies retention policy, and syncs offsite.
set -euo pipefail

# ---------- Configuration (overridable via environment) ----------
DB_HOST="${PGHOST:-db}"
DB_PORT="${PGPORT:-5432}"
DB_NAME="${PGDATABASE:-perushophub}"
DB_USER="${PGUSER:-perushophub}"
# PGPASSWORD must be set in the environment (or via .pgpass)

BACKUP_DIR="${BACKUP_DIR:-/backups}"
OFFSITE_BUCKET="${OFFSITE_BUCKET:-}"          # e.g. s3://perushophub-backups
OFFSITE_ENDPOINT="${OFFSITE_ENDPOINT:-}"      # e.g. https://s3.us-west-001.backblazeb2.com

RETAIN_DAILY="${RETAIN_DAILY:-7}"
RETAIN_WEEKLY="${RETAIN_WEEKLY:-4}"
RETAIN_MONTHLY="${RETAIN_MONTHLY:-3}"

ALERT_WEBHOOK="${ALERT_WEBHOOK:-}"            # Slack/Discord webhook URL for alerts

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
DAY_OF_WEEK=$(date +%u)   # 1=Monday … 7=Sunday
DAY_OF_MONTH=$(date +%d)

# ---------- Directories ----------
DAILY_DIR="${BACKUP_DIR}/daily"
WEEKLY_DIR="${BACKUP_DIR}/weekly"
MONTHLY_DIR="${BACKUP_DIR}/monthly"
mkdir -p "${DAILY_DIR}" "${WEEKLY_DIR}" "${MONTHLY_DIR}"

BACKUP_FILE="${DAILY_DIR}/${DB_NAME}_${TIMESTAMP}.sql.gz"

# ---------- Functions ----------
log() { echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"; }

send_alert() {
  local message="$1"
  log "ALERT: ${message}"
  if [[ -n "${ALERT_WEBHOOK}" ]]; then
    curl -sf -X POST -H 'Content-Type: application/json' \
      -d "{\"text\":\"🚨 PeruShopHub Backup: ${message}\"}" \
      "${ALERT_WEBHOOK}" || log "WARNING: Failed to send alert webhook"
  fi
}

cleanup_old() {
  local dir="$1" keep="$2"
  local count
  count=$(find "${dir}" -name "*.sql.gz" -type f | wc -l)
  if (( count > keep )); then
    find "${dir}" -name "*.sql.gz" -type f -printf '%T@ %p\n' \
      | sort -n | head -n "$(( count - keep ))" | awk '{print $2}' \
      | xargs rm -f
    log "Pruned $(( count - keep )) old backup(s) from ${dir} (kept ${keep})"
  fi
}

sync_offsite() {
  if [[ -z "${OFFSITE_BUCKET}" ]]; then
    log "No OFFSITE_BUCKET configured — skipping offsite sync"
    return 0
  fi

  local endpoint_flag=""
  if [[ -n "${OFFSITE_ENDPOINT}" ]]; then
    endpoint_flag="--endpoint-url=${OFFSITE_ENDPOINT}"
  fi

  log "Syncing backups to ${OFFSITE_BUCKET} ..."
  aws s3 sync "${BACKUP_DIR}" "${OFFSITE_BUCKET}" \
    ${endpoint_flag} \
    --exclude "*.tmp" \
    --storage-class STANDARD_IA \
    --only-show-errors
  log "Offsite sync complete"
}

# ---------- Main ----------
log "Starting backup of ${DB_NAME}@${DB_HOST}:${DB_PORT}"

# Perform the dump
if pg_dump -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" \
    --no-owner --no-privileges --clean --if-exists \
    | gzip -9 > "${BACKUP_FILE}.tmp"; then
  mv "${BACKUP_FILE}.tmp" "${BACKUP_FILE}"
  FILESIZE=$(stat -c%s "${BACKUP_FILE}" 2>/dev/null || stat -f%z "${BACKUP_FILE}")
  log "Daily backup created: ${BACKUP_FILE} ($(( FILESIZE / 1024 )) KB)"
else
  send_alert "pg_dump FAILED for ${DB_NAME} at $(date)"
  exit 1
fi

# Promote to weekly (every Sunday)
if [[ "${DAY_OF_WEEK}" == "7" ]]; then
  cp "${BACKUP_FILE}" "${WEEKLY_DIR}/${DB_NAME}_weekly_${TIMESTAMP}.sql.gz"
  log "Weekly backup promoted"
fi

# Promote to monthly (1st of month)
if [[ "${DAY_OF_MONTH}" == "01" ]]; then
  cp "${BACKUP_FILE}" "${MONTHLY_DIR}/${DB_NAME}_monthly_${TIMESTAMP}.sql.gz"
  log "Monthly backup promoted"
fi

# Apply retention policy
cleanup_old "${DAILY_DIR}"   "${RETAIN_DAILY}"
cleanup_old "${WEEKLY_DIR}"  "${RETAIN_WEEKLY}"
cleanup_old "${MONTHLY_DIR}" "${RETAIN_MONTHLY}"

# Sync offsite
sync_offsite

# Record last successful backup timestamp for monitoring
date +%s > "${BACKUP_DIR}/.last_backup_timestamp"

log "Backup pipeline complete"
