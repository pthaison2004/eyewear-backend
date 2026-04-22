/*
===========================================================
  VisionCare - Seed Test Data for Commit #3
  Sales Prescription Verification APIs
===========================================================
*/

USE VisionCare;
GO

-----------------------------------------------------------
-- TẠO PRESCRIPTION ORDERS để test Sales APIs
-----------------------------------------------------------

-- Seed PRESCRIPTIONS (link to Customer UserId=5)
SET IDENTITY_INSERT Prescriptions ON;
INSERT INTO Prescriptions (PrescriptionId, CustomerId, OD_Sphere, OD_Cylinder, OD_Axis, OS_Sphere, OS_Cylinder, OS_Axis, PD, Note, IsVerified, IsRejected, CreatedAt) VALUES
-- Prescription 1: Normal - should pass validation
(100, 5, -2.00, -0.50, 180, -1.75, -0.25, 90, 63.00, N'Đơn kính cận thị nhẹ', 0, 0, DATEADD(month, -1, GETDATE())),
-- Prescription 2: Older than 12 months - age warning
(101, 5, -4.50, -1.00, 120, -4.00, -0.75, 60, 62.00, N'Đơn kính cận thị vừa', 0, 0, DATEADD(month, -18, GETDATE())),
-- Prescription 3: Already verified
(102, 5, -1.25, 0.00, 0, -1.00, 0.00, 0, 64.00, N'Đơn kính nhẹ', 1, 0, DATEADD(month, -3, GETDATE())),
-- Prescription 4: Invalid PD (out of range)
(103, 5, -2.50, 0.00, 0, -2.25, 0.00, 0, 40.00, N'PD không hợp lệ', 0, 0, DATEADD(day, -5, GETDATE()));
SET IDENTITY_INSERT Prescriptions OFF;
GO

-- Seed ORDERS (Prescription type) linked to prescriptions
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderId, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentStatus, OrderType, ShippingAddress) VALUES
(200, 5, DATEADD(day, -1, GETDATE()), 1500000, N'Pending', N'Paid', N'Prescription', N'123 Nguyễn Trãi, Q1, TP.HCM'),
(201, 5, DATEADD(day, -5, GETDATE()), 2000000, N'Processing', N'Paid', N'Prescription', N'456 Lê Lợi, Q3, TP.HCM'),
(202, 5, DATEADD(day, -10, GETDATE()), 1800000, N'Pending', N'Paid', N'Prescription', N'789 Trần Hưng Đạo, Q5, TP.HCM');
SET IDENTITY_INSERT Orders OFF;
GO

-- Seed ORDER ITEMS linking orders to prescriptions
INSERT INTO OrderItems (OrderId, VariantId, PrescriptionId, Quantity, UnitPrice) VALUES
(200, 1, 100, 1, 1500000),
(201, 5, 101, 1, 2000000),
(202, 8, 102, 1, 1800000);
GO

-----------------------------------------------------------
-- VALIDATION RULES (already seeded in code, but ensure they exist)
-----------------------------------------------------------
SET IDENTITY_INSERT PrescriptionValidationRules ON;
INSERT INTO PrescriptionValidationRules (RuleId, RuleName, RuleType, MinValue, MaxValue, IsActive, Description, SortOrder, CreatedAt)
SELECT 1, N'Max Sphere Value', 'sphere_max', NULL, -20, 1, N'Cận thị tối đa -20.00', 1, GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM PrescriptionValidationRules WHERE RuleId = 1);
INSERT INTO PrescriptionValidationRules (RuleId, RuleName, RuleType, MinValue, MaxValue, IsActive, Description, SortOrder, CreatedAt)
SELECT 2, N'Max Cylinder Value', 'cylinder_max', NULL, -6, 1, N'Loạn thị tối đa -6.00', 2, GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM PrescriptionValidationRules WHERE RuleId = 2);
INSERT INTO PrescriptionValidationRules (RuleId, RuleName, RuleType, MinValue, MaxValue, IsActive, Description, SortOrder, CreatedAt)
SELECT 3, N'PD Range', 'pd_range', 50, 80, 1, N'PD hợp lệ 50-80mm', 3, GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM PrescriptionValidationRules WHERE RuleId = 3);
INSERT INTO PrescriptionValidationRules (RuleId, RuleName, RuleType, MinValue, MaxValue, IsActive, Description, SortOrder, CreatedAt)
SELECT 4, N'Min Age', 'min_age', 5, NULL, 1, N'Độ tuổi tối thiểu 5 tuổi', 4, GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM PrescriptionValidationRules WHERE RuleId = 4);
INSERT INTO PrescriptionValidationRules (RuleId, RuleName, RuleType, MinValue, MaxValue, IsActive, Description, SortOrder, CreatedAt)
SELECT 5, N'Expiry Months', 'expiry_months', NULL, 24, 1, N'Đơn kính có hiệu lực trong 24 tháng', 5, GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM PrescriptionValidationRules WHERE RuleId = 5);
INSERT INTO PrescriptionValidationRules (RuleId, RuleName, RuleType, MinValue, MaxValue, IsActive, Description, SortOrder, CreatedAt)
SELECT 6, N'Prescription Age Warning', 'age_warning', NULL, 12, 1, N'Cảnh báo nếu đơn kính trên 12 tháng', 6, GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM PrescriptionValidationRules WHERE RuleId = 6);
SET IDENTITY_INSERT PrescriptionValidationRules OFF;
GO

PRINT '=======================================================';
PRINT 'COMMIT #3 - SEED TEST DATA';
PRINT '=======================================================';
PRINT '';
PRINT 'SALES ACCOUNT:';
PRINT '  sales@visioncare.com / 123456 (Role=Sales, RoleId=3)';
PRINT '';
PRINT 'CUSTOMER ACCOUNT:';
PRINT '  customer@visioncare.com / 123456 (UserId=5)';
PRINT '';
PRINT 'TEST PRESCRIPTIONS:';
PRINT '  PrescriptionId=100: Normal prescription (1 month old)';
PRINT '  PrescriptionId=101: Old prescription (18 months - age warning)';
PRINT '  PrescriptionId=102: Already verified';
PRINT '  PrescriptionId=103: Invalid PD (40mm out of range)';
PRINT '';
PRINT 'TEST ORDERS:';
PRINT '  OrderId=200: Pending, PrescriptionId=100';
PRINT '  OrderId=201: Processing, PrescriptionId=101';
PRINT '  OrderId=202: Pending, PrescriptionId=102';
PRINT '=======================================================';
GO
