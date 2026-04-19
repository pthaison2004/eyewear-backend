-- VisionCare Schema: Suppliers table for inventory receipts
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Suppliers')
BEGIN
    CREATE TABLE dbo.Suppliers (
        SupplierId INT IDENTITY(1,1) PRIMARY KEY,
        SupplierCode NVARCHAR(50) NOT NULL,
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

    CREATE UNIQUE INDEX IX_Suppliers_SupplierCode ON dbo.Suppliers(SupplierCode);

    -- Seed test data
    INSERT INTO dbo.Suppliers (SupplierCode, SupplierName, ContactName, PhoneNumber, Email, Address, TaxCode, IsActive)
    VALUES
        ('SUP-001', N'Công ty TNHH Kính Mắt Quốc Tế', N'Nguyễn Văn Minh', '0901234567', 'minh@kinhmatquocte.vn', N'123 Nguyễn Trãi, Q.1, TP.HCM', '0123456789', 1),
        ('SUP-002', N'LensPro Vietnam', N'Trần Thị Lan', '0908765432', 'lan@lensprovn.com', N'456 Lê Lợi, Q.3, TP.HCM', '9876543210', 1),
        ('SUP-003', N'Thế Giới Kính Mắt', N'Lê Hoàng Nam', '0912345678', 'nam@thegioikinhmat.vn', N'789 Pasteur, Q.1, TP.HCM', '1122334455', 1);

    PRINT 'Suppliers table created and seeded.';
END
ELSE
BEGIN
    PRINT 'Suppliers table already exists.';
END
GO

-- NOTE: DepositRatio/MinDepositAmount schema is in VisionCare.sql.schema.deposit.sql