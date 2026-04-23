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
    Note NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Invoices_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT FK_Invoices_Apartments
        FOREIGN KEY (ApartmentID) REFERENCES dbo.Apartments(ApartmentID)
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
