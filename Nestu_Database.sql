/* =====================================================================
   NESTU - QUẢN LÝ NHÀ TRỌ SINH VIÊN
   Script tạo CSDL SQL Server: bảng Identity + 9 bảng nghiệp vụ + dữ liệu mẫu
   Cách dùng: mở SSMS → New Query → dán toàn bộ → Execute (F5)
   LƯU Ý: script XÓA database NestuDb cũ (nếu có) rồi tạo lại từ đầu.
   Mật khẩu mọi tài khoản mẫu: Nestu@123
   ===================================================================== */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE master;
GO

IF DB_ID(N'NestuDb') IS NOT NULL
BEGIN
    ALTER DATABASE NestuDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE NestuDb;
END
GO

CREATE DATABASE NestuDb COLLATE Vietnamese_CI_AS;
GO

USE NestuDb;
GO

/* =====================================================================
   PHẦN 1: CÁC BẢNG ASP.NET CORE IDENTITY (đúng schema Identity .NET 8)
   ===================================================================== */

CREATE TABLE AspNetRoles (
    Id               NVARCHAR(450) NOT NULL,
    Name             NVARCHAR(256) NULL,
    NormalizedName   NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetRoles PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX RoleNameIndex ON AspNetRoles (NormalizedName)
    WHERE NormalizedName IS NOT NULL;
GO

CREATE TABLE AspNetUsers (
    Id                   NVARCHAR(450)  NOT NULL,
    -- Cột bổ sung của ApplicationUser
    FullName             NVARCHAR(100)  NOT NULL,
    DateOfBirth          DATE           NULL,
    AvatarUrl            NVARCHAR(500)  NULL,
    IsActive             BIT            NOT NULL CONSTRAINT DF_AspNetUsers_IsActive  DEFAULT 1,
    CreatedAt            DATETIME2      NOT NULL CONSTRAINT DF_AspNetUsers_CreatedAt DEFAULT GETDATE(),
    -- Cột mặc định của IdentityUser
    UserName             NVARCHAR(256)  NULL,
    NormalizedUserName   NVARCHAR(256)  NULL,
    Email                NVARCHAR(256)  NULL,
    NormalizedEmail      NVARCHAR(256)  NULL,
    EmailConfirmed       BIT            NOT NULL,
    PasswordHash         NVARCHAR(MAX)  NULL,
    SecurityStamp        NVARCHAR(MAX)  NULL,
    ConcurrencyStamp     NVARCHAR(MAX)  NULL,
    PhoneNumber          NVARCHAR(MAX)  NULL,
    PhoneNumberConfirmed BIT            NOT NULL,
    TwoFactorEnabled     BIT            NOT NULL,
    LockoutEnd           DATETIMEOFFSET NULL,
    LockoutEnabled       BIT            NOT NULL,
    AccessFailedCount    INT            NOT NULL,
    CONSTRAINT PK_AspNetUsers PRIMARY KEY (Id)
);
CREATE INDEX EmailIndex ON AspNetUsers (NormalizedEmail);
CREATE UNIQUE INDEX UserNameIndex ON AspNetUsers (NormalizedUserName)
    WHERE NormalizedUserName IS NOT NULL;
GO

CREATE TABLE AspNetRoleClaims (
    Id         INT IDENTITY(1,1) NOT NULL,
    RoleId     NVARCHAR(450) NOT NULL,
    ClaimType  NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY (Id),
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId)
        REFERENCES AspNetRoles (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetRoleClaims_RoleId ON AspNetRoleClaims (RoleId);
GO

CREATE TABLE AspNetUserClaims (
    Id         INT IDENTITY(1,1) NOT NULL,
    UserId     NVARCHAR(450) NOT NULL,
    ClaimType  NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetUserClaims PRIMARY KEY (Id),
    CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId)
        REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserClaims_UserId ON AspNetUserClaims (UserId);
GO

CREATE TABLE AspNetUserLogins (
    LoginProvider       NVARCHAR(450) NOT NULL,
    ProviderKey         NVARCHAR(450) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX) NULL,
    UserId              NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId)
        REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserLogins_UserId ON AspNetUserLogins (UserId);
GO

CREATE TABLE AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL,
    RoleId NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId)
        REFERENCES AspNetRoles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId)
        REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserRoles_RoleId ON AspNetUserRoles (RoleId);
GO

CREATE TABLE AspNetUserTokens (
    UserId        NVARCHAR(450) NOT NULL,
    LoginProvider NVARCHAR(450) NOT NULL,
    Name          NVARCHAR(450) NOT NULL,
    Value         NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId)
        REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
GO

/* =====================================================================
   PHẦN 2: CÁC BẢNG NGHIỆP VỤ
   Enum lưu dạng INT:
     ApprovalStatus      : 0 Pending, 1 Approved, 2 Rejected
     RoomStatus          : 0 Available, 1 Occupied, 2 Maintenance
     RentalRequestStatus : 0 Pending, 1 Approved, 2 Rejected, 3 Cancelled
     ContractStatus      : 0 Active, 1 Expired, 2 Terminated
     InvoiceStatus       : 0 Unpaid, 1 Paid, 2 Overdue
     MaintenancePriority : 0 Low, 1 Medium, 2 High
     MaintenanceStatus   : 0 Pending, 1 InProgress, 2 Resolved, 3 Rejected
   ===================================================================== */

-- 2.1. BoardingHouses
CREATE TABLE BoardingHouses (
    Id             INT IDENTITY(1,1) NOT NULL,
    OwnerId        NVARCHAR(450)  NOT NULL,
    Name           NVARCHAR(150)  NOT NULL,
    Address        NVARCHAR(255)  NOT NULL,
    Ward           NVARCHAR(100)  NULL,
    District       NVARCHAR(100)  NOT NULL,
    City           NVARCHAR(100)  NOT NULL,
    Description    NVARCHAR(MAX)  NULL,
    ElectricPrice  DECIMAL(18,2)  NOT NULL,
    WaterPrice     DECIMAL(18,2)  NOT NULL,
    ApprovalStatus INT            NOT NULL CONSTRAINT DF_BoardingHouses_ApprovalStatus DEFAULT 0,
    IsActive       BIT            NOT NULL CONSTRAINT DF_BoardingHouses_IsActive       DEFAULT 1,
    CreatedAt      DATETIME2      NOT NULL CONSTRAINT DF_BoardingHouses_CreatedAt      DEFAULT GETDATE(),
    CONSTRAINT PK_BoardingHouses PRIMARY KEY (Id),
    CONSTRAINT FK_BoardingHouses_AspNetUsers_OwnerId FOREIGN KEY (OwnerId)
        REFERENCES AspNetUsers (Id),
    CONSTRAINT CK_BoardingHouses_ElectricPrice  CHECK (ElectricPrice >= 0),
    CONSTRAINT CK_BoardingHouses_WaterPrice     CHECK (WaterPrice >= 0),
    CONSTRAINT CK_BoardingHouses_ApprovalStatus CHECK (ApprovalStatus IN (0, 1, 2))
);
CREATE INDEX IX_BoardingHouses_OwnerId  ON BoardingHouses (OwnerId);
CREATE INDEX IX_BoardingHouses_District ON BoardingHouses (District);
GO

-- 2.2. Rooms
CREATE TABLE Rooms (
    Id              INT IDENTITY(1,1) NOT NULL,
    BoardingHouseId INT            NOT NULL,
    RoomNumber      NVARCHAR(20)   NOT NULL,
    Floor           INT            NULL,
    Area            DECIMAL(6,2)   NOT NULL,
    Price           DECIMAL(18,2)  NOT NULL,
    MaxOccupants    INT            NOT NULL,
    Status          INT            NOT NULL CONSTRAINT DF_Rooms_Status    DEFAULT 0,
    Description     NVARCHAR(1000) NULL,
    CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_Rooms_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Rooms PRIMARY KEY (Id),
    CONSTRAINT FK_Rooms_BoardingHouses_BoardingHouseId FOREIGN KEY (BoardingHouseId)
        REFERENCES BoardingHouses (Id),
    CONSTRAINT CK_Rooms_Floor        CHECK (Floor IS NULL OR Floor >= 0),
    CONSTRAINT CK_Rooms_Area         CHECK (Area > 0),
    CONSTRAINT CK_Rooms_Price        CHECK (Price > 0),
    CONSTRAINT CK_Rooms_MaxOccupants CHECK (MaxOccupants BETWEEN 1 AND 10),
    CONSTRAINT CK_Rooms_Status       CHECK (Status IN (0, 1, 2))
);
CREATE UNIQUE INDEX IX_Rooms_BoardingHouseId_RoomNumber ON Rooms (BoardingHouseId, RoomNumber);
CREATE INDEX IX_Rooms_Price ON Rooms (Price);
GO

-- 2.3. RoomImages
CREATE TABLE RoomImages (
    Id          INT IDENTITY(1,1) NOT NULL,
    RoomId      INT           NOT NULL,
    ImageUrl    NVARCHAR(500) NOT NULL,
    IsThumbnail BIT           NOT NULL CONSTRAINT DF_RoomImages_IsThumbnail DEFAULT 0,
    CONSTRAINT PK_RoomImages PRIMARY KEY (Id),
    CONSTRAINT FK_RoomImages_Rooms_RoomId FOREIGN KEY (RoomId)
        REFERENCES Rooms (Id) ON DELETE CASCADE
);
CREATE INDEX IX_RoomImages_RoomId ON RoomImages (RoomId);
GO

-- 2.4. RentalRequests
CREATE TABLE RentalRequests (
    Id               INT IDENTITY(1,1) NOT NULL,
    RoomId           INT           NOT NULL,
    TenantId         NVARCHAR(450) NOT NULL,
    DesiredStartDate DATE          NOT NULL,
    Message          NVARCHAR(500) NULL,
    Status           INT           NOT NULL CONSTRAINT DF_RentalRequests_Status    DEFAULT 0,
    ResponseNote     NVARCHAR(500) NULL,
    CreatedAt        DATETIME2     NOT NULL CONSTRAINT DF_RentalRequests_CreatedAt DEFAULT GETDATE(),
    RespondedAt      DATETIME2     NULL,
    CONSTRAINT PK_RentalRequests PRIMARY KEY (Id),
    CONSTRAINT FK_RentalRequests_Rooms_RoomId FOREIGN KEY (RoomId)
        REFERENCES Rooms (Id),
    CONSTRAINT FK_RentalRequests_AspNetUsers_TenantId FOREIGN KEY (TenantId)
        REFERENCES AspNetUsers (Id),
    CONSTRAINT CK_RentalRequests_Status CHECK (Status IN (0, 1, 2, 3))
);
-- Một sinh viên không có 2 yêu cầu đang chờ cho cùng một phòng
CREATE UNIQUE INDEX IX_RentalRequests_RoomId_TenantId_Pending
    ON RentalRequests (RoomId, TenantId) WHERE Status = 0;
CREATE INDEX IX_RentalRequests_TenantId ON RentalRequests (TenantId);
GO

-- 2.5. Contracts
CREATE TABLE Contracts (
    Id              INT IDENTITY(1,1) NOT NULL,
    RoomId          INT           NOT NULL,
    TenantId        NVARCHAR(450) NOT NULL,
    RentalRequestId INT           NULL,
    StartDate       DATE          NOT NULL,
    EndDate         DATE          NOT NULL,
    MonthlyRent     DECIMAL(18,2) NOT NULL,
    Deposit         DECIMAL(18,2) NOT NULL,
    Status          INT           NOT NULL CONSTRAINT DF_Contracts_Status    DEFAULT 0,
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Contracts_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Contracts PRIMARY KEY (Id),
    CONSTRAINT FK_Contracts_Rooms_RoomId FOREIGN KEY (RoomId)
        REFERENCES Rooms (Id),
    CONSTRAINT FK_Contracts_AspNetUsers_TenantId FOREIGN KEY (TenantId)
        REFERENCES AspNetUsers (Id),
    CONSTRAINT FK_Contracts_RentalRequests_RentalRequestId FOREIGN KEY (RentalRequestId)
        REFERENCES RentalRequests (Id),
    CONSTRAINT CK_Contracts_Dates       CHECK (EndDate > StartDate),
    CONSTRAINT CK_Contracts_MonthlyRent CHECK (MonthlyRent > 0),
    CONSTRAINT CK_Contracts_Deposit     CHECK (Deposit >= 0),
    CONSTRAINT CK_Contracts_Status      CHECK (Status IN (0, 1, 2))
);
-- Mỗi phòng chỉ có tối đa 1 hợp đồng Active
CREATE UNIQUE INDEX IX_Contracts_RoomId_Active
    ON Contracts (RoomId) WHERE Status = 0;
-- Một yêu cầu thuê chỉ tạo ra tối đa 1 hợp đồng
CREATE UNIQUE INDEX IX_Contracts_RentalRequestId
    ON Contracts (RentalRequestId) WHERE RentalRequestId IS NOT NULL;
CREATE INDEX IX_Contracts_TenantId ON Contracts (TenantId);
GO

-- 2.6. UtilityReadings
CREATE TABLE UtilityReadings (
    Id          INT IDENTITY(1,1) NOT NULL,
    RoomId      INT       NOT NULL,
    [Month]     INT       NOT NULL,
    [Year]      INT       NOT NULL,
    ElectricOld INT       NOT NULL,
    ElectricNew INT       NOT NULL,
    WaterOld    INT       NOT NULL,
    WaterNew    INT       NOT NULL,
    RecordedAt  DATETIME2 NOT NULL CONSTRAINT DF_UtilityReadings_RecordedAt DEFAULT GETDATE(),
    CONSTRAINT PK_UtilityReadings PRIMARY KEY (Id),
    CONSTRAINT FK_UtilityReadings_Rooms_RoomId FOREIGN KEY (RoomId)
        REFERENCES Rooms (Id),
    CONSTRAINT CK_UtilityReadings_Month    CHECK ([Month] BETWEEN 1 AND 12),
    CONSTRAINT CK_UtilityReadings_Year     CHECK ([Year] >= 2020),
    CONSTRAINT CK_UtilityReadings_Electric CHECK (ElectricOld >= 0 AND ElectricNew >= ElectricOld),
    CONSTRAINT CK_UtilityReadings_Water    CHECK (WaterOld >= 0 AND WaterNew >= WaterOld)
);
CREATE UNIQUE INDEX IX_UtilityReadings_RoomId_Month_Year ON UtilityReadings (RoomId, [Month], [Year]);
GO

-- 2.7. Invoices
CREATE TABLE Invoices (
    Id               INT IDENTITY(1,1) NOT NULL,
    ContractId       INT           NOT NULL,
    UtilityReadingId INT           NULL,
    [Month]          INT           NOT NULL,
    [Year]           INT           NOT NULL,
    RoomFee          DECIMAL(18,2) NOT NULL,
    ElectricFee      DECIMAL(18,2) NOT NULL,
    WaterFee         DECIMAL(18,2) NOT NULL,
    OtherFee         DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_OtherFee  DEFAULT 0,
    TotalAmount      DECIMAL(18,2) NOT NULL,
    Status           INT           NOT NULL CONSTRAINT DF_Invoices_Status    DEFAULT 0,
    DueDate          DATE          NOT NULL,
    PaidAt           DATETIME2     NULL,
    Note             NVARCHAR(500) NULL,
    CreatedAt        DATETIME2     NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Invoices PRIMARY KEY (Id),
    CONSTRAINT FK_Invoices_Contracts_ContractId FOREIGN KEY (ContractId)
        REFERENCES Contracts (Id),
    CONSTRAINT FK_Invoices_UtilityReadings_UtilityReadingId FOREIGN KEY (UtilityReadingId)
        REFERENCES UtilityReadings (Id),
    CONSTRAINT CK_Invoices_Month  CHECK ([Month] BETWEEN 1 AND 12),
    CONSTRAINT CK_Invoices_Year   CHECK ([Year] >= 2020),
    CONSTRAINT CK_Invoices_Fees   CHECK (RoomFee >= 0 AND ElectricFee >= 0 AND WaterFee >= 0 AND OtherFee >= 0),
    CONSTRAINT CK_Invoices_Total  CHECK (TotalAmount >= 0),
    CONSTRAINT CK_Invoices_Status CHECK (Status IN (0, 1, 2))
);
CREATE UNIQUE INDEX IX_Invoices_ContractId_Month_Year ON Invoices (ContractId, [Month], [Year]);
CREATE UNIQUE INDEX IX_Invoices_UtilityReadingId
    ON Invoices (UtilityReadingId) WHERE UtilityReadingId IS NOT NULL;
GO

-- 2.8. MaintenanceRequests
CREATE TABLE MaintenanceRequests (
    Id           INT IDENTITY(1,1) NOT NULL,
    RoomId       INT            NOT NULL,
    TenantId     NVARCHAR(450)  NOT NULL,
    Title        NVARCHAR(150)  NOT NULL,
    Description  NVARCHAR(1000) NOT NULL,
    Priority     INT            NOT NULL CONSTRAINT DF_MaintenanceRequests_Priority  DEFAULT 1,
    Status       INT            NOT NULL CONSTRAINT DF_MaintenanceRequests_Status    DEFAULT 0,
    LandlordNote NVARCHAR(500)  NULL,
    CreatedAt    DATETIME2      NOT NULL CONSTRAINT DF_MaintenanceRequests_CreatedAt DEFAULT GETDATE(),
    ResolvedAt   DATETIME2      NULL,
    CONSTRAINT PK_MaintenanceRequests PRIMARY KEY (Id),
    CONSTRAINT FK_MaintenanceRequests_Rooms_RoomId FOREIGN KEY (RoomId)
        REFERENCES Rooms (Id),
    CONSTRAINT FK_MaintenanceRequests_AspNetUsers_TenantId FOREIGN KEY (TenantId)
        REFERENCES AspNetUsers (Id),
    CONSTRAINT CK_MaintenanceRequests_Priority CHECK (Priority IN (0, 1, 2)),
    CONSTRAINT CK_MaintenanceRequests_Status   CHECK (Status IN (0, 1, 2, 3))
);
CREATE INDEX IX_MaintenanceRequests_RoomId   ON MaintenanceRequests (RoomId);
CREATE INDEX IX_MaintenanceRequests_TenantId ON MaintenanceRequests (TenantId);
GO

/* =====================================================================
   PHẦN 3: DỮ LIỆU MẪU
   ===================================================================== */

-- 3.1. Roles
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES
(N'10000000-0000-0000-0000-000000000001', N'Admin',    N'ADMIN',    CONVERT(NVARCHAR(36), NEWID())),
(N'10000000-0000-0000-0000-000000000002', N'Landlord', N'LANDLORD', CONVERT(NVARCHAR(36), NEWID())),
(N'10000000-0000-0000-0000-000000000003', N'Tenant',   N'TENANT',   CONVERT(NVARCHAR(36), NEWID()));
GO

-- 3.2. Users (mật khẩu tất cả: Nestu@123 - hash chuẩn ASP.NET Core Identity V3)
DECLARE @pw NVARCHAR(MAX) = N'AQAAAAIAAYagAAAAEA31/qxEPbzQLkmOufwmgpBI1md0B/1m31//qWP3PGrDwydXE1gXs//7GEZ5zCbfeA==';

INSERT INTO AspNetUsers
(Id, FullName, DateOfBirth, AvatarUrl, IsActive, CreatedAt,
 UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
 PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
 TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount)
VALUES
(N'20000000-0000-0000-0000-000000000001', N'Quản trị viên Nestu', NULL, NULL, 1, '2026-01-01T08:00:00',
 N'admin@nestu.vn', N'ADMIN@NESTU.VN', N'admin@nestu.vn', N'ADMIN@NESTU.VN', 1,
 @pw, CONVERT(NVARCHAR(36), NEWID()), CONVERT(NVARCHAR(36), NEWID()), N'0900000001', 0, 0, NULL, 1, 0),

(N'20000000-0000-0000-0000-000000000002', N'Nguyễn Văn Hùng', '1980-05-12', NULL, 1, '2026-01-05T08:00:00',
 N'landlord1@nestu.vn', N'LANDLORD1@NESTU.VN', N'landlord1@nestu.vn', N'LANDLORD1@NESTU.VN', 1,
 @pw, CONVERT(NVARCHAR(36), NEWID()), CONVERT(NVARCHAR(36), NEWID()), N'0912345601', 0, 0, NULL, 1, 0),

(N'20000000-0000-0000-0000-000000000003', N'Trần Thị Mai', '1985-09-20', NULL, 1, '2026-01-06T08:00:00',
 N'landlord2@nestu.vn', N'LANDLORD2@NESTU.VN', N'landlord2@nestu.vn', N'LANDLORD2@NESTU.VN', 1,
 @pw, CONVERT(NVARCHAR(36), NEWID()), CONVERT(NVARCHAR(36), NEWID()), N'0912345602', 0, 0, NULL, 1, 0),

(N'20000000-0000-0000-0000-000000000004', N'Lê Minh Anh', '2004-03-15', NULL, 1, '2026-02-01T08:00:00',
 N'tenant1@nestu.vn', N'TENANT1@NESTU.VN', N'tenant1@nestu.vn', N'TENANT1@NESTU.VN', 1,
 @pw, CONVERT(NVARCHAR(36), NEWID()), CONVERT(NVARCHAR(36), NEWID()), N'0987654301', 0, 0, NULL, 1, 0),

(N'20000000-0000-0000-0000-000000000005', N'Phạm Quốc Bảo', '2003-11-02', NULL, 1, '2026-02-10T08:00:00',
 N'tenant2@nestu.vn', N'TENANT2@NESTU.VN', N'tenant2@nestu.vn', N'TENANT2@NESTU.VN', 1,
 @pw, CONVERT(NVARCHAR(36), NEWID()), CONVERT(NVARCHAR(36), NEWID()), N'0987654302', 0, 0, NULL, 1, 0),

(N'20000000-0000-0000-0000-000000000006', N'Hoàng Thu Trang', '2005-07-25', NULL, 1, '2026-04-01T08:00:00',
 N'tenant3@nestu.vn', N'TENANT3@NESTU.VN', N'tenant3@nestu.vn', N'TENANT3@NESTU.VN', 1,
 @pw, CONVERT(NVARCHAR(36), NEWID()), CONVERT(NVARCHAR(36), NEWID()), N'0987654303', 0, 0, NULL, 1, 0),

(N'20000000-0000-0000-0000-000000000007', N'Vũ Đức Huy', '2003-01-30', NULL, 1, '2025-08-15T08:00:00',
 N'tenant4@nestu.vn', N'TENANT4@NESTU.VN', N'tenant4@nestu.vn', N'TENANT4@NESTU.VN', 1,
 @pw, CONVERT(NVARCHAR(36), NEWID()), CONVERT(NVARCHAR(36), NEWID()), N'0987654304', 0, 0, NULL, 1, 0),

-- Tài khoản bị khóa (IsActive = 0) để demo chức năng Admin
(N'20000000-0000-0000-0000-000000000008', N'Đỗ Ngọc Lan', '2004-12-12', NULL, 0, '2026-03-01T08:00:00',
 N'tenant5@nestu.vn', N'TENANT5@NESTU.VN', N'tenant5@nestu.vn', N'TENANT5@NESTU.VN', 1,
 @pw, CONVERT(NVARCHAR(36), NEWID()), CONVERT(NVARCHAR(36), NEWID()), N'0987654305', 0, 0, NULL, 1, 0);
GO

-- 3.3. UserRoles
INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES
(N'20000000-0000-0000-0000-000000000001', N'10000000-0000-0000-0000-000000000001'), -- Admin
(N'20000000-0000-0000-0000-000000000002', N'10000000-0000-0000-0000-000000000002'), -- Landlord
(N'20000000-0000-0000-0000-000000000003', N'10000000-0000-0000-0000-000000000002'), -- Landlord
(N'20000000-0000-0000-0000-000000000004', N'10000000-0000-0000-0000-000000000003'), -- Tenant
(N'20000000-0000-0000-0000-000000000005', N'10000000-0000-0000-0000-000000000003'),
(N'20000000-0000-0000-0000-000000000006', N'10000000-0000-0000-0000-000000000003'),
(N'20000000-0000-0000-0000-000000000007', N'10000000-0000-0000-0000-000000000003'),
(N'20000000-0000-0000-0000-000000000008', N'10000000-0000-0000-0000-000000000003');
GO

-- 3.4. BoardingHouses
SET IDENTITY_INSERT BoardingHouses ON;
INSERT INTO BoardingHouses
(Id, OwnerId, Name, Address, Ward, District, City, Description, ElectricPrice, WaterPrice, ApprovalStatus, IsActive, CreatedAt)
VALUES
(1, N'20000000-0000-0000-0000-000000000002', N'Nhà trọ Hòa Bình', N'Số 12 ngõ 45 Nguyễn Trãi', N'Thượng Đình', N'Thanh Xuân', N'Hà Nội',
    N'Gần các trường đại học khu Thanh Xuân, có chỗ để xe, camera an ninh, giờ giấc tự do.', 3500, 20000, 1, 1, '2026-01-10T09:00:00'),
(2, N'20000000-0000-0000-0000-000000000002', N'Nhà trọ Sinh Viên Xanh', N'Số 8 ngách 20 ngõ 91 Chùa Láng', N'Láng Thượng', N'Đống Đa', N'Hà Nội',
    N'Khu yên tĩnh, có máy giặt chung, wifi tốc độ cao.', 3800, 25000, 1, 1, '2026-01-15T09:00:00'),
(3, N'20000000-0000-0000-0000-000000000003', N'Nhà trọ An Khang', N'Số 25 ngõ 1 Trần Quốc Hoàn', N'Dịch Vọng Hậu', N'Cầu Giấy', N'Hà Nội',
    N'Phòng mới xây, có thang máy, điều hòa, nóng lạnh.', 4000, 30000, 1, 1, '2026-02-01T09:00:00'),
(4, N'20000000-0000-0000-0000-000000000003', N'Nhà trọ Minh Châu', N'Số 3 ngõ 88 Kim Giang', N'Đại Kim', N'Hoàng Mai', N'Hà Nội',
    N'Nhà trọ mới đăng, đang chờ Admin duyệt.', 3500, 20000, 0, 1, '2026-09-20T09:00:00');
SET IDENTITY_INSERT BoardingHouses OFF;
GO

-- 3.5. Rooms
SET IDENTITY_INSERT Rooms ON;
INSERT INTO Rooms (Id, BoardingHouseId, RoomNumber, Floor, Area, Price, MaxOccupants, Status, Description, CreatedAt) VALUES
-- Nhà trọ Hòa Bình
(1,  1, N'101', 1, 20.00, 2500000, 2, 1, N'Phòng có cửa sổ, vệ sinh khép kín.',            '2026-01-10T10:00:00'),
(2,  1, N'102', 1, 18.00, 2200000, 2, 0, N'Phòng thoáng, gần cầu thang.',                  '2026-01-10T10:00:00'),
(3,  1, N'103', 1, 25.00, 3000000, 3, 0, N'Phòng rộng, có gác xép.',                        '2026-01-10T10:00:00'),
(4,  1, N'201', 2, 20.00, 2600000, 2, 1, N'Phòng có ban công.',                             '2026-01-10T10:00:00'),
(5,  1, N'202', 2, 15.00, 1800000, 1, 2, N'Đang sửa chữa hệ thống điện.',                   '2026-01-10T10:00:00'),
-- Nhà trọ Sinh Viên Xanh
(6,  2, N'A1',  1, 16.00, 2000000, 2, 0, N'Phòng tiêu chuẩn.',                              '2026-01-15T10:00:00'),
(7,  2, N'A2',  1, 16.00, 2000000, 2, 1, N'Phòng tiêu chuẩn.',                              '2026-01-15T10:00:00'),
(8,  2, N'A3',  1, 22.00, 2800000, 3, 0, N'Có bếp riêng.',                                  '2026-01-15T10:00:00'),
(9,  2, N'B1',  2, 30.00, 3500000, 4, 0, N'Phòng lớn phù hợp nhóm bạn.',                    '2026-01-15T10:00:00'),
(10, 2, N'B2',  2, 12.00, 1500000, 1, 0, N'Phòng nhỏ giá rẻ cho 1 người.',                  '2026-01-15T10:00:00'),
-- Nhà trọ An Khang
(11, 3, N'P101', 1, 20.00, 3200000, 2, 0, N'Đầy đủ nội thất.',                              '2026-02-01T10:00:00'),
(12, 3, N'P102', 1, 25.00, 3800000, 3, 0, N'Đầy đủ nội thất, có máy giặt riêng.',           '2026-02-01T10:00:00'),
(13, 3, N'P201', 2, 28.00, 4200000, 3, 0, N'Phòng góc, 2 cửa sổ.',                          '2026-02-01T10:00:00'),
(14, 3, N'P202', 2, 18.00, 2900000, 2, 0, N'Phòng yên tĩnh.',                               '2026-02-01T10:00:00'),
(15, 3, N'P301', 3, 35.00, 4800000, 4, 0, N'Căn studio, có phòng khách nhỏ.',               '2026-02-01T10:00:00'),
-- Nhà trọ Minh Châu (chưa duyệt → Guest không thấy)
(16, 4, N'1',   1, 14.00, 1600000, 1, 0, NULL,                                               '2026-09-20T10:00:00'),
(17, 4, N'2',   1, 14.00, 1600000, 1, 0, NULL,                                               '2026-09-20T10:00:00'),
(18, 4, N'3',   2, 20.00, 2300000, 2, 0, NULL,                                               '2026-09-20T10:00:00');
SET IDENTITY_INSERT Rooms OFF;
GO

-- 3.6. RoomImages
INSERT INTO RoomImages (RoomId, ImageUrl, IsThumbnail) VALUES
(1,  N'/images/rooms/room-1-1.jpg', 1), (1,  N'/images/rooms/room-1-2.jpg', 0),
(2,  N'/images/rooms/room-2-1.jpg', 1),
(3,  N'/images/rooms/room-3-1.jpg', 1), (3,  N'/images/rooms/room-3-2.jpg', 0),
(6,  N'/images/rooms/room-6-1.jpg', 1),
(8,  N'/images/rooms/room-8-1.jpg', 1),
(9,  N'/images/rooms/room-9-1.jpg', 1),
(11, N'/images/rooms/room-11-1.jpg', 1),
(12, N'/images/rooms/room-12-1.jpg', 1),
(15, N'/images/rooms/room-15-1.jpg', 1), (15, N'/images/rooms/room-15-2.jpg', 0);
GO

-- 3.7. RentalRequests
SET IDENTITY_INSERT RentalRequests ON;
INSERT INTO RentalRequests (Id, RoomId, TenantId, DesiredStartDate, Message, Status, ResponseNote, CreatedAt, RespondedAt) VALUES
(1, 1,  N'20000000-0000-0000-0000-000000000004', '2026-03-01', N'Em là sinh viên năm 3, muốn thuê lâu dài.', 1, N'Đồng ý, mời em qua ký hợp đồng.', '2026-02-10T09:00:00', '2026-02-12T10:00:00'),
(2, 4,  N'20000000-0000-0000-0000-000000000005', '2026-03-15', N'Cho em thuê phòng có ban công ạ.',          1, N'Đồng ý.',                          '2026-03-01T09:00:00', '2026-03-02T10:00:00'),
(3, 7,  N'20000000-0000-0000-0000-000000000006', '2026-05-01', NULL,                                          1, N'Đồng ý.',                          '2026-04-15T09:00:00', '2026-04-16T10:00:00'),
(4, 13, N'20000000-0000-0000-0000-000000000007', '2025-09-01', N'Em thuê đến hết kỳ học.',                   1, N'Đồng ý.',                          '2025-08-20T09:00:00', '2025-08-21T10:00:00'),
(5, 1,  N'20000000-0000-0000-0000-000000000007', '2026-03-01', N'Em muốn thuê phòng 101.',                   2, N'Phòng đã có người thuê.',          '2026-02-11T09:00:00', '2026-02-12T10:00:00'),
(6, 3,  N'20000000-0000-0000-0000-000000000007', '2026-10-15', N'Em muốn thuê từ giữa tháng 10.',            0, NULL,                                '2026-09-25T09:00:00', NULL),
(7, 11, N'20000000-0000-0000-0000-000000000007', '2026-10-15', N'Phòng còn trống không ạ?',                  0, NULL,                                '2026-09-26T09:00:00', NULL),
(8, 9,  N'20000000-0000-0000-0000-000000000006', '2026-09-01', N'Em thuê cùng nhóm bạn.',                    3, NULL,                                '2026-08-10T09:00:00', NULL);
SET IDENTITY_INSERT RentalRequests OFF;
GO

-- 3.8. Contracts
SET IDENTITY_INSERT Contracts ON;
INSERT INTO Contracts (Id, RoomId, TenantId, RentalRequestId, StartDate, EndDate, MonthlyRent, Deposit, Status, CreatedAt) VALUES
(1, 1,  N'20000000-0000-0000-0000-000000000004', 1, '2026-03-01', '2027-02-28', 2500000, 2500000, 0, '2026-02-12T10:30:00'),
(2, 4,  N'20000000-0000-0000-0000-000000000005', 2, '2026-03-15', '2027-03-14', 2600000, 2600000, 0, '2026-03-02T10:30:00'),
(3, 7,  N'20000000-0000-0000-0000-000000000006', 3, '2026-05-01', '2026-12-31', 2000000, 2000000, 0, '2026-04-16T10:30:00'),
(4, 13, N'20000000-0000-0000-0000-000000000007', 4, '2025-09-01', '2026-06-30', 4200000, 4200000, 1, '2025-08-21T10:30:00');
SET IDENTITY_INSERT Contracts OFF;
GO

-- 3.9. UtilityReadings (tháng 7, 8, 9/2026 cho 3 phòng đang thuê)
SET IDENTITY_INSERT UtilityReadings ON;
INSERT INTO UtilityReadings (Id, RoomId, [Month], [Year], ElectricOld, ElectricNew, WaterOld, WaterNew, RecordedAt) VALUES
(1, 1, 7, 2026, 1200, 1320, 50, 56, '2026-07-31T18:00:00'),
(2, 1, 8, 2026, 1320, 1450, 56, 62, '2026-08-31T18:00:00'),
(3, 1, 9, 2026, 1450, 1565, 62, 67, '2026-09-29T18:00:00'),
(4, 4, 7, 2026,  800,  905, 30, 35, '2026-07-31T18:00:00'),
(5, 4, 8, 2026,  905, 1010, 35, 40, '2026-08-31T18:00:00'),
(6, 4, 9, 2026, 1010, 1120, 40, 44, '2026-09-29T18:00:00'),
(7, 7, 7, 2026,  300,  380, 10, 14, '2026-07-31T18:00:00'),
(8, 7, 8, 2026,  380,  470, 14, 18, '2026-08-31T18:00:00'),
(9, 7, 9, 2026,  470,  550, 18, 22, '2026-09-29T18:00:00');
SET IDENTITY_INSERT UtilityReadings OFF;
GO

-- 3.10. Invoices
-- ElectricFee = số điện × ElectricPrice ; WaterFee = số nước × WaterPrice
-- TotalAmount = RoomFee + ElectricFee + WaterFee + OtherFee (OtherFee 150.000 = wifi + rác)
SET IDENTITY_INSERT Invoices ON;
INSERT INTO Invoices (Id, ContractId, UtilityReadingId, [Month], [Year], RoomFee, ElectricFee, WaterFee, OtherFee, TotalAmount, Status, DueDate, PaidAt, Note, CreatedAt) VALUES
-- Hợp đồng 1 - phòng 101 (điện 3.500, nước 20.000)
(1, 1, 1, 7, 2026, 2500000, 420000, 120000, 150000, 3190000, 1, '2026-08-05', '2026-08-03T20:00:00', NULL, '2026-08-01T08:00:00'),
(2, 1, 2, 8, 2026, 2500000, 455000, 120000, 150000, 3225000, 1, '2026-09-05', '2026-09-04T19:30:00', NULL, '2026-09-01T08:00:00'),
(3, 1, 3, 9, 2026, 2500000, 402500, 100000, 150000, 3152500, 0, '2026-10-05', NULL, NULL, '2026-09-30T08:00:00'),
-- Hợp đồng 2 - phòng 201 (điện 3.500, nước 20.000)
(4, 2, 4, 7, 2026, 2600000, 367500, 100000, 150000, 3217500, 1, '2026-08-05', '2026-08-05T21:00:00', NULL, '2026-08-01T08:00:00'),
(5, 2, 5, 8, 2026, 2600000, 367500, 100000, 150000, 3217500, 2, '2026-09-05', NULL, N'Quá hạn thanh toán.', '2026-09-01T08:00:00'),
(6, 2, 6, 9, 2026, 2600000, 385000,  80000, 150000, 3215000, 0, '2026-10-05', NULL, NULL, '2026-09-30T08:00:00'),
-- Hợp đồng 3 - phòng A2 (điện 3.800, nước 25.000)
(7, 3, 7, 7, 2026, 2000000, 304000, 100000, 150000, 2554000, 1, '2026-08-05', '2026-08-02T18:00:00', NULL, '2026-08-01T08:00:00'),
(8, 3, 8, 8, 2026, 2000000, 342000, 100000, 150000, 2592000, 1, '2026-09-05', '2026-09-03T18:00:00', NULL, '2026-09-01T08:00:00'),
(9, 3, 9, 9, 2026, 2000000, 304000, 100000, 150000, 2554000, 0, '2026-10-05', NULL, NULL, '2026-09-30T08:00:00');
SET IDENTITY_INSERT Invoices OFF;
GO

-- 3.11. MaintenanceRequests
INSERT INTO MaintenanceRequests (RoomId, TenantId, Title, Description, Priority, Status, LandlordNote, CreatedAt, ResolvedAt) VALUES
(1, N'20000000-0000-0000-0000-000000000004', N'Vòi nước bồn rửa bị rỉ',    N'Vòi nước ở bồn rửa mặt bị rỉ liên tục, tốn nước.',        1, 2, N'Đã thay vòi mới.',                       '2026-06-10T20:00:00', '2026-06-12T10:00:00'),
(4, N'20000000-0000-0000-0000-000000000005', N'Bóng đèn nhà vệ sinh hỏng', N'Bóng đèn nhà vệ sinh không sáng từ hôm qua.',             0, 1, N'Đã mua bóng, sẽ thay trong tuần.',       '2026-09-27T21:00:00', NULL),
(7, N'20000000-0000-0000-0000-000000000006', N'Điều hòa không mát',        N'Điều hòa chạy nhưng không ra hơi lạnh, có tiếng kêu to.', 2, 0, NULL,                                      '2026-09-29T22:00:00', NULL),
(1, N'20000000-0000-0000-0000-000000000004', N'Cửa sổ bị kẹt',             N'Cửa sổ khó đóng mở.',                                     0, 3, N'Cửa bình thường, cần kéo mạnh tay hơn.', '2026-07-01T19:00:00', NULL);
GO

/* =====================================================================
   PHẦN 4: KIỂM TRA NHANH
   ===================================================================== */
SELECT N'AspNetRoles' AS [Bảng], COUNT(*) AS [Số dòng] FROM AspNetRoles
UNION ALL SELECT N'AspNetUsers',         COUNT(*) FROM AspNetUsers
UNION ALL SELECT N'AspNetUserRoles',     COUNT(*) FROM AspNetUserRoles
UNION ALL SELECT N'BoardingHouses',      COUNT(*) FROM BoardingHouses
UNION ALL SELECT N'Rooms',               COUNT(*) FROM Rooms
UNION ALL SELECT N'RoomImages',          COUNT(*) FROM RoomImages
UNION ALL SELECT N'RentalRequests',      COUNT(*) FROM RentalRequests
UNION ALL SELECT N'Contracts',           COUNT(*) FROM Contracts
UNION ALL SELECT N'UtilityReadings',     COUNT(*) FROM UtilityReadings
UNION ALL SELECT N'Invoices',            COUNT(*) FROM Invoices
UNION ALL SELECT N'MaintenanceRequests', COUNT(*) FROM MaintenanceRequests;
GO
