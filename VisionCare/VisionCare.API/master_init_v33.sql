/*
===========================================================
  VISION CARE - MASTER DATABASE INITIALIZATION SCRIPT
  Version: 3.4 (Unified, Patched, PreOrder Sync)
===========================================================
*/

USE master;
GO

-- 1. Khởi tạo Database sạch
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'VisionCare')
BEGIN
    ALTER DATABASE VisionCare SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE VisionCare;
END
GO

CREATE DATABASE VisionCare;
GO

USE VisionCare;
GO

-----------------------------------------------------------
-- 2. PHÂN QUYỀN & NGƯỜI DÙNG
-----------------------------------------------------------
CREATE TABLE Roles (
    RoleId INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(20),
    Address NVARCHAR(MAX),
    RoleId INT NOT NULL CONSTRAINT FK_Users_Roles FOREIGN KEY REFERENCES Roles(RoleId),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    IsActive BIT DEFAULT 1,
    RefreshToken VARCHAR(MAX) NULL,
    RefreshTokenExpiryTime DATETIME2 NULL
);

-----------------------------------------------------------
-- 3. QUY TẮC ĐO MẮT
-----------------------------------------------------------
CREATE TABLE PrescriptionValidationRules (
    RuleId INT IDENTITY(1,1) PRIMARY KEY,
    RuleName NVARCHAR(100) NOT NULL,
    RuleType NVARCHAR(50) NOT NULL,
    MinValue DECIMAL(10, 2) NULL,
    MaxValue DECIMAL(10, 2) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    Description NVARCHAR(500) NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);

-----------------------------------------------------------
-- 4. SẢN PHẨM & DANH MỤC
-----------------------------------------------------------
CREATE TABLE Categories (
    CategoryId INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(MAX)
);

CREATE TABLE Products (
    ProductId INT PRIMARY KEY IDENTITY(1,1),
    CategoryId INT NOT NULL CONSTRAINT FK_Products_Categories FOREIGN KEY REFERENCES Categories(CategoryId),
    ProductName NVARCHAR(200) NOT NULL,
    Brand NVARCHAR(100),
    Description NVARCHAR(MAX),
    BasePrice DECIMAL(18, 2) NOT NULL DEFAULT 0,
    IsFrame BIT NOT NULL DEFAULT 0,
    IsLens BIT NOT NULL DEFAULT 0,
    IsPreOrder BIT DEFAULT 0,
    Image2D VARCHAR(MAX),
    Model3D VARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

CREATE TABLE ProductVariants (
    VariantId INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT NOT NULL CONSTRAINT FK_Variants_Products FOREIGN KEY REFERENCES Products(ProductId),
    Color NVARCHAR(50),
    Size NVARCHAR(50),
    SKU VARCHAR(50) NOT NULL UNIQUE,
    StockQuantity INT NOT NULL DEFAULT 0,
    AdditionalPrice DECIMAL(18, 2) NOT NULL DEFAULT 0
);

-----------------------------------------------------------
-- 5. NHÀ CUNG CẤP & KHO BÃI
-----------------------------------------------------------
CREATE TABLE Suppliers (
    SupplierId INT IDENTITY(1,1) PRIMARY KEY,
    SupplierCode NVARCHAR(50) NOT NULL UNIQUE,
    SupplierName NVARCHAR(255) NOT NULL,
    ContactName NVARCHAR(255) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    Email NVARCHAR(255) NULL,
    Address NVARCHAR(500) NULL,
    TaxCode NVARCHAR(50) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    Notes NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE Warehouses (
    WarehouseId INT IDENTITY(1,1) PRIMARY KEY,
    WarehouseCode NVARCHAR(20) NOT NULL UNIQUE,
    WarehouseName NVARCHAR(200) NOT NULL,
    WarehouseType NVARCHAR(20) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    IsPrimary BIT NOT NULL DEFAULT 0,
    ProvinceName NVARCHAR(100) NULL,
    DistrictName NVARCHAR(100) NULL,
    StreetAddress NVARCHAR(500) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    ProvinceCode NVARCHAR(50) NULL,
    DistrictCode NVARCHAR(50) NULL,
    WardCode NVARCHAR(50) NULL,
    WardName NVARCHAR(100) NULL,
    Email NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Inventories (
    InventoryId INT IDENTITY(1,1) PRIMARY KEY,
    VariantId INT NOT NULL CONSTRAINT FK_Inv_Variant FOREIGN KEY REFERENCES ProductVariants(VariantId),
    WarehouseId INT NOT NULL CONSTRAINT FK_Inv_Warehouse FOREIGN KEY REFERENCES Warehouses(WarehouseId),
    QuantityOnHand INT NOT NULL DEFAULT 0,
    QuantityReserved INT NOT NULL DEFAULT 0,
    QuantityDefective INT NOT NULL DEFAULT 0,
    QuantityTransit INT NOT NULL DEFAULT 0,
    BatchNumber NVARCHAR(100) NULL,
    ManufacturingDate DATETIME2 NULL,
    ExpiryDate DATETIME2 NULL,
    LastCountAt DATETIME2 NULL,
    LastReplenishAt DATETIME2 NULL,
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Inv_Variant_WH UNIQUE (VariantId, WarehouseId)
);

CREATE TABLE StockMovements (
    MovementId INT IDENTITY(1,1) PRIMARY KEY,
    VariantId INT NOT NULL CONSTRAINT FK_SM_Variant FOREIGN KEY REFERENCES ProductVariants(VariantId),
    WarehouseId INT NOT NULL CONSTRAINT FK_SM_Warehouse FOREIGN KEY REFERENCES Warehouses(WarehouseId),
    MovementType NVARCHAR(30) NOT NULL,
    QuantityBefore INT NOT NULL DEFAULT 0,
    QuantityChange INT NOT NULL,
    QuantityAfter INT NOT NULL DEFAULT 0,
    ReferenceType NVARCHAR(30) NULL,
    ReferenceId INT NULL,
    Reason NVARCHAR(500) NULL,
    StaffNote NVARCHAR(MAX) NULL,
    PerformedBy INT NULL CONSTRAINT FK_SM_User FOREIGN KEY REFERENCES Users(UserId),
    PerformedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-----------------------------------------------------------
-- 6. NGHIỆP VỤ KÍNH & ĐƠN HÀNG
-----------------------------------------------------------
CREATE TABLE Prescriptions (
    PrescriptionId INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL CONSTRAINT FK_Prescriptions_Users FOREIGN KEY REFERENCES Users(UserId),
    OD_Sphere DECIMAL(5, 2),
    OD_Cylinder DECIMAL(5, 2),
    OD_Axis INT,
    OS_Sphere DECIMAL(5, 2),
    OS_Cylinder DECIMAL(5, 2),
    OS_Axis INT,
    PD DECIMAL(5, 2),
    Note NVARCHAR(MAX),
    IsVerified BIT NOT NULL DEFAULT 0,
    VerifiedBy INT NULL CONSTRAINT FK_Pres_VerBy FOREIGN KEY REFERENCES Users(UserId),
    VerifiedAt DATETIME2 NULL,
    IsRejected BIT NOT NULL DEFAULT 0,
    RejectedAt DATETIME2 NULL,
    RejectedBy INT NULL CONSTRAINT FK_Pres_RejBy FOREIGN KEY REFERENCES Users(UserId),
    RejectionReason NVARCHAR(500) NULL,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Orders (
    OrderId INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL CONSTRAINT FK_Orders_Users FOREIGN KEY REFERENCES Users(UserId),
    OrderDate DATETIME2 DEFAULT SYSUTCDATETIME(),
    TotalAmount DECIMAL(18, 2) NOT NULL,
    PaidAmount DECIMAL(18, 2) NULL,
    OrderStatus NVARCHAR(50) DEFAULT N'Pending', 
    PaymentStatus NVARCHAR(50) DEFAULT N'Unpaid', 
    OrderType NVARCHAR(50) DEFAULT N'Direct', 
    ShippingAddress NVARCHAR(MAX),
    TrackingNumber VARCHAR(100),
    PreOrderDeadline DATETIME NULL,
    StaffNote NVARCHAR(MAX),
    PackedAt DATETIME2 NULL,
    PackedBy INT NULL CONSTRAINT FK_Ord_PackedBy FOREIGN KEY REFERENCES Users(UserId)
);

CREATE TABLE OrderItems (
    OrderItemId INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT NOT NULL CONSTRAINT FK_Items_Orders FOREIGN KEY REFERENCES Orders(OrderId),
    VariantId INT NOT NULL CONSTRAINT FK_Items_Variants FOREIGN KEY REFERENCES ProductVariants(VariantId),
    PrescriptionId INT NULL CONSTRAINT FK_Items_Prescriptions FOREIGN KEY REFERENCES Prescriptions(PrescriptionId),
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18, 2) NOT NULL,
    AssignedLensMakerId INT NULL,
    LensCutCompletedAt DATETIME2 NULL,
    LensCutNote NVARCHAR(500) NULL
);

CREATE TABLE OrderStatusHistories (
    HistoryId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL CONSTRAINT FK_OSH_Order FOREIGN KEY REFERENCES Orders(OrderId),
    FromStatus NVARCHAR(50) NOT NULL,
    ToStatus NVARCHAR(50) NOT NULL,
    Note NVARCHAR(MAX) NULL,
    ChangedBy INT NOT NULL CONSTRAINT FK_OSH_User FOREIGN KEY REFERENCES Users(UserId),
    ChangedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-----------------------------------------------------------
-- 7. VẬN CHUYỂN
-----------------------------------------------------------
CREATE TABLE ShippingMethods (
    ShippingMethodId INT IDENTITY(1,1) PRIMARY KEY,
    MethodCode NVARCHAR(20) NOT NULL UNIQUE,
    MethodName NVARCHAR(100) NOT NULL,
    Provider NVARCHAR(50) NOT NULL,
    BaseFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    FeePerKg DECIMAL(18,2) NOT NULL DEFAULT 0,
    FreeShippingThreshold DECIMAL(18,2) NULL,
    EstimatedDaysMin INT NULL,
    EstimatedDaysMax INT NULL,
    CodAvailable BIT NOT NULL DEFAULT 1,
    MaxCodAmount DECIMAL(18,2) NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);

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

-----------------------------------------------------------
-- 8. CHIẾN DỊCH PRE-ORDER & KHIẾU NẠI
-----------------------------------------------------------
CREATE TABLE PreOrderCampaigns (
    CampaignId INT IDENTITY(1,1) PRIMARY KEY,
    CampaignCode NVARCHAR(50) NOT NULL UNIQUE,
    CampaignName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    ReleaseDate DATETIME2 NOT NULL,
    DiscountPercent INT NULL,
    DiscountAmount DECIMAL(18, 2) NULL,
    MaxQuantity INT NULL,
    MaxPerCustomer INT NOT NULL DEFAULT 0,
    CurrentReserved INT NOT NULL DEFAULT 0,
    Status NVARCHAR(50) NOT NULL DEFAULT 'draft',
    IsFeatured BIT NOT NULL DEFAULT 0,
    DepositRatio DECIMAL(5,4) NULL,
    MinDepositAmount DECIMAL(18,0) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE PreOrderCampaignProducts (
    CampaignProductId INT IDENTITY(1,1) PRIMARY KEY,
    CampaignId INT NOT NULL CONSTRAINT FK_POCP_Camp FOREIGN KEY REFERENCES PreOrderCampaigns(CampaignId),
    ProductId INT NOT NULL CONSTRAINT FK_POCP_Prod FOREIGN KEY REFERENCES Products(ProductId),
    VariantId INT NULL CONSTRAINT FK_POCP_Var FOREIGN KEY REFERENCES ProductVariants(VariantId),
    CampaignPrice DECIMAL(18, 2) NOT NULL,
    ReservedQuantity INT NOT NULL DEFAULT 0,
    ReceivedQuantity INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE PreOrderReservations (
    ReservationId INT IDENTITY(1,1) PRIMARY KEY,
    ReservationCode NVARCHAR(50) NOT NULL UNIQUE,
    CampaignId INT NOT NULL CONSTRAINT FK_POR_Camp FOREIGN KEY REFERENCES PreOrderCampaigns(CampaignId),
    CustomerId INT NOT NULL CONSTRAINT FK_POR_Cust FOREIGN KEY REFERENCES Users(UserId),
    VariantId INT NOT NULL CONSTRAINT FK_POR_Var FOREIGN KEY REFERENCES ProductVariants(VariantId),
    ReservedQuantity INT NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL DEFAULT 0,
    ShippingAddress NVARCHAR(MAX) NULL, -- Lưu địa chỉ giao hàng lúc đặt cọc
    Status NVARCHAR(50) NOT NULL DEFAULT 'reserved',
    PaymentLinkId NVARCHAR(MAX) NULL,
    ConvertedOrderId INT NULL CONSTRAINT FK_POR_ConvertedOrder FOREIGN KEY REFERENCES Orders(OrderId),
    ExpiresAt DATETIME2 NOT NULL,
    PaidAt DATETIME2 NULL,
    FulfilledAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE Complaints (
    ComplaintId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL CONSTRAINT FK_Comp_Ord FOREIGN KEY REFERENCES Orders(OrderId),
    CustomerId INT NOT NULL CONSTRAINT FK_Comp_Cust FOREIGN KEY REFERENCES Users(UserId),
    Subject NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    ComplaintStatus NVARCHAR(50) NOT NULL DEFAULT 'open',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-----------------------------------------------------------
-- 9. GIỎ HÀNG
-----------------------------------------------------------
CREATE TABLE Carts (
    CartId INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL CONSTRAINT FK_Carts_Users FOREIGN KEY REFERENCES Users(UserId),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Carts_CustomerId UNIQUE (CustomerId)
);

CREATE TABLE CartItems (
    CartItemId INT PRIMARY KEY IDENTITY(1,1),
    CartId INT NOT NULL CONSTRAINT FK_CartItems_Carts FOREIGN KEY REFERENCES Carts(CartId),
    VariantId INT NOT NULL CONSTRAINT FK_CartItems_Variants FOREIGN KEY REFERENCES ProductVariants(VariantId),
    PrescriptionId INT NULL CONSTRAINT FK_CartItems_Prescriptions FOREIGN KEY REFERENCES Prescriptions(PrescriptionId),
    Quantity INT NOT NULL DEFAULT 1 CHECK (Quantity > 0)
);

-----------------------------------------------------------
-- 10. KHUYẾN MÃI
-----------------------------------------------------------
CREATE TABLE Promotions (
    PromotionId INT PRIMARY KEY IDENTITY(1,1),
    PromoCode VARCHAR(50) NOT NULL UNIQUE,
    DiscountPercent INT NOT NULL CHECK (DiscountPercent >= 0 AND DiscountPercent <= 100),
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    IsActive BIT DEFAULT 1
);

-----------------------------------------------------------
-- 11. NHẬP HÀNG (GOODS RECEIPTS)
-----------------------------------------------------------
CREATE TABLE GoodsReceipts (
    GoodsReceiptId INT IDENTITY(1,1) PRIMARY KEY,
    ReceiptNumber NVARCHAR(50) NOT NULL,
    CampaignId INT NULL,
    CreatedBy INT NOT NULL,
    ManagerId INT NULL,
    WarehouseId INT NOT NULL,
    Status NVARCHAR(20) DEFAULT 'draft',
    Note NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    CompletedAt DATETIME2 NULL,
    CONSTRAINT FK_GoodsReceipts_Campaigns FOREIGN KEY (CampaignId) REFERENCES PreOrderCampaigns(CampaignId) ON DELETE SET NULL,
    CONSTRAINT FK_GoodsReceipts_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(UserId),
    CONSTRAINT FK_GoodsReceipts_Manager FOREIGN KEY (ManagerId) REFERENCES Users(UserId),
    CONSTRAINT FK_GoodsReceipts_Warehouse FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId)
);

CREATE TABLE GoodsReceiptDetails (
    DetailId INT IDENTITY(1,1) PRIMARY KEY,
    GoodsReceiptId INT NOT NULL,
    VariantId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NULL,
    CONSTRAINT FK_GoodsReceiptDetails_Receipt FOREIGN KEY (GoodsReceiptId) REFERENCES GoodsReceipts(GoodsReceiptId) ON DELETE CASCADE,
    CONSTRAINT FK_GoodsReceiptDetails_Variant FOREIGN KEY (VariantId) REFERENCES ProductVariants(VariantId)
);

-----------------------------------------------------------
-- 12. DỮ LIỆU MẪU
-----------------------------------------------------------

-- Roles
SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (RoleId, RoleName) VALUES (1, 'Admin'), (2, 'Manager'), (3, 'Sales'), (4, 'Operations'), (5, 'Customer');
SET IDENTITY_INSERT Roles OFF;

-- Users ('123456')
DECLARE @DefaultHash VARCHAR(255) = '$2a$11$4B8fqt/E13Omla6aizXha.a5AS8n4gFWK1IOhObvoA3BkKcXwOAJe';
INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, RoleId) VALUES 
(N'Hệ thống Admin', 'admin@visioncare.com', @DefaultHash, '0911222333', 1),
(N'Nguyễn Quản Lý', 'manager@visioncare.com', @DefaultHash, '0922333444', 2),
(N'Lê Nhân Viên', 'sales@visioncare.com', @DefaultHash, '0933444555', 3),
(N'Nhân Viên Kho', 'ops@visioncare.com', @DefaultHash, '0944555666', 4),
(N'Trần Khách Hàng', 'customer@visioncare.com', @DefaultHash, '0955666777', 5);

-- Categories
SET IDENTITY_INSERT Categories ON;
INSERT INTO Categories (CategoryId, CategoryName) VALUES (1, N'Gọng tròn'), (2, N'Gọng vuông'), (3, N'Kính mát'), (4, N'Kính râm'), (5, N'Tròng kính');
SET IDENTITY_INSERT Categories OFF;

-- Products
SET IDENTITY_INSERT Products ON;
INSERT INTO Products (ProductId, CategoryId, ProductName, Brand, Description, BasePrice, IsFrame, IsLens, IsPreOrder, Image2D) VALUES
(1, 1, N'Vision Round X1', N'VisionCare', N'Gọng kính tròn hiện đại', 800000, 1, 0, 0, 'https://thumbs.dreamstime.com/b/thin-black-metal-frame-round-clear-lens-eyeglasses-white-background-lenses-isolated-showcasing-their-simple-design-403319062.jpg'),
(2, 2, N'Aura Modern Rectangular', N'Aura', N'Gọng kính vuông mạnh mẽ', 950000, 1, 0, 0, 'https://media.istockphoto.com/id/1149723055/photo/black-rectangular-glasses-isolated-on-white.jpg?s=612x612&w=0&k=20&c=K5O_7r2R1s-K4sK1F1-qQn7y-Y6Y-8yX7M2F2aB'),
(50, 3, N'Ray-Ban Aviator Classic', N'Ray-Ban', N'Kính mát phi công huyền thoại', 3500000, 0, 0, 0, 'https://images.ray-ban.com/is/image/RayBan/805289602057__002.png?impolicy=RB_Product_DRP&width=1024'),
(51, 3, N'Oakley Holbrook', N'Oakley', N'Kính mát thể thao năng động', 4200000, 0, 0, 0, 'https://media.oakley.com/pdp-main-retina/top-navigation-oakley/888392100827_holbrook_matte-black-prizm-grey.png'),
(60, 4, N'Polaroid Navigator', N'Polaroid', N'Kính râm đặt trước - Phiên bản giới hạn', 1800000, 0, 0, 1, 'https://www.polaroid-eyewear.com/content/dam/polaroid/products/2021/PLD_2097_S_807_M9_01.png'),
(100, 5, N'Essilor Crizal Sapphire HR', N'Essilor', N'Tròng kính chống chói cao cấp', 1250000, 0, 1, 0, 'https://u-pose.com/wp-content/uploads/2023/11/Crizal-Sapphire-HR-Sapphire-Blue-Color-Sample.jpg'),
(101, 5, N'Zeiss BlueGuard Platinum', N'Zeiss', N'Tròng kính lọc ánh sáng xanh', 1850000, 0, 1, 0, 'https://www.zeiss.com/content/dam/vision-care/images/products/zeiss-blueguard-lenses/zeiss-blueguard-lenses-product-image.png');
SET IDENTITY_INSERT Products OFF;

-- ProductVariants
SET IDENTITY_INSERT ProductVariants ON;
INSERT INTO ProductVariants (VariantId, ProductId, Color, Size, SKU, StockQuantity) VALUES
(1, 1, N'Đen mờ', N'M', 'VRX1-BLK-M', 10),
(2, 1, N'Vàng hồng', N'S', 'VRX1-GLD-S', 5),
(3, 2, N'Xám khói', N'L', 'AMR-GRY-L', 15),
(501, 50, N'Xanh lá G-15', N'Standard', 'RB-AVI-G15', 20),
(511, 51, N'Đen Prizm', N'Standard', 'OK-HOL-PRZ', 12),
(601, 60, N'Đen', N'Standard', 'PL-NAV-BLK', 0),
(1001, 100, N'1.56', N'Standard', 'ES-SAP-156', 500),
(1002, 100, N'1.67 High-Index', N'Standard', 'ES-SAP-167', 200),
(1011, 101, N'1.60', N'Standard', 'ZS-BG-160', 100);
SET IDENTITY_INSERT ProductVariants OFF;

-- ShippingMethods
INSERT INTO ShippingMethods (MethodCode, MethodName, Provider, BaseFee, IsActive) VALUES
('GHTK', N'Giao hàng tiết kiệm', 'GHTK', 25000, 1), 
('GHN', N'Giao hàng nhanh', 'GHN', 30000, 1);

-- Warehouses
INSERT INTO Warehouses (WarehouseCode, WarehouseName, IsPrimary, StreetAddress) VALUES
('WH-HCM-01', N'Kho chính Hồ Chí Minh', 1, N'123 Nguyễn Lương Bằng, Quận 7');

-- Pre-order Campaign WITH LIMITS
SET IDENTITY_INSERT PreOrderCampaigns ON;
INSERT INTO PreOrderCampaigns (
    CampaignId, CampaignCode, CampaignName, Description, 
    StartDate, EndDate, ReleaseDate, 
    DiscountPercent, MaxQuantity, MaxPerCustomer, Status, 
    IsFeatured, DepositRatio, CreatedAt
)
VALUES (
    1, 'CAMP-PO-NAV', N'Pre-order Polaroid Navigator', N'Chiến dịch đặt trước kính Polaroid Navigator phiên bản giới hạn.',
    DATEADD(day, -7, GETDATE()), DATEADD(day, 7, GETDATE()), DATEADD(day, 14, GETDATE()), 
    10, 100, 5, 'active', 
    1, 0.3, GETDATE()
);
SET IDENTITY_INSERT PreOrderCampaigns OFF;

INSERT INTO PreOrderCampaignProducts (CampaignId, ProductId, VariantId, CampaignPrice, CreatedAt)
VALUES (1, 60, NULL, 1600000, GETDATE());

GO
PRINT 'MASTER DATABASE INITIALIZATION COMPLETED (V3.4 FINAL - SYNCED)!';
