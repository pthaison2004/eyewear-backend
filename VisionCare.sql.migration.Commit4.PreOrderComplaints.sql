-- ============================================================
-- Commit #4: Sales Pre-order & Complaints
-- Creates PreOrderCampaign, PreOrderReservation, Complaint tables
-- ============================================================

USE VisionCare;
GO

-- ============================================================
-- PreOrderCampaign table
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PreOrderCampaigns')
BEGIN
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
        MaxPerCustomer INT NOT NULL DEFAULT 1,
        CurrentReserved INT NOT NULL DEFAULT 0,
        Status NVARCHAR(50) NOT NULL DEFAULT 'draft',
        IsFeatured BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );
END
GO

-- ============================================================
-- PreOrderCampaignProducts (link products to campaigns)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PreOrderCampaignProducts')
BEGIN
    CREATE TABLE PreOrderCampaignProducts (
        CampaignProductId INT IDENTITY(1,1) PRIMARY KEY,
        CampaignId INT NOT NULL CONSTRAINT FK_PreOrderCP_Campaign FOREIGN KEY REFERENCES PreOrderCampaigns(CampaignId),
        ProductId INT NOT NULL CONSTRAINT FK_PreOrderCP_Product FOREIGN KEY REFERENCES Products(ProductId),
        VariantId INT NULL CONSTRAINT FK_PreOrderCP_Variant FOREIGN KEY REFERENCES ProductVariants(VariantId),
        CampaignPrice DECIMAL(18, 2) NOT NULL,
        ReservedQuantity INT NOT NULL DEFAULT 0,
        ReceivedQuantity INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- ============================================================
-- PreOrderReservation table
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PreOrderReservations')
BEGIN
    CREATE TABLE PreOrderReservations (
        ReservationId INT IDENTITY(1,1) PRIMARY KEY,
        ReservationCode NVARCHAR(50) NOT NULL UNIQUE,
        CampaignId INT NOT NULL CONSTRAINT FK_PreOR_Campaign FOREIGN KEY REFERENCES PreOrderCampaigns(CampaignId),
        CustomerId INT NOT NULL CONSTRAINT FK_PreOR_Customer FOREIGN KEY REFERENCES Users(UserId),
        VariantId INT NOT NULL CONSTRAINT FK_PreOR_Variant FOREIGN KEY REFERENCES ProductVariants(VariantId),
        ReservedQuantity INT NOT NULL,
        UnitPrice DECIMAL(18, 2) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'reserved',
        ConvertedOrderId INT NULL CONSTRAINT FK_PreOR_Order FOREIGN KEY REFERENCES Orders(OrderId),
        ExpiresAt DATETIME2 NOT NULL,
        PaidAt DATETIME2 NULL,
        FulfilledAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- ============================================================
-- Complaint table
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Complaints')
BEGIN
    CREATE TABLE Complaints (
        ComplaintId INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL CONSTRAINT FK_Complaint_Order FOREIGN KEY REFERENCES Orders(OrderId),
        CustomerId INT NOT NULL CONSTRAINT FK_Complaint_Customer FOREIGN KEY REFERENCES Users(UserId),
        ComplaintType NVARCHAR(100) NOT NULL,
        Subject NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL,
        ComplaintStatus NVARCHAR(50) NOT NULL DEFAULT 'open',
        Priority NVARCHAR(20) NOT NULL DEFAULT 'normal',
        AssignedTo INT NULL CONSTRAINT FK_Complaint_Staff FOREIGN KEY REFERENCES Users(UserId),
        ProcessedAt DATETIME2 NULL,
        ProcessedBy INT NULL CONSTRAINT FK_Complaint_ProcessedBy FOREIGN KEY REFERENCES Users(UserId),
        ProcessedNote NVARCHAR(MAX) NULL,
        ResolvedAt DATETIME2 NULL,
        ResolvedBy INT NULL CONSTRAINT FK_Complaint_ResolvedBy FOREIGN KEY REFERENCES Users(UserId),
        Resolution NVARCHAR(MAX) NULL,
        CustomerSatisfaction INT NULL CHECK (CustomerSatisfaction >= 1 AND CustomerSatisfaction <= 5),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );
END
GO

-- ============================================================
-- Seed test data
-- ============================================================

-- Seed a PreOrderCampaign
IF NOT EXISTS (SELECT 1 FROM PreOrderCampaigns)
BEGIN
    INSERT INTO PreOrderCampaigns (CampaignCode, CampaignName, Description, StartDate, EndDate, ReleaseDate, Status, MaxQuantity, MaxPerCustomer, IsFeatured, CurrentReserved)
    VALUES
    ('PO-2026-001', N'Collection Xuân 2026', N'Bộ sưu tập kính mắt mùa xuân 2026 - Limited Edition', DATEADD(day, -10, GETDATE()), DATEADD(day, 30, GETDATE()), DATEADD(day, 45, GETDATE()), 'active', 100, 2, 1, 15),
    ('PO-2026-002', N'Kính Polarized Mùa Hè', N'Kính râm polarized công nghệ mới', DATEADD(day, -5, GETDATE()), DATEADD(day, 20, GETDATE()), DATEADD(day, 35, GETDATE()), 'active', 50, 1, 0, 8);
END
GO

-- Link products to campaigns
IF NOT EXISTS (SELECT 1 FROM PreOrderCampaignProducts)
BEGIN
    INSERT INTO PreOrderCampaignProducts (CampaignId, ProductId, VariantId, CampaignPrice)
    SELECT 1, p.ProductId, v.VariantId, v.AdditionalPrice + p.BasePrice * 0.85
    FROM Products p
    CROSS JOIN ProductVariants v
    WHERE p.ProductId = v.ProductId AND p.ProductId IN (1, 2, 6)
    AND NOT EXISTS (SELECT 1 FROM PreOrderCampaignProducts WHERE CampaignId = 1 AND VariantId = v.VariantId);

    INSERT INTO PreOrderCampaignProducts (CampaignId, ProductId, VariantId, CampaignPrice)
    SELECT 2, p.ProductId, v.VariantId, v.AdditionalPrice + p.BasePrice * 0.80
    FROM Products p
    CROSS JOIN ProductVariants v
    WHERE p.ProductId = v.ProductId AND p.ProductId IN (8, 9)
    AND NOT EXISTS (SELECT 1 FROM PreOrderCampaignProducts WHERE CampaignId = 2 AND VariantId = v.VariantId);
END
GO

-- Seed PreOrderReservations (link to customer 5, orders already exist)
IF NOT EXISTS (SELECT 1 FROM PreOrderReservations)
BEGIN
    INSERT INTO PreOrderReservations (ReservationCode, CampaignId, CustomerId, VariantId, ReservedQuantity, UnitPrice, Status, ExpiresAt, PaidAt)
    VALUES
    ('RES-001', 1, 5, 1, 1, 800000 * 0.85, 'paid', DATEADD(day, 30, GETDATE()), DATEADD(day, -2, GETDATE())),
    ('RES-002', 1, 5, 5, 1, 1200000 * 0.85, 'paid', DATEADD(day, 30, GETDATE()), DATEADD(day, -1, GETDATE())),
    ('RES-003', 2, 5, 18, 1, 700000 * 0.80, 'paid', DATEADD(day, 20, GETDATE()), GETDATE());
END
GO

-- Seed Complaints
IF NOT EXISTS (SELECT 1 FROM Complaints)
BEGIN
    INSERT INTO Complaints (OrderId, CustomerId, ComplaintType, Subject, Description, ComplaintStatus, Priority, CreatedAt)
    VALUES
    (1, 5, N'Sản phẩm', N'Gọng kính bị trầy', N'Nhận hàng thấy gọng kính có vết trầy trên bề mặt', 'open', 'normal', DATEADD(day, -1, GETDATE())),
    (2, 5, N'Giao hàng', N'Giao chậm 3 ngày', N'Đơn hàng giao trễ hơn dự kiến 3 ngày', 'processing', 'low', DATEADD(day, -3, GETDATE())),
    (5, 5, N'Sản phẩm', N'Sai kích thước', N'Kính gửi về kích thước không đúng như đã đặt', 'open', 'high', DATEADD(day, -5, GETDATE()));
END
GO

PRINT 'Commit #4 migration completed: PreOrderCampaigns, PreOrderReservations, Complaints tables created and seeded.';
GO
