#!/bin/bash
# =============================================================================
# Primary backup loop — runs as a sidecar to stock_db_primary
# Takes an initial full backup, then a transaction-log backup every 5 minutes.
# Backups land in the shared /backups volume that the secondary also mounts.
# =============================================================================

set -euo pipefail

SA_PASSWORD="${MSSQL_SA_PASSWORD}"
DB_NAME="${DB_NAME:-QuantIQ_DB}"
BACKUP_DIR="${BACKUP_DIR:-/backups}"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
SQLCMD_OPTS="-S localhost,1433 -U sa -P ${SA_PASSWORD} -C -b"

mkdir -p "$BACKUP_DIR"

# ── Wait for SQL Server to accept connections ─────────────────────────────────
echo "[$(date -u +%FT%TZ)] Waiting for SQL Server..."
for i in $(seq 1 40); do
  if $SQLCMD $SQLCMD_OPTS -Q "SELECT 1" > /dev/null 2>&1; then
    echo "[$(date -u +%FT%TZ)] SQL Server is ready."
    break
  fi
  echo "[$(date -u +%FT%TZ)] Attempt $i/40 — retrying in 5 s..."
  sleep 5
done

# ── Switch DB to FULL recovery model (required for log backups) ───────────────
$SQLCMD $SQLCMD_OPTS -Q "
  IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '${DB_NAME}')
  BEGIN
    ALTER DATABASE [${DB_NAME}] SET RECOVERY FULL;
    PRINT 'Recovery model set to FULL.';
  END
  ELSE
    PRINT 'Database ${DB_NAME} does not exist yet — skipping recovery model change.';
"

# ── Initial full backup (skip if one already exists) ─────────────────────────
FULL_BAK="${BACKUP_DIR}/${DB_NAME}_FULL.bak"

if [ ! -f "$FULL_BAK" ]; then
  echo "[$(date -u +%FT%TZ)] Taking initial full backup → $FULL_BAK"
  $SQLCMD $SQLCMD_OPTS -Q "
    BACKUP DATABASE [${DB_NAME}]
    TO DISK = N'${FULL_BAK}'
    WITH COMPRESSION, STATS = 10;
    PRINT 'Full backup complete.';
  "
  echo "[$(date -u +%FT%TZ)] Full backup done."
else
  echo "[$(date -u +%FT%TZ)] Full backup already exists — skipping."
fi

# ── Periodic transaction-log backup loop (every 5 minutes) ───────────────────
echo "[$(date -u +%FT%TZ)] Starting log-backup loop (interval: 5 min)..."
while true; do
  sleep 300
  TIMESTAMP=$(date -u +%Y%m%d_%H%M%S)
  LOG_BAK="${BACKUP_DIR}/${DB_NAME}_LOG_${TIMESTAMP}.bak"
  echo "[$(date -u +%FT%TZ)] Log backup → $LOG_BAK"
  $SQLCMD $SQLCMD_OPTS -Q "
    BACKUP LOG [${DB_NAME}]
    TO DISK = N'${LOG_BAK}'
    WITH COMPRESSION, STATS = 5;
  " && echo "[$(date -u +%FT%TZ)] Log backup OK." \
    || echo "[$(date -u +%FT%TZ)] WARNING: Log backup failed — will retry next cycle."

  # Remove log files older than 24 h to prevent disk bloat
  find "$BACKUP_DIR" -name "${DB_NAME}_LOG_*.bak" -mmin +1440 -delete 2>/dev/null || true
done
