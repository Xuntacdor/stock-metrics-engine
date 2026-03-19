#!/bin/bash
# =============================================================================
# SQL Server Master → Slave Log-Shipping / Backup-Restore Replication
# =============================================================================
# This script runs on the secondary container to set up an automated
# backup-restore cycle from the primary.
#
# HOW IT WORKS:
#   1. Primary regularly backs up the QuantIQ database to a shared volume.
#   2. This script restores those backups on the secondary with NORECOVERY,
#      applying transaction log backups incrementally (log shipping).
#
# Prerequisites:
#   - Both containers share a `/backups` volume (add in docker-compose if needed)
#   - The primary must run init-primary.sql to enable backup jobs
#
# Usage (run inside secondary container):
#   docker exec stock_db_secondary bash /init-replication.sh
# =============================================================================

set -e

PRIMARY_HOST="${PRIMARY_HOST:-sqlserver,1433}"
SA_PASSWORD="${SA_PASS:-${MSSQL_SA_PASSWORD}}"
DB_NAME="${DB_NAME:-QuantIQ}"
BACKUP_DIR="${BACKUP_DIR:-/backups}"
SECONDARY_RESTORE_DIR="${SECONDARY_RESTORE_DIR:-/restore}"

SQLCMD="/opt/mssql-tools/bin/sqlcmd"

echo "[$(date)] Starting SQL Server replication setup..."
echo "[$(date)] Primary: $PRIMARY_HOST | DB: $DB_NAME"

# ── Wait for secondary SQL Server to start ──────────────────────────────────
echo "[$(date)] Waiting for secondary SQL Server..."
for i in {1..30}; do
  if $SQLCMD -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT 1" > /dev/null 2>&1; then
    echo "[$(date)] Secondary SQL Server is up."
    break
  fi
  echo "[$(date)] Waiting... ($i/30)"
  sleep 5
done

# ── Create restore directory ─────────────────────────────────────────────────
mkdir -p "$SECONDARY_RESTORE_DIR"
mkdir -p "$BACKUP_DIR"

# ── Restore full backup (first time or after reset) ──────────────────────────
FULL_BAK="$BACKUP_DIR/${DB_NAME}_FULL.bak"

if [ -f "$FULL_BAK" ]; then
  echo "[$(date)] Found full backup: $FULL_BAK. Restoring..."
  $SQLCMD -S localhost -U sa -P "$SA_PASSWORD" -Q "
    IF DB_ID('${DB_NAME}_Replica') IS NOT NULL
      DROP DATABASE [${DB_NAME}_Replica];

    RESTORE DATABASE [${DB_NAME}_Replica]
    FROM DISK = N'$FULL_BAK'
    WITH NORECOVERY,
         MOVE '${DB_NAME}' TO '/var/opt/mssql/data/${DB_NAME}_Replica.mdf',
         MOVE '${DB_NAME}_log' TO '/var/opt/mssql/data/${DB_NAME}_Replica.ldf',
         REPLACE, STATS = 10;
    PRINT 'Full restore complete (NORECOVERY).';
  "
  echo "[$(date)] Full backup restored. Applying transaction logs..."
else
  echo "[$(date)] WARNING: No full backup found at $FULL_BAK."
  echo "[$(date)] Run a full backup on the primary first:"
  echo "  BACKUP DATABASE [$DB_NAME] TO DISK = N'$FULL_BAK' WITH COMPRESSION;"
  exit 1
fi

# ── Apply transaction log backups in a loop ───────────────────────────────────
echo "[$(date)] Starting continuous log-shipping loop (every 60s)..."
while true; do
  for LOG_BAK in $(ls "$BACKUP_DIR"/${DB_NAME}_LOG_*.bak 2>/dev/null | sort); do
    echo "[$(date)] Applying log: $LOG_BAK"
    $SQLCMD -S localhost -U sa -P "$SA_PASSWORD" -Q "
      RESTORE LOG [${DB_NAME}_Replica]
      FROM DISK = N'$LOG_BAK'
      WITH NORECOVERY, STATS = 5;
    " && rm "$LOG_BAK"
  done
  sleep 60
done
