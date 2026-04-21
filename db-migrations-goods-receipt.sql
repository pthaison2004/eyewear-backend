USE VisionCare;
GO

-- 1. Create GoodsReceipts Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GoodsReceipts')
BEGIN
    CREATE TABLE GoodsReceipts (
        GoodsReceiptId INT IDENTITY(1,1) PRIMARY KEY,
        ReceiptNumber NVARCHAR(50) NOT NULL,
        CampaignId INT NULL,
        CreatedBy INT NOT NULL,
        ManagerId INT NULL,
        WarehouseId INT NOT NULL,
        Status NVARCHAR(20) DEFAULT 'draft',
        CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
        CompletedAt DATETIME2 NULL,
        Note NVARCHAR(MAX) NULL,
        CONSTRAINT FK_GoodsReceipts_Campaigns FOREIGN KEY (CampaignId) REFERENCES PreOrderCampaigns(CampaignId) ON DELETE SET NULL,
        CONSTRAINT FK_GoodsReceipts_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(UserId),
        CONSTRAINT FK_GoodsReceipts_Manager FOREIGN KEY (ManagerId) REFERENCES Users(UserId),
        CONSTRAINT FK_GoodsReceipts_Warehouse FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId)
    );
END
GO

-- 2. Create GoodsReceiptDetails Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GoodsReceiptDetails')
BEGIN
    CREATE TABLE GoodsReceiptDetails (
        DetailId INT IDENTITY(1,1) PRIMARY KEY,
        GoodsReceiptId INT NOT NULL,
        VariantId INT NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NULL,
        CONSTRAINT FK_GoodsReceiptDetails_Receipt FOREIGN KEY (GoodsReceiptId) REFERENCES GoodsReceipts(GoodsReceiptId) ON DELETE CASCADE,
        CONSTRAINT FK_GoodsReceiptDetails_Variant FOREIGN KEY (VariantId) REFERENCES ProductVariants(VariantId)
    );
END
GO

-- 3. Add Manager test account if not exists
IF NOT EXISTS (SELECT * FROM Users WHERE Email = 'manager@visioncare.com')
BEGIN
    INSERT INTO Users (RoleId, Email, PasswordHash, FullName, PhoneNumber, IsActive)
    VALUES (2, 'manager@visioncare.com', '$2a$11$w.A/1kR1R.524wL9O1rOHu/Qh4r0cOHnL4D7w/sOZh3Q4l2eMw.Oq', 'Manager Account', '0999999999', 1)
END
GO
