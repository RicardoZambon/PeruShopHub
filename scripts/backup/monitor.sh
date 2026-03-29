#!/usr/bin/env bash
# PeruShopHub — Backup Monitoring
# Alerts if no successful backup in the last 25 hours.
# Run hourly via cron to detect missed backups.
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/backups}"
MAX_AGE_HOURS="${MAX_AGE_HOURS:-25}"
ALERT_WEBHOOK="${ALERT_WEBHOOK:-}"

log() { echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"; }

send_alert() {
  local message="$1"
  log "ALERT: ${message}"
  if [[ -n "${ALERT_WEBHOOK}" ]]; then
    curl -sf -X POST -H 'Content-Type: application/json' \
      -d "{\"text\":\"🚨 PeruShopHub Backup Monitor: ${message}\"}" \
      "${ALERT_WEBHOOK}" || log "WARNING: Failed to send alert webhook"
  fi
}

TIMESTAMP_FILE="${BACKUP_DIR}/.last_backup_timestamp"

if [[ ! -f "${TIMESTAMP_FILE}" ]]; then
  send_alert "No backup timestamp file found — backup may never have run"
  exit 1
fi

LAST_BACKUP=$(cat "${TIMESTAMP_FILE}")
NOW=$(date +%s)
AGE_HOURS=$(( (NOW - LAST_BACKUP) / 3600 ))

if (( AGE_HOURS >= MAX_AGE_HOURS )); then
  send_alert "Last successful backup was ${AGE_HOURS} hours ago (threshold: ${MAX_AGE_HOURS}h)"
  exit 1
fi

log "OK: Last backup ${AGE_HOURS}h ago (threshold: ${MAX_AGE_HOURS}h)"
