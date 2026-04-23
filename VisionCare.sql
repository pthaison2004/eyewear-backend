/*
===========================================================
  VISION CARE - MASTER DATABASE INITIALIZATION SCRIPT
  Version: 3.4 (Unified, Clean, PreOrder Sync)
===========================================================
*/

-----------------------------------------------------------
-- 0. KHỞI TẠO DATABASE
-----------------------------------------------------------
USE master;
GO

IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'VisionCare')
BEGIN
    ALTER DATABASE VisionCare SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE VisionCare;
END;
GO

CREATE DATABASE VisionCare;
GO

USE VisionCare;
GO

-----------------------------------------------------------
-- 1. PHÂN QUYỀN & NGƯỜI DÙNG
-----------------------------------------------------------
CREATE TABLE Roles (
    RoleId   INT           IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50)  NOT NULL UNIQUE
);

CREATE TABLE Users (
    UserId                 INT           IDENTITY(1,1) PRIMARY KEY,
    FullName               NVARCHAR(100) NOT NULL,
    Email                  VARCHAR(100)  NOT NULL UNIQUE,
    PasswordHash           VARCHAR(255)  NOT NULL,
    PhoneNumber            VARCHAR(20)   NULL,
    Address                NVARCHAR(MAX) NULL,
    RoleId                 INT           NOT NULL,
    CreatedAt              DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    IsActive               BIT           NOT NULL DEFAULT 1,
    RefreshToken           VARCHAR(MAX)  NULL,
    RefreshTokenExpiryTime DATETIME2     NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

-----------------------------------------------------------
-- 2. QUY TẮC ĐO MẮT
-----------------------------------------------------------
CREATE TABLE PrescriptionValidationRules (
    RuleId      INT           IDENTITY(1,1) PRIMARY KEY,
    RuleName    NVARCHAR(100) NOT NULL,
    RuleType    NVARCHAR(50)  NOT NULL,
    MinValue    DECIMAL(10,2) NULL,
    MaxValue    DECIMAL(10,2) NULL,
    IsActive    BIT           NOT NULL DEFAULT 1,
    Description NVARCHAR(500) NULL,
    SortOrder   INT           NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2     NULL
);

-----------------------------------------------------------
-- 3. DANH MỤC, SẢN PHẨM, BIẾN THỂ
-----------------------------------------------------------
CREATE TABLE Categories (
    CategoryId   INT           IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description  NVARCHAR(MAX) NULL
);

CREATE TABLE Products (
    ProductId   INT           IDENTITY(1,1) PRIMARY KEY,
    CategoryId  INT           NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    Brand       NVARCHAR(100) NULL, -- giữ để tương thích
    Description NVARCHAR(MAX) NULL,
    BasePrice   DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsFrame     BIT           NOT NULL DEFAULT 0,
    IsLens      BIT           NOT NULL DEFAULT 0,
    IsPreOrder  BIT           NOT NULL DEFAULT 0,
    Image2D     VARCHAR(MAX)  NULL,
    Model3D     VARCHAR(MAX)  NULL,
    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    BrandId     INT           NULL
);

ALTER TABLE Products
ADD CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId)
    REFERENCES Categories(CategoryId);

CREATE TABLE ProductVariants (
    VariantId       INT           IDENTITY(1,1) PRIMARY KEY,
    ProductId       INT           NOT NULL,
    Color           NVARCHAR(50)  NULL,
    Size            NVARCHAR(50)  NULL,
    SKU             VARCHAR(50)   NOT NULL UNIQUE,
    StockQuantity   INT           NOT NULL DEFAULT 0,
    AdditionalPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_Variants_Products FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

-----------------------------------------------------------
-- 4. NHÀ CUNG CẤP & KHO BÃI & TỒN KHO
-----------------------------------------------------------
CREATE TABLE Suppliers (
    SupplierId   INT           IDENTITY(1,1) PRIMARY KEY,
    SupplierCode NVARCHAR(50)  NOT NULL UNIQUE,
    SupplierName NVARCHAR(255) NOT NULL,
    ContactName  NVARCHAR(255) NULL,
    PhoneNumber  NVARCHAR(20)  NULL,
    Email        NVARCHAR(255) NULL,
    Address      NVARCHAR(500) NULL,
    TaxCode      NVARCHAR(50)  NULL,
    IsActive     BIT           NOT NULL DEFAULT 1,
    Notes        NVARCHAR(MAX) NULL,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt    DATETIME2     NULL
);

CREATE TABLE Warehouses (
    WarehouseId   INT           IDENTITY(1,1) PRIMARY KEY,
    WarehouseCode NVARCHAR(20)  NOT NULL UNIQUE,
    WarehouseName NVARCHAR(200) NOT NULL,
    WarehouseType NVARCHAR(20)  NULL,
    IsActive      BIT           NOT NULL DEFAULT 1,
    IsPrimary     BIT           NOT NULL DEFAULT 0,
    ProvinceName  NVARCHAR(100) NULL,
    DistrictName  NVARCHAR(100) NULL,
    StreetAddress NVARCHAR(500) NULL,
    PhoneNumber   NVARCHAR(20)  NULL,
    ProvinceCode  NVARCHAR(50)  NULL,
    DistrictCode  NVARCHAR(50)  NULL,
    WardCode      NVARCHAR(50)  NULL,
    WardName      NVARCHAR(100) NULL,
    Email         NVARCHAR(255) NULL,
    CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Inventories (
    InventoryId       INT           IDENTITY(1,1) PRIMARY KEY,
    VariantId         INT           NOT NULL,
    WarehouseId       INT           NOT NULL,
    QuantityOnHand    INT           NOT NULL DEFAULT 0,
    QuantityReserved  INT           NOT NULL DEFAULT 0,
    QuantityDefective INT           NOT NULL DEFAULT 0,
    QuantityTransit   INT           NOT NULL DEFAULT 0,
    BatchNumber       NVARCHAR(100) NULL,
    ManufacturingDate DATETIME2     NULL,
    ExpiryDate        DATETIME2     NULL,
    LastCountAt       DATETIME2     NULL,
    LastReplenishAt   DATETIME2     NULL,
    UpdatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Inv_Variant   FOREIGN KEY (VariantId)   REFERENCES ProductVariants(VariantId),
    CONSTRAINT FK_Inv_Warehouse FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId),
    CONSTRAINT UQ_Inv_Variant_WH UNIQUE (VariantId, WarehouseId)
);

CREATE TABLE StockMovements (
    MovementId     INT           IDENTITY(1,1) PRIMARY KEY,
    VariantId      INT           NOT NULL,
    WarehouseId    INT           NOT NULL,
    MovementType   NVARCHAR(30)  NOT NULL,
    QuantityBefore INT           NOT NULL DEFAULT 0,
    QuantityChange INT           NOT NULL,
    QuantityAfter  INT           NOT NULL DEFAULT 0,
    ReferenceType  NVARCHAR(30)  NULL,
    ReferenceId    INT           NULL,
    Reason         NVARCHAR(500) NULL,
    StaffNote      NVARCHAR(MAX) NULL,
    PerformedBy    INT           NULL,
    PerformedAt    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SM_Variant   FOREIGN KEY (VariantId)   REFERENCES ProductVariants(VariantId),
    CONSTRAINT FK_SM_Warehouse FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId),
    CONSTRAINT FK_SM_User      FOREIGN KEY (PerformedBy) REFERENCES Users(UserId)
);

-----------------------------------------------------------
-- 5. NGHIỆP VỤ KÍNH & ĐƠN HÀNG
-----------------------------------------------------------
CREATE TABLE Prescriptions (
    PrescriptionId  INT           IDENTITY(1,1) PRIMARY KEY,
    CustomerId      INT           NOT NULL,
    OD_Sphere       DECIMAL(5,2)  NULL,
    OD_Cylinder     DECIMAL(5,2)  NULL,
    OD_Axis         INT           NULL,
    OS_Sphere       DECIMAL(5,2)  NULL,
    OS_Cylinder     DECIMAL(5,2)  NULL,
    OS_Axis         INT           NULL,
    PD              DECIMAL(5,2)  NULL,
    Note            NVARCHAR(MAX) NULL,
    IsVerified      BIT           NOT NULL DEFAULT 0,
    VerifiedBy      INT           NULL,
    VerifiedAt      DATETIME2     NULL,
    IsRejected      BIT           NOT NULL DEFAULT 0,
    RejectedAt      DATETIME2     NULL,
    RejectedBy      INT           NULL,
    RejectionReason NVARCHAR(500) NULL,
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Prescriptions_Users FOREIGN KEY (CustomerId) REFERENCES Users(UserId),
    CONSTRAINT FK_Pres_VerBy          FOREIGN KEY (VerifiedBy)  REFERENCES Users(UserId),
    CONSTRAINT FK_Pres_RejBy          FOREIGN KEY (RejectedBy)  REFERENCES Users(UserId)
);

CREATE TABLE Orders (
    OrderId          INT           IDENTITY(1,1) PRIMARY KEY,
    CustomerId       INT           NOT NULL,
    OrderDate        DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    TotalAmount      DECIMAL(18,2) NOT NULL,
    PaidAmount       DECIMAL(18,2) NULL,
    OrderStatus      NVARCHAR(50)  NOT NULL DEFAULT N'Pending',
    PaymentStatus    NVARCHAR(50)  NOT NULL DEFAULT N'Unpaid',
    OrderType        NVARCHAR(50)  NOT NULL DEFAULT N'Direct',
    ShippingAddress  NVARCHAR(MAX) NULL,
    TrackingNumber   VARCHAR(100)  NULL,
    PreOrderDeadline DATETIME      NULL,
    StaffNote        NVARCHAR(MAX) NULL,
    PackedAt         DATETIME2     NULL,
    PackedBy         INT           NULL,
    ShippingFee      DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_Orders_Users     FOREIGN KEY (CustomerId) REFERENCES Users(UserId),
    CONSTRAINT FK_Ord_PackedBy     FOREIGN KEY (PackedBy)   REFERENCES Users(UserId)
);

CREATE TABLE OrderItems (
    OrderItemId        INT           IDENTITY(1,1) PRIMARY KEY,
    OrderId            INT           NOT NULL,
    VariantId          INT           NOT NULL,
    PrescriptionId     INT           NULL,
    Quantity           INT           NOT NULL CHECK (Quantity > 0),
    UnitPrice          DECIMAL(18,2) NOT NULL,
    AssignedLensMakerId INT          NULL,
    LensCutCompletedAt DATETIME2     NULL,
    LensCutNote        NVARCHAR(500) NULL,
    CONSTRAINT FK_Items_Orders        FOREIGN KEY (OrderId)        REFERENCES Orders(OrderId),
    CONSTRAINT FK_Items_Variants      FOREIGN KEY (VariantId)      REFERENCES ProductVariants(VariantId),
    CONSTRAINT FK_Items_Prescriptions FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(PrescriptionId)
);

CREATE TABLE OrderStatusHistories (
    HistoryId  INT           IDENTITY(1,1) PRIMARY KEY,
    OrderId    INT           NOT NULL,
    FromStatus NVARCHAR(50)  NOT NULL,
    ToStatus   NVARCHAR(50)  NOT NULL,
    Note       NVARCHAR(MAX) NULL,
    ChangedBy  INT           NOT NULL,
    ChangedAt  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_OSH_Order FOREIGN KEY (OrderId)    REFERENCES Orders(OrderId),
    CONSTRAINT FK_OSH_User  FOREIGN KEY (ChangedBy)  REFERENCES Users(UserId)
);

-----------------------------------------------------------
-- 6. VẬN CHUYỂN
-----------------------------------------------------------
CREATE TABLE ShippingMethods (
    ShippingMethodId      INT           IDENTITY(1,1) PRIMARY KEY,
    MethodCode            NVARCHAR(20)  NOT NULL UNIQUE,
    MethodName            NVARCHAR(100) NOT NULL,
    Provider              NVARCHAR(50)  NOT NULL,
    BaseFee               DECIMAL(18,2) NOT NULL DEFAULT 0,
    FeePerKg              DECIMAL(18,2) NOT NULL DEFAULT 0,
    FreeShippingThreshold DECIMAL(18,2) NULL,
    EstimatedDaysMin      INT           NULL,
    EstimatedDaysMax      INT           NULL,
    CodAvailable          BIT           NOT NULL DEFAULT 1,
    MaxCodAmount          DECIMAL(18,2) NULL,
    SortOrder             INT           NOT NULL DEFAULT 0,
    IsActive              BIT           NOT NULL DEFAULT 1
);

CREATE TABLE ShippingStatuses (
    ShippingStatusId INT           PRIMARY KEY,
    StatusCode       NVARCHAR(50)  NOT NULL UNIQUE,
    StatusName       NVARCHAR(100) NOT NULL,
    StatusOrder      INT           NOT NULL DEFAULT 0,
    Description      NVARCHAR(500) NULL
);

INSERT INTO ShippingStatuses (ShippingStatusId, StatusCode, StatusName, StatusOrder) VALUES 
(1, 'PENDING',          N'Chờ xử lý',                    1),
(2, 'PICKED_UP',        N'Đã lấy hàng',                  2),
(3, 'IN_TRANSIT',       N'Đang giao hàng',               3),
(4, 'OUT_FOR_DELIVERY', N'Đang giao đến người nhận',     4),
(5, 'DELIVERED',        N'Giao hàng thành công',         5),
(6, 'FAILED_DELIVERY',  N'Giao hàng thất bại',           6),
(7, 'RETURNED',         N'Đã hoàn trả',                  7);

CREATE TABLE ShippingOrders (
    ShippingOrderId     INT           IDENTITY(1,1) PRIMARY KEY,
    ShippingOrderCode   NVARCHAR(50)  NOT NULL UNIQUE,
    OrderId             INT           NOT NULL,
    ShippingMethodId    INT           NOT NULL,
    CarrierTrackingNo   NVARCHAR(100) NULL,
    CarrierOrderNo      NVARCHAR(100) NULL,
    CarrierStatus       NVARCHAR(100) NULL,
    EstimatedDelivery   DATETIME2     NULL,
    RecipientName       NVARCHAR(100) NOT NULL,
    PhoneNumber         NVARCHAR(20)  NOT NULL,
    ProvinceCode        NVARCHAR(50)  NULL,
    DistrictCode        NVARCHAR(50)  NULL,
    WardCode            NVARCHAR(50)  NULL,
    StreetAddress       NVARCHAR(500) NULL,
    DeliveryInstruction NVARCHAR(500) NULL,
    ShippingFee         DECIMAL(18,2) NOT NULL DEFAULT 0,
    CodFee              DECIMAL(18,2) NOT NULL DEFAULT 0,
    InsuranceFee        DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalShippingCost   DECIMAL(18,2) NOT NULL DEFAULT 0,
    ShippingStatusId    INT           NOT NULL DEFAULT 1,
    ShippedAt           DATETIME2     NULL,
    DeliveredAt         DATETIME2     NULL,
    CreatedAt           DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SO_Order   FOREIGN KEY (OrderId)          REFERENCES Orders(OrderId),
    CONSTRAINT FK_SO_Method  FOREIGN KEY (ShippingMethodId) REFERENCES ShippingMethods(ShippingMethodId),
    CONSTRAINT FK_SO_Status  FOREIGN KEY (ShippingStatusId) REFERENCES ShippingStatuses(ShippingStatusId)
);

CREATE TABLE ShippingStatusHistories (
    HistoryId                INT           IDENTITY(1,1) PRIMARY KEY,
    ShippingOrderId          INT           NOT NULL,
    FromStatusId             INT           NULL,
    ToStatusId               INT           NOT NULL,
    CarrierStatusText        NVARCHAR(500) NULL,
    Location                 NVARCHAR(200) NULL,
    EstimatedDeliveryUpdated DATETIME2     NULL,
    UpdatedBy                INT           NULL,
    UpdatedAt                DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SSH_SO         FOREIGN KEY (ShippingOrderId) REFERENCES ShippingOrders(ShippingOrderId),
    CONSTRAINT FK_SSH_FromStatus FOREIGN KEY (FromStatusId)    REFERENCES ShippingStatuses(ShippingStatusId),
    CONSTRAINT FK_SSH_ToStatus   FOREIGN KEY (ToStatusId)      REFERENCES ShippingStatuses(ShippingStatusId),
    CONSTRAINT FK_SSH_User       FOREIGN KEY (UpdatedBy)       REFERENCES Users(UserId)
);

-----------------------------------------------------------
-- 7. PRE-ORDER & KHIẾU NẠI
-----------------------------------------------------------
CREATE TABLE PreOrderCampaigns (
    CampaignId       INT           IDENTITY(1,1) PRIMARY KEY,
    CampaignCode     NVARCHAR(50)  NOT NULL UNIQUE,
    CampaignName     NVARCHAR(200) NOT NULL,
    Description      NVARCHAR(MAX) NULL,
    StartDate        DATETIME2     NOT NULL,
    EndDate          DATETIME2     NOT NULL,
    ReleaseDate      DATETIME2     NOT NULL,
    DiscountPercent  INT           NULL,
    DiscountAmount   DECIMAL(18,2) NULL,
    MaxQuantity      INT           NULL,
    MaxPerCustomer   INT           NOT NULL DEFAULT 0,
    CurrentReserved  INT           NOT NULL DEFAULT 0,
    Status           NVARCHAR(50)  NOT NULL DEFAULT 'draft',
    IsFeatured       BIT           NOT NULL DEFAULT 0,
    DepositRatio     DECIMAL(5,4)  NULL,
    MinDepositAmount DECIMAL(18,0) NULL,
    CreatedAt        DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt        DATETIME2     NULL
);

CREATE TABLE PreOrderCampaignProducts (
    CampaignProductId INT           IDENTITY(1,1) PRIMARY KEY,
    CampaignId        INT           NOT NULL,
    ProductId         INT           NOT NULL,
    VariantId         INT           NULL,
    CampaignPrice     DECIMAL(18,2) NOT NULL,
    ReservedQuantity  INT           NOT NULL DEFAULT 0,
    ReceivedQuantity  INT           NOT NULL DEFAULT 0,
    CreatedAt         DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_POCP_Camp FOREIGN KEY (CampaignId) REFERENCES PreOrderCampaigns(CampaignId),
    CONSTRAINT FK_POCP_Prod FOREIGN KEY (ProductId)  REFERENCES Products(ProductId),
    CONSTRAINT FK_POCP_Var  FOREIGN KEY (VariantId)  REFERENCES ProductVariants(VariantId)
);

CREATE TABLE PreOrderReservations (
    ReservationId    INT           IDENTITY(1,1) PRIMARY KEY,
    ReservationCode  NVARCHAR(50)  NOT NULL UNIQUE,
    CampaignId       INT           NOT NULL,
    CustomerId       INT           NOT NULL,
    VariantId        INT           NOT NULL,
    ReservedQuantity INT           NOT NULL,
    UnitPrice        DECIMAL(18,2) NOT NULL DEFAULT 0,
    ShippingAddress  NVARCHAR(MAX) NULL,
    Status           NVARCHAR(50)  NOT NULL DEFAULT 'reserved',
    PaymentLinkId    NVARCHAR(MAX) NULL,
    ConvertedOrderId INT           NULL,
    ExpiresAt        DATETIME2     NOT NULL,
    PaidAt           DATETIME2     NULL,
    FulfilledAt      DATETIME2     NULL,
    CreatedAt        DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_POR_Camp           FOREIGN KEY (CampaignId)       REFERENCES PreOrderCampaigns(CampaignId),
    CONSTRAINT FK_POR_Cust           FOREIGN KEY (CustomerId)       REFERENCES Users(UserId),
    CONSTRAINT FK_POR_Var            FOREIGN KEY (VariantId)        REFERENCES ProductVariants(VariantId),
    CONSTRAINT FK_POR_ConvertedOrder FOREIGN KEY (ConvertedOrderId) REFERENCES Orders(OrderId)
);

CREATE TABLE Complaints (
    ComplaintId     INT           IDENTITY(1,1) PRIMARY KEY,
    OrderId         INT           NOT NULL,
    CustomerId      INT           NOT NULL,
    Subject         NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(MAX) NOT NULL,
    ComplaintStatus NVARCHAR(50)  NOT NULL DEFAULT 'open',
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Comp_Ord  FOREIGN KEY (OrderId)    REFERENCES Orders(OrderId),
    CONSTRAINT FK_Comp_Cust FOREIGN KEY (CustomerId) REFERENCES Users(UserId)
);

-----------------------------------------------------------
-- 8. GIỎ HÀNG
-----------------------------------------------------------
CREATE TABLE Carts (
    CartId     INT        IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT        NOT NULL,
    CreatedAt  DATETIME2  NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt  DATETIME2  NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Carts_Users      FOREIGN KEY (CustomerId) REFERENCES Users(UserId),
    CONSTRAINT UQ_Carts_CustomerId UNIQUE (CustomerId)
);

CREATE TABLE CartItems (
    CartItemId     INT          IDENTITY(1,1) PRIMARY KEY,
    CartId         INT          NOT NULL,
    VariantId      INT          NOT NULL,
    PrescriptionId INT          NULL,
    Quantity       INT          NOT NULL DEFAULT 1 CHECK (Quantity > 0),
    CONSTRAINT FK_CartItems_Carts         FOREIGN KEY (CartId)         REFERENCES Carts(CartId),
    CONSTRAINT FK_CartItems_Variants      FOREIGN KEY (VariantId)      REFERENCES ProductVariants(VariantId),
    CONSTRAINT FK_CartItems_Prescriptions FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(PrescriptionId)
);

-----------------------------------------------------------
-- 9. KHUYẾN MÃI
-----------------------------------------------------------
CREATE TABLE Promotions (
    PromotionId     INT           IDENTITY(1,1) PRIMARY KEY,
    PromoCode       VARCHAR(50)   NOT NULL UNIQUE,
    DiscountPercent INT           NOT NULL CHECK (DiscountPercent >= 0 AND DiscountPercent <= 100),
    StartDate       DATETIME2     NOT NULL,
    EndDate         DATETIME2     NOT NULL,
    IsActive        BIT           NOT NULL DEFAULT 1
);

-----------------------------------------------------------
-- 10. GOODS RECEIPTS
-----------------------------------------------------------
CREATE TABLE GoodsReceipts (
    GoodsReceiptId INT           IDENTITY(1,1) PRIMARY KEY,
    ReceiptNumber  NVARCHAR(50)  NOT NULL,
    CampaignId     INT           NULL,
    CreatedBy      INT           NOT NULL,
    ManagerId      INT           NULL,
    WarehouseId    INT           NOT NULL,
    Status         NVARCHAR(20)  NOT NULL DEFAULT 'draft',
    Note           NVARCHAR(MAX) NULL,
    CreatedAt      DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt    DATETIME2     NULL,
    ProofImage     NVARCHAR(MAX) NULL,
    ApprovedAt     DATETIME2     NULL,
    CONSTRAINT FK_GoodsReceipts_Campaigns FOREIGN KEY (CampaignId)  REFERENCES PreOrderCampaigns(CampaignId) ON DELETE SET NULL,
    CONSTRAINT FK_GoodsReceipts_CreatedBy  FOREIGN KEY (CreatedBy)  REFERENCES Users(UserId),
    CONSTRAINT FK_GoodsReceipts_Manager    FOREIGN KEY (ManagerId)  REFERENCES Users(UserId),
    CONSTRAINT FK_GoodsReceipts_Warehouse  FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId)
);

CREATE TABLE GoodsReceiptDetails (
    DetailId       INT           IDENTITY(1,1) PRIMARY KEY,
    GoodsReceiptId INT           NOT NULL,
    VariantId      INT           NOT NULL,
    Quantity       INT           NOT NULL,
    UnitPrice      DECIMAL(18,2) NULL,
    CONSTRAINT FK_GoodsReceiptDetails_Receipt FOREIGN KEY (GoodsReceiptId) REFERENCES GoodsReceipts(GoodsReceiptId) ON DELETE CASCADE,
    CONSTRAINT FK_GoodsReceiptDetails_Variant FOREIGN KEY (VariantId)      REFERENCES ProductVariants(VariantId)
);

UPDATE GoodsReceipts SET Status = 'PendingApproval' WHERE Status = 'draft';

-----------------------------------------------------------
-- 11. NOTIFICATIONS
-----------------------------------------------------------
IF NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'dbo.Notifications')
      AND type = N'U'
)
BEGIN
    CREATE TABLE dbo.Notifications (
        NotificationId INT            IDENTITY(1,1) PRIMARY KEY,
        UserId         INT            NOT NULL,
        Title          NVARCHAR(200)  NOT NULL,
        Message        NVARCHAR(1000) NOT NULL,
        Type           NVARCHAR(50)   NULL,
        Link           NVARCHAR(255)  NULL,
        IsRead         BIT            NOT NULL DEFAULT 0,
        CreatedAt      DATETIME       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );
END;

-----------------------------------------------------------
-- 12. SYSTEM SETTINGS, AUDIT LOGS, BLACKLISTED IPS
-----------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('SystemSettings') AND type = 'U')
BEGIN
    CREATE TABLE SystemSettings (
        SettingId    INT           IDENTITY(1,1) PRIMARY KEY,
        SettingKey   NVARCHAR(100) NOT NULL UNIQUE,
        SettingValue NVARCHAR(MAX) NULL,
        Description  NVARCHAR(500) NULL,
        GroupName    NVARCHAR(50)  NOT NULL DEFAULT 'General',
        UpdatedAt    DATETIME      NOT NULL DEFAULT GETDATE()
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('AuditLogs') AND type = 'U')
BEGIN
    CREATE TABLE AuditLogs (
        AuditId    INT           IDENTITY(1,1) PRIMARY KEY,
        UserId     INT           NULL,
        Action     NVARCHAR(100) NOT NULL,
        EntityName NVARCHAR(100) NULL,
        EntityId   NVARCHAR(100) NULL,
        OldValues  NVARCHAR(MAX) NULL,
        NewValues  NVARCHAR(MAX) NULL,
        IpAddress  NVARCHAR(50)  NULL,
        CreatedAt  DATETIME      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('BlacklistedIps') AND type = 'U')
BEGIN
    CREATE TABLE BlacklistedIps (
        Id        INT           IDENTITY(1,1) PRIMARY KEY,
        IpAddress NVARCHAR(50)  NOT NULL UNIQUE,
        Reason    NVARCHAR(500) NULL,
        CreatedAt DATETIME      NOT NULL DEFAULT GETDATE()
    );
END;

-----------------------------------------------------------
-- 13. BRANDS
-----------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('Brands') AND type = 'U')
BEGIN
    CREATE TABLE Brands (
        BrandId     INT           IDENTITY(1,1) PRIMARY KEY,
        BrandName   NVARCHAR(100) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        EvidenceUrl NVARCHAR(MAX) NULL,
        Status      NVARCHAR(50)  NOT NULL DEFAULT N'Pending',
        RequestType NVARCHAR(50)  NULL,
        RequestedBy INT           NULL,
        RequestDate DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        ApprovedBy  INT           NULL,
        ApprovalDate DATETIME2    NULL,
        ManagerNote NVARCHAR(MAX) NULL,
        Reason      NVARCHAR(MAX) NULL,
        CONSTRAINT FK_Brands_Users_RequestedBy FOREIGN KEY (RequestedBy) REFERENCES Users(UserId),
        CONSTRAINT FK_Brands_Users_ApprovedBy  FOREIGN KEY (ApprovedBy)  REFERENCES Users(UserId)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Products_Brands_BrandId'
)
BEGIN
    ALTER TABLE Products
    ADD CONSTRAINT FK_Products_Brands_BrandId
        FOREIGN KEY (BrandId) REFERENCES Brands(BrandId);
END;
GO

-----------------------------------------------------------
-- 14. DỮ LIỆU MẪU
-----------------------------------------------------------

-- Roles
SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (RoleId, RoleName) VALUES
(1, 'Admin'),
(2, 'Manager'),
(3, 'Sales'),
(4, 'Operations'),
(5, 'Customer');
SET IDENTITY_INSERT Roles OFF;

-- Users (password: '123456')
DECLARE @DefaultHash VARCHAR(255) =
    '$2a$11$4B8fqt/E13Omla6aizXha.a5AS8n4gFWK1IOhObvoA3BkKcXwOAJe';

INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, RoleId)
VALUES 
(N'Hệ thống Admin',  'admin@visioncare.com',   @DefaultHash, '0911222333', 1),
(N'Nguyễn Quản Lý',  'manager@visioncare.com', @DefaultHash, '0922333444', 2),
(N'Lê Nhân Viên',    'sales@visioncare.com',   @DefaultHash, '0933444555', 3),
(N'Nhân Viên Kho',   'ops@visioncare.com',     @DefaultHash, '0944555666', 4),
(N'Trần Khách Hàng', 'customer@visioncare.com',@DefaultHash, '0955666777', 5);

-- Categories
SET IDENTITY_INSERT Categories ON;
INSERT INTO Categories (CategoryId, CategoryName) VALUES
(1, N'Gọng tròn'),
(2, N'Gọng vuông'),
(3, N'Kính mát'),
(4, N'Kính râm'),
(5, N'Tròng kính');
SET IDENTITY_INSERT Categories OFF;

-- Products
SET IDENTITY_INSERT Products ON;
INSERT INTO Products (ProductId, CategoryId, ProductName, Brand, Description, BasePrice, IsFrame, IsLens, IsPreOrder, Image2D)
VALUES
(1,   1, N'Vision Round X1',          N'VisionCare', N'Gọng kính tròn hiện đại',                        800000, 1, 0, 0,
 'https://thumbs.dreamstime.com/b/thin-black-metal-frame-round-clear-lens-eyeglasses-white-background-lenses-isolated-showcasing-their-simple-design-403319062.jpg'),
(2,   2, N'Aura Modern Rectangular',  N'Aura',       N'Gọng kính vuông mạnh mẽ',                        950000, 1, 0, 0,
 'https://media.istockphoto.com/id/1149723055/photo/black-rectangular-glasses-isolated-on-white.jpg?s=612x612&w=0&k=20&c=K5O_7r2R1s-K4sK1F1-qQn7y-Y6Y-8yX7M2F2aB'),
(50,  3, N'Ray-Ban Aviator Classic',  N'Ray-Ban',    N'Kính mát phi công huyền thoại',                 3500000, 0, 0, 0,
 'https://images.ray-ban.com/is/image/RayBan/805289602057__002.png?impolicy=RB_Product_DRP&width=1024'),
(51,  3, N'Oakley Holbrook',          N'Oakley',     N'Kính mát thể thao năng động',                   4200000, 0, 0, 0,
 'https://media.oakley.com/pdp-main-retina/top-navigation-oakley/888392100827_holbrook_matte-black-prizm-grey.png'),
(60,  4, N'Polaroid Navigator',       N'Polaroid',   N'Kính râm đặt trước - Phiên bản giới hạn',      1800000, 0, 0, 1,
 'https://www.polaroid-eyewear.com/content/dam/polaroid/products/2021/PLD_2097_S_807_M9_01.png'),
(100, 5, N'Essilor Crizal Sapphire HR', N'Essilor',  N'Tròng kính chống chói cao cấp',                 1250000, 0, 1, 0,
 'https://u-pose.com/wp-content/uploads/2023/11/Crizal-Sapphire-HR-Sapphire-Blue-Color-Sample.jpg'),
(101, 5, N'Zeiss BlueGuard Platinum', N'Zeiss',      N'Tròng kính lọc ánh sáng xanh',                 1850000, 0, 1, 0,
 'https://www.zeiss.com/content/dam/vision-care/images/products/zeiss-blueguard-lenses/zeiss-blueguard-lenses-product-image.png');
SET IDENTITY_INSERT Products OFF;

-- ProductVariants
SET IDENTITY_INSERT ProductVariants ON;
INSERT INTO ProductVariants (VariantId, ProductId, Color, Size, SKU, StockQuantity)
VALUES
(1,    1,   N'Đen mờ',        N'M',        'VRX1-BLK-M',   10),
(2,    1,   N'Vàng hồng',     N'S',        'VRX1-GLD-S',    5),
(3,    2,   N'Xám khói',      N'L',        'AMR-GRY-L',    15),
(501,  50,  N'Xanh lá G-15',  N'Standard', 'RB-AVI-G15',   20),
(511,  51,  N'Đen Prizm',     N'Standard', 'OK-HOL-PRZ',   12),
(601,  60,  N'Đen',           N'Standard', 'PL-NAV-BLK',    0),
(1001, 100, N'1.56',          N'Standard', 'ES-SAP-156',  500),
(1002, 100, N'1.67 High-Index', N'Standard','ES-SAP-167', 200),
(1011, 101, N'1.60',          N'Standard', 'ZS-BG-160',  100);
SET IDENTITY_INSERT ProductVariants OFF;

-- ShippingMethods
INSERT INTO ShippingMethods (MethodCode, MethodName, Provider, BaseFee, IsActive)
VALUES
('GHTK', N'Giao hàng tiết kiệm', 'GHTK', 25000, 1),
('GHN',  N'Giao hàng nhanh',     'GHN',  30000, 1);

-- Warehouses
INSERT INTO Warehouses (WarehouseCode, WarehouseName, IsPrimary, StreetAddress)
VALUES ('WH-HCM-01', N'Kho chính Hồ Chí Minh', 1, N'123 Nguyễn Lương Bằng, Quận 7');

-- Pre-order Campaign
SET IDENTITY_INSERT PreOrderCampaigns ON;
INSERT INTO PreOrderCampaigns (
    CampaignId, CampaignCode, CampaignName, Description,
    StartDate, EndDate, ReleaseDate,
    DiscountPercent, MaxQuantity, MaxPerCustomer, Status,
    IsFeatured, DepositRatio, CreatedAt
)
VALUES (
    1,
    'CAMP-PO-NAV',
    N'Pre-order Polaroid Navigator',
    N'Chiến dịch đặt trước kính Polaroid Navigator phiên bản giới hạn.',
    DATEADD(day, -7, GETDATE()),
    DATEADD(day,  7, GETDATE()),
    DATEADD(day, 14, GETDATE()),
    10,
    100,
    5,
    'active',
    1,
    0.3,
    GETDATE()
);
SET IDENTITY_INSERT PreOrderCampaigns OFF;

INSERT INTO PreOrderCampaignProducts (CampaignId, ProductId, VariantId, CampaignPrice, CreatedAt)
VALUES (1, 60, NULL, 1600000, GETDATE());

-----------------------------------------------------------
-- 15. SEED SYSTEM SETTINGS
-----------------------------------------------------------
INSERT INTO SystemSettings (SettingKey, SettingValue, Description, GroupName)
VALUES 
('SiteName',              'VisionCare - Hệ thống bán lẻ kính mắt', 'Tên website chính thức',               'General'),
('SupportEmail',          'contact@visioncare.vn',                  'Email hỗ trợ khách hàng',              'General'),
('ShippingInnerCityFee',  '25000',                                  'Phí ship nội thành (VND)',            'Shipping'),
('ShippingOuterCityFee',  '35000',                                  'Phí ship ngoại thành (VND)',          'Shipping'),
('ShippingFreeThreshold', '2000000',                                'Ngưỡng miễn phí vận chuyển (VND)',    'Shipping'),
('SmtpServer',            'smtp.visioncare.vn',                     'Máy chủ gửi Email',                    'Email'),
('SmtpPort',              '587',                                    'Cổng SMTP',                            'Email');

-----------------------------------------------------------
-- 16. SEED BRANDS + MAP BrandId + REQUEST PENDING
-----------------------------------------------------------
DELETE FROM Brands;

INSERT INTO Brands (BrandName, Description, EvidenceUrl, Status, RequestType, RequestDate, ApprovedBy, ApprovalDate)
VALUES 
(N'VisionCare',
 N'Thương hiệu nội bộ của hệ thống VisionCare, chuyên cung cấp các dòng kính gọng nhựa cao cấp.',
 N'https://images.unsplash.com/photo-1591076482161-42ce6da69f67?auto=format&fit=crop&q=80&w=1000',
 N'Approved', N'Add', GETUTCDATE(), 1, GETUTCDATE()),
(N'Aura',
 N'Thương hiệu mắt kính thời trang hiện đại với thiết kế tối giản và tinh tế.',
 N'https://images.unsplash.com/photo-1572635196237-14b3f281503f?auto=format&fit=crop&q=80&w=1000',
 N'Approved', N'Add', GETUTCDATE(), 1, GETUTCDATE()),
(N'Ray-Ban',
 N'Biểu tượng kính mát toàn cầu, nổi tiếng với chất lượng và phong cách không lỗi thời.',
 N'https://images.unsplash.com/photo-1511499767390-91f896299737?auto=format&fit=crop&q=80&w=1000',
 N'Approved', N'Add', GETUTCDATE(), 1, GETUTCDATE()),
(N'Oakley',
 N'Dẫn đầu về công nghệ mắt kính thể thao, bảo vệ mắt tối đa trong mọi điều kiện.',
 N'https://images.unsplash.com/photo-1473496169904-658ba7c44d8a?auto=format&fit=crop&q=80&w=1000',
 N'Approved', N'Add', GETUTCDATE(), 1, GETUTCDATE()),
(N'Polaroid',
 N'Tiên phong trong công nghệ tròng kính phân cực, giúp loại bỏ ánh sáng chói hiệu quả.',
 N'https://images.unsplash.com/photo-1508296695146-257a814070b4?auto=format&fit=crop&q=80&w=1000',
 N'Approved', N'Add', GETUTCDATE(), 1, GETUTCDATE()),
(N'Essilor',
 N'Tròng kính kỹ thuật số cao cấp từ Pháp, chuyên gia về chăm sóc thị lực.',
 N'https://images.unsplash.com/photo-1577174881658-0f30ed549adc?auto=format&fit=crop&q=80&w=1000',
 N'Approved', N'Add', GETUTCDATE(), 1, GETUTCDATE()),
(N'Zeiss',
 N'Thương hiệu quang học hàng đầu thế giới từ Đức với độ chính xác tuyệt đối.',
 N'https://images.unsplash.com/photo-1582142306909-195724d33ffc?auto=format&fit=crop&q=80&w=1000',
 N'Approved', N'Add', GETUTCDATE(), 1, GETUTCDATE());

UPDATE P
SET P.BrandId = B.BrandId
FROM Products P
INNER JOIN Brands B ON P.Brand = B.BrandName
WHERE B.Status = N'Approved';

INSERT INTO Brands (BrandName, Description, EvidenceUrl, Status, RequestType, RequestedBy, RequestDate)
VALUES 
(N'Gucci Luxury',
 N'Yêu cầu cấp phép cho dòng sản phẩm Gucci Limited Edition 2024.',
 N'https://images.unsplash.com/photo-1591076482161-42ce6da69f67?q=80&w=1000',
 N'Pending', N'Add', 3, GETUTCDATe()),
(N'Versace Sport',
 N'Yêu cầu thêm dòng Versace Sport dành cho vận động viên chuyên nghiệp.',
 N'https://images.unsplash.com/photo-1572635196237-14b3f281503f?q=80&w=1000',
 N'Pending', N'Add', 3, GETUTCDATE());

-----------------------------------------------------------
-- 17. ĐỒNG BỘ TỒN KHO
-----------------------------------------------------------
TRUNCATE TABLE Inventories;

INSERT INTO Inventories (
    VariantId,
    WarehouseId,
    QuantityOnHand,
    QuantityReserved,
    QuantityDefective,
    QuantityTransit,
    UpdatedAt
)
SELECT
    VariantId,
    1 AS WarehouseId,
    StockQuantity,
    0, 0, 0,
    SYSUTCDATETIME()
FROM ProductVariants;

PRINT 'Đã đồng bộ số lượng sản phẩm vào kho WH-HCM-01 thành công!';
PRINT 'MASTER DATABASE INITIALIZATION COMPLETED (V3.4 FINAL - CLEAN ONE-FILE)!';