/* =====================================================================
   PRN232 - HỆ THỐNG QUẢN LÝ CỬA HÀNG CHO THUÊ XE MÁY
   Database : MotorbikeRentalDB  (SQL Server 2019+ / 2022)
   Cách dùng: Mở SSMS -> New Query -> dán toàn bộ file -> Execute (F5)
   Mật khẩu tất cả tài khoản mẫu: 123456  (BCrypt, dùng BCrypt.Net-Next)
   CẢNH BÁO: Script sẽ XOÁ và TẠO LẠI database MotorbikeRentalDB nếu đã tồn tại.
   ===================================================================== */

USE master;
GO

IF DB_ID(N'MotorbikeRentalDB') IS NOT NULL
BEGIN
    ALTER DATABASE MotorbikeRentalDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MotorbikeRentalDB;
END
GO

CREATE DATABASE MotorbikeRentalDB;
GO

USE MotorbikeRentalDB;
GO

/* =====================================================================
   1. BẢNG DANH MỤC / NGƯỜI DÙNG
   ===================================================================== */

-- 1.1 Vai trò: Admin, Staff, Customer
CREATE TABLE Roles (
    RoleId      INT IDENTITY(1,1)  NOT NULL,
    RoleName    NVARCHAR(50)       NOT NULL,
    Description NVARCHAR(255)      NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);
GO

-- 1.2 Chi nhánh cửa hàng
CREATE TABLE Branches (
    BranchId    INT IDENTITY(1,1)  NOT NULL,
    BranchName  NVARCHAR(100)      NOT NULL,
    Address     NVARCHAR(255)      NOT NULL,
    Phone       VARCHAR(15)        NOT NULL,
    OpenTime    TIME(0)            NOT NULL CONSTRAINT DF_Branches_OpenTime  DEFAULT ('07:00'),
    CloseTime   TIME(0)            NOT NULL CONSTRAINT DF_Branches_CloseTime DEFAULT ('21:00'),
    IsActive    BIT                NOT NULL CONSTRAINT DF_Branches_IsActive  DEFAULT (1),
    CreatedAt   DATETIME2(0)       NOT NULL CONSTRAINT DF_Branches_CreatedAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Branches PRIMARY KEY (BranchId),
    CONSTRAINT UQ_Branches_BranchName UNIQUE (BranchName),
    CONSTRAINT CK_Branches_Time CHECK (CloseTime > OpenTime)
);
GO

-- 1.3 Người dùng (Admin / Staff / Customer dùng chung 1 bảng, phân biệt bằng RoleId)
CREATE TABLE Users (
    UserId              INT IDENTITY(1,1) NOT NULL,
    Email               VARCHAR(100)      NOT NULL,
    PasswordHash        VARCHAR(255)      NOT NULL,
    FullName            NVARCHAR(100)     NOT NULL,
    Phone               VARCHAR(15)       NULL,
    DateOfBirth         DATE              NULL,
    Address             NVARCHAR(255)     NULL,
    IdentityNumber      VARCHAR(12)       NULL,   -- CCCD 12 số (bắt buộc với khách khi nhận xe - kiểm tra ở code)
    DriverLicenseNumber VARCHAR(20)       NULL,   -- Số GPLX
    AvatarUrl           NVARCHAR(500)     NULL,
    RoleId              INT               NOT NULL,
    BranchId            INT               NULL,   -- Chỉ dùng cho Staff: nhân viên thuộc chi nhánh nào
    IsActive            BIT               NOT NULL CONSTRAINT DF_Users_IsActive  DEFAULT (1),
    CreatedAt           DATETIME2(0)      NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt           DATETIME2(0)      NULL,
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Roles    FOREIGN KEY (RoleId)   REFERENCES Roles(RoleId),
    CONSTRAINT FK_Users_Branches FOREIGN KEY (BranchId) REFERENCES Branches(BranchId),
    CONSTRAINT CK_Users_Email    CHECK (Email LIKE '%_@_%._%'),
    CONSTRAINT CK_Users_Phone    CHECK (Phone IS NULL OR (LEN(Phone) BETWEEN 10 AND 11 AND Phone NOT LIKE '%[^0-9]%')),
    CONSTRAINT CK_Users_IdentityNumber CHECK (IdentityNumber IS NULL OR (LEN(IdentityNumber) = 12 AND IdentityNumber NOT LIKE '%[^0-9]%')),
    CONSTRAINT CK_Users_DateOfBirth    CHECK (DateOfBirth IS NULL OR DateOfBirth <= DATEADD(YEAR, -18, CAST(GETDATE() AS DATE)))
);
GO
-- CCCD và SĐT là duy nhất nếu có nhập (filtered unique index cho phép nhiều NULL)
CREATE UNIQUE INDEX UX_Users_IdentityNumber ON Users(IdentityNumber) WHERE IdentityNumber IS NOT NULL;
CREATE UNIQUE INDEX UX_Users_Phone          ON Users(Phone)          WHERE Phone IS NOT NULL;
CREATE INDEX IX_Users_RoleId ON Users(RoleId);
GO

-- 1.4 Refresh token cho JWT
CREATE TABLE RefreshTokens (
    RefreshTokenId INT IDENTITY(1,1) NOT NULL,
    UserId         INT               NOT NULL,
    Token          VARCHAR(200)      NOT NULL,
    ExpiresAt      DATETIME2(0)      NOT NULL,
    CreatedAt      DATETIME2(0)      NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT (SYSDATETIME()),
    RevokedAt      DATETIME2(0)      NULL,
    CONSTRAINT PK_RefreshTokens PRIMARY KEY (RefreshTokenId),
    CONSTRAINT UQ_RefreshTokens_Token UNIQUE (Token),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT CK_RefreshTokens_Expires CHECK (ExpiresAt > CreatedAt)
);
GO

-- 1.5 Hãng xe
CREATE TABLE Brands (
    BrandId     INT IDENTITY(1,1) NOT NULL,
    BrandName   NVARCHAR(50)      NOT NULL,
    Country     NVARCHAR(50)      NULL,
    LogoUrl     NVARCHAR(500)     NULL,
    IsActive    BIT               NOT NULL CONSTRAINT DF_Brands_IsActive DEFAULT (1),
    CONSTRAINT PK_Brands PRIMARY KEY (BrandId),
    CONSTRAINT UQ_Brands_BrandName UNIQUE (BrandName)
);
GO

-- 1.6 Loại xe (xe số, tay ga, côn tay, xe điện)
CREATE TABLE Categories (
    CategoryId   INT IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(50)      NOT NULL,
    Description  NVARCHAR(255)     NULL,
    IsActive     BIT               NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Categories_CategoryName UNIQUE (CategoryName)
);
GO

/* =====================================================================
   2. XE MÁY
   ===================================================================== */
CREATE TABLE Motorbikes (
    MotorbikeId    INT IDENTITY(1,1) NOT NULL,
    LicensePlate   VARCHAR(15)       NOT NULL,             -- Biển số, VD: 59A1-123.45
    ModelName      NVARCHAR(100)     NOT NULL,             -- VD: Honda Vision
    BrandId        INT               NOT NULL,
    CategoryId     INT               NOT NULL,
    BranchId       INT               NOT NULL,             -- Xe đang thuộc chi nhánh nào
    ManufactureYear INT              NOT NULL,
    EngineCapacity INT               NULL,                 -- cc; NULL với xe điện
    Color          NVARCHAR(30)      NOT NULL,
    PricePerDay    DECIMAL(18,2)     NOT NULL,
    DepositAmount  DECIMAL(18,2)     NOT NULL,             -- Tiền cọc / xe
    Odometer       INT               NOT NULL CONSTRAINT DF_Motorbikes_Odometer DEFAULT (0),  -- Số km đã đi
    Status         VARCHAR(20)       NOT NULL CONSTRAINT DF_Motorbikes_Status   DEFAULT ('Available'),
    ImageUrl       NVARCHAR(500)     NULL,
    Description    NVARCHAR(1000)    NULL,
    CreatedAt      DATETIME2(0)      NOT NULL CONSTRAINT DF_Motorbikes_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt      DATETIME2(0)      NULL,
    CONSTRAINT PK_Motorbikes PRIMARY KEY (MotorbikeId),
    CONSTRAINT UQ_Motorbikes_LicensePlate UNIQUE (LicensePlate),
    CONSTRAINT FK_Motorbikes_Brands     FOREIGN KEY (BrandId)    REFERENCES Brands(BrandId),
    CONSTRAINT FK_Motorbikes_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT FK_Motorbikes_Branches   FOREIGN KEY (BranchId)   REFERENCES Branches(BranchId),
    CONSTRAINT CK_Motorbikes_Year     CHECK (ManufactureYear BETWEEN 2000 AND 2100),
    CONSTRAINT CK_Motorbikes_Engine   CHECK (EngineCapacity IS NULL OR EngineCapacity BETWEEN 50 AND 1500),
    CONSTRAINT CK_Motorbikes_Price    CHECK (PricePerDay > 0),
    CONSTRAINT CK_Motorbikes_Deposit  CHECK (DepositAmount >= 0),
    CONSTRAINT CK_Motorbikes_Odometer CHECK (Odometer >= 0),
    CONSTRAINT CK_Motorbikes_Status   CHECK (Status IN ('Available', 'Rented', 'Maintenance', 'Inactive'))
);
GO
CREATE INDEX IX_Motorbikes_Status   ON Motorbikes(Status);
CREATE INDEX IX_Motorbikes_BrandId  ON Motorbikes(BrandId);
CREATE INDEX IX_Motorbikes_CategoryId ON Motorbikes(CategoryId);
CREATE INDEX IX_Motorbikes_BranchId ON Motorbikes(BranchId);
GO

/* =====================================================================
   3. ĐƠN THUÊ XE
   ===================================================================== */
CREATE TABLE Rentals (
    RentalId           INT IDENTITY(1,1) NOT NULL,
    RentalCode         VARCHAR(20)       NOT NULL,          -- VD: RNT-20261003-0003
    CustomerId         INT               NOT NULL,
    StaffId            INT               NULL,              -- Nhân viên xử lý (NULL khi khách tự đặt online, chưa duyệt)
    BranchId           INT               NOT NULL,          -- Chi nhánh nhận / trả xe
    StartDate          DATETIME2(0)      NOT NULL,          -- Thời điểm nhận xe
    ExpectedReturnDate DATETIME2(0)      NOT NULL,          -- Thời điểm hẹn trả
    ActualReturnDate   DATETIME2(0)      NULL,              -- Thời điểm trả thực tế
    Status             VARCHAR(20)       NOT NULL CONSTRAINT DF_Rentals_Status DEFAULT ('Pending'),
    SubTotal           DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Rentals_SubTotal DEFAULT (0),  -- Tổng tiền thuê các xe
    DiscountAmount     DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Rentals_Discount DEFAULT (0),
    LateFee            DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Rentals_LateFee  DEFAULT (0),  -- Phí trả trễ
    DamageFee          DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Rentals_DamageFee DEFAULT (0), -- Phí hư hỏng
    TotalAmount        AS (SubTotal - DiscountAmount + LateFee + DamageFee) PERSISTED,         -- Cột tính toán
    DepositAmount      DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Rentals_Deposit DEFAULT (0),   -- Tổng cọc
    Note               NVARCHAR(500)     NULL,
    CancelReason       NVARCHAR(500)     NULL,
    CreatedAt          DATETIME2(0)      NOT NULL CONSTRAINT DF_Rentals_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt          DATETIME2(0)      NULL,
    CONSTRAINT PK_Rentals PRIMARY KEY (RentalId),
    CONSTRAINT UQ_Rentals_RentalCode UNIQUE (RentalCode),
    CONSTRAINT FK_Rentals_Customer FOREIGN KEY (CustomerId) REFERENCES Users(UserId),
    CONSTRAINT FK_Rentals_Staff    FOREIGN KEY (StaffId)    REFERENCES Users(UserId),
    CONSTRAINT FK_Rentals_Branches FOREIGN KEY (BranchId)   REFERENCES Branches(BranchId),
    CONSTRAINT CK_Rentals_Dates       CHECK (ExpectedReturnDate > StartDate),
    CONSTRAINT CK_Rentals_ActualDate  CHECK (ActualReturnDate IS NULL OR ActualReturnDate >= StartDate),
    CONSTRAINT CK_Rentals_Status      CHECK (Status IN ('Pending', 'Confirmed', 'Renting', 'Completed', 'Cancelled')),
    CONSTRAINT CK_Rentals_Amounts     CHECK (SubTotal >= 0 AND DiscountAmount >= 0 AND LateFee >= 0 AND DamageFee >= 0 AND DepositAmount >= 0),
    CONSTRAINT CK_Rentals_Discount    CHECK (DiscountAmount <= SubTotal),
    CONSTRAINT CK_Rentals_Completed   CHECK (Status <> 'Completed' OR ActualReturnDate IS NOT NULL)
);
GO
CREATE INDEX IX_Rentals_CustomerId ON Rentals(CustomerId);
CREATE INDEX IX_Rentals_Status     ON Rentals(Status);
CREATE INDEX IX_Rentals_StartDate  ON Rentals(StartDate, ExpectedReturnDate);
GO

-- 3.1 Chi tiết đơn thuê: 1 đơn có thể thuê nhiều xe
CREATE TABLE RentalDetails (
    RentalDetailId  INT IDENTITY(1,1) NOT NULL,
    RentalId        INT               NOT NULL,
    MotorbikeId     INT               NOT NULL,
    PricePerDay     DECIMAL(18,2)     NOT NULL,   -- Lưu lại giá tại thời điểm thuê (snapshot)
    NumberOfDays    INT               NOT NULL,
    LineTotal       AS (PricePerDay * NumberOfDays) PERSISTED,
    DepositAmount   DECIMAL(18,2)     NOT NULL CONSTRAINT DF_RentalDetails_Deposit DEFAULT (0),
    PickupOdometer  INT               NULL,       -- Số km lúc giao xe
    ReturnOdometer  INT               NULL,       -- Số km lúc nhận lại xe
    PickupCondition NVARCHAR(500)     NULL,       -- Tình trạng xe lúc giao
    ReturnCondition NVARCHAR(500)     NULL,       -- Tình trạng xe lúc trả
    CONSTRAINT PK_RentalDetails PRIMARY KEY (RentalDetailId),
    CONSTRAINT UQ_RentalDetails_Rental_Motorbike UNIQUE (RentalId, MotorbikeId),
    CONSTRAINT FK_RentalDetails_Rentals    FOREIGN KEY (RentalId)    REFERENCES Rentals(RentalId) ON DELETE CASCADE,
    CONSTRAINT FK_RentalDetails_Motorbikes FOREIGN KEY (MotorbikeId) REFERENCES Motorbikes(MotorbikeId),
    CONSTRAINT CK_RentalDetails_Price    CHECK (PricePerDay > 0),
    CONSTRAINT CK_RentalDetails_Days     CHECK (NumberOfDays BETWEEN 1 AND 90),
    CONSTRAINT CK_RentalDetails_Deposit  CHECK (DepositAmount >= 0),
    CONSTRAINT CK_RentalDetails_Odometer CHECK (ReturnOdometer IS NULL OR PickupOdometer IS NULL OR ReturnOdometer >= PickupOdometer)
);
GO
CREATE INDEX IX_RentalDetails_MotorbikeId ON RentalDetails(MotorbikeId);
GO

/* =====================================================================
   4. THANH TOÁN
   ===================================================================== */
CREATE TABLE Payments (
    PaymentId      INT IDENTITY(1,1) NOT NULL,
    RentalId       INT               NOT NULL,
    Amount         DECIMAL(18,2)     NOT NULL,
    PaymentType    VARCHAR(20)       NOT NULL,   -- Deposit: cọc | RentalFee: tiền thuê | Penalty: phạt | Refund: hoàn cọc
    PaymentMethod  VARCHAR(20)       NOT NULL,   -- Cash | BankTransfer | Momo | VNPay
    Status         VARCHAR(20)       NOT NULL CONSTRAINT DF_Payments_Status DEFAULT ('Pending'),
    TransactionRef VARCHAR(100)      NULL,       -- Mã giao dịch ngân hàng / ví
    PaidAt         DATETIME2(0)      NULL,
    ProcessedBy    INT               NULL,       -- Staff xác nhận thanh toán
    Note           NVARCHAR(255)     NULL,
    CreatedAt      DATETIME2(0)      NOT NULL CONSTRAINT DF_Payments_CreatedAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Payments PRIMARY KEY (PaymentId),
    CONSTRAINT FK_Payments_Rentals FOREIGN KEY (RentalId)    REFERENCES Rentals(RentalId),
    CONSTRAINT FK_Payments_Users   FOREIGN KEY (ProcessedBy) REFERENCES Users(UserId),
    CONSTRAINT CK_Payments_Amount  CHECK (Amount > 0),
    CONSTRAINT CK_Payments_Type    CHECK (PaymentType   IN ('Deposit', 'RentalFee', 'Penalty', 'Refund')),
    CONSTRAINT CK_Payments_Method  CHECK (PaymentMethod IN ('Cash', 'BankTransfer', 'Momo', 'VNPay')),
    CONSTRAINT CK_Payments_Status  CHECK (Status IN ('Pending', 'Paid', 'Failed', 'Cancelled')),
    CONSTRAINT CK_Payments_PaidAt  CHECK (Status <> 'Paid' OR PaidAt IS NOT NULL)
);
GO
CREATE INDEX IX_Payments_RentalId ON Payments(RentalId);
GO

/* =====================================================================
   5. BẢO DƯỠNG & ĐÁNH GIÁ
   ===================================================================== */
CREATE TABLE MaintenanceRecords (
    MaintenanceId INT IDENTITY(1,1) NOT NULL,
    MotorbikeId   INT               NOT NULL,
    StartDate     DATE              NOT NULL,
    EndDate       DATE              NULL,
    Description   NVARCHAR(500)     NOT NULL,
    Cost          DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Maintenance_Cost DEFAULT (0),
    Status        VARCHAR(20)       NOT NULL CONSTRAINT DF_Maintenance_Status DEFAULT ('Scheduled'),
    CreatedBy     INT               NULL,
    CreatedAt     DATETIME2(0)      NOT NULL CONSTRAINT DF_Maintenance_CreatedAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_MaintenanceRecords PRIMARY KEY (MaintenanceId),
    CONSTRAINT FK_Maintenance_Motorbikes FOREIGN KEY (MotorbikeId) REFERENCES Motorbikes(MotorbikeId),
    CONSTRAINT FK_Maintenance_Users      FOREIGN KEY (CreatedBy)   REFERENCES Users(UserId),
    CONSTRAINT CK_Maintenance_Dates  CHECK (EndDate IS NULL OR EndDate >= StartDate),
    CONSTRAINT CK_Maintenance_Cost   CHECK (Cost >= 0),
    CONSTRAINT CK_Maintenance_Status CHECK (Status IN ('Scheduled', 'InProgress', 'Completed', 'Cancelled'))
);
GO
CREATE INDEX IX_Maintenance_MotorbikeId ON MaintenanceRecords(MotorbikeId);
GO

CREATE TABLE Reviews (
    ReviewId    INT IDENTITY(1,1) NOT NULL,
    RentalId    INT               NOT NULL,
    MotorbikeId INT               NOT NULL,
    CustomerId  INT               NOT NULL,
    Rating      TINYINT           NOT NULL,
    Comment     NVARCHAR(1000)    NULL,
    CreatedAt   DATETIME2(0)      NOT NULL CONSTRAINT DF_Reviews_CreatedAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Reviews PRIMARY KEY (ReviewId),
    CONSTRAINT UQ_Reviews_Rental_Motorbike UNIQUE (RentalId, MotorbikeId),   -- Mỗi xe trong 1 đơn chỉ đánh giá 1 lần
    CONSTRAINT FK_Reviews_Rentals    FOREIGN KEY (RentalId)    REFERENCES Rentals(RentalId),
    CONSTRAINT FK_Reviews_Motorbikes FOREIGN KEY (MotorbikeId) REFERENCES Motorbikes(MotorbikeId),
    CONSTRAINT FK_Reviews_Users      FOREIGN KEY (CustomerId)  REFERENCES Users(UserId),
    CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5)
);
GO
CREATE INDEX IX_Reviews_MotorbikeId ON Reviews(MotorbikeId);
GO

/* =====================================================================
   6. VIEW THỐNG KÊ (dùng cho Dashboard Admin)
   ===================================================================== */
CREATE VIEW vw_MonthlyRevenue
AS
SELECT
    YEAR(p.PaidAt)  AS [Year],
    MONTH(p.PaidAt) AS [Month],
    SUM(CASE WHEN p.PaymentType = 'RentalFee' THEN p.Amount ELSE 0 END) AS RentalRevenue,
    SUM(CASE WHEN p.PaymentType = 'Penalty'   THEN p.Amount ELSE 0 END) AS PenaltyRevenue,
    SUM(CASE WHEN p.PaymentType IN ('RentalFee', 'Penalty') THEN p.Amount ELSE 0 END) AS TotalRevenue,
    COUNT(DISTINCT p.RentalId) AS RentalCount
FROM Payments p
WHERE p.Status = 'Paid' AND p.PaymentType IN ('RentalFee', 'Penalty')
GROUP BY YEAR(p.PaidAt), MONTH(p.PaidAt);
GO

CREATE VIEW vw_MotorbikeStatistics
AS
SELECT
    m.MotorbikeId,
    m.LicensePlate,
    m.ModelName,
    b.BrandName,
    c.CategoryName,
    br.BranchName,
    m.Status,
    m.PricePerDay,
    (SELECT COUNT(*) FROM RentalDetails rd
        JOIN Rentals r ON r.RentalId = rd.RentalId
        WHERE rd.MotorbikeId = m.MotorbikeId AND r.Status = 'Completed') AS CompletedRentals,
    (SELECT ISNULL(SUM(rd.LineTotal), 0) FROM RentalDetails rd
        JOIN Rentals r ON r.RentalId = rd.RentalId
        WHERE rd.MotorbikeId = m.MotorbikeId AND r.Status = 'Completed') AS TotalEarned,
    (SELECT CAST(AVG(CAST(rv.Rating AS DECIMAL(3,2))) AS DECIMAL(3,2)) FROM Reviews rv
        WHERE rv.MotorbikeId = m.MotorbikeId) AS AverageRating
FROM Motorbikes m
JOIN Brands     b  ON b.BrandId     = m.BrandId
JOIN Categories c  ON c.CategoryId  = m.CategoryId
JOIN Branches   br ON br.BranchId   = m.BranchId;
GO

/* =====================================================================
   7. DỮ LIỆU MẪU (SEED DATA)
   ===================================================================== */

SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (RoleId, RoleName, Description) VALUES
(1, N'Admin',    N'Quản trị hệ thống: quản lý chi nhánh, nhân viên, xe, xem thống kê'),
(2, N'Staff',    N'Nhân viên cửa hàng: duyệt đơn, giao/nhận xe, thu tiền, bảo dưỡng'),
(3, N'Customer', N'Khách hàng: xem xe, đặt thuê, thanh toán, đánh giá');
SET IDENTITY_INSERT Roles OFF;
GO

SET IDENTITY_INSERT Branches ON;
INSERT INTO Branches (BranchId, BranchName, Address, Phone, OpenTime, CloseTime) VALUES
(1, N'MotoRent Quận 1',   N'123 Lê Lợi, Phường Bến Thành, Quận 1, TP.HCM',       '0281234567', '07:00', '21:00'),
(2, N'MotoRent Thủ Đức',  N'45 Võ Văn Ngân, Phường Linh Chiểu, TP. Thủ Đức, TP.HCM', '0287654321', '07:30', '20:30');
SET IDENTITY_INSERT Branches OFF;
GO

-- Mật khẩu của tất cả tài khoản: 123456
SET IDENTITY_INSERT Users ON;
INSERT INTO Users (UserId, Email, PasswordHash, FullName, Phone, DateOfBirth, Address, IdentityNumber, DriverLicenseNumber, RoleId, BranchId) VALUES
(1, 'admin@motorent.vn',     '$2a$11$QQoUEC.vdL8K7uxcOQX/Vu2cWPOp9BIvYfadnziczuQ2pxQQPno4.', N'Nguyễn Văn Quản',  '0900000001', '1990-01-15', N'Quận 3, TP.HCM',      NULL,           NULL,           1, NULL),
(2, 'staff1@motorent.vn',    '$2a$11$QQoUEC.vdL8K7uxcOQX/Vu2cWPOp9BIvYfadnziczuQ2pxQQPno4.', N'Trần Thị Lan',     '0900000002', '1996-05-20', N'Quận 1, TP.HCM',      NULL,           NULL,           2, 1),
(3, 'staff2@motorent.vn',    '$2a$11$QQoUEC.vdL8K7uxcOQX/Vu2cWPOp9BIvYfadnziczuQ2pxQQPno4.', N'Lê Văn Hùng',      '0900000003', '1995-09-10', N'TP. Thủ Đức, TP.HCM', NULL,           NULL,           2, 2),
(4, 'customer1@example.com', '$2a$11$QQoUEC.vdL8K7uxcOQX/Vu2cWPOp9BIvYfadnziczuQ2pxQQPno4.', N'Phạm Minh Tuấn',   '0911111111', '2001-03-12', N'Quận 7, TP.HCM',      '079201001111', '790123456789', 3, NULL),
(5, 'customer2@example.com', '$2a$11$QQoUEC.vdL8K7uxcOQX/Vu2cWPOp9BIvYfadnziczuQ2pxQQPno4.', N'Hoàng Thị Mai',    '0922222222', '1999-11-02', N'Quận Bình Thạnh, TP.HCM', '079199002222', '790223456789', 3, NULL),
(6, 'customer3@example.com', '$2a$11$QQoUEC.vdL8K7uxcOQX/Vu2cWPOp9BIvYfadnziczuQ2pxQQPno4.', N'Võ Quốc Bảo',      '0933333333', '2000-07-25', N'TP. Thủ Đức, TP.HCM', '079200003333', '790323456789', 3, NULL),
(7, 'customer4@example.com', '$2a$11$QQoUEC.vdL8K7uxcOQX/Vu2cWPOp9BIvYfadnziczuQ2pxQQPno4.', N'Đặng Thu Hà',      '0944444444', '1998-02-14', N'Quận 10, TP.HCM',     '079198004444', '790423456789', 3, NULL),
(8, 'customer5@example.com', '$2a$11$QQoUEC.vdL8K7uxcOQX/Vu2cWPOp9BIvYfadnziczuQ2pxQQPno4.', N'Bùi Đức Anh',      '0955555555', '2002-12-30', N'Quận Gò Vấp, TP.HCM', NULL,           NULL,           3, NULL);
SET IDENTITY_INSERT Users OFF;
GO

SET IDENTITY_INSERT Brands ON;
INSERT INTO Brands (BrandId, BrandName, Country) VALUES
(1, N'Honda',   N'Nhật Bản'),
(2, N'Yamaha',  N'Nhật Bản'),
(3, N'Suzuki',  N'Nhật Bản'),
(4, N'Piaggio', N'Ý'),
(5, N'VinFast', N'Việt Nam');
SET IDENTITY_INSERT Brands OFF;
GO

SET IDENTITY_INSERT Categories ON;
INSERT INTO Categories (CategoryId, CategoryName, Description) VALUES
(1, N'Xe số',      N'Xe số phổ thông, tiết kiệm xăng, phù hợp đi phố và đường dài'),
(2, N'Xe tay ga',  N'Xe tay ga dễ điều khiển, cốp rộng, phù hợp đi trong thành phố'),
(3, N'Xe côn tay', N'Xe côn tay mạnh mẽ, phù hợp phượt / đi tour'),
(4, N'Xe điện',    N'Xe máy điện thân thiện môi trường, không cần xăng');
SET IDENTITY_INSERT Categories OFF;
GO

SET IDENTITY_INSERT Motorbikes ON;
INSERT INTO Motorbikes (MotorbikeId, LicensePlate, ModelName, BrandId, CategoryId, BranchId, ManufactureYear, EngineCapacity, Color, PricePerDay, DepositAmount, Odometer, Status, ImageUrl, Description) VALUES
(1,  '59A1-123.45', N'Honda Wave Alpha',    1, 1, 1, 2022, 110,  N'Đỏ',        120000, 1000000, 15200, 'Available',   N'/images/bikes/wave-alpha.jpg',  N'Xe số bền bỉ, tiết kiệm xăng'),
(2,  '59A1-234.56', N'Honda Vision',        1, 2, 1, 2023, 110,  N'Trắng',     150000, 2000000,  8300, 'Rented',      N'/images/bikes/vision.jpg',      N'Tay ga nhỏ gọn, phù hợp nữ'),
(3,  '59A1-345.67', N'Honda Air Blade 125', 1, 2, 1, 2023, 125,  N'Đen',       180000, 2000000,  9100, 'Available',   N'/images/bikes/airblade.jpg',    N'Tay ga thể thao, có smartkey'),
(4,  '59A1-456.78', N'Honda SH Mode',       1, 2, 1, 2024, 125,  N'Xanh',      250000, 3000000,  4200, 'Available',   N'/images/bikes/shmode.jpg',      N'Tay ga cao cấp, phanh ABS'),
(5,  '59A1-567.89', N'Yamaha Sirius',       2, 1, 1, 2021, 110,  N'Đen',       110000, 1000000, 22500, 'Available',   N'/images/bikes/sirius.jpg',      N'Xe số giá rẻ'),
(6,  '59A1-678.90', N'Yamaha Exciter 155',  2, 3, 1, 2023, 155,  N'Xanh GP',   250000, 3000000, 11000, 'Maintenance', N'/images/bikes/exciter.jpg',     N'Côn tay 155cc, mạnh mẽ'),
(7,  '59A2-789.01', N'Yamaha NVX 155',      2, 2, 2, 2022, 155,  N'Xám',       220000, 3000000, 13400, 'Rented',      N'/images/bikes/nvx.jpg',         N'Tay ga thể thao 155cc'),
(8,  '59A2-890.12', N'Yamaha Grande',       2, 2, 2, 2023, 125,  N'Hồng',      170000, 2000000,  7600, 'Rented',      N'/images/bikes/grande.jpg',      N'Tay ga hybrid tiết kiệm xăng'),
(9,  '59A2-901.23', N'Suzuki Raider R150',  3, 3, 2, 2022, 150,  N'Đỏ đen',    230000, 3000000, 16800, 'Available',   N'/images/bikes/raider.jpg',      N'Côn tay hyper underbone'),
(10, '59A2-012.34', N'Suzuki Axelo',        3, 1, 2, 2021, 125,  N'Xanh',      120000, 1000000, 25000, 'Available',   N'/images/bikes/axelo.jpg',       N'Xe số côn tay 125cc'),
(11, '59A2-111.22', N'Vespa Sprint 125',    4, 2, 2, 2023, 125,  N'Vàng',      350000, 5000000,  5300, 'Available',   N'/images/bikes/vespa-sprint.jpg',N'Tay ga phong cách Ý'),
(12, '59A1-222.33', N'Piaggio Liberty',     4, 2, 1, 2022, 125,  N'Trắng',     280000, 4000000,  9800, 'Available',   N'/images/bikes/liberty.jpg',     N'Tay ga bánh lớn'),
(13, '59A1-333.44', N'VinFast Klara S',     5, 4, 1, 2023, NULL, N'Đen',       150000, 2000000,  3100, 'Available',   N'/images/bikes/klara-s.jpg',     N'Xe điện, quãng đường ~120km/lần sạc'),
(14, '59A2-444.55', N'VinFast Feliz S',     5, 4, 2, 2024, NULL, N'Trắng',     140000, 2000000,  1500, 'Available',   N'/images/bikes/feliz-s.jpg',     N'Xe điện phổ thông'),
(15, '59A2-555.66', N'Honda Winner X',      1, 3, 2, 2023, 150,  N'Đỏ',        230000, 3000000, 12700, 'Inactive',    N'/images/bikes/winner-x.jpg',    N'Tạm ngừng cho thuê');
SET IDENTITY_INSERT Motorbikes OFF;
GO

-- Đơn thuê: 3 Completed, 2 Renting, 1 Confirmed, 1 Pending, 1 Cancelled
SET IDENTITY_INSERT Rentals ON;
INSERT INTO Rentals (RentalId, RentalCode, CustomerId, StaffId, BranchId, StartDate, ExpectedReturnDate, ActualReturnDate, Status, SubTotal, DiscountAmount, LateFee, DamageFee, DepositAmount, Note, CancelReason, CreatedAt) VALUES
(1, 'RNT-20260905-0001', 4, 2,    1, '2026-09-05 08:00', '2026-09-07 08:00', '2026-09-07 07:30', 'Completed',  360000,     0,      0,      0, 2000000, N'Khách thuê đi Vũng Tàu', NULL,                       '2026-09-04 20:15'),
(2, 'RNT-20260912-0002', 5, 3,    2, '2026-09-12 09:00', '2026-09-15 09:00', '2026-09-15 15:00', 'Completed', 1050000,     0, 100000,      0, 5000000, N'Trả trễ 6 tiếng',        NULL,                       '2026-09-11 10:00'),
(3, 'RNT-20260915-0003', 6, 2,    1, '2026-09-15 08:00', '2026-09-16 08:00', '2026-09-16 08:00', 'Completed',  250000,     0,      0, 300000, 3000000, N'Trầy dàn áo bên trái',   NULL,                       '2026-09-14 18:30'),
(4, 'RNT-20260920-0004', 5, NULL, 1, '2026-09-20 08:00', '2026-09-22 08:00', NULL,               'Cancelled',  560000,     0,      0,      0, 4000000, NULL,                      N'Khách đổi lịch trình',    '2026-09-18 09:00'),
(5, 'RNT-20261002-0005', 7, 3,    2, '2026-10-02 10:00', '2026-10-05 10:00', NULL,               'Renting',   1170000,     0,      0,      0, 5000000, N'Thuê 2 xe đi Đà Lạt',    NULL,                       '2026-10-01 14:00'),
(6, 'RNT-20261003-0006', 6, 2,    1, '2026-10-03 08:00', '2026-10-06 08:00', NULL,               'Renting',    450000,     0,      0,      0, 2000000, NULL,                      NULL,                       '2026-10-02 19:45'),
(7, 'RNT-20261008-0007', 8, NULL, 1, '2026-10-08 08:00', '2026-10-10 08:00', NULL,               'Pending',    500000, 50000,      0,      0, 3000000, N'Đặt online, chờ duyệt',  NULL,                       '2026-10-04 08:20'),
(8, 'RNT-20261010-0008', 4, 2,    1, '2026-10-10 07:00', '2026-10-11 07:00', NULL,               'Confirmed',  120000,     0,      0,      0, 1000000, NULL,                      NULL,                       '2026-10-03 21:10');
SET IDENTITY_INSERT Rentals OFF;
GO

INSERT INTO RentalDetails (RentalId, MotorbikeId, PricePerDay, NumberOfDays, DepositAmount, PickupOdometer, ReturnOdometer, PickupCondition, ReturnCondition) VALUES
(1, 3,  180000, 2, 2000000,  8800,  9100, N'Xe tốt, đầy xăng',      N'Bình thường'),
(2, 11, 350000, 3, 5000000,  5000,  5300, N'Xe tốt',                N'Bình thường'),
(3, 6,  250000, 1, 3000000, 10850, 11000, N'Xe tốt',                N'Trầy dàn áo bên trái, phanh trước yếu'),
(4, 12, 280000, 2, 4000000,  NULL,  NULL, NULL,                     NULL),
(5, 8,  170000, 3, 2000000,  7600,  NULL, N'Xe tốt',                NULL),
(5, 7,  220000, 3, 3000000, 13400,  NULL, N'Xe tốt, xước nhẹ yếm',  NULL),
(6, 2,  150000, 3, 2000000,  8300,  NULL, N'Xe tốt',                NULL),
(7, 4,  250000, 2, 3000000,  NULL,  NULL, NULL,                     NULL),
(8, 1,  120000, 1, 1000000,  NULL,  NULL, NULL,                     NULL);
GO

INSERT INTO Payments (RentalId, Amount, PaymentType, PaymentMethod, Status, TransactionRef, PaidAt, ProcessedBy, Note) VALUES
-- Đơn 1
(1, 2000000, 'Deposit',   'Cash',         'Paid',      NULL,            '2026-09-05 08:00', 2, N'Thu cọc khi giao xe'),
(1,  360000, 'RentalFee', 'Cash',         'Paid',      NULL,            '2026-09-07 07:30', 2, NULL),
(1, 2000000, 'Refund',    'Cash',         'Paid',      NULL,            '2026-09-07 07:35', 2, N'Hoàn cọc'),
-- Đơn 2
(2, 5000000, 'Deposit',   'BankTransfer', 'Paid',      'VCB0912000123', '2026-09-11 10:05', 3, NULL),
(2, 1050000, 'RentalFee', 'BankTransfer', 'Paid',      'VCB0915000456', '2026-09-15 15:00', 3, NULL),
(2,  100000, 'Penalty',   'Cash',         'Paid',      NULL,            '2026-09-15 15:05', 3, N'Phạt trả trễ'),
(2, 5000000, 'Refund',    'BankTransfer', 'Paid',      'VCB0915000789', '2026-09-15 15:10', 3, N'Hoàn cọc'),
-- Đơn 3
(3, 3000000, 'Deposit',   'Cash',         'Paid',      NULL,            '2026-09-15 08:00', 2, NULL),
(3,  250000, 'RentalFee', 'Cash',         'Paid',      NULL,            '2026-09-16 08:00', 2, NULL),
(3,  300000, 'Penalty',   'Cash',         'Paid',      NULL,            '2026-09-16 08:05', 2, N'Phí hư hỏng dàn áo'),
(3, 3000000, 'Refund',    'Cash',         'Paid',      NULL,            '2026-09-16 08:10', 2, N'Hoàn cọc'),
-- Đơn 4 (huỷ)
(4, 4000000, 'Deposit',   'BankTransfer', 'Paid',      'TCB0918000111', '2026-09-18 09:10', NULL, NULL),
(4, 4000000, 'Refund',    'BankTransfer', 'Paid',      'TCB0919000222', '2026-09-19 10:00', 2,    N'Hoàn cọc do khách huỷ'),
-- Đơn 5, 6, 8 (đang thuê / đã xác nhận)
(5, 5000000, 'Deposit',   'Cash',         'Paid',      NULL,            '2026-10-02 10:00', 3, NULL),
(6, 2000000, 'Deposit',   'Momo',         'Paid',      'MOMO1003998877','2026-10-02 19:50', 2, NULL),
(8, 1000000, 'Deposit',   'BankTransfer', 'Paid',      'ACB1003000333', '2026-10-03 21:15', 2, NULL),
-- Đơn 7 (chờ thanh toán VNPay)
(7, 3000000, 'Deposit',   'VNPay',        'Pending',   NULL,            NULL,               NULL, N'Chờ khách thanh toán');
GO

INSERT INTO MaintenanceRecords (MotorbikeId, StartDate, EndDate, Description, Cost, Status, CreatedBy) VALUES
(5,  '2026-08-20', '2026-08-21', N'Thay nhớt, vệ sinh nồi định kỳ',               120000, 'Completed',  2),
(10, '2026-09-01', '2026-09-02', N'Thay lốp sau',                                 350000, 'Completed',  3),
(6,  '2026-09-16', NULL,         N'Sơn lại dàn áo trái, thay má phanh trước',     450000, 'InProgress', 2),
(3,  '2026-10-15', NULL,         N'Bảo dưỡng định kỳ mốc 10.000 km',              300000, 'Scheduled',  2);
GO

INSERT INTO Reviews (RentalId, MotorbikeId, CustomerId, Rating, Comment, CreatedAt) VALUES
(1, 3,  4, 5, N'Xe chạy êm, nhân viên nhiệt tình, giao xe đúng giờ.',   '2026-09-07 10:00'),
(2, 11, 5, 4, N'Xe đẹp, chụp hình rất xịn. Phí trả trễ hơi cao.',        '2026-09-15 20:00'),
(3, 6,  6, 3, N'Xe mạnh nhưng phanh trước hơi yếu.',                     '2026-09-16 12:00');
GO

/* =====================================================================
   8. KIỂM TRA NHANH
   ===================================================================== */
SELECT 'Roles' AS TableName, COUNT(*) AS Total FROM Roles
UNION ALL SELECT 'Branches',           COUNT(*) FROM Branches
UNION ALL SELECT 'Users',              COUNT(*) FROM Users
UNION ALL SELECT 'Brands',             COUNT(*) FROM Brands
UNION ALL SELECT 'Categories',         COUNT(*) FROM Categories
UNION ALL SELECT 'Motorbikes',         COUNT(*) FROM Motorbikes
UNION ALL SELECT 'Rentals',            COUNT(*) FROM Rentals
UNION ALL SELECT 'RentalDetails',      COUNT(*) FROM RentalDetails
UNION ALL SELECT 'Payments',           COUNT(*) FROM Payments
UNION ALL SELECT 'MaintenanceRecords', COUNT(*) FROM MaintenanceRecords
UNION ALL SELECT 'Reviews',            COUNT(*) FROM Reviews;
GO
