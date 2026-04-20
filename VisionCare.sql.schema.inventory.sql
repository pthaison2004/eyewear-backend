IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE Name = 'Warehouses')
BEGIN
    CREATE TABLE dbo.Warehouses (
        WarehouseId INT IDENTITY(1,1) PRIMARY KEY,
        WarehouseCode NVARCHAR(20) NOT NULL,
        WarehouseName NVARCHAR(200) NOT NULL,
        WarehouseType NVARCHAR(20) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        IsPrimary BIT NOT NULL DEFAULT 0,
        ProvinceCode NVARCHAR(20) NULL,
        ProvinceName NVARCHAR(100) NULL,
        DistrictCode NVARCHAR(20) NULL,
        DistrictName NVARCHAR(100) NULL,
        WardCode NVARCHAR(20) NULL,
        WardName NVARCHAR(100) NULL,
        StreetAddress NVARCHAR(500) NULL,
        PhoneNumber NVARCHAR(20) NULL,
        Email NVARCHAR(100) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Warehouses table created.';
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE Name = 'Inventories')
BEGIN
    CREATE TABLE dbo.Inventories (
        InventoryId INT IDENTITY(1,1) PRIMARY KEY,
        VariantId INT NOT NULL,
        WarehouseId INT NOT NULL,
        QuantityOnHand INT NOT NULL DEFAULT 0,
        QuantityReserved INT NOT NULL DEFAULT 0,
        QuantityDefective INT NOT NULL DEFAULT 0,
        QuantityTransit INT NOT NULL DEFAULT 0,
        BatchNumber NVARCHAR(50) NULL,
        ManufacturingDate DATE NULL,
        ExpiryDate DATE NULL,
        LastCountAt DATETIME2 NULL,
        LastReplenishAt DATETIME2 NULL,
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK__Inventory__Variant FOREIGN KEY (VariantId) REFERENCES dbo.ProductVariants(VariantId),
        CONSTRAINT FK__Inventory__Warehouse FOREIGN KEY (WarehouseId) REFERENCES dbo.Warehouses(WarehouseId),
        CONSTRAINT UQ__Inventory__Variant__Warehouse UNIQUE (VariantId, WarehouseId)
    );
    PRINT 'Inventories table created.';
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE Name = 'StockMovements')
BEGIN
    CREATE TABLE dbo.StockMovements (
        MovementId INT IDENTITY(1,1) PRIMARY KEY,
        VariantId INT NOT NULL,
        WarehouseId INT NOT NULL,
        MovementType NVARCHAR(30) NOT NULL,
        QuantityBefore INT NOT NULL DEFAULT 0,
        QuantityChange INT NOT NULL DEFAULT 0,
        QuantityAfter INT NOT NULL DEFAULT 0,
        ReferenceType NVARCHAR(30) NULL,
        ReferenceId INT NULL,
        Reason NVARCHAR(500) NULL,
        StaffNote NVARCHAR(1000) NULL,
        PerformedBy INT NULL,
        PerformedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK__StockMovement__Variant FOREIGN KEY (VariantId) REFERENCES dbo.ProductVariants(VariantId),
        CONSTRAINT FK__StockMovement__Warehouse FOREIGN KEY (WarehouseId) REFERENCES dbo.Warehouses(WarehouseId),
        CONSTRAINT FK__StockMovement__User FOREIGN KEY (PerformedBy) REFERENCES dbo.Users(UserId)
    );
    PRINT 'StockMovements table created.';
END

IF NOT EXISTS (SELECT 1 FROM dbo.Warehouses)
BEGIN
    INSERT INTO dbo.Warehouses (WarehouseCode, WarehouseName, WarehouseType, IsActive, IsPrimary, ProvinceCode, ProvinceName, DistrictCode, DistrictName, StreetAddress)
    VALUES ('WH-HCM-01', N'Kho Hồ Chí Minh', 'warehouse', 1, 1, '79', N'TP. Hồ Chí Minh', '760', N'Quận 7', N'123 Đường Nguyễn Lương Bằng, Quận 7');
    INSERT INTO dbo.Warehouses (WarehouseCode, WarehouseName, WarehouseType, IsActive, IsPrimary, ProvinceCode, ProvinceName, DistrictCode, DistrictName, StreetAddress)
    VALUES ('WH-HN-01', N'Kho Hà Nội', 'warehouse', 1, 0, '01', N'Hà Nội', '001', N'Quận Hoàng Mai', N'456 Đường Giải Phóng, Quận Hoàng Mai');
    PRINT 'Default warehouses seeded.';
END

IF NOT EXISTS (SELECT 1 FROM dbo.Inventories) AND EXISTS (SELECT 1 FROM dbo.ProductVariants WHERE StockQuantity > 0)
BEGIN
    INSERT INTO dbo.Inventories (VariantId, WarehouseId, QuantityOnHand)
    SELECT VariantId, 1, ISNULL(StockQuantity, 0) FROM dbo.ProductVariants WHERE StockQuantity > 0;
    PRINT 'Initial inventory seeded from ProductVariants.';
END
GO
PRINT 'Inventory schema complete.';
