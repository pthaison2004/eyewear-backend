/*
===========================================================
  VISION CARE - DATABASE INITIALIZATION SCRIPT
  Version: 2.0 (Optimized for .NET 10 & JWT Auth)
===========================================================
*/

USE master;
GO

-- 1. Khởi tạo Database sạch
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'VisionCare')
BEGIN
    ALTER DATABASE VisionCare SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE VisionCare;
END
GO

CREATE DATABASE VisionCare;
GO

USE VisionCare;
GO

-----------------------------------------------------------
-- 2. PHÂN QUYỀN & NGƯỜI DÙNG
-----------------------------------------------------------
CREATE TABLE Roles (
    RoleId INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(20),
    Address NVARCHAR(MAX),
    RoleId INT NOT NULL CONSTRAINT FK_Users_Roles FOREIGN KEY REFERENCES Roles(RoleId),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    IsActive BIT DEFAULT 1,
    -- Authentication
    RefreshToken VARCHAR(MAX) NULL,
    RefreshTokenExpiryTime DATETIME2 NULL
);

-----------------------------------------------------------
-- 3. SẢN PHẨM & DANH MỤC
-----------------------------------------------------------
CREATE TABLE Categories (
    CategoryId INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(MAX)
);

CREATE TABLE Products (
    ProductId INT PRIMARY KEY IDENTITY(1,1),
    CategoryId INT NOT NULL CONSTRAINT FK_Products_Categories FOREIGN KEY REFERENCES Categories(CategoryId),
    ProductName NVARCHAR(200) NOT NULL,
    Brand NVARCHAR(100),
    Description NVARCHAR(MAX),
    BasePrice DECIMAL(18, 2) NOT NULL DEFAULT 0,
    IsPreOrder BIT DEFAULT 0,
    Image2D VARCHAR(MAX),
    Model3D VARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

CREATE TABLE ProductVariants (
    VariantId INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT NOT NULL CONSTRAINT FK_Variants_Products FOREIGN KEY REFERENCES Products(ProductId),
    Color NVARCHAR(50),
    Size NVARCHAR(50),
    SKU VARCHAR(50) NOT NULL UNIQUE,
    StockQuantity INT NOT NULL DEFAULT 0,
    AdditionalPrice DECIMAL(18, 2) NOT NULL DEFAULT 0
);

-----------------------------------------------------------
-- 4. NGHIỆP VỤ KÍNH & ĐƠN HÀNG
-----------------------------------------------------------
CREATE TABLE Prescriptions (
    PrescriptionId INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL CONSTRAINT FK_Prescriptions_Users FOREIGN KEY REFERENCES Users(UserId),
    OD_Sphere DECIMAL(5, 2), -- Kính phải - Cầu
    OD_Cylinder DECIMAL(5, 2), -- Kính phải - Trụ
    OD_Axis INT,
    OS_Sphere DECIMAL(5, 2), -- Kính trái - Cầu
    OS_Cylinder DECIMAL(5, 2), -- Kính trái - Trụ
    OS_Axis INT,
    PD DECIMAL(5, 2), -- Khoảng cách đồng tử
    Note NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Orders (
    OrderId INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL CONSTRAINT FK_Orders_Users FOREIGN KEY REFERENCES Users(UserId),
    OrderDate DATETIME2 DEFAULT SYSUTCDATETIME(),
    TotalAmount DECIMAL(18, 2) NOT NULL,
    OrderStatus NVARCHAR(50) DEFAULT N'Pending', 
    PaymentStatus NVARCHAR(50) DEFAULT N'Unpaid', 
    OrderType NVARCHAR(50) DEFAULT N'Direct', -- Online/AtStore
    ShippingAddress NVARCHAR(MAX),
    TrackingNumber VARCHAR(100),
    StaffNote NVARCHAR(MAX)
);

CREATE TABLE OrderItems (
    OrderItemId INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT NOT NULL CONSTRAINT FK_Items_Orders FOREIGN KEY REFERENCES Orders(OrderId),
    VariantId INT NOT NULL CONSTRAINT FK_Items_Variants FOREIGN KEY REFERENCES ProductVariants(VariantId),
    PrescriptionId INT NULL CONSTRAINT FK_Items_Prescriptions FOREIGN KEY REFERENCES Prescriptions(PrescriptionId),
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18, 2) NOT NULL
);

-----------------------------------------------------------
-- 5. CHƯƠNG TRÌNH KHUYẾN MÃI
-----------------------------------------------------------
CREATE TABLE Promotions (
    PromotionId INT PRIMARY KEY IDENTITY(1,1),
    PromoCode VARCHAR(50) NOT NULL UNIQUE,
    DiscountPercent INT NOT NULL CHECK (DiscountPercent >= 0 AND DiscountPercent <= 100),
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    IsActive BIT DEFAULT 1
);

-----------------------------------------------------------
-- 6. DỮ LIỆU MẪU 
-----------------------------------------------------------
-- Chèn Roles chuẩn
SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (RoleId, RoleName) VALUES 
(1, 'Admin'), (2, 'Manager'), (3, 'Sales'), (4, 'Operations'), (5, 'Customer');
SET IDENTITY_INSERT Roles OFF;

-- Mật khẩu mặc định: 123456 (Đã hash BCrypt)
DECLARE @DefaultHash VARCHAR(255) = '$2a$11$4B8fqt/E13Omla6aizXha.a5AS8n4gFWK1IOhObvoA3BkKcXwOAJe';

INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, RoleId) VALUES 
(N'Hệ thống Admin', 'admin@visioncare.com', @DefaultHash, '0911222333', 1),
(N'Nguyễn Quản Lý', 'manager@visioncare.com', @DefaultHash, '0922333444', 2),
(N'Lê Nhân Viên', 'sales@visioncare.com', @DefaultHash, '0933444555', 3),
(N'Nhân Viên Kho', 'ops@visioncare.com', @DefaultHash, '0944555666', 4),
(N'Trần Khách Hàng', 'customer@visioncare.com', @DefaultHash, '0955666777', 5);

GO

-- Kiểm tra
SELECT U.FullName, R.RoleName, U.Email 
FROM Users U JOIN Roles R ON U.RoleId = R.RoleId;
