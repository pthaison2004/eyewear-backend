-- Add DepositRatio and MinDepositAmount columns to PreOrderCampaigns
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PreOrderCampaigns') AND name = 'DepositRatio')
BEGIN
    ALTER TABLE dbo.PreOrderCampaigns
    ADD DepositRatio DECIMAL(5,4) NULL;
    PRINT 'Added DepositRatio column.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PreOrderCampaigns') AND name = 'MinDepositAmount')
BEGIN
    ALTER TABLE dbo.PreOrderCampaigns
    ADD MinDepositAmount DECIMAL(18,0) NULL;
    PRINT 'Added MinDepositAmount column.';
END
GO
