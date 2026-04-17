/*
===========================================================
  VISION CARE - SEED DATA FOR OPS ORDER RETRIEVAL APIs
  Run AFTER VisionCare.sql (database initialization)
  This script creates test orders across all types and statuses
===========================================================
*/

USE VisionCare;
GO

-----------------------------------------------------------
-- CLEANUP OLD OPS TEST DATA
-----------------------------------------------------------
DELETE FROM OrderItems WHERE OrderId >= 1;
DELETE FROM Orders;
DBCC CHECKIDENT ('Orders', RESEED, 0);
GO

-----------------------------------------------------------
-- READY-MADE ORDERS (OrderType = 'Direct')
-- Using VariantIds that exist in the test seed data
-----------------------------------------------------------

-- RM-001: Pending + Unpaid
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderId, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentStatus, OrderType, ShippingAddress, TrackingNumber, StaffNote)
VALUES (1, 5, DATEADD(DAY, -10, GETUTCDATE()), 1600000.00, N'Pending', N'Unpaid', N'Direct',
    N'123 Nguyễn Trãi, Quận 1, TP.HCM', NULL, NULL);
SET IDENTITY_INSERT Orders OFF;
GO

-- RM-002: Processing + Paid
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderId, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentStatus, OrderType, ShippingAddress, TrackingNumber, StaffNote)
VALUES (2, 5, DATEADD(DAY, -8, GETUTCDATE()), 2400000.00, N'Processing', N'Paid', N'Direct',
    N'456 Lê Văn Việt, Quận 9, TP.HCM', 'VC-TRK-002', NULL);
SET IDENTITY_INSERT Orders OFF;
GO

-- RM-003: Packed + Paid
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderId, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentStatus, OrderType, ShippingAddress, TrackingNumber, StaffNote)
VALUES (3, 5, DATEADD(DAY, -5, GETUTCDATE()), 950000.00, N'Packed', N'Paid', N'Direct',
    N'789 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM', 'VC-TRK-003', NULL);
SET IDENTITY_INSERT Orders OFF;
GO

-- RM-004: Confirmed + Unpaid (older)
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderId, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentStatus, OrderType, ShippingAddress, TrackingNumber, StaffNote)
VALUES (4, 5, DATEADD(DAY, -30, GETUTCDATE()), 1500000.00, N'Confirmed', N'Unpaid', N'Direct',
    N'321 Phạm Văn Đồng, Quận Gò Vấp, TP.HCM', NULL, NULL);
SET IDENTITY_INSERT Orders OFF;
GO

-----------------------------------------------------------
-- PRESCRIPTION ORDERS (OrderType = 'Prescription')
-----------------------------------------------------------

-- RX-001: Pending + Unpaid
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderId, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentStatus, OrderType, ShippingAddress, TrackingNumber, StaffNote)
VALUES (5, 5, DATEADD(DAY, -7, GETUTCDATE()), 3200000.00, N'Pending', N'Unpaid', N'Prescription',
    N'555 Nguyễn Oanh, Quận Gò Vấp, TP.HCM', NULL, NULL);
SET IDENTITY_INSERT Orders OFF;
GO

-- RX-002: Processing + Paid
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderId, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentStatus, OrderType, ShippingAddress, TrackingNumber, StaffNote)
VALUES (6, 5, DATEADD(DAY, -3, GETUTCDATE()), 4800000.00, N'Processing', N'Paid', N'Prescription',
    N'777 Phan Văn Trị, Quận Bình Thạnh, TP.HCM', 'VC-TRK-006', NULL);
SET IDENTITY_INSERT Orders OFF;
GO

-- RX-003: Confirmed + Refunded (for testing Refunded filter)
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderId, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentStatus, OrderType, ShippingAddress, TrackingNumber, StaffNote)
VALUES (7, 5, DATEADD(DAY, -20, GETUTCDATE()), 2100000.00, N'Cancelled', N'Refunded', N'Prescription',
    N'999 Trường Chinh, Quận Tân Bình, TP.HCM', NULL, N'Khách hủy đơn');
SET IDENTITY_INSERT Orders OFF;
GO

-----------------------------------------------------------
-- PRE-ORDER ORDERS (OrderType = 'Pre-order')
-----------------------------------------------------------

-- PO-001: Pending + Unpaid
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderId, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentStatus, OrderType, ShippingAddress, TrackingNumber, StaffNote)
VALUES (8, 5, DATEADD(DAY, -2, GETUTCDATE()), 5600000.00, N'Pending', N'Unpaid', N'Pre-order',
    N'111 Pasteur, Quận 3, TP.HCM', NULL, NULL);
SET IDENTITY_INSERT Orders OFF;
GO

-- PO-002: Processing + Paid
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderId, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentStatus, OrderType, ShippingAddress, TrackingNumber, StaffNote)
VALUES (9, 5, DATEADD(DAY, -15, GETUTCDATE()), 3800000.00, N'Processing', N'Paid', N'Pre-order',
    N'222 Võ Văn Tần, Quận 3, TP.HCM', 'VC-TRK-009', NULL);
SET IDENTITY_INSERT Orders OFF;
GO

-- PO-003: Shipped + Paid (older)
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderId, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentStatus, OrderType, ShippingAddress, TrackingNumber, StaffNote)
VALUES (10, 5, DATEADD(DAY, -25, GETUTCDATE()), 2700000.00, N'Shipped', N'Paid', N'Pre-order',
    N'333 Nguyễn Đình Chiểu, Quận 1, TP.HCM', 'VC-TRK-010', NULL);
SET IDENTITY_INSERT Orders OFF;
GO

-----------------------------------------------------------
-- ORDER ITEMS (linking to existing ProductVariants)
-----------------------------------------------------------

-- RM-001 items (OrderId=1): 2 products
INSERT INTO OrderItems (OrderId, VariantId, PrescriptionId, Quantity, UnitPrice) VALUES
(1, 1, NULL, 1, 800000.00),   -- Vision Round X1 Đen/M
(1, 3, NULL, 1, 800000.00);  -- Vision Round X1 Vàng/M

-- RM-002 items (OrderId=2): 2 products
INSERT INTO OrderItems (OrderId, VariantId, PrescriptionId, Quantity, UnitPrice) VALUES
(2, 6, NULL, 1, 1200000.00), -- Vision Round X2 Đen/L
(2, 10, NULL, 1, 1200000.00); -- Vision Square A1 Nâu/M

-- RM-003 items (OrderId=3): 1 product
INSERT INTO OrderItems (OrderId, VariantId, PrescriptionId, Quantity, UnitPrice) VALUES
(3, 14, NULL, 1, 950000.00); -- Vision CatEye B1 Hồng/M

-- RM-004 items (OrderId=4): 1 product
INSERT INTO OrderItems (OrderId, VariantId, PrescriptionId, Quantity, UnitPrice) VALUES
(4, 8, NULL, 1, 1500000.00); -- Vision Square A2 Bạc/M

-- RX-001 items (OrderId=5): Prescription order with PrescriptionId=1
INSERT INTO OrderItems (OrderId, VariantId, PrescriptionId, Quantity, UnitPrice) VALUES
(5, 1, 1, 1, 1600000.00),    -- Vision Round X1 + Prescription
(5, 18, NULL, 1, 1600000.00); -- Vision Sun S1 Đen/M

-- RX-002 items (OrderId=6): Prescription order with PrescriptionId=2
INSERT INTO OrderItems (OrderId, VariantId, PrescriptionId, Quantity, UnitPrice) VALUES
(6, 2, 2, 1, 2400000.00),    -- Vision Round X2 Đen/M + Prescription
(6, 19, NULL, 1, 2400000.00); -- Vision Sun S1 Đen/L

-- RX-003 items (OrderId=7)
INSERT INTO OrderItems (OrderId, VariantId, PrescriptionId, Quantity, UnitPrice) VALUES
(7, 5, 1, 1, 2100000.00);   -- Vision Round X2 Đen/M + Prescription

-- PO-001 items (OrderId=8): Pre-order
INSERT INTO OrderItems (OrderId, VariantId, PrescriptionId, Quantity, UnitPrice) VALUES
(8, 17, NULL, 2, 2800000.00); -- Vision CatEye B2 Đỏ/M x2

-- PO-002 items (OrderId=9): Pre-order
INSERT INTO OrderItems (OrderId, VariantId, PrescriptionId, Quantity, UnitPrice) VALUES
(9, 11, NULL, 1, 1900000.00), -- Vision Square A2 Bạc/M
(9, 16, NULL, 1, 1900000.00); -- Vision CatEye B1 Đen/M

-- PO-003 items (OrderId=10): Pre-order
INSERT INTO OrderItems (OrderId, VariantId, PrescriptionId, Quantity, UnitPrice) VALUES
(10, 9, NULL, 1, 1350000.00), -- Vision Square A1 Đen/L
(10, 12, NULL, 1, 1350000.00); -- Vision Square A2 Bạc/L

GO

-----------------------------------------------------------
-- VERIFICATION
-----------------------------------------------------------
PRINT '=======================================================';
PRINT 'OPS ORDERS SEED - VERIFICATION';
PRINT '=======================================================';
PRINT '';
PRINT 'Order Summary by Type and Status:';
SELECT
    OrderType,
    OrderStatus,
    PaymentStatus,
    COUNT(*) AS OrderCount,
    SUM(TotalAmount) AS TotalRevenue
FROM Orders
GROUP BY OrderType, OrderStatus, PaymentStatus
ORDER BY OrderType, OrderStatus;
PRINT '';
PRINT 'All Orders:';
SELECT
    OrderId,
    'ORD-' + RIGHT('000000' + CAST(OrderId AS VARCHAR(6)), 6) AS OrderCode,
    OrderType,
    OrderStatus,
    PaymentStatus,
    TotalAmount,
    OrderDate
FROM Orders
ORDER BY OrderId;
PRINT '';
PRINT 'Order Items Count per Order:';
SELECT
    o.OrderId,
    'ORD-' + RIGHT('000000' + CAST(o.OrderId AS VARCHAR(6)), 6) AS OrderCode,
    o.OrderType,
    COUNT(oi.OrderItemId) AS ItemCount
FROM Orders o
LEFT JOIN OrderItems oi ON o.OrderId = oi.OrderId
GROUP BY o.OrderId, o.OrderType
ORDER BY o.OrderId;
PRINT '';
PRINT '=======================================================';
PRINT 'SEED COMPLETE - 10 test orders created';
PRINT '=======================================================';
GO
