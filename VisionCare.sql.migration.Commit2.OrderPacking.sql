-- ============================================================
-- Commit #2: Order Packing
-- Adds PackedAt and PackedBy columns to Orders table
-- Run on database: VisionCare
-- ============================================================

USE VisionCare;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'PackedAt')
    ALTER TABLE Orders ADD PackedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'PackedBy')
    ALTER TABLE Orders ADD PackedBy INT NULL;
GO

PRINT 'Commit #2 migration completed: PackedAt and PackedBy columns added to Orders table.';
GO
