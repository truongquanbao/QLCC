-- =====================================================
-- APARTMENT MANAGER - VERIFICATION SCRIPT
-- Run this after importing the database
-- =====================================================

USE ApartmentManagerDB;
GO

PRINT N'===== TABLES =====';
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = N'dbo'
ORDER BY TABLE_NAME;
GO

PRINT N'===== USERS =====';
SELECT TOP 10 u.UserID, u.Username, u.FullName, u.Email, r.RoleName, u.Status, u.IsApproved
FROM dbo.Users u
INNER JOIN dbo.Roles r ON u.RoleID = r.RoleID
ORDER BY u.UserID;
GO

PRINT N'===== ROLES & PERMISSIONS =====';
SELECT r.RoleName, p.PermissionName
FROM dbo.RolePermissions rp
INNER JOIN dbo.Roles r ON rp.RoleID = r.RoleID
INNER JOIN dbo.Permissions p ON rp.PermissionID = p.PermissionID
ORDER BY r.RoleName, p.PermissionName;
GO

PRINT N'===== BUILDINGS =====';
SELECT BuildingID, BuildingName, Address, CreatedAt, UpdatedAt
FROM dbo.Buildings
ORDER BY BuildingID;
GO

PRINT N'===== APARTMENTS =====';
SELECT TOP 10 ApartmentID, ApartmentCode, FloorID, Area, ApartmentType, Status, MaxResidents
FROM dbo.Apartments
ORDER BY ApartmentCode;
GO

PRINT N'===== FEE TYPES =====';
SELECT FeeTypeID, FeeTypeName, Description, UnitOfMeasurement, Status
FROM dbo.FeeTypes
ORDER BY FeeTypeID;
GO

PRINT N'===== INVOICES =====';
SELECT TOP 10 InvoiceID, ApartmentID, [Month], [Year], DueDate, PaymentStatus, TotalAmount, PaidAmount
FROM dbo.Invoices
ORDER BY CreatedAt DESC;
GO

PRINT N'===== RESIDENTS =====';
SELECT TOP 10 r.ResidentID, r.FullName, r.Phone, r.Email, r.CCCD, r.Status, a.ApartmentCode
FROM dbo.Residents r
LEFT JOIN dbo.Apartments a ON r.ApartmentID = a.ApartmentID
ORDER BY r.ResidentID;
GO

PRINT N'===== COMPLAINTS =====';
SELECT TOP 10 ComplaintID, ResidentID, ApartmentID, Category, Title, Priority, Status
FROM dbo.Complaints
ORDER BY CreatedAt DESC;
GO

PRINT N'===== NOTIFICATIONS =====';
SELECT TOP 10 NotificationID, UserID, ResidentID, Title, NotificationType, Priority, Status, IsRead, SentDate
FROM dbo.Notifications
ORDER BY CreatedAt DESC;
GO

PRINT N'===== VISITORS =====';
SELECT TOP 10 VisitorID, ResidentID, VisitorName, Purpose, ArrivalTime, DepartureTime, Status
FROM dbo.Visitors
ORDER BY CreatedAt DESC;
GO

PRINT N'===== VEHICLES =====';
SELECT TOP 10 VehicleID, ResidentID, VehicleType, LicensePlate, Brand, Status
FROM dbo.Vehicles
ORDER BY CreatedAt DESC;
GO

PRINT N'===== SYSTEM CONFIG =====';
SELECT ConfigKey, ConfigValue, Description
FROM dbo.SystemConfig
ORDER BY ConfigKey;
GO

PRINT N'===== DATABASE INITIALIZATION COMPLETE =====';
GO
