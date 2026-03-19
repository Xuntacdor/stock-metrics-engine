-- =============================================================================
-- Primary SQL Server: Schedule regular backups for log-shipping to secondary
-- Run once on the primary server (stock_db_primary container)
-- =============================================================================

USE master;
GO

-- ── Enable SQL Server Agent (required for jobs) ───────────────────────────────
-- Note: In Docker, you may need to run this manually or via sqlcmd

-- ── Create backup directory ───────────────────────────────────────────────────
DECLARE @BackupDir NVARCHAR(200) = N'/backups';

-- ── Full backup (run manually first time, then weekly) ───────────────────────
-- Execute this SQL to take the initial full backup:
-- BACKUP DATABASE [QuantIQ]
-- TO DISK = N'/backups/QuantIQ_FULL.bak'
-- WITH COMPRESSION, STATS = 10;

-- ── Transaction log backup job (every 5 minutes) ─────────────────────────────
-- Note: SQL Server Express does not support SQL Agent.
-- For Docker environments, use the companion cron script below.
-- For SQL Server Standard/Enterprise, create a SQL Agent job:

-- EXEC msdb.dbo.sp_add_job @job_name = N'QuantIQ_LogShipping_Backup';
-- EXEC msdb.dbo.sp_add_jobstep
--     @job_name  = N'QuantIQ_LogShipping_Backup',
--     @step_name = N'Backup Transaction Log',
--     @command   = N'
--         BACKUP LOG [QuantIQ]
--         TO DISK = N''/backups/QuantIQ_LOG_'' + CONVERT(VARCHAR, GETDATE(), 112) + ''_'' + REPLACE(CONVERT(VARCHAR(8), GETDATE(), 108),'':'','''') + ''.bak''
--         WITH COMPRESSION;
--     ';

-- ── Simple manual log backup script ──────────────────────────────────────────
-- Run this periodically from a cron job or Task Scheduler pointing to sqlcmd:
/*
DECLARE @LogBak NVARCHAR(500) =
    N'/backups/QuantIQ_LOG_' +
    CONVERT(NVARCHAR, GETDATE(), 112) + N'_' +
    REPLACE(CONVERT(NVARCHAR(8), GETDATE(), 108), N':', N'') + N'.bak';

BACKUP LOG [QuantIQ]
TO DISK = @LogBak
WITH COMPRESSION, STATS = 5;
*/

PRINT 'Primary backup configuration script loaded.';
PRINT 'Run the BACKUP DATABASE command manually to create the initial full backup.';
GO
