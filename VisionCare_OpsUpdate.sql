USE master;
GO

USE VisionCare;
GO

PRINT '--- START: BẢN VÁ LỖI SCHEMA MODULE OPERATIONS ---';

-- 1. Bảng Suppliers: Thiếu cột UpdatedAt
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Suppliers') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE Suppliers ADD UpdatedAt DATETIME2 NULL;
    PRINT 'Đã thêm UpdatedAt vào Suppliers.';
END

-- 2. Bảng Warehouses: Thiếu thông tin địa chỉ chi tiết
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Warehouses') AND name = 'ProvinceCode')
BEGIN
    ALTER TABLE Warehouses ADD ProvinceCode NVARCHAR(50) NULL;
    ALTER TABLE Warehouses ADD DistrictCode NVARCHAR(50) NULL;
    ALTER TABLE Warehouses ADD WardCode NVARCHAR(50) NULL;
    ALTER TABLE Warehouses ADD WardName NVARCHAR(100) NULL;
    ALTER TABLE Warehouses ADD Email NVARCHAR(255) NULL;
    PRINT 'Đã thêm thông tin hành chính vào Warehouses.';
END

-- 3. Bảng Inventories: Thiếu nhiều trường về tình trạng kho
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Inventories') AND name = 'QuantityDefective')
BEGIN
    ALTER TABLE Inventories ADD QuantityDefective INT NOT NULL DEFAULT 0;
    ALTER TABLE Inventories ADD QuantityTransit INT NOT NULL DEFAULT 0;
    ALTER TABLE Inventories ADD BatchNumber NVARCHAR(100) NULL;
    ALTER TABLE Inventories ADD ManufacturingDate DATETIME2 NULL;
    ALTER TABLE Inventories ADD ExpiryDate DATETIME2 NULL;
    ALTER TABLE Inventories ADD LastCountAt DATETIME2 NULL;
    ALTER TABLE Inventories ADD LastReplenishAt DATETIME2 NULL;
    PRINT 'Đã thêm thông số nâng cao vào Inventories.';
END

-- 4. Bảng StockMovements: Thiếu QuantityBefore, QuantityAfter, Reason, StaffNote
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'StockMovements') AND name = 'QuantityBefore')
BEGIN
    ALTER TABLE StockMovements ADD QuantityBefore INT NOT NULL DEFAULT 0;
    ALTER TABLE StockMovements ADD QuantityAfter INT NOT NULL DEFAULT 0;
    ALTER TABLE StockMovements ADD Reason NVARCHAR(500) NULL;
    ALTER TABLE StockMovements ADD StaffNote NVARCHAR(MAX) NULL;
    PRINT 'Đã thêm đối soát số lượng và ghi chú vào StockMovements.';
END

-- 5. Bảng ShippingMethods: Thiếu nhiều thuật toán tính phí vận chuyển
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ShippingMethods') AND name = 'FeePerKg')
BEGIN
    ALTER TABLE ShippingMethods ADD FeePerKg DECIMAL(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE ShippingMethods ADD FreeShippingThreshold DECIMAL(18,2) NULL;
    ALTER TABLE ShippingMethods ADD EstimatedDaysMin INT NULL;
    ALTER TABLE ShippingMethods ADD EstimatedDaysMax INT NULL;
    ALTER TABLE ShippingMethods ADD CodAvailable BIT NOT NULL DEFAULT 1;
    ALTER TABLE ShippingMethods ADD MaxCodAmount DECIMAL(18,2) NULL;
    ALTER TABLE ShippingMethods ADD SortOrder INT NOT NULL DEFAULT 0;
    PRINT 'Đã cấu hình tính phí nâng cao vào ShippingMethods.';
END

-- Xóa các bảng sai lệch nặng nề do khác biệt type
PRINT 'Bắt đầu cập nhật cấu trúc Shipping...';

IF OBJECT_ID('ShippingStatusHistories', 'U') IS NOT NULL DROP TABLE ShippingStatusHistories;
IF OBJECT_ID('ShippingOrders', 'U') IS NOT NULL DROP TABLE ShippingOrders;
IF OBJECT_ID('ShippingStatuses', 'U') IS NOT NULL DROP TABLE ShippingStatuses;

CREATE TABLE ShippingStatuses (
    ShippingStatusId INT PRIMARY KEY,
    StatusCode NVARCHAR(50) NOT NULL UNIQUE,
    StatusName NVARCHAR(100) NOT NULL,
    StatusOrder INT NOT NULL DEFAULT 0,
    Description NVARCHAR(500) NULL
);

INSERT INTO ShippingStatuses (ShippingStatusId, StatusCode, StatusName, StatusOrder) VALUES 
(1, 'PENDING', N'Chờ xử lý', 1),
(2, 'PICKED_UP', N'Đã lấy hàng', 2),
(3, 'IN_TRANSIT', N'Đang giao hàng', 3),
(4, 'OUT_FOR_DELIVERY', N'Đang giao đến người nhận', 4),
(5, 'DELIVERED', N'Giao hàng thành công', 5),
(6, 'FAILED_DELIVERY', N'Giao hàng thất bại', 6),
(7, 'RETURNED', N'Đã hoàn trả', 7);
PRINT 'Đã tái tạo ShippingStatuses thành công.';

CREATE TABLE ShippingOrders (
    ShippingOrderId INT IDENTITY(1,1) PRIMARY KEY,
    ShippingOrderCode NVARCHAR(50) NOT NULL UNIQUE,
    OrderId INT NOT NULL CONSTRAINT FK_SO_Order FOREIGN KEY REFERENCES Orders(OrderId),
    ShippingMethodId INT NOT NULL CONSTRAINT FK_SO_Method FOREIGN KEY REFERENCES ShippingMethods(ShippingMethodId),
    CarrierTrackingNo NVARCHAR(100) NULL,
    CarrierOrderNo NVARCHAR(100) NULL,
    CarrierStatus NVARCHAR(100) NULL,
    EstimatedDelivery DATETIME2 NULL,
    RecipientName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    ProvinceCode NVARCHAR(50) NULL,
    DistrictCode NVARCHAR(50) NULL,
    WardCode NVARCHAR(50) NULL,
    StreetAddress NVARCHAR(500) NULL,
    DeliveryInstruction NVARCHAR(500) NULL,
    ShippingFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    CodFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    InsuranceFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalShippingCost DECIMAL(18,2) NOT NULL DEFAULT 0,
    ShippingStatusId INT NOT NULL DEFAULT 1 CONSTRAINT FK_SO_Status FOREIGN KEY REFERENCES ShippingStatuses(ShippingStatusId),
    ShippedAt DATETIME2 NULL,
    DeliveredAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
PRINT 'Đã dựng lại ShippingOrders hoàn chỉnh.';

CREATE TABLE ShippingStatusHistories (
    HistoryId INT IDENTITY(1,1) PRIMARY KEY,
    ShippingOrderId INT NOT NULL CONSTRAINT FK_SSH_SO FOREIGN KEY REFERENCES ShippingOrders(ShippingOrderId),
    FromStatusId INT NULL CONSTRAINT FK_SSH_FromStatus FOREIGN KEY REFERENCES ShippingStatuses(ShippingStatusId),
    ToStatusId INT NOT NULL CONSTRAINT FK_SSH_ToStatus FOREIGN KEY REFERENCES ShippingStatuses(ShippingStatusId),
    CarrierStatusText NVARCHAR(500) NULL,
    Location NVARCHAR(200) NULL,
    EstimatedDeliveryUpdated DATETIME2 NULL,
    UpdatedBy INT NULL CONSTRAINT FK_SSH_User FOREIGN KEY REFERENCES Users(UserId),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
PRINT 'Đã tái tạo ShippingStatusHistories.';

PRINT '--- FINISH: ĐÃ CẬP NHẬT XONG DATABASE CHO OPS ---';
GO
