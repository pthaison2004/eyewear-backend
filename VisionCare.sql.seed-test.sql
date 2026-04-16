/*
===========================================================
  VISION CARE - SEED DATA CHO TESTING ORDER & CART API
  Chạy SAU khi đã chạy VisionCare.sql và migration Carts
===========================================================
*/

USE VisionCare;
GO

-----------------------------------------------------------
-- XÓA DỮ LIỆU CŨ (để test sạch)
-----------------------------------------------------------
DELETE FROM CartItems;
DELETE FROM Carts;
DELETE FROM Orders;
DELETE FROM OrderItems;
DELETE FROM Products;
DELETE FROM Categories;
DBCC CHECKIDENT ('Products', RESEED, 0);
DBCC CHECKIDENT ('Categories', RESEED, 0);
GO

-----------------------------------------------------------
-- 1. SEED CATEGORIES
-----------------------------------------------------------
SET IDENTITY_INSERT Categories ON;
INSERT INTO Categories (CategoryId, CategoryName, Description) VALUES
(1, N'Kính gọng tròn', N'Kính gọng tròn classic'),
(2, N'Kính gọng vuông', N'Kính gọng vuông hiện đại'),
(3, N'Kính gọng cat-eye', N'Kính gọng cat-eye thời trang'),
(4, N'Kính râm', N'Kính râm nam nữ'),
(5, N'Tròng kính', N'Tròng kính các loại');
SET IDENTITY_INSERT Categories OFF;
GO

-----------------------------------------------------------
-- 2. SEED PRODUCTS
-----------------------------------------------------------
SET IDENTITY_INSERT Products ON;
INSERT INTO Products (ProductId, CategoryId, ProductName, Brand, Description, BasePrice, IsPreOrder) VALUES
-- Gọng tròn (CategoryId=1)
(1, 1, N'Vision Round X1', N'VisionCare', N'Gọng tròn classic, nhẹ và thoải mái', 800000, 0),
(2, 1, N'Vision Round X2', N'VisionCare', N'Gọng tròn phiên bản premium', 1200000, 0),
-- Gọng vuông (CategoryId=2)
(3, 2, N'Vision Square A1', N'VisionCare', N'Gọng vuông nam nữ', 900000, 0),
(4, 2, N'Vision Square A2', N'VisionCare', N'Gọng vuông phiên bản metal', 1500000, 0),
(5, 2, N'Vision Square A3', N'VisionCare', N'Gọng vuông oversized', 1100000, 0),
-- Gọng cat-eye (CategoryId=3)
(6, 3, N'Vision CatEye B1', N'VisionCare', N'Gọng cat-eye nữ tính', 950000, 0),
(7, 3, N'Vision CatEye B2', N'VisionCare', N'Gọng cat-eye vintage', 1300000, 0),
-- Kính râm (CategoryId=4)
(8, 4, N'Vision Sun S1', N'VisionCare', N'Kính râm UV400', 700000, 0),
(9, 4, N'Vision Sun S2', N'VisionCare', N'Kính râm polarised', 1400000, 0);
SET IDENTITY_INSERT Products OFF;
GO

-----------------------------------------------------------
-- 3. SEED PRODUCT VARIANTS
-----------------------------------------------------------
SET IDENTITY_INSERT ProductVariants ON;
INSERT INTO ProductVariants (VariantId, ProductId, Color, Size, SKU, StockQuantity, AdditionalPrice) VALUES
-- Product 1: Vision Round X1 - 3 variants
(1,  1, N'Đen',    N'M',  N'VRX1-BLK-M',   10,  0),
(2,  1, N'Đen',    N'L',  N'VRX1-BLK-L',   5,   0),
(3,  1, N'Vàng',   N'M',  N'VRX1-YLW-M',   3,   50000),
(4,  1, N'Vàng',   N'L',  N'VRX1-YLW-L',   0,   50000),   -- HẾT HÀNG (stock=0)

-- Product 2: Vision Round X2 - 2 variants
(5,  2, N'Đen',    N'M',  N'VRX2-BLK-M',   8,   0),
(6,  2, N'Đen',    N'L',  N'VRX2-BLK-L',   20,  0),
(7,  2, N'Xám',    N'M',  N'VRX2-GRY-M',   15,  100000),

-- Product 3: Vision Square A1 - 2 variants
(8,  3, N'Đen',    N'M',  N'VSA1-BLK-M',   12,  0),
(9,  3, N'Đen',    N'L',  N'VSA1-BLK-L',   6,   0),
(10, 3, N'Nâu',    N'M',  N'VSA1-BRN-M',   4,   80000),

-- Product 4: Vision Square A2 - 2 variants
(11, 4, N'Bạc',    N'M',  N'VSA2-SLV-M',   7,   0),
(12, 4, N'Bạc',    N'L',  N'VSA2-SLV-L',   3,   0),

-- Product 5: Vision Square A3 - 1 variant
(13, 5, N'Đen',    N'M',  N'VSA3-BLK-M',   0,   0),       -- HẾT HÀNG (stock=0)

-- Product 6: Vision CatEye B1 - 2 variants
(14, 6, N'Hồng',   N'M',  N'VCB1-PNK-M',   9,   0),
(15, 6, N'Hồng',   N'L',  N'VCB1-PNK-L',   5,   0),
(16, 6, N'Đen',    N'M',  N'VCB1-BLK-M',   11,  0),

-- Product 7: Vision CatEye B2 - 1 variant
(17, 7, N'Đỏ',     N'M',  N'VCB2-RED-M',   6,   0),

-- Product 8: Vision Sun S1 - 2 variants
(18, 8, N'Đen',    N'M',  N'VSS1-BLK-M',   20,  0),
(19, 8, N'Đen',    N'L',  N'VSS1-BLK-L',   10,  0),

-- Product 9: Vision Sun S2 - 1 variant
(20, 9, N'Nâu',    N'M',  N'VSS2-BRN-M',   4,   0);
SET IDENTITY_INSERT ProductVariants OFF;
GO

-----------------------------------------------------------
-- 4. SEED PRESCRIPTIONS cho customer
-----------------------------------------------------------
SET IDENTITY_INSERT Prescriptions ON;
INSERT INTO Prescriptions (PrescriptionId, CustomerId, OD_Sphere, OD_Cylinder, OD_Axis, OS_Sphere, OS_Cylinder, OS_Axis, PD, Note) VALUES
(1, 5, -2.00, -0.50, 180, -1.75, -0.25, 90, 63.0, N'Đơn kính cận thị nhẹ'),
(2, 5, -4.50, -1.00, 120, -4.00, -0.75, 60, 62.0, N'Đơn kính cận thị vừa');
SET IDENTITY_INSERT Prescriptions OFF;
GO

-----------------------------------------------------------
-- 5. TÓM TẮT DỮ LIỆU TEST
-----------------------------------------------------------
PRINT '=======================================================';
PRINT 'SEED DATA TEST - TÓM TẮT';
PRINT '=======================================================';
PRINT '';
PRINT 'TÀI KHOẢN TEST (password: 123456):';
PRINT '  - customer@visioncare.com (UserId=5, Role=Customer)';
PRINT '';
PRINT 'SẢN PHẨM CÓ SẴN:';
PRINT '  Product 1 (VRX1) - gọng tròn:';
PRINT '    - VariantId=1: Đen/M, stock=10, price=800,000';
PRINT '    - VariantId=2: Đen/L, stock=5,  price=800,000';
PRINT '    - VariantId=3: Vàng/M, stock=3,  price=850,000';
PRINT '    - VariantId=4: Vàng/L, stock=0,  price=850,000 [HẾT HÀNG]';
PRINT '  Product 2 (VRX2) - gọng tròn premium:';
PRINT '    - VariantId=5: Đen/M, stock=8,  price=1,200,000';
PRINT '  Product 5 (VSA3) - gọng vuông:';
PRINT '    - VariantId=13: Đen/M, stock=0,  price=1,100,000 [HẾT HÀNG]';
PRINT '';
PRINT 'ĐƠN THUỐC (cho customer UserId=5):';
PRINT '  - PrescriptionId=1: OD -2.00/-0.50, OS -1.75/-0.25, PD 63';
PRINT '  - PrescriptionId=2: OD -4.50/-1.00, OS -4.00/-0.75, PD 62';
PRINT '';
PRINT '=======================================================';
PRINT 'TEST CASES';
PRINT '=======================================================';
PRINT '';
PRINT 'TEST 1 - Cart 1 sản phẩm (OK):';
PRINT '  POST /api/cart/items { variantId=1, quantity=2 }';
PRINT '';
PRINT 'TEST 2 - Cart 2 sản phẩm (OK):';
PRINT '  POST /api/cart/items { variantId=1, quantity=2 }';
PRINT '  POST /api/cart/items { variantId=5, quantity=1 }';
PRINT '';
PRINT 'TEST 3 - Cart 2 sản phẩm nhưng 1 HẾT HÀNG:';
PRINT '  POST /api/cart/items { variantId=1, quantity=2 }';
PRINT '  POST /api/cart/items { variantId=4, quantity=1 } -- hết stock';
PRINT '  --> Lỗi khi tạo order';
GO
