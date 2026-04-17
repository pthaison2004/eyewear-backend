/*
===========================================================
  VISION CARE - MIGRATION SCRIPT
  Bổ sung bảng Carts và CartItems cho nghiệp vụ giỏ hàng
  Migration: Carts + CartItems
===========================================================
*/

USE VisionCare;
GO

-----------------------------------------------------------
-- 1. TẠO BẢNG CARTS
-----------------------------------------------------------
CREATE TABLE Carts (
    CartId INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL CONSTRAINT FK_Carts_Users FOREIGN KEY REFERENCES Users(UserId),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

-- 1 khách hàng chỉ có tối đa 1 giỏ hàng (Unique trên CustomerId)
ALTER TABLE Carts
ADD CONSTRAINT UQ_Carts_CustomerId UNIQUE (CustomerId);
GO

-----------------------------------------------------------
-- 2. TẠO BẢNG CARTITEMS
-----------------------------------------------------------
CREATE TABLE CartItems (
    CartItemId INT PRIMARY KEY IDENTITY(1,1),
    CartId INT NOT NULL CONSTRAINT FK_CartItems_Carts FOREIGN KEY REFERENCES Carts(CartId),
    VariantId INT NOT NULL CONSTRAINT FK_CartItems_Variants FOREIGN KEY REFERENCES ProductVariants(VariantId),
    PrescriptionId INT NULL CONSTRAINT FK_CartItems_Prescriptions FOREIGN KEY REFERENCES Prescriptions(PrescriptionId),
    Quantity INT NOT NULL DEFAULT 1
);
GO

-- CHECK: Quantity phải lớn hơn 0
ALTER TABLE CartItems
ADD CONSTRAINT CK_CartItems_Quantity CHECK (Quantity > 0);
GO

-- Ràng buộc duy nhất: 1 variant + 1 prescription chỉ xuất hiện 1 lần trong 1 giỏ hàng
-- PrescriptionId cho phép NULL nên dùng filtered unique index thay cho unique constraint
CREATE UNIQUE INDEX IX_CartItems_CartId_VariantId_PrescriptionId
ON CartItems (CartId, VariantId, PrescriptionId)
WHERE PrescriptionId IS NOT NULL;
GO

-----------------------------------------------------------
-- 3. TẠO INDEX CHO CÁC KHÓA NGOẠI (Nếu chưa có)
-----------------------------------------------------------
-- Index trên CartItems.CartId (FK → Carts.CartId)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE i.name = 'IX_CartItems_CartId'
      AND i.object_id = OBJECT_ID('CartItems')
      AND c.name = 'CartId'
)
BEGIN
    CREATE INDEX IX_CartItems_CartId ON CartItems (CartId);
END
GO

-- Index trên CartItems.VariantId (FK → ProductVariants.VariantId)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE i.name = 'IX_CartItems_VariantId'
      AND i.object_id = OBJECT_ID('CartItems')
      AND c.name = 'VariantId'
)
BEGIN
    CREATE INDEX IX_CartItems_VariantId ON CartItems (VariantId);
END
GO

-- Index trên CartItems.PrescriptionId (FK → Prescriptions.PrescriptionId, cho phép NULL)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE i.name = 'IX_CartItems_PrescriptionId'
      AND i.object_id = OBJECT_ID('CartItems')
      AND c.name = 'PrescriptionId'
)
BEGIN
    CREATE INDEX IX_CartItems_PrescriptionId ON CartItems (PrescriptionId);
END
GO

-----------------------------------------------------------
-- 4. XÁC MINH CẤU TRÚC
-----------------------------------------------------------
SELECT 'Carts' AS TableName, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Carts'
ORDER BY ORDINAL_POSITION;

SELECT 'CartItems' AS TableName, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'CartItems'
ORDER BY ORDINAL_POSITION;
GO