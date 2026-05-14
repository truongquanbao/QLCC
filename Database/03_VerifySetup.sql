-- =====================================================
-- APARTMENT MANAGER - DATABASE VERIFICATION SCRIPT
-- Run this after importing the reset demo database.
-- =====================================================

USE ApartmentManagerDB;
GO

SET NOCOUNT ON;
GO

DECLARE @ActiveResidentCount INT = (SELECT COUNT(*) FROM dbo.Residents WHERE Status = N'Active');
DECLARE @BuildingCount INT = (SELECT COUNT(*) FROM dbo.Buildings);
DECLARE @ApartmentCount INT = (SELECT COUNT(*) FROM dbo.Apartments);
DECLARE @AssetCount INT = (SELECT COUNT(*) FROM dbo.Assets);
DECLARE @MaintenanceCount INT = (SELECT COUNT(*) FROM dbo.MaintenanceSchedules);
DECLARE @VehicleCount INT = (SELECT COUNT(*) FROM dbo.Vehicles);
DECLARE @VisitorCount INT = (SELECT COUNT(*) FROM dbo.Visitors);
DECLARE @ComplaintCount INT = (SELECT COUNT(*) FROM dbo.Complaints);
DECLARE @NotificationCount INT = (SELECT COUNT(*) FROM dbo.Notifications);
DECLARE @InvoiceCount INT = (SELECT COUNT(*) FROM dbo.Invoices);
DECLARE @PaymentCount INT = (SELECT COUNT(*) FROM dbo.Payments);
DECLARE @ReceiptCount INT = (SELECT COUNT(*) FROM dbo.Receipts);
DECLARE @FinancialPeriodCount INT = (SELECT COUNT(*) FROM dbo.FinancialPeriods);
DECLARE @ExpenseCount INT = (SELECT COUNT(*) FROM dbo.Expenses);

IF @ActiveResidentCount <> 200
BEGIN
    THROW 52000, 'Verification failed: active resident count must be exactly 200.', 1;
END;

IF @BuildingCount <> 3
BEGIN
    THROW 52001, 'Verification failed: demo database must contain exactly 3 buildings.', 1;
END;

IF @ApartmentCount < 300
BEGIN
    THROW 52002, 'Verification failed: apartment inventory is lower than expected.', 1;
END;

IF @AssetCount = 0 OR @MaintenanceCount = 0 OR @VehicleCount = 0 OR @VisitorCount = 0
   OR @ComplaintCount = 0 OR @NotificationCount = 0 OR @InvoiceCount = 0 OR @PaymentCount = 0
   OR @ReceiptCount = 0 OR @FinancialPeriodCount = 0 OR @ExpenseCount = 0
BEGIN
    THROW 52003, 'Verification failed: one or more operational demo modules have no data.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.Residents r
    LEFT JOIN dbo.Apartments a ON a.ApartmentID = r.ApartmentID
    WHERE a.ApartmentID IS NULL
)
BEGIN
    THROW 52004, 'Verification failed: resident orphan rows detected.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.Apartments a
    LEFT JOIN dbo.Floors f ON f.FloorID = a.FloorID
    WHERE f.FloorID IS NULL
)
BEGIN
    THROW 52005, 'Verification failed: apartment orphan rows detected.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.Invoices i
    LEFT JOIN dbo.Apartments a ON a.ApartmentID = i.ApartmentID
    WHERE a.ApartmentID IS NULL
)
BEGIN
    THROW 52006, 'Verification failed: invoice orphan rows detected.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.Payments p
    LEFT JOIN dbo.Invoices i ON i.InvoiceID = p.InvoiceID
    LEFT JOIN dbo.Apartments a ON a.ApartmentID = p.ApartmentID
    LEFT JOIN dbo.Residents r ON r.ResidentID = p.ResidentID
    WHERE i.InvoiceID IS NULL
       OR a.ApartmentID IS NULL
       OR (p.ResidentID IS NOT NULL AND r.ResidentID IS NULL)
)
BEGIN
    THROW 52007, 'Verification failed: payment orphan rows detected.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.MaintenanceSchedules m
    LEFT JOIN dbo.Assets a ON a.AssetID = m.AssetID
    WHERE a.AssetID IS NULL
)
BEGIN
    THROW 52008, 'Verification failed: asset maintenance orphan rows detected.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.Invoices
    WHERE RemainingAmount <> CASE WHEN TotalAmount - PaidAmount < 0 THEN 0 ELSE TotalAmount - PaidAmount END
)
BEGIN
    THROW 52009, 'Verification failed: invoice remaining amount mismatch detected.', 1;
END;

PRINT N'===== DATABASE SUMMARY =====';
SELECT N'Buildings' AS Metric, @BuildingCount AS [Value]
UNION ALL SELECT N'Apartments', @ApartmentCount
UNION ALL SELECT N'Active residents', @ActiveResidentCount
UNION ALL SELECT N'Vehicles', @VehicleCount
UNION ALL SELECT N'Visitors', @VisitorCount
UNION ALL SELECT N'Complaints', @ComplaintCount
UNION ALL SELECT N'Notifications', @NotificationCount
UNION ALL SELECT N'Invoices', @InvoiceCount
UNION ALL SELECT N'Payments', @PaymentCount
UNION ALL SELECT N'Receipts', @ReceiptCount
UNION ALL SELECT N'Financial periods', @FinancialPeriodCount
UNION ALL SELECT N'Expenses', @ExpenseCount
UNION ALL SELECT N'Assets', @AssetCount
UNION ALL SELECT N'Maintenance schedules', @MaintenanceCount;

PRINT N'===== RESIDENT DISTRIBUTION BY BUILDING =====';
SELECT b.BuildingName,
       COUNT(DISTINCT a.ApartmentID) AS TotalApartments,
       COUNT(DISTINCT CASE WHEN r.ResidentID IS NOT NULL THEN a.ApartmentID END) AS ApartmentsWithResidents,
       COUNT(r.ResidentID) AS ActiveResidents
FROM dbo.Buildings b
INNER JOIN dbo.Blocks bl ON bl.BuildingID = b.BuildingID
INNER JOIN dbo.Floors f ON f.BlockID = bl.BlockID
INNER JOIN dbo.Apartments a ON a.FloorID = f.FloorID
LEFT JOIN dbo.Residents r ON r.ApartmentID = a.ApartmentID AND r.Status = N'Active'
GROUP BY b.BuildingName
ORDER BY b.BuildingName;

PRINT N'===== APARTMENT STATUS =====';
SELECT Status, COUNT(*) AS Total
FROM dbo.Apartments
GROUP BY Status
ORDER BY Status;

PRINT N'===== INVOICE STATUS =====';
SELECT PaymentStatus, COUNT(*) AS Total, SUM(TotalAmount) AS TotalAmount, SUM(RemainingAmount) AS RemainingAmount
FROM dbo.Invoices
GROUP BY PaymentStatus
ORDER BY PaymentStatus;

PRINT N'===== ASSET CONDITION =====';
SELECT Condition, COUNT(*) AS Total
FROM dbo.Assets
GROUP BY Condition
ORDER BY Condition;

PRINT N'===== SAMPLE USERS =====';
SELECT TOP 10 u.UserID, u.Username, u.FullName, u.Email, r.RoleName, u.Status, u.IsApproved
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.RoleID = u.RoleID
ORDER BY u.UserID;

PRINT N'===== DATABASE VERIFICATION PASSED =====';
GO
