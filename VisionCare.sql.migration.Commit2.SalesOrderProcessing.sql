-- ============================================================
-- Commit #2: Sales Order Processing
-- Adds StaffNote column if not exists (Order table)
-- Seeds test orders for Sales API testing
-- ============================================================

USE VisionCare;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'StaffNote')
    ALTER TABLE Orders ADD StaffNote NVARCHAR(MAX) NULL;
GO

-- Seed test orders for Sales API (if not exists)
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = 100)
BEGIN
    INSERT INTO Orders (OrderId, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentStatus, OrderType, ShippingAddress, StaffNote)
    VALUES
    (100, 5, DATEADD(day, -2, GETDATE()), 1500000, N'Pending', N'Paid', N'Ready-made', N'123 Nguyễn Trãi, Q1, TP.HCM', NULL),
    (101, 5, DATEADD(day, -3, GETDATE()), 2200000, N'Pending', N'Unpaid', N'Prescription', N'456 Lê Văn Việt, Q9, TP.HCM', NULL),
    (102, 5, DATEADD(day, -5, GETDATE()), 800000, N'Confirmed', N'Paid', N'Ready-made', N'789 Trần Hưng Đạo, Q5, TP.HCM', NULL),
    (103, 5, DATEADD(day, -1, GETDATE()), 3500000, N'Pending', N'Paid', N'Ready-made', N'321 Phạm Văn Đồng, Q.Gò Vấp', NULL);

    INSERT INTO OrderItems (OrderId, VariantId, Quantity, UnitPrice)
    VALUES
    (100, 1, 1, 1500000),
    (101, 5, 1, 2200000),
    (102, 8, 1, 800000),
    (103, 6, 2, 1750000);
END
GO

PRINT 'Commit #2: Sales Order Processing migration completed.';
GO