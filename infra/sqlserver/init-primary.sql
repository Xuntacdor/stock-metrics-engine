-- =============================================================================
-- Primary SQL Server: set FULL recovery model so transaction-log backups work.
-- This script is executed once by the db-backup sidecar on first start.
-- The actual backup scheduling is handled by backup-loop.sh.
-- =============================================================================

USE master;
GO

-- Switch the database to FULL recovery (SIMPLE does not support log shipping)
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'QuantIQ_DB')
BEGIN
    ALTER DATABASE [QuantIQ_DB] SET RECOVERY FULL;
    PRINT 'QuantIQ_DB recovery model set to FULL.';
END
ELSE
BEGIN
    PRINT 'QuantIQ_DB not found — will be set to FULL when the app creates it.';
END
GO

PRINT 'init-primary.sql complete.';
GO
