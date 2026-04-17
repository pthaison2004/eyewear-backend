USE [VisionCare];
GO

-- 1. Thêm cột PaidAmount để lưu số tiền đã cọc (30% hoặc 100%)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'PaidAmount')
BEGIN
    ALTER TABLE [dbo].[Orders] ADD [PaidAmount] DECIMAL(18, 2) NULL;
    PRINT 'Đã thêm cột PaidAmount vào bảng Orders';
END

-- 2. Thêm cột PreOrderDeadline để lưu hạn chót 15 ngày chờ hàng
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = N'PreOrderDeadline')
BEGIN
    ALTER TABLE [dbo].[Orders] ADD [PreOrderDeadline] DATETIME NULL;
    PRINT 'Đã thêm cột PreOrderDeadline vào bảng Orders';
END

GO
PRINT 'Đồng bộ Database hoàn tất!';
