-- ============================================================
-- VisionCare Backend — OrderStatusHistory Schema
-- Run this script in SQL Server to create the table manually
-- (No EF migration used for this commit)
-- ============================================================

IF OBJECT_ID('dbo.OrderStatusHistories', 'U') IS NOT NULL
BEGIN
    PRINT 'Table OrderStatusHistories already exists. Skipping creation.';
END
ELSE
BEGIN
    CREATE TABLE dbo.OrderStatusHistories (
        HistoryId   INT IDENTITY(1,1) NOT NULL,
        OrderId     INT                NOT NULL,
        FromStatus  NVARCHAR(50)       NOT NULL,
        ToStatus    NVARCHAR(50)       NOT NULL,
        Note        NVARCHAR(MAX)      NULL,
        ChangedBy   INT                NOT NULL,
        ChangedAt   DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK__OrderStat__FD25C5D9E3F5A3B6
            PRIMARY KEY CLUSTERED (HistoryId),

        CONSTRAINT FK__OrderStatus__Order__7C7BAF4E
            FOREIGN KEY (OrderId)
            REFERENCES dbo.Orders (OrderId),

        CONSTRAINT FK__OrderStatus__User__7D6B9B87
            FOREIGN KEY (ChangedBy)
            REFERENCES dbo.Users (UserId)
    );

    PRINT 'Table OrderStatusHistory created successfully.';
END
GO