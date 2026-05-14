-- =====================================================
-- APARTMENT MANAGER - SCHEMA SETUP
-- Drops and recreates ApartmentManagerDB for a clean import
-- =====================================================

SET NOCOUNT ON;
GO

IF DB_ID(N'ApartmentManagerDB') IS NOT NULL
BEGIN
    ALTER DATABASE ApartmentManagerDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ApartmentManagerDB;
END
GO

CREATE DATABASE ApartmentManagerDB
COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

USE ApartmentManagerDB;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE dbo.Roles
(
    RoleID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT GETDATE()
);
GO

CREATE UNIQUE INDEX UX_Roles_RoleName ON dbo.Roles(RoleName);
GO

CREATE TABLE dbo.Permissions
(
    PermissionID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Permissions PRIMARY KEY,
    PermissionName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Permissions_CreatedAt DEFAULT GETDATE()
);
GO

CREATE UNIQUE INDEX UX_Permissions_PermissionName ON dbo.Permissions(PermissionName);
GO

CREATE TABLE dbo.Buildings
(
    BuildingID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Buildings PRIMARY KEY,
    BuildingName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Buildings_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Buildings_UpdatedAt DEFAULT GETDATE()
);
GO

CREATE UNIQUE INDEX UX_Buildings_BuildingName ON dbo.Buildings(BuildingName);
GO

CREATE TABLE dbo.Blocks
(
    BlockID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Blocks PRIMARY KEY,
    BlockName NVARCHAR(100) NOT NULL,
    BuildingID INT NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Blocks_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Blocks_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Blocks_Buildings
        FOREIGN KEY (BuildingID) REFERENCES dbo.Buildings(BuildingID)
);
GO

CREATE UNIQUE INDEX UX_Blocks_Building_BlockName ON dbo.Blocks(BuildingID, BlockName);
GO

CREATE TABLE dbo.Floors
(
    FloorID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Floors PRIMARY KEY,
    FloorNumber INT NOT NULL,
    BlockID INT NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Floors_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Floors_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Floors_Blocks
        FOREIGN KEY (BlockID) REFERENCES dbo.Blocks(BlockID)
);
GO

CREATE UNIQUE INDEX UX_Floors_Block_FloorNumber ON dbo.Floors(BlockID, FloorNumber);
GO

CREATE TABLE dbo.Apartments
(
    ApartmentID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Apartments PRIMARY KEY,
    ApartmentCode NVARCHAR(20) NOT NULL,
    FloorID INT NOT NULL,
    Area DECIMAL(10,2) NOT NULL,
    ApartmentType NVARCHAR(100) NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Apartments_Status DEFAULT N'Empty',
    MaxResidents INT NOT NULL CONSTRAINT DF_Apartments_MaxResidents DEFAULT 0,
    Note NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Apartments_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Apartments_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Apartments_Floors
        FOREIGN KEY (FloorID) REFERENCES dbo.Floors(FloorID)
);
GO

CREATE UNIQUE INDEX UX_Apartments_ApartmentCode ON dbo.Apartments(ApartmentCode);
GO

CREATE TABLE dbo.Users
(
    UserID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    RoleID INT NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_Status DEFAULT N'Pending',
    AvatarPath NVARCHAR(255) NULL,
    LastLoginAt DATETIME2(0) NULL,
    FailedLoginCount INT NOT NULL CONSTRAINT DF_Users_FailedLoginCount DEFAULT 0,
    LockedUntil DATETIME2(0) NULL,
    IsApproved BIT NOT NULL CONSTRAINT DF_Users_IsApproved DEFAULT 0,
    ApprovedAt DATETIME2(0) NULL,
    ApprovedBy INT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles
        FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID),
    CONSTRAINT FK_Users_ApprovedBy
        FOREIGN KEY (ApprovedBy) REFERENCES dbo.Users(UserID)
);
GO

CREATE UNIQUE INDEX UX_Users_Username ON dbo.Users(Username);
GO

CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);
GO

CREATE TABLE dbo.RolePermissions
(
    RoleID INT NOT NULL,
    PermissionID INT NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_RolePermissions_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleID, PermissionID),
    CONSTRAINT FK_RolePermissions_Roles
        FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID)
        ON DELETE CASCADE,
    CONSTRAINT FK_RolePermissions_Permissions
        FOREIGN KEY (PermissionID) REFERENCES dbo.Permissions(PermissionID)
        ON DELETE CASCADE
);
GO

CREATE TABLE dbo.Residents
(
    ResidentID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Residents PRIMARY KEY,
    UserID INT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    CCCD NVARCHAR(20) NOT NULL,
    DOB DATE NOT NULL,
    Gender NVARCHAR(10) NULL,
    AddressRegistration NVARCHAR(255) NULL,
    ApartmentID INT NOT NULL,
    RelationshipWithOwner NVARCHAR(50) NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Residents_Status DEFAULT N'Active',
    ResidentStatus NVARCHAR(20) NULL,
    MoveInDate DATETIME2(0) NULL,
    StartDate DATETIME2(0) NOT NULL CONSTRAINT DF_Residents_StartDate DEFAULT GETDATE(),
    MoveOutDate DATETIME2(0) NULL,
    EndDate DATETIME2(0) NULL,
    AvatarPath NVARCHAR(255) NULL,
    Note NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Residents_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Residents_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Residents_Users
        FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
        ON DELETE SET NULL,
    CONSTRAINT FK_Residents_Apartments
        FOREIGN KEY (ApartmentID) REFERENCES dbo.Apartments(ApartmentID)
);
GO

CREATE UNIQUE INDEX UX_Residents_CCCD ON dbo.Residents(CCCD);
GO

CREATE UNIQUE INDEX UX_Residents_UserID ON dbo.Residents(UserID) WHERE UserID IS NOT NULL;
GO

CREATE TABLE dbo.Contracts
(
    ContractID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Contracts PRIMARY KEY,
    ApartmentID INT NOT NULL,
    ResidentID INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    RentAmount DECIMAL(18,2) NOT NULL,
    DepositAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Contracts_DepositAmount DEFAULT 0,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Contracts_Status DEFAULT N'Pending',
    Note NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Contracts_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Contracts_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Contracts_Apartments
        FOREIGN KEY (ApartmentID) REFERENCES dbo.Apartments(ApartmentID),
    CONSTRAINT FK_Contracts_Residents
        FOREIGN KEY (ResidentID) REFERENCES dbo.Residents(ResidentID)
);
GO

CREATE TABLE dbo.FeeTypes
(
    FeeTypeID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FeeTypes PRIMARY KEY,
    FeeTypeName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL CONSTRAINT DF_FeeTypes_Description DEFAULT N'',
    UnitOfMeasurement NVARCHAR(50) NOT NULL CONSTRAINT DF_FeeTypes_UnitOfMeasurement DEFAULT N'VND',
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_FeeTypes_Status DEFAULT N'Active',
    CalculationType NVARCHAR(50) NOT NULL CONSTRAINT DF_FeeTypes_CalculationType DEFAULT N'Fixed',
    FundID INT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_FeeTypes_IsActive DEFAULT 1,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_FeeTypes_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_FeeTypes_UpdatedAt DEFAULT GETDATE()
);
GO

CREATE UNIQUE INDEX UX_FeeTypes_FeeTypeName ON dbo.FeeTypes(FeeTypeName);
GO

CREATE TABLE dbo.Invoices
(
    InvoiceID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
    ApartmentID INT NOT NULL,
    [Month] INT NOT NULL,
    [Year] INT NOT NULL,
    DueDate DATE NOT NULL,
    PaymentStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_Invoices_PaymentStatus DEFAULT N'Unpaid',
    TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_TotalAmount DEFAULT 0,
    PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_PaidAmount DEFAULT 0,
    RemainingAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_RemainingAmount DEFAULT 0,
    PaidAt DATETIME2(0) NULL,
    ConfirmedBy INT NULL,
    CancelReason NVARCHAR(500) NULL,
    AdjustmentNote NVARCHAR(500) NULL,
    Note NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Invoices_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Invoices_Apartments
        FOREIGN KEY (ApartmentID) REFERENCES dbo.Apartments(ApartmentID),
    CONSTRAINT FK_Invoices_ConfirmedBy
        FOREIGN KEY (ConfirmedBy) REFERENCES dbo.Users(UserID)
);
GO

CREATE UNIQUE INDEX UX_Invoices_Apartment_Month_Year ON dbo.Invoices(ApartmentID, [Month], [Year]);
GO

CREATE TABLE dbo.InvoiceDetails
(
    InvoiceDetailID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InvoiceDetails PRIMARY KEY,
    InvoiceID INT NOT NULL,
    FeeTypeID INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_InvoiceDetails_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_InvoiceDetails_Invoices
        FOREIGN KEY (InvoiceID) REFERENCES dbo.Invoices(InvoiceID)
        ON DELETE CASCADE,
    CONSTRAINT FK_InvoiceDetails_FeeTypes
        FOREIGN KEY (FeeTypeID) REFERENCES dbo.FeeTypes(FeeTypeID)
);
GO

CREATE TABLE dbo.Complaints
(
    ComplaintID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Complaints PRIMARY KEY,
    ResidentID INT NOT NULL,
    ApartmentID INT NOT NULL,
    Category NVARCHAR(50) NOT NULL CONSTRAINT DF_Complaints_Category DEFAULT N'General',
    ComplaintType NVARCHAR(50) NOT NULL CONSTRAINT DF_Complaints_ComplaintType DEFAULT N'',
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Priority NVARCHAR(20) NOT NULL CONSTRAINT DF_Complaints_Priority DEFAULT N'Medium',
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Complaints_Status DEFAULT N'Open',
    AssignedToUserID INT NULL,
    ResolutionNotes NVARCHAR(MAX) NULL,
    ImageAttachmentPath NVARCHAR(255) NULL,
    SatisfactionRating INT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Complaints_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Complaints_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Complaints_Residents
        FOREIGN KEY (ResidentID) REFERENCES dbo.Residents(ResidentID),
    CONSTRAINT FK_Complaints_Apartments
        FOREIGN KEY (ApartmentID) REFERENCES dbo.Apartments(ApartmentID),
    CONSTRAINT FK_Complaints_AssignedToUser
        FOREIGN KEY (AssignedToUserID) REFERENCES dbo.Users(UserID)
        ON DELETE SET NULL
);
GO

CREATE TABLE dbo.Notifications
(
    NotificationID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY,
    UserID INT NULL,
    ResidentID INT NULL,
    Title NVARCHAR(200) NOT NULL,
    Subject NVARCHAR(200) NOT NULL CONSTRAINT DF_Notifications_Subject DEFAULT N'',
    Message NVARCHAR(MAX) NOT NULL,
    Body NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Notifications_Body DEFAULT N'',
    NotificationType NVARCHAR(50) NOT NULL,
    Priority NVARCHAR(20) NOT NULL CONSTRAINT DF_Notifications_Priority DEFAULT N'Medium',
    IsRead BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0,
    ReadAt DATETIME2(0) NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Notifications_Status DEFAULT N'Draft',
    SentDate DATETIME2(0) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Notifications_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Notifications_Users
        FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
        ON DELETE SET NULL,
    CONSTRAINT FK_Notifications_Residents
        FOREIGN KEY (ResidentID) REFERENCES dbo.Residents(ResidentID)
        ON DELETE SET NULL
);
GO

CREATE TABLE dbo.Visitors
(
    VisitorID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Visitors PRIMARY KEY,
    ResidentID INT NOT NULL,
    VisitorName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Email NVARCHAR(150) NOT NULL CONSTRAINT DF_Visitors_Email DEFAULT N'',
    IDNumber NVARCHAR(50) NOT NULL CONSTRAINT DF_Visitors_IDNumber DEFAULT N'',
    Purpose NVARCHAR(255) NOT NULL,
    ArrivalTime DATETIME2(0) NOT NULL,
    DepartureTime DATETIME2(0) NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Visitors_Status DEFAULT N'Pending',
    ApprovedByUserID INT NULL,
    Note NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Visitors_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Visitors_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Visitors_Residents
        FOREIGN KEY (ResidentID) REFERENCES dbo.Residents(ResidentID),
    CONSTRAINT FK_Visitors_ApprovedByUser
        FOREIGN KEY (ApprovedByUserID) REFERENCES dbo.Users(UserID)
        ON DELETE SET NULL
);
GO

CREATE TABLE dbo.Vehicles
(
    VehicleID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Vehicles PRIMARY KEY,
    ResidentID INT NOT NULL,
    VehicleType NVARCHAR(50) NOT NULL,
    LicensePlate NVARCHAR(20) NOT NULL,
    Color NVARCHAR(50) NOT NULL CONSTRAINT DF_Vehicles_Color DEFAULT N'',
    Brand NVARCHAR(100) NOT NULL CONSTRAINT DF_Vehicles_Brand DEFAULT N'',
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Vehicles_Status DEFAULT N'Active',
    Note NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Vehicles_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Vehicles_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Vehicles_Residents
        FOREIGN KEY (ResidentID) REFERENCES dbo.Residents(ResidentID)
);
GO

CREATE UNIQUE INDEX UX_Vehicles_LicensePlate ON dbo.Vehicles(LicensePlate);
GO

CREATE TABLE dbo.AuditLogs
(
    LogID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
    UserID INT NULL,
    Action NVARCHAR(100) NOT NULL,
    EntityName NVARCHAR(100) NOT NULL,
    EntityID INT NULL,
    OldValue NVARCHAR(MAX) NULL,
    NewValue NVARCHAR(MAX) NULL,
    [Timestamp] DATETIME2(0) NOT NULL CONSTRAINT DF_AuditLogs_Timestamp DEFAULT GETDATE(),
    Description NVARCHAR(MAX) NULL,
    IPAddress NVARCHAR(50) NULL,
    CONSTRAINT FK_AuditLogs_Users
        FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
        ON DELETE SET NULL
);
GO

CREATE TABLE dbo.SystemConfig
(
    ConfigKey NVARCHAR(100) NOT NULL CONSTRAINT PK_SystemConfig PRIMARY KEY,
    ConfigValue NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SystemConfig_UpdatedAt DEFAULT GETDATE(),
    UpdatedBy INT NULL,
    CONSTRAINT FK_SystemConfig_Users
        FOREIGN KEY (UpdatedBy) REFERENCES dbo.Users(UserID)
        ON DELETE SET NULL
);
GO

CREATE TABLE dbo.Assets
(
    AssetID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Assets PRIMARY KEY,
    AssetCode NVARCHAR(50) NOT NULL,
    AssetName NVARCHAR(200) NOT NULL,
    AssetType NVARCHAR(100) NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    PurchaseDate DATE NULL,
    Condition NVARCHAR(50) NOT NULL CONSTRAINT DF_Assets_Condition DEFAULT N'Tốt',
    LastMaintenanceDate DATE NULL,
    NextMaintenanceDate DATE NULL,
    RepairCost DECIMAL(18,2) NOT NULL CONSTRAINT DF_Assets_RepairCost DEFAULT 0,
    Note NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Assets_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Assets_UpdatedAt DEFAULT GETDATE()
);
GO

CREATE UNIQUE INDEX UX_Assets_AssetCode ON dbo.Assets(AssetCode);
GO

CREATE TABLE dbo.MaintenanceSchedules
(
    MaintenanceID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MaintenanceSchedules PRIMARY KEY,
    AssetID INT NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    ScheduledDate DATE NOT NULL,
    Status NVARCHAR(50) NOT NULL CONSTRAINT DF_MaintenanceSchedules_Status DEFAULT N'Chờ xử lý',
    AssignedTo NVARCHAR(150) NULL,
    Note NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_MaintenanceSchedules_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_MaintenanceSchedules_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_MaintenanceSchedules_Assets
        FOREIGN KEY (AssetID) REFERENCES dbo.Assets(AssetID)
);
GO

CREATE TABLE dbo.Funds
(
    FundID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Funds PRIMARY KEY,
    FundName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    InitialBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Funds_InitialBalance DEFAULT 0,
    IsActive BIT NOT NULL CONSTRAINT DF_Funds_IsActive DEFAULT 1,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Funds_CreatedAt DEFAULT GETDATE()
);
GO

CREATE UNIQUE INDEX UX_Funds_FundName ON dbo.Funds(FundName);
GO

ALTER TABLE dbo.FeeTypes
ADD CONSTRAINT FK_FeeTypes_Funds
    FOREIGN KEY (FundID) REFERENCES dbo.Funds(FundID);
GO

ALTER TABLE dbo.FeeTypes
ADD CONSTRAINT CK_FeeTypes_CalculationType
    CHECK (CalculationType IN (N'Fixed', N'PerArea', N'PerVehicle', N'Manual'));
GO

CREATE TABLE dbo.PaymentAccounts
(
    PaymentAccountID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentAccounts PRIMARY KEY,
    AccountName NVARCHAR(150) NOT NULL,
    AccountType NVARCHAR(50) NOT NULL,
    BankName NVARCHAR(150) NULL,
    AccountNumber NVARCHAR(100) NULL,
    AccountHolder NVARCHAR(150) NULL,
    QRImagePath NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_PaymentAccounts_IsActive DEFAULT 1,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PaymentAccounts_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT CK_PaymentAccounts_AccountType
        CHECK (AccountType IN (N'Cash', N'Bank', N'QR', N'EWallet'))
);
GO

CREATE TABLE dbo.ExpenseCategories
(
    ExpenseCategoryID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExpenseCategories PRIMARY KEY,
    CategoryName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_ExpenseCategories_IsActive DEFAULT 1,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ExpenseCategories_CreatedAt DEFAULT GETDATE()
);
GO

CREATE UNIQUE INDEX UX_ExpenseCategories_CategoryName ON dbo.ExpenseCategories(CategoryName);
GO

CREATE TABLE dbo.Vendors
(
    VendorID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Vendors PRIMARY KEY,
    VendorName NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(30) NULL,
    Email NVARCHAR(150) NULL,
    Address NVARCHAR(300) NULL,
    TaxCode NVARCHAR(50) NULL,
    BankAccount NVARCHAR(100) NULL,
    BankName NVARCHAR(150) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Vendors_IsActive DEFAULT 1,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Vendors_CreatedAt DEFAULT GETDATE()
);
GO

CREATE UNIQUE INDEX UX_Vendors_VendorName ON dbo.Vendors(VendorName);
GO

CREATE TABLE dbo.Payments
(
    PaymentID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    PaymentCode NVARCHAR(50) NOT NULL,
    InvoiceID INT NOT NULL,
    ApartmentID INT NOT NULL,
    ResidentID INT NULL,
    PaymentAccountID INT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,
    PaymentDate DATETIME2(0) NOT NULL CONSTRAINT DF_Payments_PaymentDate DEFAULT GETDATE(),
    TransactionCode NVARCHAR(100) NULL,
    ProofImagePath NVARCHAR(500) NULL,
    PaymentStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_Payments_PaymentStatus DEFAULT N'Pending',
    ConfirmedBy INT NULL,
    ConfirmedAt DATETIME2(0) NULL,
    RejectedReason NVARCHAR(500) NULL,
    CancelReason NVARCHAR(500) NULL,
    Note NVARCHAR(500) NULL,
    CreatedBy INT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Payments_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Payments_Invoices FOREIGN KEY (InvoiceID) REFERENCES dbo.Invoices(InvoiceID),
    CONSTRAINT FK_Payments_Apartments FOREIGN KEY (ApartmentID) REFERENCES dbo.Apartments(ApartmentID),
    CONSTRAINT FK_Payments_Residents FOREIGN KEY (ResidentID) REFERENCES dbo.Residents(ResidentID),
    CONSTRAINT FK_Payments_PaymentAccounts FOREIGN KEY (PaymentAccountID) REFERENCES dbo.PaymentAccounts(PaymentAccountID),
    CONSTRAINT FK_Payments_ConfirmedBy FOREIGN KEY (ConfirmedBy) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Payments_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserID),
    CONSTRAINT CK_Payments_PaymentMethod CHECK (PaymentMethod IN (N'Cash', N'BankTransfer', N'QR', N'EWallet', N'Other')),
    CONSTRAINT CK_Payments_PaymentStatus CHECK (PaymentStatus IN (N'Pending', N'Confirmed', N'Rejected', N'Cancelled', N'Refunded'))
);
GO

CREATE UNIQUE INDEX UX_Payments_PaymentCode ON dbo.Payments(PaymentCode);
GO

CREATE INDEX IX_Payments_InvoiceID_Status ON dbo.Payments(InvoiceID, PaymentStatus);
GO

CREATE INDEX IX_Payments_ResidentID ON dbo.Payments(ResidentID, PaymentDate DESC);
GO

CREATE TABLE dbo.Receipts
(
    ReceiptID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Receipts PRIMARY KEY,
    ReceiptCode NVARCHAR(50) NOT NULL,
    PaymentID INT NOT NULL,
    InvoiceID INT NOT NULL,
    ApartmentID INT NOT NULL,
    PayerName NVARCHAR(150) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    ReceiptDate DATETIME2(0) NOT NULL CONSTRAINT DF_Receipts_ReceiptDate DEFAULT GETDATE(),
    CreatedBy INT NOT NULL,
    Note NVARCHAR(500) NULL,
    CONSTRAINT FK_Receipts_Payments FOREIGN KEY (PaymentID) REFERENCES dbo.Payments(PaymentID),
    CONSTRAINT FK_Receipts_Invoices FOREIGN KEY (InvoiceID) REFERENCES dbo.Invoices(InvoiceID),
    CONSTRAINT FK_Receipts_Apartments FOREIGN KEY (ApartmentID) REFERENCES dbo.Apartments(ApartmentID),
    CONSTRAINT FK_Receipts_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserID)
);
GO

CREATE UNIQUE INDEX UX_Receipts_ReceiptCode ON dbo.Receipts(ReceiptCode);
GO

CREATE UNIQUE INDEX UX_Receipts_PaymentID ON dbo.Receipts(PaymentID);
GO

CREATE TABLE dbo.Expenses
(
    ExpenseID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Expenses PRIMARY KEY,
    ExpenseCode NVARCHAR(50) NOT NULL,
    ExpenseCategoryID INT NOT NULL,
    VendorID INT NULL,
    FundID INT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    Amount DECIMAL(18,2) NOT NULL,
    ExpenseDate DATETIME2(0) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,
    ReceiverName NVARCHAR(150) NULL,
    ReceiptImagePath NVARCHAR(500) NULL,
    ExpenseStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_Expenses_ExpenseStatus DEFAULT N'Draft',
    CreatedBy INT NOT NULL,
    ApprovedBy INT NULL,
    ApprovedAt DATETIME2(0) NULL,
    PaidBy INT NULL,
    PaidAt DATETIME2(0) NULL,
    RejectedReason NVARCHAR(500) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Expenses_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Expenses_ExpenseCategories FOREIGN KEY (ExpenseCategoryID) REFERENCES dbo.ExpenseCategories(ExpenseCategoryID),
    CONSTRAINT FK_Expenses_Vendors FOREIGN KEY (VendorID) REFERENCES dbo.Vendors(VendorID),
    CONSTRAINT FK_Expenses_Funds FOREIGN KEY (FundID) REFERENCES dbo.Funds(FundID),
    CONSTRAINT FK_Expenses_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Expenses_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Expenses_PaidBy FOREIGN KEY (PaidBy) REFERENCES dbo.Users(UserID),
    CONSTRAINT CK_Expenses_PaymentMethod CHECK (PaymentMethod IN (N'Cash', N'BankTransfer', N'Other')),
    CONSTRAINT CK_Expenses_Status CHECK (ExpenseStatus IN (N'Draft', N'PendingApproval', N'Approved', N'Rejected', N'Paid', N'Cancelled'))
);
GO

CREATE UNIQUE INDEX UX_Expenses_ExpenseCode ON dbo.Expenses(ExpenseCode);
GO

CREATE TABLE dbo.PaymentVouchers
(
    VoucherID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentVouchers PRIMARY KEY,
    VoucherCode NVARCHAR(50) NOT NULL,
    ExpenseID INT NOT NULL,
    PayeeName NVARCHAR(150) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    VoucherDate DATETIME2(0) NOT NULL CONSTRAINT DF_PaymentVouchers_VoucherDate DEFAULT GETDATE(),
    CreatedBy INT NOT NULL,
    Note NVARCHAR(500) NULL,
    CONSTRAINT FK_PaymentVouchers_Expenses FOREIGN KEY (ExpenseID) REFERENCES dbo.Expenses(ExpenseID),
    CONSTRAINT FK_PaymentVouchers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserID)
);
GO

CREATE UNIQUE INDEX UX_PaymentVouchers_VoucherCode ON dbo.PaymentVouchers(VoucherCode);
GO

CREATE UNIQUE INDEX UX_PaymentVouchers_ExpenseID ON dbo.PaymentVouchers(ExpenseID);
GO

CREATE TABLE dbo.FundTransactions
(
    FundTransactionID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FundTransactions PRIMARY KEY,
    FundID INT NOT NULL,
    TransactionType NVARCHAR(50) NOT NULL,
    ReferenceType NVARCHAR(50) NULL,
    ReferenceID INT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransactionDate DATETIME2(0) NOT NULL CONSTRAINT DF_FundTransactions_TransactionDate DEFAULT GETDATE(),
    CreatedBy INT NOT NULL,
    Note NVARCHAR(500) NULL,
    CONSTRAINT FK_FundTransactions_Funds FOREIGN KEY (FundID) REFERENCES dbo.Funds(FundID),
    CONSTRAINT FK_FundTransactions_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserID),
    CONSTRAINT CK_FundTransactions_TransactionType CHECK (TransactionType IN (N'Income', N'Expense', N'Adjustment'))
);
GO

CREATE INDEX IX_FundTransactions_FundID_TransactionDate ON dbo.FundTransactions(FundID, TransactionDate DESC);
GO

CREATE TABLE dbo.FinancialPeriods
(
    PeriodID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FinancialPeriods PRIMARY KEY,
    [Month] INT NOT NULL,
    [Year] INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Status NVARCHAR(50) NOT NULL CONSTRAINT DF_FinancialPeriods_Status DEFAULT N'Open',
    ClosedBy INT NULL,
    ClosedAt DATETIME2(0) NULL,
    Note NVARCHAR(500) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_FinancialPeriods_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_FinancialPeriods_ClosedBy FOREIGN KEY (ClosedBy) REFERENCES dbo.Users(UserID),
    CONSTRAINT CK_FinancialPeriods_Status CHECK (Status IN (N'Open', N'Closed'))
);
GO

CREATE UNIQUE INDEX UX_FinancialPeriods_Month_Year ON dbo.FinancialPeriods([Month], [Year]);
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConfirmPayment
    @PaymentID INT,
    @ConfirmedBy INT,
    @ConfirmationNote NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @InvoiceID INT;
    DECLARE @ApartmentID INT;
    DECLARE @Amount DECIMAL(18,2);
    DECLARE @PaymentStatus NVARCHAR(50);
    DECLARE @InvoiceTotal DECIMAL(18,2);
    DECLARE @InvoicePaid DECIMAL(18,2);
    DECLARE @InvoiceRemaining DECIMAL(18,2);
    DECLARE @InvoiceDueDate DATE;
    DECLARE @PeriodStatus NVARCHAR(50);
    DECLARE @PayerName NVARCHAR(150);
    DECLARE @FundID INT;
    DECLARE @NewPaidAmount DECIMAL(18,2);
    DECLARE @NewRemainingAmount DECIMAL(18,2);
    DECLARE @NewInvoiceStatus NVARCHAR(50);
    DECLARE @ReceiptCode NVARCHAR(50);

    BEGIN TRANSACTION;

    SELECT
        @InvoiceID = p.InvoiceID,
        @ApartmentID = p.ApartmentID,
        @Amount = p.Amount,
        @PaymentStatus = p.PaymentStatus,
        @PayerName = COALESCE(r.FullName, u.FullName, N'Cư dân')
    FROM dbo.Payments p
    LEFT JOIN dbo.Residents r ON p.ResidentID = r.ResidentID
    LEFT JOIN dbo.Users u ON p.CreatedBy = u.UserID
    WHERE p.PaymentID = @PaymentID;

    IF @InvoiceID IS NULL
    BEGIN
        RAISERROR(N'Payment does not exist.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    IF @PaymentStatus <> N'Pending'
    BEGIN
        RAISERROR(N'Only pending payments can be confirmed.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    SELECT
        @InvoiceTotal = i.TotalAmount,
        @InvoicePaid = i.PaidAmount,
        @InvoiceRemaining = i.RemainingAmount,
        @InvoiceDueDate = i.DueDate,
        @PeriodStatus = fp.Status
    FROM dbo.Invoices i
    LEFT JOIN dbo.FinancialPeriods fp
        ON fp.[Month] = i.[Month]
       AND fp.[Year] = i.[Year]
    WHERE i.InvoiceID = @InvoiceID;

    IF @PeriodStatus = N'Closed'
    BEGIN
        RAISERROR(N'Cannot confirm payment in a closed financial period.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    IF @Amount <= 0
    BEGIN
        RAISERROR(N'Payment amount must be greater than 0.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    IF @Amount > @InvoiceRemaining
    BEGIN
        RAISERROR(N'Payment amount exceeds invoice remaining amount.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    SET @NewPaidAmount = @InvoicePaid + @Amount;
    SET @NewRemainingAmount = CASE WHEN @InvoiceTotal - @NewPaidAmount < 0 THEN 0 ELSE @InvoiceTotal - @NewPaidAmount END;
    SET @NewInvoiceStatus = CASE
        WHEN @NewRemainingAmount = 0 THEN N'Paid'
        WHEN @NewPaidAmount > 0 THEN N'PartiallyPaid'
        WHEN @InvoiceDueDate < CAST(GETDATE() AS DATE) THEN N'Overdue'
        ELSE N'Unpaid'
    END;

    SELECT TOP (1) @FundID = ft.FundID
    FROM dbo.InvoiceDetails id
    INNER JOIN dbo.FeeTypes ft ON id.FeeTypeID = ft.FeeTypeID
    WHERE id.InvoiceID = @InvoiceID
      AND ft.FundID IS NOT NULL
    ORDER BY id.InvoiceDetailID;

    IF @FundID IS NULL
    BEGIN
        SELECT TOP (1) @FundID = FundID
        FROM dbo.Funds
        WHERE FundName = N'Quỹ vận hành'
          AND IsActive = 1;
    END;

    UPDATE dbo.Payments
    SET PaymentStatus = N'Confirmed',
        ConfirmedBy = @ConfirmedBy,
        ConfirmedAt = GETDATE(),
        Note = COALESCE(@ConfirmationNote, Note)
    WHERE PaymentID = @PaymentID;

    UPDATE dbo.Invoices
    SET PaidAmount = @NewPaidAmount,
        RemainingAmount = @NewRemainingAmount,
        PaymentStatus = @NewInvoiceStatus,
        ConfirmedBy = @ConfirmedBy,
        PaidAt = CASE WHEN @NewRemainingAmount = 0 THEN GETDATE() ELSE PaidAt END,
        UpdatedAt = GETDATE()
    WHERE InvoiceID = @InvoiceID;

    SET @ReceiptCode = CONCAT(N'RCP-', CONVERT(VARCHAR(8), GETDATE(), 112), N'-', RIGHT(CONCAT(N'000000', CAST(@PaymentID AS NVARCHAR(20))), 6));

    IF NOT EXISTS (SELECT 1 FROM dbo.Receipts WHERE PaymentID = @PaymentID)
    BEGIN
        INSERT INTO dbo.Receipts
            (ReceiptCode, PaymentID, InvoiceID, ApartmentID, PayerName, Amount, ReceiptDate, CreatedBy, Note)
        VALUES
            (@ReceiptCode, @PaymentID, @InvoiceID, @ApartmentID, @PayerName, @Amount, GETDATE(), @ConfirmedBy, @ConfirmationNote);
    END;

    IF @FundID IS NOT NULL
    BEGIN
        INSERT INTO dbo.FundTransactions
            (FundID, TransactionType, ReferenceType, ReferenceID, Amount, TransactionDate, CreatedBy, Note)
        VALUES
            (@FundID, N'Income', N'Payment', @PaymentID, @Amount, GETDATE(), @ConfirmedBy, COALESCE(@ConfirmationNote, CONCAT(N'Confirmed payment ', @PaymentID)));
    END;

    COMMIT TRANSACTION;
END;
GO
