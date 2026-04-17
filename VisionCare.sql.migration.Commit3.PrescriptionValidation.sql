-- ============================================================
-- Commit #3: Sales Prescription Verification
-- Adds verification & rejection columns to Prescriptions table
-- ============================================================

USE VisionCare;
GO

-- Verification columns
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'IsVerified')
    ALTER TABLE Prescriptions ADD IsVerified BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'VerifiedAt')
    ALTER TABLE Prescriptions ADD VerifiedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'VerifiedBy')
    ALTER TABLE Prescriptions ADD VerifiedBy INT NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'IsRejected')
    ALTER TABLE Prescriptions ADD IsRejected BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'RejectedAt')
    ALTER TABLE Prescriptions ADD RejectedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'RejectedBy')
    ALTER TABLE Prescriptions ADD RejectedBy INT NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'RejectionReason')
    ALTER TABLE Prescriptions ADD RejectionReason NVARCHAR(500) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PrescriptionValidationRules')
BEGIN
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

    -- Seed default validation rules
    INSERT INTO PrescriptionValidationRules (RuleName, RuleType, MinValue, MaxValue, IsActive, Description, SortOrder, CreatedAt) VALUES
    (N'Max Sphere Value', 'sphere_max', NULL, -20, 1, N'Cận thị tối đa -20.00', 1, GETUTCDATE()),
    (N'Max Cylinder Value', 'cylinder_max', NULL, -6, 1, N'Loạn thị tối đa -6.00', 2, GETUTCDATE()),
    (N'PD Range', 'pd_range', 50, 80, 1, N'PD hợp lệ 50-80mm', 3, GETUTCDATE()),
    (N'Min Age', 'min_age', 5, NULL, 1, N'Độ tuổi tối thiểu 5 tuổi', 4, GETUTCDATE()),
    (N'Expiry Months', 'expiry_months', NULL, 24, 1, N'Đơn kính có hiệu lực trong 24 tháng', 5, GETUTCDATE()),
    (N'Prescription Age Warning', 'age_warning', NULL, 12, 1, N'Cảnh báo nếu đơn kính trên 12 tháng', 6, GETUTCDATE());
END
GO

PRINT 'Commit #3 migration completed: prescription verification columns and rules table added.';
GO