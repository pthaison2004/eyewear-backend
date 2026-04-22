-- Pre-order Operations Schema Seed
-- This script seeds test data for pre-order receive/fulfill workflows.
-- All required tables (PreOrderCampaigns, PreOrderReservations, PreOrderCampaignProducts,
-- Inventories, StockMovements, Warehouses) are assumed to already exist from prior commits.

-- Seed a test pre-order campaign
IF NOT EXISTS (SELECT 1 FROM dbo.PreOrderCampaigns)
BEGIN
    INSERT INTO dbo.PreOrderCampaigns
        (CampaignCode, CampaignName, StartDate, EndDate, ReleaseDate, DiscountPercent, MaxQuantity, MaxPerCustomer, Status, IsFeatured, CurrentReserved)
    VALUES
        ('PO-2026-001', N'Khuyến mãi mùa hè 2026', '2026-03-01', '2026-06-30', '2026-04-15', 15, 100, 3, 'active', 1, 50);

    PRINT 'Pre-order campaign seeded.';
END
GO
