-- =====================================================
-- APARTMENT MANAGER - SEED DATA
-- Initial data for testing and development
-- =====================================================

USE ApartmentManagerDB;
GO

-- =====================================================
-- Transaction to ensure all-or-nothing insertion
-- =====================================================
BEGIN TRANSACTION;

BEGIN TRY
    -- =====================================================
    -- 1. CHECK IF ALREADY SEEDED
    -- =====================================================
    DECLARE @IsSeeded BIT;
    SELECT @IsSeeded = CAST(ConfigValue AS BIT) 
    FROM SystemConfig 
    WHERE ConfigKey = 'IsSeeded';

    IF @IsSeeded = 1
    BEGIN
        PRINT 'Database is already seeded. Skipping...';
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    PRINT '========== STARTING SEED DATA INSERTION ==========';

    -- =====================================================
    -- 2. INSERT ROLES (Super Admin, Manager, Resident)
    -- =====================================================
    PRINT 'Inserting Roles...';
    
    IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Super Admin')
        INSERT INTO Roles (RoleName, Description, CreatedAt)
        VALUES ('Super Admin', 'Quản trị viên cao nhất của hệ thống', GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Manager')
        INSERT INTO Roles (RoleName, Description, CreatedAt)
        VALUES ('Manager', 'Quản lý khu chung cư', GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Resident')
        INSERT INTO Roles (RoleName, Description, CreatedAt)
        VALUES ('Resident', 'Cư dân chung cư', GETDATE());

    -- Get role IDs for later use
    DECLARE @SuperAdminRoleID INT;
    DECLARE @ManagerRoleID INT;
    DECLARE @ResidentRoleID INT;

    SELECT @SuperAdminRoleID = RoleID FROM Roles WHERE RoleName = 'Super Admin';
    SELECT @ManagerRoleID = RoleID FROM Roles WHERE RoleName = 'Manager';
    SELECT @ResidentRoleID = RoleID FROM Roles WHERE RoleName = 'Resident';

    -- =====================================================
    -- 3. INSERT PERMISSIONS
    -- =====================================================
    PRINT 'Inserting Permissions...';

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PermissionName = 'UserManagement')
        INSERT INTO Permissions (PermissionName, Description, CreatedAt)
        VALUES ('UserManagement', 'Quản lý tài khoản người dùng', GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PermissionName = 'ApartmentManagement')
        INSERT INTO Permissions (PermissionName, Description, CreatedAt)
        VALUES ('ApartmentManagement', 'Quản lý căn hộ', GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PermissionName = 'ResidentManagement')
        INSERT INTO Permissions (PermissionName, Description, CreatedAt)
        VALUES ('ResidentManagement', 'Quản lý cư dân', GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PermissionName = 'InvoiceManagement')
        INSERT INTO Permissions (PermissionName, Description, CreatedAt)
        VALUES ('InvoiceManagement', 'Quản lý hóa đơn', GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PermissionName = 'ComplaintManagement')
        INSERT INTO Permissions (PermissionName, Description, CreatedAt)
        VALUES ('ComplaintManagement', 'Quản lý phản ánh', GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PermissionName = 'NotificationManagement')
        INSERT INTO Permissions (PermissionName, Description, CreatedAt)
        VALUES ('NotificationManagement', 'Quản lý thông báo', GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PermissionName = 'ReportGeneration')
        INSERT INTO Permissions (PermissionName, Description, CreatedAt)
        VALUES ('ReportGeneration', 'Tạo báo cáo', GETDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PermissionName = 'SystemConfiguration')
        INSERT INTO Permissions (PermissionName, Description, CreatedAt)
        VALUES ('SystemConfiguration', 'Cấu hình hệ thống', GETDATE());

    -- =====================================================
    -- 4. INSERT ROLE PERMISSIONS
    -- =====================================================
    PRINT 'Assigning permissions to roles...';

    -- Clear existing role permissions
    DELETE FROM RolePermissions WHERE RoleID IN (@SuperAdminRoleID, @ManagerRoleID, @ResidentRoleID);

    -- Get all permission IDs
    DECLARE @PermID_UserMgmt INT, @PermID_AptMgmt INT, @PermID_ResMgmt INT;
    DECLARE @PermID_InvMgmt INT, @PermID_ComplMgmt INT, @PermID_NotifMgmt INT;
    DECLARE @PermID_Report INT, @PermID_SysCfg INT;

    SELECT @PermID_UserMgmt = PermissionID FROM Permissions WHERE PermissionName = 'UserManagement';
    SELECT @PermID_AptMgmt = PermissionID FROM Permissions WHERE PermissionName = 'ApartmentManagement';
    SELECT @PermID_ResMgmt = PermissionID FROM Permissions WHERE PermissionName = 'ResidentManagement';
    SELECT @PermID_InvMgmt = PermissionID FROM Permissions WHERE PermissionName = 'InvoiceManagement';
    SELECT @PermID_ComplMgmt = PermissionID FROM Permissions WHERE PermissionName = 'ComplaintManagement';
    SELECT @PermID_NotifMgmt = PermissionID FROM Permissions WHERE PermissionName = 'NotificationManagement';
    SELECT @PermID_Report = PermissionID FROM Permissions WHERE PermissionName = 'ReportGeneration';
    SELECT @PermID_SysCfg = PermissionID FROM Permissions WHERE PermissionName = 'SystemConfiguration';

    -- Super Admin has all permissions
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@SuperAdminRoleID, @PermID_UserMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@SuperAdminRoleID, @PermID_AptMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@SuperAdminRoleID, @PermID_ResMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@SuperAdminRoleID, @PermID_InvMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@SuperAdminRoleID, @PermID_ComplMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@SuperAdminRoleID, @PermID_NotifMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@SuperAdminRoleID, @PermID_Report);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@SuperAdminRoleID, @PermID_SysCfg);

    -- Manager has most permissions except SystemConfiguration
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@ManagerRoleID, @PermID_AptMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@ManagerRoleID, @PermID_ResMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@ManagerRoleID, @PermID_InvMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@ManagerRoleID, @PermID_ComplMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@ManagerRoleID, @PermID_NotifMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@ManagerRoleID, @PermID_Report);

    -- Resident has limited permissions
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@ResidentRoleID, @PermID_InvMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@ResidentRoleID, @PermID_ComplMgmt);
    INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@ResidentRoleID, @PermID_NotifMgmt);

    -- =====================================================
    -- 5. INSERT USERS
    -- =====================================================
    PRINT 'Inserting sample user accounts...';

    -- Super Admin: superadmin / Admin@123456
    -- BCrypt hash for Admin@123456: $2a$12$R9h/cIPz0gi.URNNX3kh2OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUm
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'superadmin')
        INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, RoleID, Status, IsApproved, ApprovedAt, CreatedAt)
        VALUES ('superadmin', 
                '$2a$12$R9h/cIPz0gi.URNNX3kh2OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUm',
                'Super Administrator',
                'superadmin@system.local',
                '0900000001',
                @SuperAdminRoleID,
                'Active',
                1,
                GETDATE(),
                GETDATE());

    -- Manager: manager1 / Manager@123
    -- BCrypt hash for Manager@123: $2a$12$LQv3c1yqBWVHxkd0LHAkCOYvEgcZhO3h.s.EJEE0nWMOhRiZlPO1S
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'manager1')
        INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, RoleID, Status, IsApproved, ApprovedAt, CreatedAt)
        VALUES ('manager1',
                '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYvEgcZhO3h.s.EJEE0nWMOhRiZlPO1S',
                'Manager One',
                'manager1@system.local',
                '0900000002',
                @ManagerRoleID,
                'Active',
                1,
                GETDATE(),
                GETDATE());

    -- Resident: resident1 / Resident@123
    -- BCrypt hash for Resident@123: $2a$12$y2E5lfR3Uv6B1Qj1Qj.eOeI6VVlH8VQvVL9U.cE9qPkh7Hqla
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'resident1')
        INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, RoleID, Status, IsApproved, ApprovedAt, CreatedAt)
        VALUES ('resident1',
                '$2a$12$y2E5lfR3Uv6B1Qj1Qj.eOeI6VVlH8VQvVL9U.cE9qPkh7Hqla',
                'Resident One',
                'resident1@system.local',
                '0900000003',
                @ResidentRoleID,
                'Active',
                1,
                GETDATE(),
                GETDATE());

    DECLARE @SuperAdminUserID INT, @Manager1UserID INT, @Resident1UserID INT;
    SELECT @SuperAdminUserID = UserID FROM Users WHERE Username = 'superadmin';
    SELECT @Manager1UserID = UserID FROM Users WHERE Username = 'manager1';
    SELECT @Resident1UserID = UserID FROM Users WHERE Username = 'resident1';

    -- =====================================================
    -- 6. INSERT BUILDINGS, BLOCKS, FLOORS, APARTMENTS
    -- =====================================================
    PRINT 'Inserting buildings and apartments...';

    -- Create Building
    DECLARE @BuildingID INT;
    IF NOT EXISTS (SELECT 1 FROM Buildings WHERE BuildingName = 'Tòa Nhà A')
    BEGIN
        INSERT INTO Buildings (BuildingName, Address, Description, CreatedAt)
        VALUES ('Tòa Nhà A', '123 Đường Nguyễn Huệ, Quận 1, TP.HCM', 'Tòa nhà cao tầng mẫu', GETDATE());
        SELECT @BuildingID = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @BuildingID = BuildingID FROM Buildings WHERE BuildingName = 'Tòa Nhà A';
    END

    -- Create Block 1
    DECLARE @Block1ID INT, @Block2ID INT;
    IF NOT EXISTS (SELECT 1 FROM Blocks WHERE BlockName = 'Block A')
    BEGIN
        INSERT INTO Blocks (BlockName, BuildingID, CreatedAt)
        VALUES ('Block A', @BuildingID, GETDATE());
        SELECT @Block1ID = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @Block1ID = BlockID FROM Blocks WHERE BlockName = 'Block A';
    END

    -- Create Block 2
    IF NOT EXISTS (SELECT 1 FROM Blocks WHERE BlockName = 'Block B')
    BEGIN
        INSERT INTO Blocks (BlockName, BuildingID, CreatedAt)
        VALUES ('Block B', @BuildingID, GETDATE());
        SELECT @Block2ID = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @Block2ID = BlockID FROM Blocks WHERE BlockName = 'Block B';
    END

    -- Create 5 Floors for Block 1
    DECLARE @FloorID1_1 INT, @FloorID1_2 INT, @FloorID1_3 INT, @FloorID1_4 INT, @FloorID1_5 INT;
    
    IF NOT EXISTS (SELECT 1 FROM Floors WHERE FloorNumber = 1 AND BlockID = @Block1ID)
    BEGIN
        INSERT INTO Floors (FloorNumber, BlockID, CreatedAt) VALUES (1, @Block1ID, GETDATE());
        SELECT @FloorID1_1 = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @FloorID1_1 = FloorID FROM Floors WHERE FloorNumber = 1 AND BlockID = @Block1ID;
    END

    IF NOT EXISTS (SELECT 1 FROM Floors WHERE FloorNumber = 2 AND BlockID = @Block1ID)
    BEGIN
        INSERT INTO Floors (FloorNumber, BlockID, CreatedAt) VALUES (2, @Block1ID, GETDATE());
        SELECT @FloorID1_2 = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @FloorID1_2 = FloorID FROM Floors WHERE FloorNumber = 2 AND BlockID = @Block1ID;
    END

    IF NOT EXISTS (SELECT 1 FROM Floors WHERE FloorNumber = 3 AND BlockID = @Block1ID)
    BEGIN
        INSERT INTO Floors (FloorNumber, BlockID, CreatedAt) VALUES (3, @Block1ID, GETDATE());
        SELECT @FloorID1_3 = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @FloorID1_3 = FloorID FROM Floors WHERE FloorNumber = 3 AND BlockID = @Block1ID;
    END

    IF NOT EXISTS (SELECT 1 FROM Floors WHERE FloorNumber = 4 AND BlockID = @Block1ID)
    BEGIN
        INSERT INTO Floors (FloorNumber, BlockID, CreatedAt) VALUES (4, @Block1ID, GETDATE());
        SELECT @FloorID1_4 = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @FloorID1_4 = FloorID FROM Floors WHERE FloorNumber = 4 AND BlockID = @Block1ID;
    END

    IF NOT EXISTS (SELECT 1 FROM Floors WHERE FloorNumber = 5 AND BlockID = @Block1ID)
    BEGIN
        INSERT INTO Floors (FloorNumber, BlockID, CreatedAt) VALUES (5, @Block1ID, GETDATE());
        SELECT @FloorID1_5 = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @FloorID1_5 = FloorID FROM Floors WHERE FloorNumber = 5 AND BlockID = @Block1ID;
    END

    -- Create 5 Floors for Block 2
    DECLARE @FloorID2_1 INT, @FloorID2_2 INT, @FloorID2_3 INT, @FloorID2_4 INT, @FloorID2_5 INT;
    
    IF NOT EXISTS (SELECT 1 FROM Floors WHERE FloorNumber = 1 AND BlockID = @Block2ID)
    BEGIN
        INSERT INTO Floors (FloorNumber, BlockID, CreatedAt) VALUES (1, @Block2ID, GETDATE());
        SELECT @FloorID2_1 = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @FloorID2_1 = FloorID FROM Floors WHERE FloorNumber = 1 AND BlockID = @Block2ID;
    END

    IF NOT EXISTS (SELECT 1 FROM Floors WHERE FloorNumber = 2 AND BlockID = @Block2ID)
    BEGIN
        INSERT INTO Floors (FloorNumber, BlockID, CreatedAt) VALUES (2, @Block2ID, GETDATE());
        SELECT @FloorID2_2 = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @FloorID2_2 = FloorID FROM Floors WHERE FloorNumber = 2 AND BlockID = @Block2ID;
    END

    IF NOT EXISTS (SELECT 1 FROM Floors WHERE FloorNumber = 3 AND BlockID = @Block2ID)
    BEGIN
        INSERT INTO Floors (FloorNumber, BlockID, CreatedAt) VALUES (3, @Block2ID, GETDATE());
        SELECT @FloorID2_3 = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @FloorID2_3 = FloorID FROM Floors WHERE FloorNumber = 3 AND BlockID = @Block2ID;
    END

    IF NOT EXISTS (SELECT 1 FROM Floors WHERE FloorNumber = 4 AND BlockID = @Block2ID)
    BEGIN
        INSERT INTO Floors (FloorNumber, BlockID, CreatedAt) VALUES (4, @Block2ID, GETDATE());
        SELECT @FloorID2_4 = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @FloorID2_4 = FloorID FROM Floors WHERE FloorNumber = 4 AND BlockID = @Block2ID;
    END

    IF NOT EXISTS (SELECT 1 FROM Floors WHERE FloorNumber = 5 AND BlockID = @Block2ID)
    BEGIN
        INSERT INTO Floors (FloorNumber, BlockID, CreatedAt) VALUES (5, @Block2ID, GETDATE());
        SELECT @FloorID2_5 = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @FloorID2_5 = FloorID FROM Floors WHERE FloorNumber = 5 AND BlockID = @Block2ID;
    END

    -- Create Apartments (4 units per floor)
    DECLARE @FloorIDs TABLE (FloorID INT, FloorNumber INT);
    INSERT INTO @FloorIDs VALUES (@FloorID1_1, 1), (@FloorID1_2, 2), (@FloorID1_3, 3), (@FloorID1_4, 4), (@FloorID1_5, 5),
                                  (@FloorID2_1, 1), (@FloorID2_2, 2), (@FloorID2_3, 3), (@FloorID2_4, 4), (@FloorID2_5, 5);

    DECLARE @ApartmentCode NVARCHAR(20), @FloorID_Temp INT, @FloorNum INT, @UnitCount INT;
    DECLARE @AptCursor CURSOR;
    
    SET @AptCursor = CURSOR FOR SELECT FloorID, FloorNumber FROM @FloorIDs;
    OPEN @AptCursor;

    FETCH NEXT FROM @AptCursor INTO @FloorID_Temp, @FloorNum;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @UnitCount = 1;
        WHILE @UnitCount <= 4
        BEGIN
            SET @ApartmentCode = CONCAT(
                CASE WHEN @FloorID_Temp IN (@FloorID1_1, @FloorID1_2, @FloorID1_3, @FloorID1_4, @FloorID1_5) 
                     THEN 'A' ELSE 'B' END,
                @FloorNum,
                CASE @UnitCount WHEN 1 THEN '01' WHEN 2 THEN '02' WHEN 3 THEN '03' WHEN 4 THEN '04' END
            );

            IF NOT EXISTS (SELECT 1 FROM Apartments WHERE ApartmentCode = @ApartmentCode)
            BEGIN
                INSERT INTO Apartments (ApartmentCode, FloorID, Area, ApartmentType, Status, MaxResidents, CreatedAt)
                VALUES (@ApartmentCode, @FloorID_Temp, 75.0, N'2 Phòng Ngủ', 'Empty', 4, GETDATE());
            END

            SET @UnitCount = @UnitCount + 1;
        END
        FETCH NEXT FROM @AptCursor INTO @FloorID_Temp, @FloorNum;
    END

    CLOSE @AptCursor;
    DEALLOCATE @AptCursor;

    -- =====================================================
    -- 7. INSERT FEE TYPES
    -- =====================================================
    PRINT 'Inserting fee types...';

    IF NOT EXISTS (SELECT 1 FROM FeeTypes WHERE FeeName = N'Phí Quản Lý')
        INSERT INTO FeeTypes (FeeName, Description, Amount, Unit, CreatedAt)
        VALUES (N'Phí Quản Lý', N'Phí quản lý khu chung cư hàng tháng', 500000, N'VND', GETDATE());

    IF NOT EXISTS (SELECT 1 FROM FeeTypes WHERE FeeName = N'Phí Gửi Xe')
        INSERT INTO FeeTypes (FeeName, Description, Amount, Unit, CreatedAt)
        VALUES (N'Phí Gửi Xe', N'Phí gửi ô tô hàng tháng', 300000, N'VND', GETDATE());

    IF NOT EXISTS (SELECT 1 FROM FeeTypes WHERE FeeName = N'Phí Vệ Sinh')
        INSERT INTO FeeTypes (FeeName, Description, Amount, Unit, CreatedAt)
        VALUES (N'Phí Vệ Sinh', N'Phí vệ sinh chung cư hàng tháng', 200000, N'VND', GETDATE());

    -- =====================================================
    -- 8. CREATE RESIDENT PROFILE FOR RESIDENT1
    -- =====================================================
    PRINT 'Creating resident profile for test resident...';

    DECLARE @ApartmentID_Sample INT;
    SELECT @ApartmentID_Sample = ApartmentID FROM Apartments WHERE ApartmentCode = 'A101' LIMIT 1;

    IF NOT EXISTS (SELECT 1 FROM Residents WHERE UserID = @Resident1UserID)
    BEGIN
        INSERT INTO Residents (UserID, FullName, DOB, Gender, CCCD, Phone, Email, AddressRegistration, 
                               ApartmentID, ResidentStatus, MoveInDate, CreatedAt)
        VALUES (@Resident1UserID, 'Resident One', '1990-05-15', N'Nam', '123456789012', '0900000003', 
                'resident1@system.local', '123 Đường Nguyễn Huệ, Quận 1, TP.HCM',
                @ApartmentID_Sample, N'Đang Ở', GETDATE(), GETDATE());
    END

    -- =====================================================
    -- 9. INSERT SAMPLE INVOICES
    -- =====================================================
    PRINT 'Inserting sample invoices...';

    DECLARE @FeeType1 INT, @FeeType2 INT, @FeeType3 INT;
    SELECT @FeeType1 = FeeTypeID FROM FeeTypes WHERE FeeName = N'Phí Quản Lý';
    SELECT @FeeType2 = FeeTypeID FROM FeeTypes WHERE FeeName = N'Phí Gửi Xe';
    SELECT @FeeType3 = FeeTypeID FROM FeeTypes WHERE FeeName = N'Phí Vệ Sinh';

    -- Invoice 1: Current month (Paid)
    DECLARE @Invoice1ID INT;
    IF NOT EXISTS (SELECT 1 FROM Invoices WHERE ApartmentID = @ApartmentID_Sample AND InvoiceMonth = MONTH(GETDATE()) AND InvoiceYear = YEAR(GETDATE()))
    BEGIN
        INSERT INTO Invoices (ApartmentID, InvoiceMonth, InvoiceYear, TotalAmount, Status, CreatedAt)
        VALUES (@ApartmentID_Sample, MONTH(GETDATE()), YEAR(GETDATE()), 1000000, N'Paid', GETDATE());
        SELECT @Invoice1ID = SCOPE_IDENTITY();

        -- Add invoice details
        INSERT INTO InvoiceDetails (InvoiceID, FeeTypeID, Amount, CreatedAt)
        VALUES (@Invoice1ID, @FeeType1, 500000, GETDATE());
        INSERT INTO InvoiceDetails (InvoiceID, FeeTypeID, Amount, CreatedAt)
        VALUES (@Invoice1ID, @FeeType2, 300000, GETDATE());
        INSERT INTO InvoiceDetails (InvoiceID, FeeTypeID, Amount, CreatedAt)
        VALUES (@Invoice1ID, @FeeType3, 200000, GETDATE());
    END

    -- Invoice 2: Previous month (Unpaid)
    DECLARE @Invoice2ID INT;
    DECLARE @PrevMonth INT = MONTH(DATEADD(MONTH, -1, GETDATE()));
    DECLARE @PrevYear INT = YEAR(DATEADD(MONTH, -1, GETDATE()));
    
    IF NOT EXISTS (SELECT 1 FROM Invoices WHERE ApartmentID = @ApartmentID_Sample AND InvoiceMonth = @PrevMonth AND InvoiceYear = @PrevYear)
    BEGIN
        INSERT INTO Invoices (ApartmentID, InvoiceMonth, InvoiceYear, TotalAmount, Status, CreatedAt)
        VALUES (@ApartmentID_Sample, @PrevMonth, @PrevYear, 1000000, N'Unpaid', GETDATE());
        SELECT @Invoice2ID = SCOPE_IDENTITY();

        INSERT INTO InvoiceDetails (InvoiceID, FeeTypeID, Amount, CreatedAt)
        VALUES (@Invoice2ID, @FeeType1, 500000, GETDATE());
        INSERT INTO InvoiceDetails (InvoiceID, FeeTypeID, Amount, CreatedAt)
        VALUES (@Invoice2ID, @FeeType2, 300000, GETDATE());
        INSERT INTO InvoiceDetails (InvoiceID, FeeTypeID, Amount, CreatedAt)
        VALUES (@Invoice2ID, @FeeType3, 200000, GETDATE());
    END

    -- Invoice 3: 2 months ago (Overdue)
    DECLARE @Invoice3ID INT;
    DECLARE @Prev2Month INT = MONTH(DATEADD(MONTH, -2, GETDATE()));
    DECLARE @Prev2Year INT = YEAR(DATEADD(MONTH, -2, GETDATE()));
    
    IF NOT EXISTS (SELECT 1 FROM Invoices WHERE ApartmentID = @ApartmentID_Sample AND InvoiceMonth = @Prev2Month AND InvoiceYear = @Prev2Year)
    BEGIN
        INSERT INTO Invoices (ApartmentID, InvoiceMonth, InvoiceYear, TotalAmount, Status, CreatedAt)
        VALUES (@ApartmentID_Sample, @Prev2Month, @Prev2Year, 1000000, N'Overdue', GETDATE());
        SELECT @Invoice3ID = SCOPE_IDENTITY();

        INSERT INTO InvoiceDetails (InvoiceID, FeeTypeID, Amount, CreatedAt)
        VALUES (@Invoice3ID, @FeeType1, 500000, GETDATE());
        INSERT INTO InvoiceDetails (InvoiceID, FeeTypeID, Amount, CreatedAt)
        VALUES (@Invoice3ID, @FeeType2, 300000, GETDATE());
        INSERT INTO InvoiceDetails (InvoiceID, FeeTypeID, Amount, CreatedAt)
        VALUES (@Invoice3ID, @FeeType3, 200000, GETDATE());
    END

    -- =====================================================
    -- 10. INSERT SAMPLE COMPLAINTS
    -- =====================================================
    PRINT 'Inserting sample complaints...';

    IF NOT EXISTS (SELECT 1 FROM Complaints WHERE ResidentID = (SELECT ResidentID FROM Residents WHERE UserID = @Resident1UserID))
    BEGIN
        INSERT INTO Complaints (ResidentID, ComplaintType, Title, Description, Priority, Status, CreatedAt, UpdatedAt)
        VALUES ((SELECT ResidentID FROM Residents WHERE UserID = @Resident1UserID), 
                N'Vệ Sinh',
                N'Thang máy bẩn',
                N'Thang máy chung không được vệ sinh sạch sẽ',
                N'Medium',
                N'Open',
                GETDATE(),
                GETDATE());
    END

    IF NOT EXISTS (SELECT 1 FROM Complaints WHERE ResidentID = (SELECT ResidentID FROM Residents WHERE UserID = @Resident1UserID) AND Title = N'Tiếng ồn')
    BEGIN
        INSERT INTO Complaints (ResidentID, ComplaintType, Title, Description, Priority, Status, CreatedAt, UpdatedAt)
        VALUES ((SELECT ResidentID FROM Residents WHERE UserID = @Resident1UserID),
                N'Khác',
                N'Tiếng ồn vào ban đêm',
                N'Nhân viên trong tòa nhà gây tiếng ồn vào ban đêm',
                N'Low',
                N'Resolved',
                DATEADD(DAY, -10, GETDATE()),
                DATEADD(DAY, -5, GETDATE()));
    END

    -- =====================================================
    -- 11. INSERT SAMPLE NOTIFICATIONS
    -- =====================================================
    PRINT 'Inserting sample notifications...';

    DECLARE @NotifID1 INT, @NotifID2 INT, @NotifID3 INT;

    IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Title = N'Bảo trì thang máy')
    BEGIN
        INSERT INTO Notifications (Title, Description, Type, Priority, Status, CreatedAt)
        VALUES (N'Bảo trì thang máy', N'Thang máy sẽ được bảo trì từ 8h đến 12h hôm nay', N'Maintenance', N'High', N'Published', GETDATE());
        SELECT @NotifID1 = SCOPE_IDENTITY();
    END

    IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Title = N'Thanh toán phí tháng này')
    BEGIN
        INSERT INTO Notifications (Title, Description, Type, Priority, Status, CreatedAt)
        VALUES (N'Thanh toán phí tháng này', N'Vui lòng thanh toán phí quản lý, gửi xe và vệ sinh trước ngày 25/04/2026', N'Payment', N'Medium', N'Published', GETDATE());
        SELECT @NotifID2 = SCOPE_IDENTITY();
    END

    IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Title = N'Cúp điện bảo trì')
    BEGIN
        INSERT INTO Notifications (Title, Description, Type, Priority, Status, CreatedAt)
        VALUES (N'Cúp điện bảo trì', N'Hệ thống điện sẽ bảo trì, khu vực có thể mất điện từ 14h-16h ngày 20/04/2026', N'Maintenance', N'High', N'Published', GETDATE());
        SELECT @NotifID3 = SCOPE_IDENTITY();
    END

    -- =====================================================
    -- 12. INSERT SYSTEM CONFIGURATION
    -- =====================================================
    PRINT 'Inserting system configuration...';

    IF NOT EXISTS (SELECT 1 FROM SystemConfig WHERE ConfigKey = 'IsSeeded')
        INSERT INTO SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES ('IsSeeded', 'true', 'Dữ liệu mẫu đã được khởi tạo', GETDATE(), @SuperAdminUserID);

    IF NOT EXISTS (SELECT 1 FROM SystemConfig WHERE ConfigKey = 'ApartmentNameFormat')
        INSERT INTO SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES ('ApartmentNameFormat', 'Block {block} - Tầng {floor} - Căn {code}', 'Định dạng tên căn hộ', GETDATE(), @SuperAdminUserID);

    IF NOT EXISTS (SELECT 1 FROM SystemConfig WHERE ConfigKey = 'MaxLoginAttempts')
        INSERT INTO SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES ('MaxLoginAttempts', '5', 'Số lần đăng nhập sai tối đa trước khi khóa', GETDATE(), @SuperAdminUserID);

    IF NOT EXISTS (SELECT 1 FROM SystemConfig WHERE ConfigKey = 'LockDurationMinutes')
        INSERT INTO SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES ('LockDurationMinutes', '15', 'Thời gian khóa tài khoản (phút)', GETDATE(), @SuperAdminUserID);

    IF NOT EXISTS (SELECT 1 FROM SystemConfig WHERE ConfigKey = 'AppVersion')
        INSERT INTO SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES ('AppVersion', '1.0.0', 'Phiên bản ứng dụng', GETDATE(), @SuperAdminUserID);

    -- =====================================================
    -- 13. INSERT AUDIT LOG FOR SEED DATA
    -- =====================================================
    PRINT 'Logging seed data initialization...';

    INSERT INTO AuditLogs (UserID, Action, TableName, Description, CreatedAt)
    VALUES (@SuperAdminUserID, 'SEED_DATA_INIT', 'Multiple', 'Initial seed data created', GETDATE());

    -- =====================================================
    -- COMMIT TRANSACTION
    -- =====================================================
    COMMIT TRANSACTION;
    PRINT '========== SEED DATA INSERTION COMPLETED SUCCESSFULLY ==========';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR DURING SEED DATA INSERTION:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH

GO

-- =====================================================
-- VERIFICATION SCRIPT
-- =====================================================
PRINT '';
PRINT '========== VERIFICATION ==========';

SELECT 'Users Created:' AS [Info];
SELECT UserID, Username, FullName, Email, RoleName, Status 
FROM Users u
INNER JOIN Roles r ON u.RoleID = r.RoleID
ORDER BY UserID;

PRINT '';
SELECT 'Buildings and Apartments:' AS [Info];
SELECT COUNT(*) AS TotalApartments FROM Apartments;
SELECT DISTINCT BuildingName FROM Buildings;

PRINT '';
SELECT 'Fee Types:' AS [Info];
SELECT FeeName, Amount, Unit FROM FeeTypes;

PRINT '';
SELECT 'Sample Invoices:' AS [Info];
SELECT InvoiceID, ApartmentID, InvoiceMonth, InvoiceYear, TotalAmount, Status FROM Invoices;

PRINT '';
SELECT 'Configuration:' AS [Info];
SELECT ConfigKey, ConfigValue FROM SystemConfig;

PRINT '';
PRINT '========== SEED DATA VERIFICATION COMPLETED ==========';
