USE ApartmentManagerDB;
GO

IF OBJECT_ID(N'dbo.Assets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Assets
    (
        AssetID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Assets PRIMARY KEY,
        AssetCode NVARCHAR(50) NOT NULL CONSTRAINT UX_Assets_AssetCode UNIQUE,
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
END
GO

IF OBJECT_ID(N'dbo.MaintenanceSchedules', N'U') IS NULL
BEGIN
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
        CONSTRAINT FK_MaintenanceSchedules_Assets FOREIGN KEY (AssetID) REFERENCES dbo.Assets(AssetID)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Assets WHERE AssetCode = N'TS-A-LIFT-01')
BEGIN
    INSERT INTO dbo.Assets
        (AssetCode, AssetName, AssetType, Location, PurchaseDate, Condition, LastMaintenanceDate, NextMaintenanceDate, RepairCost, Note)
    VALUES
        (N'TS-A-LIFT-01', N'Thang máy A1', N'Thang máy', N'Tòa A', '2020-01-12', N'Tốt', '2024-04-18', '2024-05-18', 2500000, N'Bảo trì định kỳ theo tháng'),
        (N'TS-B-PCCC-02', N'Tủ PCCC tầng 12', N'PCCC', N'Tòa B', '2021-03-20', N'Cần bảo trì', '2024-02-20', '2024-05-20', 850000, N'Kiểm tra áp suất bình'),
        (N'TS-C-CAM-08', N'Camera sảnh C', N'An ninh', N'Sảnh C', '2022-08-02', N'Hỏng', '2024-04-10', '2024-05-17', 1250000, N'Mất tín hiệu hình ảnh'),
        (N'TS-G-PUMP-01', N'Máy bơm nước', N'Cấp nước', N'Tầng hầm', '2019-09-15', N'Tốt', '2024-05-01', '2024-06-01', 0, N'Hoạt động ổn định'),
        (N'TS-Y-GARDEN-04', N'Máy cắt cỏ', N'Cảnh quan', N'Sân vườn', '2022-11-21', N'Bảo trì', '2024-05-11', '2024-05-25', 430000, N'Thay lưỡi cắt');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.MaintenanceSchedules)
BEGIN
    INSERT INTO dbo.MaintenanceSchedules (AssetID, Category, ScheduledDate, Status, AssignedTo, Note)
    SELECT AssetID, N'Bảo trì thang máy', '2024-05-18', N'Đã lên lịch', N'Nguyễn Đức Hải', N'Bảo trì định kỳ'
    FROM dbo.Assets WHERE AssetCode = N'TS-A-LIFT-01';

    INSERT INTO dbo.MaintenanceSchedules (AssetID, Category, ScheduledDate, Status, AssignedTo, Note)
    SELECT AssetID, N'Kiểm tra PCCC định kỳ', '2024-05-20', N'Đã lên lịch', N'Lê Hoàng Nam', N'Kiểm tra tủ PCCC tầng 12'
    FROM dbo.Assets WHERE AssetCode = N'TS-B-PCCC-02';

    INSERT INTO dbo.MaintenanceSchedules (AssetID, Category, ScheduledDate, Status, AssignedTo, Note)
    SELECT AssetID, N'Sửa camera', '2024-05-17', N'Chờ xử lý', N'Trần Minh Tuấn', N'Khôi phục tín hiệu camera'
    FROM dbo.Assets WHERE AssetCode = N'TS-C-CAM-08';
END
GO
