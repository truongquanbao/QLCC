-- =====================================================
-- APARTMENT MANAGER - SEED DATA
-- Initial data for testing and development
-- =====================================================

USE ApartmentManagerDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IsSeeded BIT = 0;
    SELECT @IsSeeded =
        CASE
            WHEN EXISTS (
                SELECT 1
                FROM dbo.SystemConfig
                WHERE ConfigKey = N'IsSeeded'
                  AND TRY_CONVERT(BIT, ConfigValue) = 1
            ) THEN 1
            ELSE 0
        END;

    IF @IsSeeded = 1
    BEGIN
        PRINT N'Database is already seeded. Skipping...';
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    DECLARE @Now DATETIME2(0) = GETDATE();
    DECLARE @HostName NVARCHAR(128) = HOST_NAME();
    DECLARE @SuperAdminRoleID INT;
    DECLARE @ManagerRoleID INT;
    DECLARE @ResidentRoleID INT;
    DECLARE @SuperAdminUserID INT;
    DECLARE @ManagerUserID INT;
    DECLARE @Resident1UserID INT;
    DECLARE @Resident2UserID INT;
    DECLARE @Resident1ID INT;
    DECLARE @ApartmentA101ID INT;
    DECLARE @BuildingID INT;
    DECLARE @BlockAID INT;
    DECLARE @BlockBID INT;

    PRINT N'Inserting roles...';
    DECLARE @RoleSeed TABLE
    (
        RoleName NVARCHAR(50) NOT NULL,
        Description NVARCHAR(255) NULL
    );

    INSERT INTO @RoleSeed (RoleName, Description)
    VALUES
        (N'Super Admin', N'Quản trị viên cao nhất của hệ thống'),
        (N'Manager', N'Quản lý khu chung cư'),
        (N'Resident', N'Cư dân chung cư');

    INSERT INTO dbo.Roles (RoleName, Description)
    SELECT s.RoleName, s.Description
    FROM @RoleSeed s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Roles r WHERE r.RoleName = s.RoleName
    );

    SELECT @SuperAdminRoleID = RoleID FROM dbo.Roles WHERE RoleName = N'Super Admin';
    SELECT @ManagerRoleID = RoleID FROM dbo.Roles WHERE RoleName = N'Manager';
    SELECT @ResidentRoleID = RoleID FROM dbo.Roles WHERE RoleName = N'Resident';

    PRINT N'Inserting permissions...';
    DECLARE @PermissionSeed TABLE
    (
        PermissionName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255) NULL
    );

    INSERT INTO @PermissionSeed (PermissionName, Description)
    VALUES
        (N'UserManagement', N'Quản lý tài khoản người dùng'),
        (N'ManageApartments', N'Quản lý căn hộ'),
        (N'ManageResidents', N'Quản lý cư dân'),
        (N'ManageContracts', N'Quản lý hợp đồng'),
        (N'ManageInvoices', N'Quản lý hóa đơn'),
        (N'ManageComplaints', N'Quản lý phản ánh'),
        (N'ManageNotifications', N'Quản lý thông báo'),
        (N'ManageVehicles', N'Quản lý phương tiện'),
        (N'ManageVisitors', N'Quản lý khách'),
        (N'ManageFeeTypes', N'Quản lý loại phí'),
        (N'ReportGeneration', N'Tạo báo cáo'),
        (N'SystemConfiguration', N'Cấu hình hệ thống');

    INSERT INTO dbo.Permissions (PermissionName, Description)
    SELECT s.PermissionName, s.Description
    FROM @PermissionSeed s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Permissions p WHERE p.PermissionName = s.PermissionName
    );

    DECLARE @PermUserMgmt INT;
    DECLARE @PermAptMgmt INT;
    DECLARE @PermResMgmt INT;
    DECLARE @PermContractMgmt INT;
    DECLARE @PermInvoiceMgmt INT;
    DECLARE @PermComplaintMgmt INT;
    DECLARE @PermNotifMgmt INT;
    DECLARE @PermVehicleMgmt INT;
    DECLARE @PermVisitorMgmt INT;
    DECLARE @PermFeeTypeMgmt INT;
    DECLARE @PermReport INT;
    DECLARE @PermSysCfg INT;

    SELECT @PermUserMgmt = PermissionID FROM dbo.Permissions WHERE PermissionName = N'UserManagement';
    SELECT @PermAptMgmt = PermissionID FROM dbo.Permissions WHERE PermissionName = N'ManageApartments';
    SELECT @PermResMgmt = PermissionID FROM dbo.Permissions WHERE PermissionName = N'ManageResidents';
    SELECT @PermContractMgmt = PermissionID FROM dbo.Permissions WHERE PermissionName = N'ManageContracts';
    SELECT @PermInvoiceMgmt = PermissionID FROM dbo.Permissions WHERE PermissionName = N'ManageInvoices';
    SELECT @PermComplaintMgmt = PermissionID FROM dbo.Permissions WHERE PermissionName = N'ManageComplaints';
    SELECT @PermNotifMgmt = PermissionID FROM dbo.Permissions WHERE PermissionName = N'ManageNotifications';
    SELECT @PermVehicleMgmt = PermissionID FROM dbo.Permissions WHERE PermissionName = N'ManageVehicles';
    SELECT @PermVisitorMgmt = PermissionID FROM dbo.Permissions WHERE PermissionName = N'ManageVisitors';
    SELECT @PermFeeTypeMgmt = PermissionID FROM dbo.Permissions WHERE PermissionName = N'ManageFeeTypes';
    SELECT @PermReport = PermissionID FROM dbo.Permissions WHERE PermissionName = N'ReportGeneration';
    SELECT @PermSysCfg = PermissionID FROM dbo.Permissions WHERE PermissionName = N'SystemConfiguration';

    PRINT N'Assigning permissions...';
    DECLARE @RolePermissionSeed TABLE
    (
        RoleName NVARCHAR(50) NOT NULL,
        PermissionName NVARCHAR(100) NOT NULL
    );

    INSERT INTO @RolePermissionSeed (RoleName, PermissionName)
    VALUES
        (N'Super Admin', N'UserManagement'),
        (N'Super Admin', N'ManageApartments'),
        (N'Super Admin', N'ManageResidents'),
        (N'Super Admin', N'ManageContracts'),
        (N'Super Admin', N'ManageInvoices'),
        (N'Super Admin', N'ManageComplaints'),
        (N'Super Admin', N'ManageNotifications'),
        (N'Super Admin', N'ManageVehicles'),
        (N'Super Admin', N'ManageVisitors'),
        (N'Super Admin', N'ManageFeeTypes'),
        (N'Super Admin', N'ReportGeneration'),
        (N'Super Admin', N'SystemConfiguration'),
        (N'Manager', N'ManageApartments'),
        (N'Manager', N'ManageResidents'),
        (N'Manager', N'ManageContracts'),
        (N'Manager', N'ManageInvoices'),
        (N'Manager', N'ManageComplaints'),
        (N'Manager', N'ManageNotifications'),
        (N'Manager', N'ManageVehicles'),
        (N'Manager', N'ManageVisitors'),
        (N'Manager', N'ManageFeeTypes'),
        (N'Manager', N'ReportGeneration'),
        (N'Resident', N'ManageInvoices'),
        (N'Resident', N'ManageComplaints'),
        (N'Resident', N'ManageNotifications');

    INSERT INTO dbo.RolePermissions (RoleID, PermissionID)
    SELECT r.RoleID, p.PermissionID
    FROM @RolePermissionSeed s
    INNER JOIN dbo.Roles r ON r.RoleName = s.RoleName
    INNER JOIN dbo.Permissions p ON p.PermissionName = s.PermissionName
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.RolePermissions rp
        WHERE rp.RoleID = r.RoleID
          AND rp.PermissionID = p.PermissionID
    );

    PRINT N'Inserting user accounts...';
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'superadmin')
    BEGIN
        INSERT INTO dbo.Users (Username, PasswordHash, FullName, Email, Phone, RoleID, Status, IsApproved, ApprovedAt, ApprovedBy, CreatedAt)
        VALUES
        (
            N'superadmin',
            N'$2a$11$MMo5UgTCNVe8I46ISQIgn.cKjGDjy23cRcIL9o4fSnJauhGUqcWBG',
            N'Super Administrator',
            N'superadmin@system.local',
            N'0900000001',
            @SuperAdminRoleID,
            N'Active',
            1,
            @Now,
            NULL,
            @Now
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'manager1')
    BEGIN
        INSERT INTO dbo.Users (Username, PasswordHash, FullName, Email, Phone, RoleID, Status, IsApproved, ApprovedAt, ApprovedBy, CreatedAt)
        VALUES
        (
            N'manager1',
            N'$2a$11$SJFoJ.3L2GmMUWAIhHy16Of3TdXNYA5e7fLFtEwRKhYDcGTCJkoGu',
            N'Manager One',
            N'manager1@system.local',
            N'0900000002',
            @ManagerRoleID,
            N'Active',
            1,
            @Now,
            NULL,
            @Now
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'resident1')
    BEGIN
        INSERT INTO dbo.Users (Username, PasswordHash, FullName, Email, Phone, RoleID, Status, IsApproved, ApprovedAt, ApprovedBy, CreatedAt)
        VALUES
        (
            N'resident1',
            N'$2a$11$MzoZYHX9e5Gqk4XzJt82xugDRXYfoPMHaEzR985kt4OkmAqwHlW6y',
            N'Resident One',
            N'resident1@system.local',
            N'0900000003',
            @ResidentRoleID,
            N'Active',
            1,
            @Now,
            NULL,
            @Now
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'resident2')
    BEGIN
        INSERT INTO dbo.Users (Username, PasswordHash, FullName, Email, Phone, RoleID, Status, IsApproved, CreatedAt)
        VALUES
        (
            N'resident2',
            N'$2a$11$MzoZYHX9e5Gqk4XzJt82xugDRXYfoPMHaEzR985kt4OkmAqwHlW6y',
            N'Resident Two',
            N'resident2@system.local',
            N'0900000004',
            @ResidentRoleID,
            N'Pending',
            0,
            @Now
        );
    END;

    SELECT @SuperAdminUserID = UserID FROM dbo.Users WHERE Username = N'superadmin';
    SELECT @ManagerUserID = UserID FROM dbo.Users WHERE Username = N'manager1';
    SELECT @Resident1UserID = UserID FROM dbo.Users WHERE Username = N'resident1';
    SELECT @Resident2UserID = UserID FROM dbo.Users WHERE Username = N'resident2';

    UPDATE dbo.Users
    SET ApprovedBy = @SuperAdminUserID,
        ApprovedAt = @Now,
        Status = N'Active',
        IsApproved = 1,
        UpdatedAt = GETDATE()
    WHERE UserID IN (@ManagerUserID, @Resident1UserID);

    PRINT N'Creating building hierarchy...';
    IF NOT EXISTS (SELECT 1 FROM dbo.Buildings WHERE BuildingName = N'Tòa Nhà A')
    BEGIN
        INSERT INTO dbo.Buildings (BuildingName, Address, Description)
        VALUES (N'Tòa Nhà A', N'123 Đường Nguyễn Huệ, Quận 1, TP.HCM', N'Tòa nhà mẫu cho dữ liệu thử nghiệm');
    END;

    SELECT @BuildingID = BuildingID FROM dbo.Buildings WHERE BuildingName = N'Tòa Nhà A';

    IF NOT EXISTS (SELECT 1 FROM dbo.Blocks WHERE BlockName = N'Block A' AND BuildingID = @BuildingID)
    BEGIN
        INSERT INTO dbo.Blocks (BlockName, BuildingID)
        VALUES (N'Block A', @BuildingID);
    END;
    SELECT @BlockAID = BlockID FROM dbo.Blocks WHERE BlockName = N'Block A' AND BuildingID = @BuildingID;

    IF NOT EXISTS (SELECT 1 FROM dbo.Blocks WHERE BlockName = N'Block B' AND BuildingID = @BuildingID)
    BEGIN
        INSERT INTO dbo.Blocks (BlockName, BuildingID)
        VALUES (N'Block B', @BuildingID);
    END;
    SELECT @BlockBID = BlockID FROM dbo.Blocks WHERE BlockName = N'Block B' AND BuildingID = @BuildingID;

    DECLARE @FloorMap TABLE
    (
        BlockPrefix CHAR(1) NOT NULL,
        FloorNumber INT NOT NULL,
        FloorID INT NOT NULL,
        PRIMARY KEY (BlockPrefix, FloorNumber)
    );

    DECLARE @FloorNumber INT = 1;
    DECLARE @FloorID INT;

    WHILE @FloorNumber <= 5
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Floors WHERE BlockID = @BlockAID AND FloorNumber = @FloorNumber)
        BEGIN
            INSERT INTO dbo.Floors (FloorNumber, BlockID)
            VALUES (@FloorNumber, @BlockAID);
        END;
        SELECT @FloorID = FloorID FROM dbo.Floors WHERE BlockID = @BlockAID AND FloorNumber = @FloorNumber;
        IF NOT EXISTS (SELECT 1 FROM @FloorMap WHERE BlockPrefix = 'A' AND FloorNumber = @FloorNumber)
            INSERT INTO @FloorMap (BlockPrefix, FloorNumber, FloorID) VALUES ('A', @FloorNumber, @FloorID);

        IF NOT EXISTS (SELECT 1 FROM dbo.Floors WHERE BlockID = @BlockBID AND FloorNumber = @FloorNumber)
        BEGIN
            INSERT INTO dbo.Floors (FloorNumber, BlockID)
            VALUES (@FloorNumber, @BlockBID);
        END;
        SELECT @FloorID = FloorID FROM dbo.Floors WHERE BlockID = @BlockBID AND FloorNumber = @FloorNumber;
        IF NOT EXISTS (SELECT 1 FROM @FloorMap WHERE BlockPrefix = 'B' AND FloorNumber = @FloorNumber)
            INSERT INTO @FloorMap (BlockPrefix, FloorNumber, FloorID) VALUES ('B', @FloorNumber, @FloorID);

        SET @FloorNumber += 1;
    END;

    DECLARE @BlockPrefix CHAR(1);
    DECLARE @UnitNumber INT;
    DECLARE @ApartmentCode NVARCHAR(20);
    DECLARE @ApartmentArea DECIMAL(10,2);
    DECLARE @ApartmentType NVARCHAR(100);
    DECLARE @ApartmentStatus NVARCHAR(20);
    DECLARE @ApartmentNote NVARCHAR(MAX);
    DECLARE @ApartmentMaxResidents INT;
    DECLARE floor_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT BlockPrefix, FloorNumber, FloorID
        FROM @FloorMap
        ORDER BY BlockPrefix, FloorNumber;

    OPEN floor_cursor;
    FETCH NEXT FROM floor_cursor INTO @BlockPrefix, @FloorNumber, @FloorID;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @UnitNumber = 1;
        WHILE @UnitNumber <= 4
        BEGIN
            IF @UnitNumber = 1
            BEGIN
                SET @ApartmentArea = 45.00;
                SET @ApartmentType = N'1 Phòng Ngủ';
                SET @ApartmentMaxResidents = 2;
            END
            ELSE IF @UnitNumber = 2
            BEGIN
                SET @ApartmentArea = 60.00;
                SET @ApartmentType = N'2 Phòng Ngủ';
                SET @ApartmentMaxResidents = 4;
            END
            ELSE IF @UnitNumber = 3
            BEGIN
                SET @ApartmentArea = 75.00;
                SET @ApartmentType = N'3 Phòng Ngủ';
                SET @ApartmentMaxResidents = 6;
            END
            ELSE
            BEGIN
                SET @ApartmentArea = 90.00;
                SET @ApartmentType = N'4 Phòng Ngủ';
                SET @ApartmentMaxResidents = 8;
            END;

            SET @ApartmentCode = CONCAT(@BlockPrefix, @FloorNumber, RIGHT(CONCAT('0', CAST(@UnitNumber AS VARCHAR(2))), 2));
            SET @ApartmentStatus = CASE
                WHEN @BlockPrefix = 'A' AND @FloorNumber = 1 AND @UnitNumber = 1 THEN N'Occupied'
                ELSE N'Empty'
            END;
            SET @ApartmentNote = CASE
                WHEN @BlockPrefix = 'A' AND @FloorNumber = 1 AND @UnitNumber = 1 THEN N'Căn hộ mẫu có cư dân ở'
                ELSE N''
            END;

            IF NOT EXISTS (SELECT 1 FROM dbo.Apartments WHERE ApartmentCode = @ApartmentCode)
            BEGIN
                INSERT INTO dbo.Apartments (ApartmentCode, FloorID, Area, ApartmentType, Status, MaxResidents, Note)
                VALUES (@ApartmentCode, @FloorID, @ApartmentArea, @ApartmentType, @ApartmentStatus, @ApartmentMaxResidents, @ApartmentNote);
            END;

            SET @UnitNumber += 1;
        END;

        FETCH NEXT FROM floor_cursor INTO @BlockPrefix, @FloorNumber, @FloorID;
    END;

    CLOSE floor_cursor;
    DEALLOCATE floor_cursor;

    SELECT @ApartmentA101ID = ApartmentID FROM dbo.Apartments WHERE ApartmentCode = N'A101';

    PRINT N'Inserting fee types...';
    DECLARE @FeeSeed TABLE
    (
        FeeTypeName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL,
        UnitOfMeasurement NVARCHAR(50) NOT NULL
    );

    INSERT INTO @FeeSeed (FeeTypeName, Description, UnitOfMeasurement)
    VALUES
        (N'Phí Quản Lý', N'Phí quản lý khu chung cư hàng tháng', N'VND'),
        (N'Phí Gửi Xe', N'Phí gửi xe hàng tháng', N'VND'),
        (N'Phí Vệ Sinh', N'Phí vệ sinh khu chung cư hàng tháng', N'VND');

    INSERT INTO dbo.FeeTypes (FeeTypeName, Description, UnitOfMeasurement)
    SELECT s.FeeTypeName, s.Description, s.UnitOfMeasurement
    FROM @FeeSeed s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.FeeTypes f WHERE f.FeeTypeName = s.FeeTypeName
    );

    DECLARE @FeeTypeManagementID INT;
    DECLARE @FeeTypeParkingID INT;
    DECLARE @FeeTypeCleaningID INT;

    SELECT @FeeTypeManagementID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Quản Lý';
    SELECT @FeeTypeParkingID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Gửi Xe';
    SELECT @FeeTypeCleaningID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Vệ Sinh';

    PRINT N'Creating resident profile...';
    SELECT @Resident1ID = ResidentID FROM dbo.Residents WHERE UserID = @Resident1UserID;

    IF @Resident1ID IS NULL
    BEGIN
        INSERT INTO dbo.Residents
        (
            UserID, FullName, Phone, Email, CCCD, DOB, Gender, AddressRegistration,
            ApartmentID, RelationshipWithOwner, Status, ResidentStatus, MoveInDate, StartDate, Note
        )
        VALUES
        (
            @Resident1UserID,
            N'Resident One',
            N'0900000003',
            N'resident1@system.local',
            N'123456789012',
            '1990-05-15',
            N'Nam',
            N'123 Đường Nguyễn Huệ, Quận 1, TP.HCM',
            @ApartmentA101ID,
            N'Chủ hộ',
            N'Active',
            N'Đang ở',
            @Now,
            @Now,
            N'Cư dân mẫu'
        );

        SELECT @Resident1ID = SCOPE_IDENTITY();
    END;

    IF @ApartmentA101ID IS NOT NULL
    BEGIN
        UPDATE dbo.Apartments
        SET Status = N'Occupied',
            Note = CASE
                WHEN ISNULL(Note, N'') = N'' THEN N'Căn hộ mẫu có cư dân ở'
                ELSE Note
            END,
            UpdatedAt = GETDATE()
        WHERE ApartmentID = @ApartmentA101ID;
    END;

    PRINT N'Inserting contracts...';
    IF NOT EXISTS (SELECT 1 FROM dbo.Contracts WHERE ApartmentID = @ApartmentA101ID AND ResidentID = @Resident1ID)
    BEGIN
        INSERT INTO dbo.Contracts
        (
            ApartmentID, ResidentID, StartDate, EndDate, RentAmount, DepositAmount, Status, Note
        )
        VALUES
        (
            @ApartmentA101ID,
            @Resident1ID,
            CONVERT(DATE, DATEADD(MONTH, -1, @Now)),
            CONVERT(DATE, DATEADD(MONTH, 11, @Now)),
            8500000,
            17000000,
            N'Active',
            N'ContractType:Lease;AutoRenewal:False|Hợp đồng mẫu căn hộ A101'
        );
    END;

    PRINT N'Inserting sample invoices...';
    DECLARE @Invoice1ID INT;
    DECLARE @Invoice2ID INT;
    DECLARE @Invoice3ID INT;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Invoices
        WHERE ApartmentID = @ApartmentA101ID
          AND [Month] = MONTH(GETDATE())
          AND [Year] = YEAR(GETDATE())
    )
    BEGIN
        INSERT INTO dbo.Invoices (ApartmentID, [Month], [Year], DueDate, PaymentStatus, TotalAmount, PaidAmount, Note)
        VALUES (
            @ApartmentA101ID,
            MONTH(GETDATE()),
            YEAR(GETDATE()),
            EOMONTH(GETDATE()),
            N'Paid',
            1000000,
            1000000,
            N'Hóa đơn tháng hiện tại'
        );
        SET @Invoice1ID = SCOPE_IDENTITY();

        INSERT INTO dbo.InvoiceDetails (InvoiceID, FeeTypeID, Amount)
        VALUES
            (@Invoice1ID, @FeeTypeManagementID, 500000),
            (@Invoice1ID, @FeeTypeParkingID, 300000),
            (@Invoice1ID, @FeeTypeCleaningID, 200000);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Invoices
        WHERE ApartmentID = @ApartmentA101ID
          AND [Month] = MONTH(DATEADD(MONTH, -1, GETDATE()))
          AND [Year] = YEAR(DATEADD(MONTH, -1, GETDATE()))
    )
    BEGIN
        INSERT INTO dbo.Invoices (ApartmentID, [Month], [Year], DueDate, PaymentStatus, TotalAmount, PaidAmount, Note)
        VALUES (
            @ApartmentA101ID,
            MONTH(DATEADD(MONTH, -1, GETDATE())),
            YEAR(DATEADD(MONTH, -1, GETDATE())),
            EOMONTH(DATEADD(MONTH, -1, GETDATE())),
            N'Unpaid',
            1000000,
            0,
            N'Hóa đơn quá hạn'
        );
        SET @Invoice2ID = SCOPE_IDENTITY();

        INSERT INTO dbo.InvoiceDetails (InvoiceID, FeeTypeID, Amount)
        VALUES
            (@Invoice2ID, @FeeTypeManagementID, 500000),
            (@Invoice2ID, @FeeTypeParkingID, 300000),
            (@Invoice2ID, @FeeTypeCleaningID, 200000);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Invoices
        WHERE ApartmentID = @ApartmentA101ID
          AND [Month] = MONTH(DATEADD(MONTH, -2, GETDATE()))
          AND [Year] = YEAR(DATEADD(MONTH, -2, GETDATE()))
    )
    BEGIN
        INSERT INTO dbo.Invoices (ApartmentID, [Month], [Year], DueDate, PaymentStatus, TotalAmount, PaidAmount, Note)
        VALUES (
            @ApartmentA101ID,
            MONTH(DATEADD(MONTH, -2, GETDATE())),
            YEAR(DATEADD(MONTH, -2, GETDATE())),
            EOMONTH(DATEADD(MONTH, -2, GETDATE())),
            N'Partial',
            1000000,
            300000,
            N'Hóa đơn thanh toán một phần'
        );
        SET @Invoice3ID = SCOPE_IDENTITY();

        INSERT INTO dbo.InvoiceDetails (InvoiceID, FeeTypeID, Amount)
        VALUES
            (@Invoice3ID, @FeeTypeManagementID, 500000),
            (@Invoice3ID, @FeeTypeParkingID, 300000),
            (@Invoice3ID, @FeeTypeCleaningID, 200000);
    END;

    PRINT N'Inserting sample complaints...';
    IF NOT EXISTS (SELECT 1 FROM dbo.Complaints WHERE ResidentID = @Resident1ID AND Title = N'Thang máy chậm')
    BEGIN
        INSERT INTO dbo.Complaints
        (
            ResidentID, ApartmentID, Category, ComplaintType, Title, Description,
            Priority, Status, AssignedToUserID, ResolutionNotes, SatisfactionRating
        )
        VALUES
        (
            @Resident1ID,
            @ApartmentA101ID,
            N'Maintenance',
            N'Maintenance',
            N'Thang máy chậm',
            N'Thang máy Block A hoạt động chậm trong giờ cao điểm.',
            N'Medium',
            N'Open',
            NULL,
            NULL,
            NULL
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Complaints WHERE ResidentID = @Resident1ID AND Title = N'Tiếng ồn ban đêm')
    BEGIN
        INSERT INTO dbo.Complaints
        (
            ResidentID, ApartmentID, Category, ComplaintType, Title, Description,
            Priority, Status, AssignedToUserID, ResolutionNotes, SatisfactionRating
        )
        VALUES
        (
            @Resident1ID,
            @ApartmentA101ID,
            N'General',
            N'Noise',
            N'Tiếng ồn ban đêm',
            N'Có tiếng ồn lớn vào khung giờ nghỉ đêm.',
            N'Low',
            N'Resolved',
            @ManagerUserID,
            N'Đã nhắc nhở và xử lý xong.',
            4
        );
    END;

    PRINT N'Inserting sample notifications...';
    IF NOT EXISTS (SELECT 1 FROM dbo.Notifications WHERE Title = N'Bảo trì thang máy')
    BEGIN
        INSERT INTO dbo.Notifications
        (
            UserID, ResidentID, Title, Subject, Message, Body, NotificationType,
            Priority, IsRead, Status, SentDate, ReadAt
        )
        VALUES
        (
            @Resident1UserID,
            @Resident1ID,
            N'Bảo trì thang máy',
            N'Bảo trì thang máy',
            N'Thang máy Block A sẽ bảo trì từ 8h đến 12h hôm nay.',
            N'Thang máy Block A sẽ bảo trì từ 8h đến 12h hôm nay.',
            N'Maintenance',
            N'High',
            0,
            N'Sent',
            @Now,
            NULL
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Notifications WHERE Title = N'Thanh toán phí tháng này')
    BEGIN
        INSERT INTO dbo.Notifications
        (
            UserID, ResidentID, Title, Subject, Message, Body, NotificationType,
            Priority, IsRead, Status, SentDate, ReadAt
        )
        VALUES
        (
            @Resident1UserID,
            @Resident1ID,
            N'Thanh toán phí tháng này',
            N'Thanh toán phí tháng này',
            N'Vui lòng thanh toán phí quản lý, gửi xe và vệ sinh trước ngày 25.',
            N'Vui lòng thanh toán phí quản lý, gửi xe và vệ sinh trước ngày 25.',
            N'Payment',
            N'Medium',
            0,
            N'Draft',
            NULL,
            NULL
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Notifications WHERE Title = N'Cảnh báo an ninh')
    BEGIN
        INSERT INTO dbo.Notifications
        (
            UserID, ResidentID, Title, Subject, Message, Body, NotificationType,
            Priority, IsRead, Status, SentDate, ReadAt
        )
        VALUES
        (
            @ManagerUserID,
            NULL,
            N'Cảnh báo an ninh',
            N'Cảnh báo an ninh',
            N'Hệ thống ghi nhận một cảnh báo an ninh cần kiểm tra.',
            N'Hệ thống ghi nhận một cảnh báo an ninh cần kiểm tra.',
            N'Warning',
            N'High',
            0,
            N'Failed',
            NULL,
            NULL
        );
    END;

    PRINT N'Inserting sample visitors...';
    IF NOT EXISTS (SELECT 1 FROM dbo.Visitors WHERE ResidentID = @Resident1ID AND VisitorName = N'Nguyễn Văn A')
    BEGIN
        INSERT INTO dbo.Visitors
        (
            ResidentID, VisitorName, Phone, Email, IDNumber, Purpose,
            ArrivalTime, DepartureTime, Status, ApprovedByUserID, Note
        )
        VALUES
        (
            @Resident1ID,
            N'Nguyễn Văn A',
            N'0911111111',
            N'guest1@example.com',
            N'',
            N'Giao hàng bưu phẩm',
            DATEADD(HOUR, -2, @Now),
            NULL,
            N'Pending',
            NULL,
            N'Delivery'
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Visitors WHERE ResidentID = @Resident1ID AND VisitorName = N'Trần Thị B')
    BEGIN
        INSERT INTO dbo.Visitors
        (
            ResidentID, VisitorName, Phone, Email, IDNumber, Purpose,
            ArrivalTime, DepartureTime, Status, ApprovedByUserID, Note
        )
        VALUES
        (
            @Resident1ID,
            N'Trần Thị B',
            N'0922222222',
            N'family@example.com',
            N'',
            N'Thăm người thân',
            DATEADD(HOUR, -4, @Now),
            DATEADD(HOUR, -1, @Now),
            N'Approved',
            @ManagerUserID,
            N'Family'
        );
    END;

    PRINT N'Inserting sample vehicles...';
    IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE LicensePlate = N'59X1-123.45')
    BEGIN
        INSERT INTO dbo.Vehicles
        (
            ResidentID, VehicleType, LicensePlate, Color, Brand, Status, Note
        )
        VALUES
        (
            @Resident1ID,
            N'Motorcycle',
            N'59X1-123.45',
            N'Đỏ',
            N'Honda',
            N'Active',
            N'MODEL=Wave Alpha;YEAR=2023;NOTE=Xe máy cá nhân'
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE LicensePlate = N'51A-999.99')
    BEGIN
        INSERT INTO dbo.Vehicles
        (
            ResidentID, VehicleType, LicensePlate, Color, Brand, Status, Note
        )
        VALUES
        (
            @Resident1ID,
            N'Car',
            N'51A-999.99',
            N'Trắng',
            N'Toyota',
            N'Inactive',
            N'MODEL=Vios;YEAR=2021;NOTE=Xe ô tô gia đình'
        );
    END;

    PRINT N'Inserting system configuration...';
    IF NOT EXISTS (SELECT 1 FROM dbo.SystemConfig WHERE ConfigKey = N'IsSeeded')
    BEGIN
        INSERT INTO dbo.SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES (N'IsSeeded', N'1', N'Dữ liệu mẫu đã được khởi tạo', @Now, @SuperAdminUserID);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.SystemConfig WHERE ConfigKey = N'ApartmentNameFormat')
    BEGIN
        INSERT INTO dbo.SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES (N'ApartmentNameFormat', N'Block {block} - Floor {floor} - Apt {code}', N'Định dạng tên căn hộ', @Now, @SuperAdminUserID);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.SystemConfig WHERE ConfigKey = N'MaxLoginAttempts')
    BEGIN
        INSERT INTO dbo.SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES (N'MaxLoginAttempts', N'5', N'Số lần đăng nhập sai tối đa trước khi khóa', @Now, @SuperAdminUserID);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.SystemConfig WHERE ConfigKey = N'LockDurationMinutes')
    BEGIN
        INSERT INTO dbo.SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES (N'LockDurationMinutes', N'15', N'Thời gian khóa tài khoản (phút)', @Now, @SuperAdminUserID);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.SystemConfig WHERE ConfigKey = N'AppVersion')
    BEGIN
        INSERT INTO dbo.SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES (N'AppVersion', N'1.0.0', N'Phiên bản ứng dụng', @Now, @SuperAdminUserID);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.SystemConfig WHERE ConfigKey = N'ThemeColor')
    BEGIN
        INSERT INTO dbo.SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES (N'ThemeColor', N'#215C9B', N'Màu giao diện mặc định', @Now, @SuperAdminUserID);
    END;

    PRINT N'Logging seed data initialization...';
    INSERT INTO dbo.AuditLogs
    (
        UserID, Action, EntityName, EntityID, OldValue, NewValue, [Timestamp], Description, IPAddress
    )
    VALUES
    (
        @SuperAdminUserID,
        N'SEED_DATA_INIT',
        N'Database',
        NULL,
        NULL,
        NULL,
        @Now,
        N'Initial seed data created',
        @HostName
    );

    COMMIT TRANSACTION;
    PRINT N'========== SEED DATA INSERTION COMPLETED SUCCESSFULLY =========='; 
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT N'ERROR DURING SEED DATA INSERTION:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH
GO
