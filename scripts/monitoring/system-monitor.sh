#!/usr/bin/env bash
# PeruShopHub — System Resource Monitor
# Checks disk space and Redis memory usage, alerts when thresholds exceeded.
# Run via cron every 5 minutes in the backup/monitoring container.
set -euo pipefail

# --- Configuration (override via environment) ---
DISK_THRESHOLD_PERCENT="${DISK_THRESHOLD_PERCENT:-80}"
REDIS_THRESHOLD_PERCENT="${REDIS_THRESHOLD_PERCENT:-80}"
REDIS_HOST="${REDIS_HOST:-redis}"
REDIS_PORT="${REDIS_PORT:-6379}"
REDIS_PASSWORD="${REDIS_PASSWORD:-}"
ALERT_WEBHOOK="${ALERT_WEBHOOK:-}"
HEALTH_URL="${HEALTH_URL:-http://api:5000/health}"

log() { echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"; }

send_alert() {
  local severity="$1"
  local message="$2"
  log "ALERT [${severity}]: ${message}"
  if [[ -n "${ALERT_WEBHOOK}" ]]; then
    local icon="⚠️"
    [[ "${severity}" == "critical" ]] && icon="🚨"
    curl -sf -X POST -H 'Content-Type: application/json' \
      -d "{\"text\":\"${icon} PeruShopHub Monitor [${severity^^}]: ${message}\"}" \
      "${ALERT_WEBHOOK}" || log "WARNING: Failed to send alert webhook"
  fi
}

# --- Disk Space Check ---
check_disk() {
  local usage
  usage=$(df / | awk 'NR==2 {print $5}' | tr -d '%')
  log "Disk usage: ${usage}%"
  if (( usage >= DISK_THRESHOLD_PERCENT )); then
    local available
    available=$(df -h / | awk 'NR==2 {print $4}')
    send_alert "warning" "Disk usage at ${usage}% (threshold: ${DISK_THRESHOLD_PERCENT}%). Available: ${available}"
    return 1
  fi
  return 0
}

# --- Redis Memory Check ---
check_redis() {
  local redis_cmd="redis-cli -h ${REDIS_HOST} -p ${REDIS_PORT}"
  [[ -n "${REDIS_PASSWORD}" ]] && redis_cmd="${redis_cmd} -a ${REDIS_PASSWORD}"

  local info
  info=$(${redis_cmd} INFO memory 2>/dev/null) || {
    send_alert "critical" "Cannot connect to Redis at ${REDIS_HOST}:${REDIS_PORT}"
    return 1
  }

  local used_memory
  local max_memory
  used_memory=$(echo "${info}" | grep -E '^used_memory:' | cut -d: -f2 | tr -d '\r')
  max_memory=$(echo "${info}" | grep -E '^maxmemory:' | cut -d: -f2 | tr -d '\r')

  if [[ -z "${max_memory}" || "${max_memory}" == "0" ]]; then
    # maxmemory not set — check against system memory
    local total_mem
    total_mem=$(awk '/MemTotal/ {print $2 * 1024}' /proc/meminfo 2>/dev/null || echo "0")
    if [[ "${total_mem}" == "0" ]]; then
      log "Redis memory: ${used_memory} bytes (maxmemory not configured, system memory unknown)"
      return 0
    fi
    max_memory="${total_mem}"
  fi

  local pct=0
  if (( max_memory > 0 )); then
    pct=$(( (used_memory * 100) / max_memory ))
  fi

  local used_human
  used_human=$(echo "${info}" | grep -E '^used_memory_human:' | cut -d: -f2 | tr -d '\r')
  log "Redis memory: ${used_human} (${pct}% of max)"

  if (( pct >= REDIS_THRESHOLD_PERCENT )); then
    send_alert "warning" "Redis memory at ${pct}% (${used_human}). Threshold: ${REDIS_THRESHOLD_PERCENT}%"
    return 1
  fi
  return 0
}

# --- Health Endpoint Check ---
check_health() {
  local status_code
  status_code=$(curl -sf -o /dev/null -w '%{http_code}' --max-time 10 "${HEALTH_URL}" 2>/dev/null) || status_code="000"
  log "Health check: HTTP ${status_code}"
  if [[ "${status_code}" != "200" ]]; then
    send_alert "critical" "Health endpoint returned HTTP ${status_code} (expected 200)"
    return 1
  fi
  return 0
}

# --- Main ---
log "=== System monitoring check ==="
exit_code=0

check_disk   || exit_code=1
check_redis  || exit_code=1
check_health || exit_code=1

if (( exit_code == 0 )); then
  log "All checks passed"
else
  log "Some checks failed — see alerts above"
fi

exit ${exit_code}
