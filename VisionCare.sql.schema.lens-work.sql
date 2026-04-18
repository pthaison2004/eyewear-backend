-- VisionCare Schema: Lens Work Management (Commit 6)
-- Add lens tracking fields to OrderItems table
-- Run via: sqlcmd -S <server> -d <database> -i VisionCare.sql.schema.lens-work.sql

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'AssignedLensMakerId' AND Object_ID = OBJECT_ID('dbo.OrderItems'))
BEGIN
    ALTER TABLE dbo.OrderItems ADD AssignedLensMakerId INT NULL;
    ALTER TABLE dbo.OrderItems ADD LensCutCompletedAt DATETIME2 NULL;
    ALTER TABLE dbo.OrderItems ADD LensCutNote NVARCHAR(500) NULL;
END
GO
PRINT 'Lens work columns added to OrderItems.';
