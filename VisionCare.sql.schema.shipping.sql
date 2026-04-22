IF OBJECT_ID('dbo.ShippingMethods', 'U') IS NOT NULL DROP TABLE dbo.ShippingMethods;
IF OBJECT_ID('dbo.ShippingOrders', 'U') IS NOT NULL DROP TABLE dbo.ShippingOrders;

CREATE TABLE dbo.ShippingMethods (
    ShippingMethodId INT IDENTITY(1,1) PRIMARY KEY,
    MethodCode NVARCHAR(20) NOT NULL,
    MethodName NVARCHAR(100) NOT NULL,
    Provider NVARCHAR(50) NOT NULL,
    BaseFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    FeePerKg DECIMAL(18,2) NOT NULL DEFAULT 0,
    FreeShippingThreshold DECIMAL(18,2) NULL,
    EstimatedDaysMin INT NULL,
    EstimatedDaysMax INT NULL,
    CodAvailable BIT NOT NULL DEFAULT 1,
    MaxCodAmount DECIMAL(18,2) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    SortOrder INT NOT NULL DEFAULT 0
);

CREATE TABLE dbo.ShippingOrders (
    ShippingOrderId INT IDENTITY(1,1) PRIMARY KEY,
    ShippingOrderCode NVARCHAR(50) NOT NULL UNIQUE,
    OrderId INT NOT NULL FOREIGN KEY REFERENCES dbo.Orders(OrderId),
    ShippingMethodId INT NOT NULL FOREIGN KEY REFERENCES dbo.ShippingMethods(ShippingMethodId),
    CarrierTrackingNo NVARCHAR(100) NULL,
    CarrierOrderNo NVARCHAR(100) NULL,
    CarrierStatus NVARCHAR(100) NULL,
    EstimatedDelivery DATETIME2 NULL,
    RecipientName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    ProvinceCode NVARCHAR(20) NULL,
    DistrictCode NVARCHAR(20) NULL,
    WardCode NVARCHAR(20) NULL,
    StreetAddress NVARCHAR(500) NULL,
    DeliveryInstruction NVARCHAR(MAX) NULL,
    ShippingFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    CodFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    InsuranceFee DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalShippingCost DECIMAL(18,2) NOT NULL DEFAULT 0,
    ShippingStatusId INT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ShippedAt DATETIME2 NULL,
    DeliveredAt DATETIME2 NULL
);

INSERT INTO ShippingMethods (MethodCode, MethodName, Provider, BaseFee, FeePerKg, EstimatedDaysMin, EstimatedDaysMax, CodAvailable, SortOrder)
VALUES
    ('GHTK', N'Giao hàng tiết kiệm', 'GHTK', 25000, 3500, 2, 4, 1, 1),
    ('GHN', N'Giao hàng nhanh', 'GHN', 30000, 4000, 1, 3, 1, 2);
