USE VisionCare;
GO

-- Thêm cột ProofImage và ApprovedAt vào bảng GoodsReceipts
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('GoodsReceipts') AND name = 'ProofImage')
BEGIN
    ALTER TABLE GoodsReceipts ADD ProofImage NVARCHAR(MAX) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('GoodsReceipts') AND name = 'ApprovedAt')
BEGIN
    ALTER TABLE GoodsReceipts ADD ApprovedAt DATETIME2 NULL;
END

-- Cập nhật giá trị mặc định cho Status nếu cần (từ draft sang PendingApproval)
UPDATE GoodsReceipts SET Status = 'PendingApproval' WHERE Status = 'draft';

PRINT 'Migration: GoodsReceipts table updated successfully.';
GO
