IF OBJECT_ID('dbo.ShippingStatusHistories', 'U') IS NOT NULL DROP TABLE dbo.ShippingStatusHistories;
IF OBJECT_ID('dbo.ShippingStatuses', 'U') IS NOT NULL DROP TABLE dbo.ShippingStatuses;

CREATE TABLE dbo.ShippingStatuses (
    ShippingStatusId INT IDENTITY(1,1) PRIMARY KEY,
    StatusCode NVARCHAR(50) NOT NULL UNIQUE,
    StatusName NVARCHAR(100) NOT NULL,
    StatusOrder INT NOT NULL DEFAULT 0,
    Description NVARCHAR(MAX) NULL
);

CREATE TABLE dbo.ShippingStatusHistories (
    HistoryId INT IDENTITY(1,1) PRIMARY KEY,
    ShippingOrderId INT NOT NULL FOREIGN KEY REFERENCES dbo.ShippingOrders(ShippingOrderId),
    FromStatusId INT NULL FOREIGN KEY REFERENCES dbo.ShippingStatuses(ShippingStatusId),
    ToStatusId INT NOT NULL FOREIGN KEY REFERENCES dbo.ShippingStatuses(ShippingStatusId),
    CarrierStatusText NVARCHAR(200) NULL,
    Location NVARCHAR(200) NULL,
    EstimatedDeliveryUpdated DATETIME2 NULL,
    UpdatedBy INT NULL,
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

INSERT INTO ShippingStatuses (StatusCode, StatusName, StatusOrder, Description) VALUES
    ('Pending', N'Chờ lấy hàng', 1, N'Đơn đang chờ được lấy'),
    ('PickedUp', N'Đã lấy hàng', 2, N'Nhà vận chuyển đã lấy hàng'),
    ('InTransit', N'Đang vận chuyển', 3, N'Hàng đang trên đường giao'),
    ('OutForDelivery', N'Đang giao', 4, N'Nhân viên đang giao đến bạn'),
    ('Delivered', N'Đã giao', 5, N'Đã giao thành công'),
    ('DeliveryFailed', N'Giao thất bại', 6, N'Giao không thành công'),
    ('Returned', N'Đã hoàn về', 7, N'Hàng đã được hoàn về người gửi');

PRINT 'ShippingStatuses and ShippingStatusHistories tables created and seeded.';
