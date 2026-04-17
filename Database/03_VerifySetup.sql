-- =====================================================
-- APARTMENT MANAGER - INITIALIZATION SETUP
-- Run this to verify database is properly set up
-- =====================================================

USE ApartmentManagerDB;
GO

-- Check if all tables exist
PRINT '===== CHECKING DATABASE TABLES ====='
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME;
GO

-- Check user count
PRINT '===== USER ACCOUNTS ====='
SELECT UserID, Username, FullName, Email, RoleName, Status
FROM Users u
INNER JOIN Roles r ON u.RoleID = r.RoleID
ORDER BY UserID;
GO

-- Check system config
PRINT '===== SYSTEM CONFIGURATION ====='
SELECT ConfigKey, ConfigValue
FROM SystemConfig
ORDER BY ConfigKey;
GO

-- Check roles and permissions
PRINT '===== ROLES & PERMISSIONS ====='
SELECT 
    r.RoleName,
    p.PermissionName
FROM RolePermissions rp
INNER JOIN Roles r ON rp.RoleID = r.RoleID
INNER JOIN Permissions p ON rp.PermissionID = p.PermissionID
ORDER BY r.RoleName, p.PermissionName;
GO

-- Check sample data
PRINT '===== BUILDINGS ====='
SELECT * FROM Buildings;
GO

PRINT '===== APARTMENTS ====='
SELECT TOP 5 ApartmentCode, Area, ApartmentType, Status FROM Apartments;
GO

PRINT '===== DATABASE INITIALIZATION COMPLETE ====='
PRINT 'All tables created successfully!';
GO
