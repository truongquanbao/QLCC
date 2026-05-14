USE ApartmentManagerDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2(0) = GETDATE();
    DECLARE @Today DATE = CAST(@Now AS DATE);
    DECLARE @HostName NVARCHAR(128) = HOST_NAME();
    DECLARE @DefaultResidentPassword NVARCHAR(255) = N'$2a$11$MzoZYHX9e5Gqk4XzJt82xugDRXYfoPMHaEzR985kt4OkmAqwHlW6y';
    DECLARE @SuperAdminRoleID INT;
    DECLARE @ManagerRoleID INT;
    DECLARE @ResidentRoleID INT;
    DECLARE @SuperAdminUserID INT;
    DECLARE @ManagerUserID INT;
    DECLARE @OperatingFundID INT;
    DECLARE @MaintenanceFundID INT;

    PRINT N'Resetting demo operational data...';

    SELECT @SuperAdminRoleID = RoleID FROM dbo.Roles WHERE RoleName = N'Super Admin';
    SELECT @ManagerRoleID = RoleID FROM dbo.Roles WHERE RoleName = N'Manager';
    SELECT @ResidentRoleID = RoleID FROM dbo.Roles WHERE RoleName = N'Resident';

    IF @SuperAdminRoleID IS NULL OR @ManagerRoleID IS NULL OR @ResidentRoleID IS NULL
    BEGIN
        THROW 51000, 'Required base roles are missing. Run 02_SeedData.sql first.', 1;
    END;

    SELECT @SuperAdminUserID = UserID FROM dbo.Users WHERE Username = N'superadmin';
    SELECT @ManagerUserID = UserID FROM dbo.Users WHERE Username = N'manager1';

    IF @SuperAdminUserID IS NULL OR @ManagerUserID IS NULL
    BEGIN
        THROW 51001, 'Required admin/manager accounts are missing. Run 02_SeedData.sql first.', 1;
    END;

    DELETE FROM dbo.PaymentVouchers;
    DELETE FROM dbo.Receipts;
    DELETE FROM dbo.FundTransactions;
    DELETE FROM dbo.Payments;
    DELETE FROM dbo.Expenses;
    DELETE FROM dbo.FinancialPeriods;
    DELETE FROM dbo.MaintenanceSchedules;
    DELETE FROM dbo.Assets;
    DELETE FROM dbo.Visitors;
    DELETE FROM dbo.Vehicles;
    DELETE FROM dbo.Notifications;
    DELETE FROM dbo.Complaints;
    DELETE FROM dbo.InvoiceDetails;
    DELETE FROM dbo.Invoices;
    DELETE FROM dbo.Contracts;
    DELETE FROM dbo.Residents;
    DELETE FROM dbo.AuditLogs;
    DELETE FROM dbo.Users WHERE RoleID = @ResidentRoleID;
    DELETE FROM dbo.Apartments;
    DELETE FROM dbo.Floors;
    DELETE FROM dbo.Blocks;
    DELETE FROM dbo.Buildings;
    DELETE FROM dbo.Vendors;

    DBCC CHECKIDENT (N'dbo.PaymentVouchers', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Receipts', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.FundTransactions', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Payments', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Expenses', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.FinancialPeriods', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.MaintenanceSchedules', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Assets', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Visitors', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Vehicles', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Notifications', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Complaints', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.InvoiceDetails', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Invoices', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Contracts', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Residents', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Apartments', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Floors', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Blocks', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Buildings', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Vendors', RESEED, 0) WITH NO_INFOMSGS;

    DECLARE @Numbers TABLE (N INT NOT NULL PRIMARY KEY);
    DECLARE @Number INT = 1;
    WHILE @Number <= 400
    BEGIN
        INSERT INTO @Numbers (N) VALUES (@Number);
        SET @Number += 1;
    END;

    PRINT N'Seeding funds, payment accounts, categories and fee types...';

    DECLARE @FundSeed TABLE
    (
        FundName NVARCHAR(150) NOT NULL PRIMARY KEY,
        Description NVARCHAR(500) NOT NULL,
        InitialBalance DECIMAL(18,2) NOT NULL
    );

    INSERT INTO @FundSeed (FundName, Description, InitialBalance)
    VALUES
        (N'Quỹ vận hành', N'Quỹ phục vụ thu chi vận hành thường xuyên', 250000000),
        (N'Quỹ bảo trì', N'Quỹ dành cho bảo trì tài sản và hạ tầng chung cư', 800000000),
        (N'Quỹ an ninh', N'Quỹ phục vụ bảo vệ, camera và kiểm soát ra vào', 120000000);

    UPDATE f
    SET Description = s.Description,
        InitialBalance = s.InitialBalance,
        IsActive = 1
    FROM dbo.Funds f
    INNER JOIN @FundSeed s ON s.FundName = f.FundName;

    INSERT INTO dbo.Funds (FundName, Description, InitialBalance, IsActive)
    SELECT s.FundName, s.Description, s.InitialBalance, 1
    FROM @FundSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Funds f WHERE f.FundName = s.FundName);

    SELECT @OperatingFundID = FundID FROM dbo.Funds WHERE FundName = N'Quỹ vận hành';
    SELECT @MaintenanceFundID = FundID FROM dbo.Funds WHERE FundName = N'Quỹ bảo trì';

    DECLARE @PaymentAccountSeed TABLE
    (
        AccountName NVARCHAR(150) NOT NULL PRIMARY KEY,
        AccountType NVARCHAR(50) NOT NULL,
        BankName NVARCHAR(150) NULL,
        AccountNumber NVARCHAR(100) NULL,
        AccountHolder NVARCHAR(150) NULL,
        QRImagePath NVARCHAR(500) NULL
    );

    INSERT INTO @PaymentAccountSeed (AccountName, AccountType, BankName, AccountNumber, AccountHolder, QRImagePath)
    VALUES
        (N'Tiền mặt quầy thu', N'Cash', NULL, NULL, N'Ban quản lý chung cư', NULL),
        (N'Tài khoản ngân hàng vận hành', N'Bank', N'Vietcombank', N'0123456789', N'BAN QUAN LY CHUNG CU', NULL),
        (N'Tài khoản QR cư dân', N'QR', N'Vietcombank', N'0123456789', N'BAN QUAN LY CHUNG CU', N'Assets\qr_payment_test.jpg');

    UPDATE pa
    SET AccountType = s.AccountType,
        BankName = s.BankName,
        AccountNumber = s.AccountNumber,
        AccountHolder = s.AccountHolder,
        QRImagePath = s.QRImagePath,
        IsActive = 1
    FROM dbo.PaymentAccounts pa
    INNER JOIN @PaymentAccountSeed s ON s.AccountName = pa.AccountName;

    INSERT INTO dbo.PaymentAccounts (AccountName, AccountType, BankName, AccountNumber, AccountHolder, QRImagePath, IsActive)
    SELECT s.AccountName, s.AccountType, s.BankName, s.AccountNumber, s.AccountHolder, s.QRImagePath, 1
    FROM @PaymentAccountSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PaymentAccounts pa WHERE pa.AccountName = s.AccountName);

    DECLARE @ExpenseCategorySeed TABLE
    (
        CategoryName NVARCHAR(150) NOT NULL PRIMARY KEY,
        Description NVARCHAR(500) NOT NULL
    );

    INSERT INTO @ExpenseCategorySeed (CategoryName, Description)
    VALUES
        (N'Lương bảo vệ', N'Chi trả lương đội bảo vệ'),
        (N'Lương vệ sinh', N'Chi trả lương nhân viên vệ sinh'),
        (N'Sửa chữa bảo trì', N'Chi phí sửa chữa và bảo trì tài sản chung'),
        (N'Điện khu chung', N'Điện sử dụng cho hành lang, hầm xe, thang máy'),
        (N'Nước khu chung', N'Nước dùng cho cảnh quan và vệ sinh khu chung'),
        (N'Văn phòng phẩm', N'Chi phí vận hành văn phòng ban quản lý'),
        (N'Dịch vụ bên ngoài', N'Chi cho nhà thầu và đơn vị dịch vụ thuê ngoài');

    UPDATE ec
    SET Description = s.Description,
        IsActive = 1
    FROM dbo.ExpenseCategories ec
    INNER JOIN @ExpenseCategorySeed s ON s.CategoryName = ec.CategoryName;

    INSERT INTO dbo.ExpenseCategories (CategoryName, Description, IsActive)
    SELECT s.CategoryName, s.Description, 1
    FROM @ExpenseCategorySeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ExpenseCategories ec WHERE ec.CategoryName = s.CategoryName);

    DECLARE @FeeSeed TABLE
    (
        FeeTypeName NVARCHAR(100) NOT NULL PRIMARY KEY,
        Description NVARCHAR(MAX) NOT NULL,
        UnitOfMeasurement NVARCHAR(50) NOT NULL,
        CalculationType NVARCHAR(50) NOT NULL,
        FundID INT NOT NULL
    );

    INSERT INTO @FeeSeed (FeeTypeName, Description, UnitOfMeasurement, CalculationType, FundID)
    VALUES
        (N'Phí Quản Lý', N'Phí quản lý tính theo diện tích căn hộ', N'm2', N'PerArea', @OperatingFundID),
        (N'Phí Gửi Xe', N'Phí gửi xe theo phương tiện đã đăng ký', N'xe', N'PerVehicle', @OperatingFundID),
        (N'Phí Vệ Sinh', N'Phí vệ sinh khu chung theo căn hộ', N'căn hộ', N'Fixed', @OperatingFundID),
        (N'Phí Điện', N'Tiền điện sinh hoạt theo định mức demo', N'kWh', N'Manual', @OperatingFundID),
        (N'Phí Nước', N'Tiền nước sinh hoạt theo định mức demo', N'm3', N'Manual', @OperatingFundID),
        (N'Phí Internet', N'Gói internet nội khu theo tháng', N'tháng', N'Fixed', @OperatingFundID),
        (N'Phí Bảo Trì', N'Phí bảo trì hạ tầng và thiết bị chung', N'căn hộ', N'Fixed', @MaintenanceFundID);

    UPDATE ft
    SET Description = s.Description,
        UnitOfMeasurement = s.UnitOfMeasurement,
        Status = N'Active',
        CalculationType = s.CalculationType,
        FundID = s.FundID,
        IsActive = 1,
        UpdatedAt = @Now
    FROM dbo.FeeTypes ft
    INNER JOIN @FeeSeed s ON s.FeeTypeName = ft.FeeTypeName;

    INSERT INTO dbo.FeeTypes
        (FeeTypeName, Description, UnitOfMeasurement, Status, CalculationType, FundID, IsActive, CreatedAt, UpdatedAt)
    SELECT s.FeeTypeName, s.Description, s.UnitOfMeasurement, N'Active', s.CalculationType, s.FundID, 1, @Now, @Now
    FROM @FeeSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.FeeTypes ft WHERE ft.FeeTypeName = s.FeeTypeName);

    PRINT N'Creating 3-building apartment hierarchy...';

    DECLARE @BuildingSeed TABLE
    (
        BuildingName NVARCHAR(100) NOT NULL PRIMARY KEY,
        Address NVARCHAR(255) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL
    );

    INSERT INTO @BuildingSeed (BuildingName, Address, Description)
    VALUES
        (N'Tòa A', N'01 Nguyễn Hữu Cảnh, Bình Thạnh, TP.HCM', N'Tòa A - khu căn hộ gia đình, kết nối trực tiếp sảnh chính'),
        (N'Tòa B', N'03 Nguyễn Hữu Cảnh, Bình Thạnh, TP.HCM', N'Tòa B - khu căn hộ hỗn hợp chủ sở hữu và khách thuê'),
        (N'Tòa C', N'05 Nguyễn Hữu Cảnh, Bình Thạnh, TP.HCM', N'Tòa C - khu dịch vụ, căn hộ trẻ và chuyên gia');

    INSERT INTO dbo.Buildings (BuildingName, Address, Description, CreatedAt, UpdatedAt)
    SELECT BuildingName, Address, Description, @Now, @Now
    FROM @BuildingSeed;

    DECLARE @BlockSeed TABLE
    (
        BuildingName NVARCHAR(100) NOT NULL,
        BlockName NVARCHAR(100) NOT NULL,
        CodePrefix NVARCHAR(4) NOT NULL,
        MaxFloor INT NOT NULL,
        UnitsPerFloor INT NOT NULL,
        PRIMARY KEY (BuildingName, BlockName)
    );

    INSERT INTO @BlockSeed (BuildingName, BlockName, CodePrefix, MaxFloor, UnitsPerFloor)
    VALUES
        (N'Tòa A', N'Block A', N'A', 10, 5),
        (N'Tòa A', N'Block B', N'B', 10, 5),
        (N'Tòa B', N'Block C', N'C', 10, 5),
        (N'Tòa B', N'Block D', N'D', 10, 5),
        (N'Tòa C', N'Block E', N'E', 10, 5),
        (N'Tòa C', N'Block F', N'F', 10, 5);

    INSERT INTO dbo.Blocks (BlockName, BuildingID, CreatedAt, UpdatedAt)
    SELECT bs.BlockName, b.BuildingID, @Now, @Now
    FROM @BlockSeed bs
    INNER JOIN dbo.Buildings b ON b.BuildingName = bs.BuildingName;

    DECLARE @SeedBuildingName NVARCHAR(100);
    DECLARE @SeedBlockName NVARCHAR(100);
    DECLARE @SeedPrefix NVARCHAR(4);
    DECLARE @SeedMaxFloor INT;
    DECLARE @SeedUnitsPerFloor INT;
    DECLARE @SeedBlockID INT;
    DECLARE @FloorNumber INT;
    DECLARE @FloorID INT;
    DECLARE @UnitNumber INT;
    DECLARE @ApartmentCode NVARCHAR(20);
    DECLARE @ApartmentArea DECIMAL(10,2);
    DECLARE @ApartmentType NVARCHAR(100);
    DECLARE @ApartmentMaxResidents INT;

    DECLARE block_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT BuildingName, BlockName, CodePrefix, MaxFloor, UnitsPerFloor
        FROM @BlockSeed
        ORDER BY CodePrefix;

    OPEN block_cursor;
    FETCH NEXT FROM block_cursor INTO @SeedBuildingName, @SeedBlockName, @SeedPrefix, @SeedMaxFloor, @SeedUnitsPerFloor;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @SeedBlockID = bl.BlockID
        FROM dbo.Blocks bl
        INNER JOIN dbo.Buildings b ON b.BuildingID = bl.BuildingID
        WHERE b.BuildingName = @SeedBuildingName
          AND bl.BlockName = @SeedBlockName;

        SET @FloorNumber = 1;
        WHILE @FloorNumber <= @SeedMaxFloor
        BEGIN
            INSERT INTO dbo.Floors (FloorNumber, BlockID, CreatedAt, UpdatedAt)
            VALUES (@FloorNumber, @SeedBlockID, @Now, @Now);

            SET @FloorID = SCOPE_IDENTITY();
            SET @UnitNumber = 1;

            WHILE @UnitNumber <= @SeedUnitsPerFloor
            BEGIN
                SET @ApartmentCode = CONCAT(
                    @SeedPrefix,
                    N'-',
                    RIGHT(CONCAT(N'0', CONVERT(NVARCHAR(2), @FloorNumber)), 2),
                    RIGHT(CONCAT(N'0', CONVERT(NVARCHAR(2), @UnitNumber)), 2)
                );

                SET @ApartmentType = CASE @UnitNumber
                    WHEN 1 THEN N'1 PN - 1 WC'
                    WHEN 2 THEN N'2 PN - 1 WC'
                    WHEN 3 THEN N'2 PN - 2 WC'
                    WHEN 4 THEN N'3 PN - 2 WC'
                    ELSE N'3 PN góc - 2 WC'
                END;

                SET @ApartmentArea = CASE @UnitNumber
                    WHEN 1 THEN 48.50
                    WHEN 2 THEN 65.20
                    WHEN 3 THEN 72.80
                    WHEN 4 THEN 88.60
                    ELSE 96.40
                END;

                SET @ApartmentMaxResidents = CASE @UnitNumber
                    WHEN 1 THEN 2
                    WHEN 2 THEN 4
                    WHEN 3 THEN 4
                    ELSE 6
                END;

                INSERT INTO dbo.Apartments
                    (ApartmentCode, FloorID, Area, ApartmentType, Status, MaxResidents, Note, CreatedAt, UpdatedAt)
                VALUES
                    (@ApartmentCode, @FloorID, @ApartmentArea, @ApartmentType, N'Empty', @ApartmentMaxResidents,
                     N'Căn hộ demo theo sơ đồ 3 tòa / 6 block / 10 tầng', @Now, @Now);

                SET @UnitNumber += 1;
            END;

            SET @FloorNumber += 1;
        END;

        FETCH NEXT FROM block_cursor INTO @SeedBuildingName, @SeedBlockName, @SeedPrefix, @SeedMaxFloor, @SeedUnitsPerFloor;
    END;

    CLOSE block_cursor;
    DEALLOCATE block_cursor;

    DECLARE @OccupiedApartments TABLE
    (
        ApartmentID INT NOT NULL PRIMARY KEY,
        ApartmentCode NVARCHAR(20) NOT NULL,
        BuildingName NVARCHAR(100) NOT NULL,
        Seq INT NOT NULL,
        IsRenting BIT NOT NULL
    );

    ;WITH RankedApartments AS
    (
        SELECT a.ApartmentID,
               a.ApartmentCode,
               b.BuildingName,
               ROW_NUMBER() OVER (PARTITION BY b.BuildingName ORDER BY bl.BlockName, f.FloorNumber, a.ApartmentCode) AS BuildingSeq
        FROM dbo.Apartments a
        INNER JOIN dbo.Floors f ON f.FloorID = a.FloorID
        INNER JOIN dbo.Blocks bl ON bl.BlockID = f.BlockID
        INNER JOIN dbo.Buildings b ON b.BuildingID = bl.BuildingID
    ),
    SelectedApartments AS
    (
        SELECT ApartmentID, ApartmentCode, BuildingName
        FROM RankedApartments
        WHERE (BuildingName = N'Tòa A' AND BuildingSeq <= 34)
           OR (BuildingName = N'Tòa B' AND BuildingSeq <= 33)
           OR (BuildingName = N'Tòa C' AND BuildingSeq <= 33)
    )
    INSERT INTO @OccupiedApartments (ApartmentID, ApartmentCode, BuildingName, Seq, IsRenting)
    SELECT ApartmentID,
           ApartmentCode,
           BuildingName,
           ROW_NUMBER() OVER (ORDER BY BuildingName, ApartmentCode),
           CASE WHEN ROW_NUMBER() OVER (ORDER BY BuildingName, ApartmentCode) % 5 IN (0, 1) THEN 1 ELSE 0 END
    FROM SelectedApartments;

    UPDATE a
    SET Status = CASE WHEN oa.IsRenting = 1 THEN N'Renting' ELSE N'Occupied' END,
        Note = CONCAT(N'Căn hộ có 2 cư dân demo - ', CASE WHEN oa.IsRenting = 1 THEN N'đang cho thuê' ELSE N'chủ hộ đang ở' END),
        UpdatedAt = @Now
    FROM dbo.Apartments a
    INNER JOIN @OccupiedApartments oa ON oa.ApartmentID = a.ApartmentID;

    ;WITH RemainingApartments AS
    (
        SELECT a.ApartmentID,
               ROW_NUMBER() OVER (ORDER BY a.ApartmentCode) AS RowNo
        FROM dbo.Apartments a
        WHERE NOT EXISTS (SELECT 1 FROM @OccupiedApartments oa WHERE oa.ApartmentID = a.ApartmentID)
    )
    UPDATE a
    SET Status = CASE
            WHEN r.RowNo % 47 = 0 THEN N'Locked'
            WHEN r.RowNo % 31 = 0 THEN N'Maintenance'
            ELSE N'Empty'
        END,
        Note = CASE
            WHEN r.RowNo % 47 = 0 THEN N'Căn tạm khóa để xử lý hồ sơ pháp lý'
            WHEN r.RowNo % 31 = 0 THEN N'Căn đang bảo trì trước khi bàn giao'
            ELSE N'Căn trống sẵn sàng khai thác'
        END,
        UpdatedAt = @Now
    FROM dbo.Apartments a
    INNER JOIN RemainingApartments r ON r.ApartmentID = a.ApartmentID;

    PRINT N'Generating exactly 200 active residents...';

    DECLARE @LastNames TABLE (Id INT NOT NULL PRIMARY KEY, Name NVARCHAR(30) NOT NULL);
    DECLARE @MiddleNames TABLE (Id INT NOT NULL PRIMARY KEY, Name NVARCHAR(30) NOT NULL);
    DECLARE @MaleFirstNames TABLE (Id INT NOT NULL PRIMARY KEY, Name NVARCHAR(30) NOT NULL);
    DECLARE @FemaleFirstNames TABLE (Id INT NOT NULL PRIMARY KEY, Name NVARCHAR(30) NOT NULL);

    INSERT INTO @LastNames (Id, Name)
    VALUES
        (1, N'Nguyễn'), (2, N'Trần'), (3, N'Lê'), (4, N'Phạm'), (5, N'Hoàng'), (6, N'Võ'),
        (7, N'Đặng'), (8, N'Bùi'), (9, N'Đỗ'), (10, N'Ngô'), (11, N'Phan'), (12, N'Vũ');

    INSERT INTO @MiddleNames (Id, Name)
    VALUES
        (1, N'Minh'), (2, N'Thanh'), (3, N'Quốc'), (4, N'Gia'), (5, N'Thu'), (6, N'Hoài'),
        (7, N'Khánh'), (8, N'Nhật'), (9, N'Bảo'), (10, N'Ngọc');

    INSERT INTO @MaleFirstNames (Id, Name)
    VALUES
        (1, N'An'), (2, N'Bảo'), (3, N'Cường'), (4, N'Dũng'), (5, N'Đức'), (6, N'Huy'), (7, N'Khang'), (8, N'Long'), (9, N'Minh'), (10, N'Nam'),
        (11, N'Phong'), (12, N'Quân'), (13, N'Sơn'), (14, N'Thành'), (15, N'Tuấn'), (16, N'Việt'), (17, N'Khoa'), (18, N'Lâm'), (19, N'Phúc'), (20, N'Tín');

    INSERT INTO @FemaleFirstNames (Id, Name)
    VALUES
        (1, N'Anh'), (2, N'Bình'), (3, N'Chi'), (4, N'Dung'), (5, N'Hà'), (6, N'Hạnh'), (7, N'Hoa'), (8, N'Lan'), (9, N'Linh'), (10, N'Mai'),
        (11, N'Ngân'), (12, N'Phương'), (13, N'Quỳnh'), (14, N'Thảo'), (15, N'Trang'), (16, N'Trúc'), (17, N'Uyên'), (18, N'Vy'), (19, N'Yến'), (20, N'Nhi');

    DECLARE @ResidentSeed TABLE
    (
        ResidentSeq INT NOT NULL PRIMARY KEY,
        ApartmentID INT NOT NULL,
        ApartmentCode NVARCHAR(20) NOT NULL,
        BuildingName NVARCHAR(100) NOT NULL,
        IsRenting BIT NOT NULL,
        SlotNo INT NOT NULL,
        Username NVARCHAR(50) NOT NULL,
        FullName NVARCHAR(150) NOT NULL,
        Phone NVARCHAR(20) NOT NULL,
        Email NVARCHAR(150) NOT NULL,
        CCCD NVARCHAR(20) NOT NULL,
        DOB DATE NOT NULL,
        Gender NVARCHAR(10) NOT NULL,
        AddressRegistration NVARCHAR(255) NOT NULL,
        RelationshipWithOwner NVARCHAR(50) NOT NULL,
        ResidentStatus NVARCHAR(20) NOT NULL,
        MoveInDate DATETIME2(0) NOT NULL,
        Note NVARCHAR(MAX) NOT NULL
    );

    ;WITH ResidentBase AS
    (
        SELECT oa.ApartmentID,
               oa.ApartmentCode,
               oa.BuildingName,
               oa.IsRenting,
               s.SlotNo,
               ROW_NUMBER() OVER (ORDER BY oa.Seq, s.SlotNo) AS ResidentSeq
        FROM @OccupiedApartments oa
        CROSS JOIN (VALUES (1), (2)) AS s(SlotNo)
    )
    INSERT INTO @ResidentSeed
        (ResidentSeq, ApartmentID, ApartmentCode, BuildingName, IsRenting, SlotNo, Username, FullName, Phone, Email, CCCD,
         DOB, Gender, AddressRegistration, RelationshipWithOwner, ResidentStatus, MoveInDate, Note)
    SELECT rb.ResidentSeq,
           rb.ApartmentID,
           rb.ApartmentCode,
           rb.BuildingName,
           rb.IsRenting,
           rb.SlotNo,
           CONCAT(N'resident', CONVERT(NVARCHAR(10), rb.ResidentSeq)),
           CONCAT(ln.Name, N' ', mn.Name, N' ', CASE WHEN gender.Gender = N'Nam' THEN mf.Name ELSE ff.Name END),
           CONCAT(N'09', RIGHT(CONCAT(N'00000000', CONVERT(NVARCHAR(8), 51000000 + rb.ResidentSeq)), 8)),
           CONCAT(N'resident', RIGHT(CONCAT(N'000', CONVERT(NVARCHAR(3), rb.ResidentSeq)), 3), N'@sunrise.local'),
           CONCAT(N'079', RIGHT(CONCAT(N'000000000', CONVERT(NVARCHAR(9), rb.ResidentSeq)), 9)),
           CONVERT(DATE, DATEADD(DAY, -((rb.ResidentSeq * 19) % 365), DATEADD(YEAR, -(22 + (rb.ResidentSeq % 43)), @Today))),
           gender.Gender,
           CASE rb.ResidentSeq % 8
               WHEN 0 THEN N'Quận 1, TP.HCM'
               WHEN 1 THEN N'Bình Thạnh, TP.HCM'
               WHEN 2 THEN N'Thủ Đức, TP.HCM'
               WHEN 3 THEN N'Quận 7, TP.HCM'
               WHEN 4 THEN N'Gò Vấp, TP.HCM'
               WHEN 5 THEN N'Tân Bình, TP.HCM'
               WHEN 6 THEN N'Phú Nhuận, TP.HCM'
               ELSE N'Nhà Bè, TP.HCM'
           END,
           CASE
               WHEN rb.SlotNo = 1 AND rb.IsRenting = 1 THEN N'Khách thuê'
               WHEN rb.SlotNo = 1 THEN N'Chủ hộ'
               WHEN rb.ResidentSeq % 3 = 0 THEN N'Con'
               ELSE N'Thành viên'
           END,
           CASE WHEN rb.IsRenting = 1 THEN N'Đang thuê' ELSE N'Đang ở' END,
           DATEADD(MONTH, -(2 + (rb.ResidentSeq % 48)), @Now),
           CONCAT(N'Dữ liệu demo cư dân ', rb.BuildingName, N' / căn ', rb.ApartmentCode)
    FROM ResidentBase rb
    CROSS APPLY (SELECT CASE WHEN rb.ResidentSeq % 2 = 0 THEN N'Nam' ELSE N'Nữ' END AS Gender) gender
    INNER JOIN @LastNames ln ON ln.Id = ((rb.ResidentSeq - 1) % 12) + 1
    INNER JOIN @MiddleNames mn ON mn.Id = ((rb.ResidentSeq - 1) % 10) + 1
    LEFT JOIN @MaleFirstNames mf ON mf.Id = ((rb.ResidentSeq - 1) % 20) + 1
    LEFT JOIN @FemaleFirstNames ff ON ff.Id = ((rb.ResidentSeq - 1) % 20) + 1;

    INSERT INTO dbo.Users
        (Username, PasswordHash, FullName, Email, Phone, RoleID, Status, IsApproved, ApprovedAt, ApprovedBy, CreatedAt, UpdatedAt)
    SELECT Username, @DefaultResidentPassword, FullName, Email, Phone, @ResidentRoleID, N'Active', 1, @Now, @SuperAdminUserID, @Now, @Now
    FROM @ResidentSeed
    ORDER BY ResidentSeq;

    INSERT INTO dbo.Residents
        (UserID, FullName, Phone, Email, CCCD, DOB, Gender, AddressRegistration, ApartmentID,
         RelationshipWithOwner, Status, ResidentStatus, MoveInDate, StartDate, Note, CreatedAt, UpdatedAt)
    SELECT u.UserID,
           rs.FullName,
           rs.Phone,
           rs.Email,
           rs.CCCD,
           rs.DOB,
           rs.Gender,
           rs.AddressRegistration,
           rs.ApartmentID,
           rs.RelationshipWithOwner,
           N'Active',
           rs.ResidentStatus,
           rs.MoveInDate,
           rs.MoveInDate,
           rs.Note,
           @Now,
           @Now
    FROM @ResidentSeed rs
    INNER JOIN dbo.Users u ON u.Username = rs.Username
    ORDER BY rs.ResidentSeq;

    INSERT INTO dbo.Contracts
        (ApartmentID, ResidentID, StartDate, EndDate, RentAmount, DepositAmount, Status, Note, CreatedAt, UpdatedAt)
    SELECT rs.ApartmentID,
           r.ResidentID,
           CONVERT(DATE, rs.MoveInDate),
           CONVERT(DATE, DATEADD(YEAR, 1, rs.MoveInDate)),
           CASE WHEN rs.IsRenting = 1 THEN ROUND(a.Area * 180000, 0) ELSE 0 END,
           CASE WHEN rs.IsRenting = 1 THEN ROUND(a.Area * 360000, 0) ELSE 0 END,
           N'Active',
           CASE WHEN rs.IsRenting = 1 THEN N'Hợp đồng thuê căn hộ demo' ELSE N'Hồ sơ cư trú chủ sở hữu demo' END,
           @Now,
           @Now
    FROM @ResidentSeed rs
    INNER JOIN dbo.Residents r ON r.CCCD = rs.CCCD
    INNER JOIN dbo.Apartments a ON a.ApartmentID = rs.ApartmentID
    WHERE rs.SlotNo = 1;

    PRINT N'Seeding resident vehicles...';

    DECLARE @PrimaryResidents TABLE
    (
        ApartmentID INT NOT NULL PRIMARY KEY,
        ApartmentCode NVARCHAR(20) NOT NULL,
        Seq INT NOT NULL,
        ResidentID INT NOT NULL,
        UserID INT NOT NULL,
        IsRenting BIT NOT NULL
    );

    INSERT INTO @PrimaryResidents (ApartmentID, ApartmentCode, Seq, ResidentID, UserID, IsRenting)
    SELECT rs.ApartmentID, rs.ApartmentCode, rs.ResidentSeq, r.ResidentID, r.UserID, rs.IsRenting
    FROM @ResidentSeed rs
    INNER JOIN dbo.Residents r ON r.CCCD = rs.CCCD
    WHERE rs.SlotNo = 1;

    INSERT INTO dbo.Vehicles (ResidentID, VehicleType, LicensePlate, Color, Brand, Status, Note, CreatedAt, UpdatedAt)
    SELECT ResidentID,
           N'Motorcycle',
           CONCAT(N'59X1-', RIGHT(CONCAT(N'000', CONVERT(NVARCHAR(3), Seq)), 3), N'.', RIGHT(CONCAT(N'00', CONVERT(NVARCHAR(2), Seq % 100)), 2)),
           CASE Seq % 5 WHEN 0 THEN N'Đen' WHEN 1 THEN N'Trắng' WHEN 2 THEN N'Xám' WHEN 3 THEN N'Xanh' ELSE N'Đỏ' END,
           CASE Seq % 5 WHEN 0 THEN N'Honda' WHEN 1 THEN N'Yamaha' WHEN 2 THEN N'VinFast' WHEN 3 THEN N'SYM' ELSE N'Suzuki' END,
           N'Active',
           CONCAT(N'MODEL=Đăng ký tháng;YEAR=', 2020 + (Seq % 5), N';NOTE=Xe máy căn ', ApartmentCode),
           @Now,
           @Now
    FROM @PrimaryResidents;

    INSERT INTO dbo.Vehicles (ResidentID, VehicleType, LicensePlate, Color, Brand, Status, Note, CreatedAt, UpdatedAt)
    SELECT ResidentID,
           N'Car',
           CONCAT(N'51G-', RIGHT(CONCAT(N'000', CONVERT(NVARCHAR(3), Seq)), 3), N'.', RIGHT(CONCAT(N'00', CONVERT(NVARCHAR(2), (Seq * 7) % 100)), 2)),
           CASE Seq % 4 WHEN 0 THEN N'Trắng' WHEN 1 THEN N'Đen' WHEN 2 THEN N'Xám' ELSE N'Đỏ' END,
           CASE Seq % 6 WHEN 0 THEN N'Toyota' WHEN 1 THEN N'Mazda' WHEN 2 THEN N'Kia' WHEN 3 THEN N'Hyundai' WHEN 4 THEN N'Ford' ELSE N'VinFast' END,
           CASE WHEN Seq % 11 = 0 THEN N'Pending' ELSE N'Active' END,
           CONCAT(N'MODEL=Ô tô gia đình;YEAR=', 2018 + (Seq % 7), N';NOTE=Ô hầm B', RIGHT(CONCAT(N'00', Seq), 2)),
           @Now,
           @Now
    FROM @PrimaryResidents
    WHERE Seq % 3 = 0;

    INSERT INTO dbo.Vehicles (ResidentID, VehicleType, LicensePlate, Color, Brand, Status, Note, CreatedAt, UpdatedAt)
    SELECT ResidentID,
           N'ElectricBike',
           CONCAT(N'EBIKE-', RIGHT(CONCAT(N'000', CONVERT(NVARCHAR(3), Seq)), 3)),
           N'Xanh',
           N'VinFast',
           N'Active',
           CONCAT(N'MODEL=Feliz;YEAR=', 2021 + (Seq % 4), N';NOTE=Xe điện căn ', ApartmentCode),
           @Now,
           @Now
    FROM @PrimaryResidents
    WHERE Seq % 5 = 0;

    INSERT INTO dbo.Vehicles (ResidentID, VehicleType, LicensePlate, Color, Brand, Status, Note, CreatedAt, UpdatedAt)
    SELECT ResidentID,
           N'Bicycle',
           CONCAT(N'BIKE-', RIGHT(CONCAT(N'000', CONVERT(NVARCHAR(3), Seq)), 3)),
           N'Trắng',
           N'Giant',
           N'Active',
           CONCAT(N'MODEL=Escape;YEAR=', 2020 + (Seq % 5), N';NOTE=Gửi khu xe đạp căn ', ApartmentCode),
           @Now,
           @Now
    FROM @PrimaryResidents
    WHERE Seq % 7 = 0;

    PRINT N'Generating invoices, details, payments and receipts...';

    DECLARE @FeeManagementID INT;
    DECLARE @FeeParkingID INT;
    DECLARE @FeeCleaningID INT;
    DECLARE @FeeElectricID INT;
    DECLARE @FeeWaterID INT;
    DECLARE @FeeInternetID INT;
    DECLARE @FeeMaintenanceID INT;
    DECLARE @CashAccountID INT;
    DECLARE @BankAccountID INT;
    DECLARE @QrAccountID INT;

    SELECT @FeeManagementID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Quản Lý';
    SELECT @FeeParkingID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Gửi Xe';
    SELECT @FeeCleaningID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Vệ Sinh';
    SELECT @FeeElectricID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Điện';
    SELECT @FeeWaterID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Nước';
    SELECT @FeeInternetID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Internet';
    SELECT @FeeMaintenanceID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Bảo Trì';
    SELECT @CashAccountID = PaymentAccountID FROM dbo.PaymentAccounts WHERE AccountName = N'Tiền mặt quầy thu';
    SELECT @BankAccountID = PaymentAccountID FROM dbo.PaymentAccounts WHERE AccountName = N'Tài khoản ngân hàng vận hành';
    SELECT @QrAccountID = PaymentAccountID FROM dbo.PaymentAccounts WHERE AccountName = N'Tài khoản QR cư dân';

    IF @FeeManagementID IS NULL OR @FeeParkingID IS NULL OR @FeeCleaningID IS NULL OR @FeeElectricID IS NULL
       OR @FeeWaterID IS NULL OR @FeeInternetID IS NULL OR @FeeMaintenanceID IS NULL
    BEGIN
        THROW 51002, 'Required fee types are missing.', 1;
    END;

    DECLARE @MonthOffsets TABLE (MonthOffset INT NOT NULL PRIMARY KEY);
    INSERT INTO @MonthOffsets (MonthOffset) VALUES (0), (-1), (-2);

    INSERT INTO dbo.FinancialPeriods ([Month], [Year], StartDate, EndDate, Status, ClosedBy, ClosedAt, Note, CreatedAt)
    SELECT MONTH(DATEADD(MONTH, mo.MonthOffset, @Today)),
           YEAR(DATEADD(MONTH, mo.MonthOffset, @Today)),
           DATEFROMPARTS(YEAR(DATEADD(MONTH, mo.MonthOffset, @Today)), MONTH(DATEADD(MONTH, mo.MonthOffset, @Today)), 1),
           EOMONTH(DATEADD(MONTH, mo.MonthOffset, @Today)),
           CASE WHEN mo.MonthOffset = 0 THEN N'Open' ELSE N'Closed' END,
           CASE WHEN mo.MonthOffset = 0 THEN NULL ELSE @ManagerUserID END,
           CASE WHEN mo.MonthOffset = 0 THEN NULL ELSE DATEADD(DAY, 3, EOMONTH(DATEADD(MONTH, mo.MonthOffset, @Today))) END,
           CASE WHEN mo.MonthOffset = 0 THEN N'Kỳ tài chính hiện tại' ELSE N'Kỳ tài chính demo đã chốt' END,
           @Now
    FROM @MonthOffsets mo;

    INSERT INTO dbo.Invoices
        (ApartmentID, [Month], [Year], DueDate, PaymentStatus, TotalAmount, PaidAmount, RemainingAmount,
         PaidAt, ConfirmedBy, Note, CreatedAt, UpdatedAt)
    SELECT oa.ApartmentID,
           MONTH(period.TargetDate),
           YEAR(period.TargetDate),
           DATEFROMPARTS(YEAR(period.TargetDate), MONTH(period.TargetDate), 25),
           payment.PaymentStatus,
           total.TotalAmount,
           payment.PaidAmount,
           total.TotalAmount - payment.PaidAmount,
           CASE WHEN payment.PaymentStatus = N'Paid' THEN DATEADD(DAY, -2, DATEFROMPARTS(YEAR(period.TargetDate), MONTH(period.TargetDate), 25)) ELSE NULL END,
           CASE WHEN payment.PaymentStatus = N'Paid' THEN @ManagerUserID ELSE NULL END,
           CONCAT(N'Hóa đơn dịch vụ căn ', oa.ApartmentCode, N' tháng ', RIGHT(CONCAT(N'0', MONTH(period.TargetDate)), 2), N'/', YEAR(period.TargetDate)),
           DATEADD(DAY, -2, DATEFROMPARTS(YEAR(period.TargetDate), MONTH(period.TargetDate), 1)),
           @Now
    FROM @OccupiedApartments oa
    INNER JOIN dbo.Apartments a ON a.ApartmentID = oa.ApartmentID
    CROSS JOIN @MonthOffsets mo
    CROSS APPLY (SELECT DATEADD(MONTH, mo.MonthOffset, @Today) AS TargetDate) period
    CROSS APPLY
    (
        SELECT
            COUNT(CASE WHEN v.VehicleType = N'Car' THEN 1 END) AS CarCount,
            COUNT(CASE WHEN v.VehicleType IN (N'Motorcycle', N'ElectricBike') THEN 1 END) AS MotorbikeCount,
            COUNT(CASE WHEN v.VehicleType = N'Bicycle' THEN 1 END) AS BicycleCount
        FROM dbo.Vehicles v
        INNER JOIN dbo.Residents r ON r.ResidentID = v.ResidentID
        WHERE r.ApartmentID = oa.ApartmentID
          AND v.Status IN (N'Active', N'Pending')
    ) vc
    CROSS APPLY
    (
        SELECT
            ROUND(a.Area * 12000, 0) AS ManagementAmount,
            CONVERT(DECIMAL(18,2), 90000) AS CleaningAmount,
            CONVERT(DECIMAL(18,2), vc.CarCount * 1200000 + vc.MotorbikeCount * 150000 + vc.BicycleCount * 30000) AS ParkingAmount,
            CONVERT(DECIMAL(18,2), 260000 + ((oa.Seq + ABS(mo.MonthOffset) * 13) % 18) * 18000) AS ElectricAmount,
            CONVERT(DECIMAL(18,2), 95000 + ((oa.Seq + ABS(mo.MonthOffset) * 7) % 12) * 9000) AS WaterAmount,
            CONVERT(DECIMAL(18,2), CASE WHEN oa.Seq % 4 = 0 THEN 0 ELSE 180000 END) AS InternetAmount,
            CONVERT(DECIMAL(18,2), CASE WHEN mo.MonthOffset = -2 THEN 300000 ELSE 0 END) AS MaintenanceAmount
    ) fees
    CROSS APPLY
    (
        SELECT fees.ManagementAmount + fees.CleaningAmount + fees.ParkingAmount + fees.ElectricAmount
             + fees.WaterAmount + fees.InternetAmount + fees.MaintenanceAmount AS TotalAmount
    ) total
    CROSS APPLY
    (
        SELECT CASE
                WHEN mo.MonthOffset = 0 AND oa.Seq % 4 = 0 THEN N'Paid'
                WHEN mo.MonthOffset = 0 AND oa.Seq % 4 = 1 THEN N'PartiallyPaid'
                WHEN mo.MonthOffset = 0 AND oa.Seq % 4 = 2 THEN N'Unpaid'
                WHEN mo.MonthOffset = 0 THEN N'Pending'
                WHEN mo.MonthOffset = -1 AND oa.Seq % 7 IN (0, 1) THEN N'Overdue'
                WHEN mo.MonthOffset = -1 AND oa.Seq % 7 = 2 THEN N'PartiallyPaid'
                WHEN mo.MonthOffset = -2 AND oa.Seq % 11 = 0 THEN N'PartiallyPaid'
                ELSE N'Paid'
            END AS PaymentStatus
    ) status_calc
    CROSS APPLY
    (
        SELECT status_calc.PaymentStatus,
               CASE
                   WHEN status_calc.PaymentStatus = N'Paid' THEN total.TotalAmount
                   WHEN status_calc.PaymentStatus = N'PartiallyPaid' THEN ROUND(total.TotalAmount * 0.55, 0)
                   ELSE 0
               END AS PaidAmount
    ) payment;

    INSERT INTO dbo.InvoiceDetails (InvoiceID, FeeTypeID, Amount, CreatedAt)
    SELECT i.InvoiceID, detail.FeeTypeID, detail.Amount, @Now
    FROM dbo.Invoices i
    INNER JOIN dbo.Apartments a ON a.ApartmentID = i.ApartmentID
    INNER JOIN @OccupiedApartments oa ON oa.ApartmentID = a.ApartmentID
    CROSS APPLY
    (
        SELECT
            COUNT(CASE WHEN v.VehicleType = N'Car' THEN 1 END) AS CarCount,
            COUNT(CASE WHEN v.VehicleType IN (N'Motorcycle', N'ElectricBike') THEN 1 END) AS MotorbikeCount,
            COUNT(CASE WHEN v.VehicleType = N'Bicycle' THEN 1 END) AS BicycleCount
        FROM dbo.Vehicles v
        INNER JOIN dbo.Residents r ON r.ResidentID = v.ResidentID
        WHERE r.ApartmentID = oa.ApartmentID
          AND v.Status IN (N'Active', N'Pending')
    ) vc
    CROSS APPLY
    (
        SELECT
            DATEDIFF(MONTH, DATEFROMPARTS(i.[Year], i.[Month], 1), DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1)) AS MonthDistance,
            ROUND(a.Area * 12000, 0) AS ManagementAmount,
            CONVERT(DECIMAL(18,2), 90000) AS CleaningAmount,
            CONVERT(DECIMAL(18,2), vc.CarCount * 1200000 + vc.MotorbikeCount * 150000 + vc.BicycleCount * 30000) AS ParkingAmount
    ) base_fee
    CROSS APPLY
    (
        SELECT
            CONVERT(DECIMAL(18,2), 260000 + ((oa.Seq + ABS(base_fee.MonthDistance) * 13) % 18) * 18000) AS ElectricAmount,
            CONVERT(DECIMAL(18,2), 95000 + ((oa.Seq + ABS(base_fee.MonthDistance) * 7) % 12) * 9000) AS WaterAmount,
            CONVERT(DECIMAL(18,2), CASE WHEN oa.Seq % 4 = 0 THEN 0 ELSE 180000 END) AS InternetAmount,
            CONVERT(DECIMAL(18,2), CASE WHEN base_fee.MonthDistance = 2 THEN 300000 ELSE 0 END) AS MaintenanceAmount
    ) variable_fee
    CROSS APPLY
    (
        VALUES
            (@FeeManagementID, base_fee.ManagementAmount),
            (@FeeCleaningID, base_fee.CleaningAmount),
            (@FeeParkingID, base_fee.ParkingAmount),
            (@FeeElectricID, variable_fee.ElectricAmount),
            (@FeeWaterID, variable_fee.WaterAmount),
            (@FeeInternetID, variable_fee.InternetAmount),
            (@FeeMaintenanceID, variable_fee.MaintenanceAmount)
    ) detail(FeeTypeID, Amount)
    WHERE detail.Amount > 0;

    INSERT INTO dbo.Payments
        (PaymentCode, InvoiceID, ApartmentID, ResidentID, PaymentAccountID, Amount, PaymentMethod, PaymentDate,
         TransactionCode, PaymentStatus, ConfirmedBy, ConfirmedAt, Note, CreatedBy, CreatedAt)
    SELECT CONCAT(N'PAY-', i.[Year], RIGHT(CONCAT(N'0', i.[Month]), 2), N'-', RIGHT(CONCAT(N'000000', CONVERT(NVARCHAR(6), i.InvoiceID)), 6)),
           i.InvoiceID,
           i.ApartmentID,
           pr.ResidentID,
           CASE i.InvoiceID % 3 WHEN 0 THEN @CashAccountID WHEN 1 THEN @BankAccountID ELSE @QrAccountID END,
           i.PaidAmount,
           CASE i.InvoiceID % 3 WHEN 0 THEN N'Cash' WHEN 1 THEN N'BankTransfer' ELSE N'QR' END,
           COALESCE(i.PaidAt, DATEADD(DAY, -1, i.DueDate)),
           CONCAT(N'TXN', i.[Year], RIGHT(CONCAT(N'0', i.[Month]), 2), RIGHT(CONCAT(N'000000', CONVERT(NVARCHAR(6), i.InvoiceID)), 6)),
           N'Confirmed',
           @ManagerUserID,
           COALESCE(i.PaidAt, DATEADD(DAY, -1, i.DueDate)),
           N'Thanh toán demo đã xác nhận',
           pr.UserID,
           @Now
    FROM dbo.Invoices i
    INNER JOIN @PrimaryResidents pr ON pr.ApartmentID = i.ApartmentID
    WHERE i.PaidAmount > 0;

    INSERT INTO dbo.Receipts
        (ReceiptCode, PaymentID, InvoiceID, ApartmentID, PayerName, Amount, ReceiptDate, CreatedBy, Note)
    SELECT CONCAT(N'RCP-', CONVERT(VARCHAR(8), p.PaymentDate, 112), N'-', RIGHT(CONCAT(N'000000', CONVERT(NVARCHAR(6), p.PaymentID)), 6)),
           p.PaymentID,
           p.InvoiceID,
           p.ApartmentID,
           r.FullName,
           p.Amount,
           p.PaymentDate,
           @ManagerUserID,
           N'Biên lai demo'
    FROM dbo.Payments p
    INNER JOIN dbo.Residents r ON r.ResidentID = p.ResidentID
    WHERE p.PaymentStatus = N'Confirmed';

    INSERT INTO dbo.FundTransactions
        (FundID, TransactionType, ReferenceType, ReferenceID, Amount, TransactionDate, CreatedBy, Note)
    SELECT @OperatingFundID,
           N'Income',
           N'Payment',
           p.PaymentID,
           p.Amount,
           p.PaymentDate,
           @ManagerUserID,
           N'Thu phí dịch vụ demo'
    FROM dbo.Payments p
    WHERE p.PaymentStatus = N'Confirmed';

    ;WITH PendingInvoices AS
    (
        SELECT TOP (12) i.InvoiceID, i.ApartmentID, i.RemainingAmount, pr.ResidentID, pr.UserID,
               ROW_NUMBER() OVER (ORDER BY i.DueDate, i.InvoiceID) AS RowNo
        FROM dbo.Invoices i
        INNER JOIN @PrimaryResidents pr ON pr.ApartmentID = i.ApartmentID
        WHERE i.RemainingAmount > 0
          AND i.PaymentStatus IN (N'Unpaid', N'Overdue', N'Pending')
        ORDER BY i.DueDate, i.InvoiceID
    )
    INSERT INTO dbo.Payments
        (PaymentCode, InvoiceID, ApartmentID, ResidentID, PaymentAccountID, Amount, PaymentMethod, PaymentDate,
         TransactionCode, PaymentStatus, Note, CreatedBy, CreatedAt)
    SELECT CONCAT(N'PEND-', RIGHT(CONCAT(N'000000', CONVERT(NVARCHAR(6), InvoiceID)), 6)),
           InvoiceID,
           ApartmentID,
           ResidentID,
           @QrAccountID,
           CASE WHEN RemainingAmount > 500000 THEN 500000 ELSE RemainingAmount END,
           N'QR',
           DATEADD(HOUR, -RowNo, @Now),
           CONCAT(N'PENDING', RIGHT(CONCAT(N'000000', CONVERT(NVARCHAR(6), InvoiceID)), 6)),
           N'Pending',
           N'Minh chứng thanh toán đang chờ duyệt',
           UserID,
           @Now
    FROM PendingInvoices;

    PRINT N'Seeding vendors and operating expenses...';

    DECLARE @VendorSeed TABLE
    (
        VendorName NVARCHAR(200) NOT NULL PRIMARY KEY,
        Phone NVARCHAR(30) NOT NULL,
        Email NVARCHAR(150) NOT NULL,
        Address NVARCHAR(300) NOT NULL,
        TaxCode NVARCHAR(50) NOT NULL,
        BankAccount NVARCHAR(100) NOT NULL,
        BankName NVARCHAR(150) NOT NULL
    );

    INSERT INTO @VendorSeed (VendorName, Phone, Email, Address, TaxCode, BankAccount, BankName)
    VALUES
        (N'Công ty Bảo vệ An Tâm', N'02838220001', N'contract@antamsecurity.vn', N'Quận 1, TP.HCM', N'0310000001', N'001100001', N'Vietcombank'),
        (N'Công ty Vệ sinh Xanh Sạch', N'02838220002', N'info@xanhsach.vn', N'Bình Thạnh, TP.HCM', N'0310000002', N'001100002', N'ACB'),
        (N'Thành Phát Elevator', N'02838220003', N'service@thanhphat-elevator.vn', N'Thủ Đức, TP.HCM', N'0310000003', N'001100003', N'Techcombank'),
        (N'PCCC Sài Gòn', N'02838220004', N'ops@pcccsaigon.vn', N'Quận 7, TP.HCM', N'0310000004', N'001100004', N'MB Bank'),
        (N'Điện nước Minh Long', N'02838220005', N'admin@minhlong.vn', N'Gò Vấp, TP.HCM', N'0310000005', N'001100005', N'VietinBank');

    INSERT INTO dbo.Vendors (VendorName, Phone, Email, Address, TaxCode, BankAccount, BankName, IsActive, CreatedAt)
    SELECT VendorName, Phone, Email, Address, TaxCode, BankAccount, BankName, 1, @Now
    FROM @VendorSeed;

    DECLARE @ExpenseSeed TABLE
    (
        Seq INT NOT NULL PRIMARY KEY,
        CategoryName NVARCHAR(150) NOT NULL,
        VendorName NVARCHAR(200) NULL,
        FundName NVARCHAR(150) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        ExpenseDate DATETIME2(0) NOT NULL,
        PaymentMethod NVARCHAR(50) NOT NULL,
        ReceiverName NVARCHAR(150) NULL,
        ExpenseStatus NVARCHAR(50) NOT NULL
    );

    INSERT INTO @ExpenseSeed (Seq, CategoryName, VendorName, FundName, Title, Amount, ExpenseDate, PaymentMethod, ReceiverName, ExpenseStatus)
    VALUES
        (1, N'Lương bảo vệ', N'Công ty Bảo vệ An Tâm', N'Quỹ an ninh', N'Lương bảo vệ ca ngày tháng hiện tại', 48000000, DATEADD(DAY, -10, @Now), N'BankTransfer', N'Công ty Bảo vệ An Tâm', N'Paid'),
        (2, N'Lương vệ sinh', N'Công ty Vệ sinh Xanh Sạch', N'Quỹ vận hành', N'Dịch vụ vệ sinh khu chung tháng hiện tại', 36000000, DATEADD(DAY, -9, @Now), N'BankTransfer', N'Công ty Vệ sinh Xanh Sạch', N'Paid'),
        (3, N'Sửa chữa bảo trì', N'Thành Phát Elevator', N'Quỹ bảo trì', N'Kiểm tra bộ cứu hộ thang máy Block A', 8500000, DATEADD(DAY, -7, @Now), N'BankTransfer', N'Thành Phát Elevator', N'Approved'),
        (4, N'Điện khu chung', NULL, N'Quỹ vận hành', N'Điện hành lang và hầm xe tháng trước', 27500000, DATEADD(DAY, -18, @Now), N'BankTransfer', N'Điện lực TP.HCM', N'Paid'),
        (5, N'Nước khu chung', NULL, N'Quỹ vận hành', N'Nước tưới cây và vệ sinh khu chung', 6800000, DATEADD(DAY, -17, @Now), N'BankTransfer', N'Cấp nước Gia Định', N'Paid'),
        (6, N'Dịch vụ bên ngoài', N'PCCC Sài Gòn', N'Quỹ bảo trì', N'Kiểm định bình chữa cháy định kỳ', 12600000, DATEADD(DAY, -4, @Now), N'BankTransfer', N'PCCC Sài Gòn', N'PendingApproval'),
        (7, N'Văn phòng phẩm', NULL, N'Quỹ vận hành', N'Mua giấy in, mực in và thẻ từ dự phòng', 3200000, DATEADD(DAY, -3, @Now), N'Cash', N'Nhân viên hành chính', N'Paid'),
        (8, N'Sửa chữa bảo trì', N'Điện nước Minh Long', N'Quỹ bảo trì', N'Sửa máy bơm nước tầng hầm B2', 14500000, DATEADD(DAY, -1, @Now), N'BankTransfer', N'Điện nước Minh Long', N'Approved');

    INSERT INTO dbo.Expenses
        (ExpenseCode, ExpenseCategoryID, VendorID, FundID, Title, Description, Amount, ExpenseDate, PaymentMethod,
         ReceiverName, ExpenseStatus, CreatedBy, ApprovedBy, ApprovedAt, PaidBy, PaidAt, CreatedAt)
    SELECT CONCAT(N'EXP-', CONVERT(VARCHAR(8), es.ExpenseDate, 112), N'-', RIGHT(CONCAT(N'000', es.Seq), 3)),
           ec.ExpenseCategoryID,
           v.VendorID,
           f.FundID,
           es.Title,
           CONCAT(N'Khoản chi demo: ', es.Title),
           es.Amount,
           es.ExpenseDate,
           es.PaymentMethod,
           es.ReceiverName,
           es.ExpenseStatus,
           @ManagerUserID,
           CASE WHEN es.ExpenseStatus IN (N'Approved', N'Paid') THEN @SuperAdminUserID ELSE NULL END,
           CASE WHEN es.ExpenseStatus IN (N'Approved', N'Paid') THEN DATEADD(HOUR, 2, es.ExpenseDate) ELSE NULL END,
           CASE WHEN es.ExpenseStatus = N'Paid' THEN @ManagerUserID ELSE NULL END,
           CASE WHEN es.ExpenseStatus = N'Paid' THEN DATEADD(HOUR, 4, es.ExpenseDate) ELSE NULL END,
           @Now
    FROM @ExpenseSeed es
    INNER JOIN dbo.ExpenseCategories ec ON ec.CategoryName = es.CategoryName
    INNER JOIN dbo.Funds f ON f.FundName = es.FundName
    LEFT JOIN dbo.Vendors v ON v.VendorName = es.VendorName;

    INSERT INTO dbo.PaymentVouchers (VoucherCode, ExpenseID, PayeeName, Amount, VoucherDate, CreatedBy, Note)
    SELECT CONCAT(N'PV-', CONVERT(VARCHAR(8), e.PaidAt, 112), N'-', RIGHT(CONCAT(N'000000', e.ExpenseID), 6)),
           e.ExpenseID,
           COALESCE(e.ReceiverName, N'Nhà cung cấp'),
           e.Amount,
           e.PaidAt,
           @ManagerUserID,
           N'Phiếu chi demo'
    FROM dbo.Expenses e
    WHERE e.ExpenseStatus = N'Paid';

    INSERT INTO dbo.FundTransactions (FundID, TransactionType, ReferenceType, ReferenceID, Amount, TransactionDate, CreatedBy, Note)
    SELECT e.FundID,
           N'Expense',
           N'Expense',
           e.ExpenseID,
           e.Amount,
           e.PaidAt,
           @ManagerUserID,
           e.Title
    FROM dbo.Expenses e
    WHERE e.ExpenseStatus = N'Paid'
      AND e.FundID IS NOT NULL;

    PRINT N'Seeding shared assets and maintenance schedules...';

    INSERT INTO dbo.Assets
        (AssetCode, AssetName, AssetType, Location, PurchaseDate, Condition, LastMaintenanceDate, NextMaintenanceDate, RepairCost, Note, CreatedAt, UpdatedAt)
    SELECT CONCAT(N'AST-', bs.CodePrefix, N'-LIFT-', RIGHT(CONCAT(N'0', lift.N), 2)),
           CONCAT(N'Thang máy ', bs.CodePrefix, lift.N),
           N'Thang máy',
           CONCAT(bs.BuildingName, N' - ', bs.BlockName),
           DATEADD(YEAR, -4 - (lift.N % 2), @Today),
           CASE WHEN bs.CodePrefix IN (N'C', N'F') AND lift.N = 2 THEN N'Cần bảo trì' ELSE N'Tốt' END,
           DATEADD(DAY, -24, @Today),
           DATEADD(DAY, 6 + lift.N, @Today),
           CASE WHEN bs.CodePrefix IN (N'C', N'F') AND lift.N = 2 THEN 3200000 ELSE 0 END,
           N'Bảo trì định kỳ hàng tháng, kiểm định tải trọng hằng năm',
           @Now,
           @Now
    FROM @BlockSeed bs
    CROSS JOIN (VALUES (1), (2)) AS lift(N);

    INSERT INTO dbo.Assets
        (AssetCode, AssetName, AssetType, Location, PurchaseDate, Condition, LastMaintenanceDate, NextMaintenanceDate, RepairCost, Note, CreatedAt, UpdatedAt)
    SELECT CONCAT(N'AST-', bs.CodePrefix, N'-PCCC-', RIGHT(CONCAT(N'00', n.N), 2)),
           CONCAT(N'Tủ PCCC tầng ', RIGHT(CONCAT(N'00', n.N), 2), N' - ', bs.CodePrefix),
           N'PCCC',
           CONCAT(bs.BuildingName, N' - ', bs.BlockName, N' - Tầng ', n.N),
           DATEADD(YEAR, -3, @Today),
           CASE WHEN n.N % 9 = 0 THEN N'Cần bảo trì' ELSE N'Tốt' END,
           DATEADD(DAY, -35, @Today),
           DATEADD(DAY, 25 + (n.N % 5), @Today),
           CASE WHEN n.N % 9 = 0 THEN 850000 ELSE 0 END,
           N'Bình chữa cháy, vòi và đầu báo cháy khu hành lang',
           @Now,
           @Now
    FROM @BlockSeed bs
    INNER JOIN @Numbers n ON n.N <= bs.MaxFloor;

    INSERT INTO dbo.Assets
        (AssetCode, AssetName, AssetType, Location, PurchaseDate, Condition, LastMaintenanceDate, NextMaintenanceDate, RepairCost, Note, CreatedAt, UpdatedAt)
    SELECT CONCAT(N'AST-', bs.CodePrefix, N'-CAM-', RIGHT(CONCAT(N'00', cam.N), 2)),
           CASE cam.N WHEN 1 THEN N'Camera sảnh' WHEN 2 THEN N'Camera hầm xe' WHEN 3 THEN N'Camera tầng 5' ELSE N'Camera tầng mái' END,
           N'An ninh',
           CONCAT(bs.BuildingName, N' - ', bs.BlockName),
           DATEADD(YEAR, -2, @Today),
           CASE WHEN bs.CodePrefix = N'E' AND cam.N = 2 THEN N'Hỏng' ELSE N'Tốt' END,
           DATEADD(DAY, -20, @Today),
           DATEADD(DAY, 40, @Today),
           CASE WHEN bs.CodePrefix = N'E' AND cam.N = 2 THEN 1800000 ELSE 0 END,
           N'Camera IP kết nối phòng bảo vệ trung tâm',
           @Now,
           @Now
    FROM @BlockSeed bs
    CROSS JOIN (VALUES (1), (2), (3), (4)) AS cam(N);

    INSERT INTO dbo.Assets
        (AssetCode, AssetName, AssetType, Location, PurchaseDate, Condition, LastMaintenanceDate, NextMaintenanceDate, RepairCost, Note, CreatedAt, UpdatedAt)
    VALUES
        (N'AST-COM-PUMP-01', N'Máy bơm cấp nước 1', N'Cấp nước', N'Tầng hầm B2', DATEADD(YEAR, -5, @Today), N'Tốt', DATEADD(DAY, -12, @Today), DATEADD(DAY, 18, @Today), 0, N'Bơm luân phiên theo áp lực bồn mái', @Now, @Now),
        (N'AST-COM-PUMP-02', N'Máy bơm cấp nước 2', N'Cấp nước', N'Tầng hầm B2', DATEADD(YEAR, -5, @Today), N'Bảo trì', DATEADD(DAY, -45, @Today), DATEADD(DAY, -2, @Today), 14500000, N'Cần thay phớt và kiểm tra motor', @Now, @Now),
        (N'AST-COM-GEN-01', N'Máy phát điện dự phòng', N'Điện', N'Nhà kỹ thuật trung tâm', DATEADD(YEAR, -6, @Today), N'Tốt', DATEADD(DAY, -18, @Today), DATEADD(DAY, 12, @Today), 0, N'Cấp nguồn thang máy và chiếu sáng khẩn cấp', @Now, @Now),
        (N'AST-COM-BAR-01', N'Barrier vào hầm xe', N'An ninh', N'Cổng hầm B1', DATEADD(YEAR, -2, @Today), N'Tốt', DATEADD(DAY, -15, @Today), DATEADD(DAY, 45, @Today), 0, N'Tích hợp thẻ xe cư dân', @Now, @Now),
        (N'AST-COM-BAR-02', N'Barrier ra hầm xe', N'An ninh', N'Cổng hầm B1', DATEADD(YEAR, -2, @Today), N'Cần bảo trì', DATEADD(DAY, -40, @Today), DATEADD(DAY, 3, @Today), 2100000, N'Cảm biến đóng mở chậm', @Now, @Now),
        (N'AST-COM-LIGHT-01', N'Hệ đèn sân vườn', N'Chiếu sáng', N'Sân nội khu', DATEADD(YEAR, -3, @Today), N'Tốt', DATEADD(DAY, -16, @Today), DATEADD(DAY, 32, @Today), 0, N'Đèn cảnh quan tự động theo giờ', @Now, @Now),
        (N'AST-COM-GARDEN-01', N'Máy cắt cỏ', N'Cảnh quan', N'Kho cảnh quan', DATEADD(YEAR, -2, @Today), N'Bảo trì', DATEADD(DAY, -60, @Today), DATEADD(DAY, 5, @Today), 650000, N'Thay lưỡi cắt và vệ sinh lọc gió', @Now, @Now),
        (N'AST-COM-POOL-01', N'Hệ lọc hồ bơi', N'Tiện ích', N'Khu tiện ích tầng 3', DATEADD(YEAR, -4, @Today), N'Tốt', DATEADD(DAY, -10, @Today), DATEADD(DAY, 20, @Today), 0, N'Lọc tuần hoàn hồ bơi cư dân', @Now, @Now);

    INSERT INTO dbo.MaintenanceSchedules (AssetID, Category, ScheduledDate, Status, AssignedTo, Note, CreatedAt, UpdatedAt)
    SELECT AssetID,
           N'Bảo trì thang máy định kỳ',
           NextMaintenanceDate,
           CASE WHEN NextMaintenanceDate < @Today THEN N'Quá hạn' ELSE N'Đã lên lịch' END,
           N'Thành Phát Elevator',
           N'Kiểm tra cửa tầng, cứu hộ tự động và phanh cabin',
           @Now,
           @Now
    FROM dbo.Assets
    WHERE AssetType = N'Thang máy';

    INSERT INTO dbo.MaintenanceSchedules (AssetID, Category, ScheduledDate, Status, AssignedTo, Note, CreatedAt, UpdatedAt)
    SELECT AssetID,
           CASE AssetType WHEN N'PCCC' THEN N'Kiểm tra PCCC định kỳ' ELSE N'Sửa chữa tài sản cần xử lý' END,
           CASE WHEN Condition = N'Hỏng' THEN DATEADD(DAY, 1, @Today) ELSE NextMaintenanceDate END,
           CASE WHEN Condition = N'Hỏng' THEN N'Chờ xử lý' ELSE N'Đã lên lịch' END,
           CASE AssetType WHEN N'PCCC' THEN N'PCCC Sài Gòn' WHEN N'Cấp nước' THEN N'Điện nước Minh Long' ELSE N'Đội kỹ thuật nội bộ' END,
           N'Lịch phát sinh từ tình trạng tài sản demo',
           @Now,
           @Now
    FROM dbo.Assets
    WHERE Condition IN (N'Cần bảo trì', N'Bảo trì', N'Hỏng');

    INSERT INTO dbo.MaintenanceSchedules (AssetID, Category, ScheduledDate, Status, AssignedTo, Note, CreatedAt, UpdatedAt)
    SELECT TOP (20) AssetID,
           N'Bảo trì hoàn tất',
           DATEADD(DAY, -14 - (AssetID % 10), @Today),
           N'Hoàn tất',
           N'Đội kỹ thuật nội bộ',
           N'Hoàn tất kiểm tra định kỳ tháng trước',
           @Now,
           @Now
    FROM dbo.Assets
    WHERE Condition = N'Tốt'
    ORDER BY AssetID;

    PRINT N'Seeding complaints, visitors and notifications...';

    ;WITH ComplaintResidents AS
    (
        SELECT TOP (45) r.ResidentID, r.ApartmentID, r.FullName, a.ApartmentCode,
               ROW_NUMBER() OVER (ORDER BY r.ResidentID) AS RowNo
        FROM dbo.Residents r
        INNER JOIN dbo.Apartments a ON a.ApartmentID = r.ApartmentID
        ORDER BY r.ResidentID
    )
    INSERT INTO dbo.Complaints
        (ResidentID, ApartmentID, Category, ComplaintType, Title, Description, Priority, Status,
         AssignedToUserID, ResolutionNotes, SatisfactionRating, CreatedAt, UpdatedAt)
    SELECT ResidentID,
           ApartmentID,
           CASE RowNo % 8 WHEN 0 THEN N'Elevator' WHEN 1 THEN N'Water' WHEN 2 THEN N'Electrical' WHEN 3 THEN N'Noise'
                WHEN 4 THEN N'Security' WHEN 5 THEN N'Cleaning' WHEN 6 THEN N'Parking' ELSE N'Facility' END,
           CASE RowNo % 4 WHEN 0 THEN N'Maintenance' WHEN 1 THEN N'General' WHEN 2 THEN N'Security' ELSE N'Billing' END,
           CASE RowNo % 8 WHEN 0 THEN N'Thang máy dừng lâu giờ cao điểm' WHEN 1 THEN N'Áp lực nước yếu buổi tối' WHEN 2 THEN N'Đèn hành lang chập chờn' WHEN 3 THEN N'Tiếng ồn sau 22 giờ'
                WHEN 4 THEN N'Khách lạ vào khu căn hộ' WHEN 5 THEN N'Phòng rác có mùi' WHEN 6 THEN N'Xe đỗ sai vị trí' ELSE N'Cửa kính sảnh khó đóng' END,
           CONCAT(N'Phản ánh demo từ căn ', ApartmentCode, N' do cư dân ', FullName, N' gửi.'),
           CASE RowNo % 5 WHEN 0 THEN N'High' WHEN 1 THEN N'Medium' ELSE N'Low' END,
           CASE RowNo % 4 WHEN 0 THEN N'New' WHEN 1 THEN N'InProgress' WHEN 2 THEN N'Resolved' ELSE N'Closed' END,
           CASE WHEN RowNo % 4 IN (1, 2, 3) THEN @ManagerUserID ELSE NULL END,
           CASE WHEN RowNo % 4 IN (2, 3) THEN N'Ban quản lý đã xử lý và ghi nhận phản hồi cư dân.' ELSE NULL END,
           CASE WHEN RowNo % 4 IN (2, 3) THEN 4 + (RowNo % 2) ELSE NULL END,
           DATEADD(DAY, -RowNo, @Now),
           DATEADD(DAY, -CASE WHEN RowNo > 2 THEN RowNo - 1 ELSE RowNo END, @Now)
    FROM ComplaintResidents;

    ;WITH VisitorResidents AS
    (
        SELECT TOP (80) r.ResidentID, r.FullName,
               ROW_NUMBER() OVER (ORDER BY r.ResidentID DESC) AS RowNo
        FROM dbo.Residents r
        ORDER BY r.ResidentID DESC
    )
    INSERT INTO dbo.Visitors
        (ResidentID, VisitorName, Phone, Email, IDNumber, Purpose, ArrivalTime, DepartureTime, Status,
         ApprovedByUserID, Note, CreatedAt, UpdatedAt)
    SELECT ResidentID,
           CONCAT(N'Khách của ', FullName),
           CONCAT(N'08', RIGHT(CONCAT(N'00000000', CONVERT(NVARCHAR(8), 61000000 + RowNo)), 8)),
           CONCAT(N'visitor', RIGHT(CONCAT(N'000', RowNo), 3), N'@example.com'),
           CONCAT(N'0798', RIGHT(CONCAT(N'00000000', RowNo), 8)),
           CASE RowNo % 5 WHEN 0 THEN N'Thăm gia đình' WHEN 1 THEN N'Giao hàng' WHEN 2 THEN N'Sửa chữa thiết bị' WHEN 3 THEN N'Họp cư dân' ELSE N'Dịch vụ căn hộ' END,
           DATEADD(HOUR, -RowNo, @Now),
           CASE WHEN RowNo % 4 = 0 THEN DATEADD(HOUR, -RowNo + 2, @Now) ELSE NULL END,
           CASE RowNo % 4 WHEN 0 THEN N'CheckedOut' WHEN 1 THEN N'Pending' WHEN 2 THEN N'Approved' ELSE N'Rejected' END,
           CASE WHEN RowNo % 4 IN (0, 2, 3) THEN @ManagerUserID ELSE NULL END,
           CONCAT(N'TYPE=', CASE RowNo % 5 WHEN 1 THEN N'Delivery' WHEN 2 THEN N'Service' WHEN 0 THEN N'Family' ELSE N'Guest' END, N';NOTE=Dữ liệu khách ra vào demo'),
           @Now,
           @Now
    FROM VisitorResidents;

    INSERT INTO dbo.Notifications
        (UserID, ResidentID, Title, Subject, Message, Body, NotificationType, Priority, IsRead, ReadAt, Status, SentDate, CreatedAt, UpdatedAt)
    SELECT NULL, NULL, v.Title, v.Title, v.Message, v.Message, v.NotificationType, v.Priority, 0, NULL, N'Sent', @Now, @Now, @Now
    FROM
    (
        VALUES
            (N'Lịch bảo trì thang máy tuần này', N'Thang máy các block sẽ được bảo trì luân phiên ngoài giờ cao điểm.', N'Maintenance', N'High'),
            (N'Diễn tập PCCC định kỳ', N'Buổi diễn tập PCCC toàn khu diễn ra vào sáng thứ Bảy tuần này.', N'Security', N'Medium'),
            (N'Nhắc thanh toán phí dịch vụ', N'Cư dân vui lòng thanh toán phí trước ngày 25 hằng tháng.', N'Payment', N'Medium'),
            (N'Cập nhật quy định khách ra vào', N'Khách đến thăm cần đăng ký CCCD/hộ chiếu tại quầy lễ tân.', N'General', N'Low'),
            (N'Vệ sinh bể nước sinh hoạt', N'Tòa nhà sẽ vệ sinh bể nước từ 22:00 đến 04:00 theo từng block.', N'Maintenance', N'High')
    ) AS v(Title, Message, NotificationType, Priority);

    ;WITH ResidentNotifications AS
    (
        SELECT TOP (30) r.UserID, r.ResidentID, r.FullName, a.ApartmentCode,
               ROW_NUMBER() OVER (ORDER BY r.ResidentID) AS RowNo
        FROM dbo.Residents r
        INNER JOIN dbo.Apartments a ON a.ApartmentID = r.ApartmentID
        WHERE r.UserID IS NOT NULL
        ORDER BY r.ResidentID
    )
    INSERT INTO dbo.Notifications
        (UserID, ResidentID, Title, Subject, Message, Body, NotificationType, Priority, IsRead, ReadAt, Status, SentDate, CreatedAt, UpdatedAt)
    SELECT UserID,
           ResidentID,
           CONCAT(N'Thông báo căn ', ApartmentCode),
           CONCAT(N'Thông báo căn ', ApartmentCode),
           CONCAT(N'Xin chào ', FullName, N', ban quản lý đã cập nhật hóa đơn và thông tin dịch vụ cho căn ', ApartmentCode, N'.'),
           CONCAT(N'Xin chào ', FullName, N', ban quản lý đã cập nhật hóa đơn và thông tin dịch vụ cho căn ', ApartmentCode, N'.'),
           CASE RowNo % 3 WHEN 0 THEN N'Payment' WHEN 1 THEN N'Maintenance' ELSE N'General' END,
           CASE RowNo % 4 WHEN 0 THEN N'High' WHEN 1 THEN N'Medium' ELSE N'Low' END,
           CASE WHEN RowNo % 5 = 0 THEN 1 ELSE 0 END,
           CASE WHEN RowNo % 5 = 0 THEN DATEADD(HOUR, -RowNo, @Now) ELSE NULL END,
           N'Sent',
           DATEADD(HOUR, -RowNo, @Now),
           @Now,
           @Now
    FROM ResidentNotifications;

    INSERT INTO dbo.AuditLogs
        (UserID, Action, EntityName, EntityID, OldValue, NewValue, [Timestamp], Description, IPAddress)
    VALUES
        (@SuperAdminUserID, N'ResetDemoDatabase', N'Database', NULL, NULL, N'05_SeedDemoData.sql', @Now, N'Reset và seed database demo chung cư 200 cư dân', @HostName),
        (@ManagerUserID, N'ApproveVisitor', N'Visitors', NULL, N'Pending', N'Approved', DATEADD(MINUTE, -35, @Now), N'Duyệt khách ra vào demo', @HostName),
        (@ManagerUserID, N'UpdateComplaint', N'Complaints', NULL, N'New', N'InProgress', DATEADD(MINUTE, -20, @Now), N'Cập nhật trạng thái phản ánh demo', @HostName),
        (@ManagerUserID, N'ConfirmPayment', N'Payments', NULL, N'Pending', N'Confirmed', DATEADD(MINUTE, -15, @Now), N'Xác nhận thanh toán demo', @HostName);

    UPDATE dbo.SystemConfig
    SET ConfigValue = N'1',
        Description = N'Dữ liệu mẫu đã được khởi tạo',
        UpdatedAt = @Now,
        UpdatedBy = @SuperAdminUserID
    WHERE ConfigKey = N'IsSeeded';

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO dbo.SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES (N'IsSeeded', N'1', N'Dữ liệu mẫu đã được khởi tạo', @Now, @SuperAdminUserID);
    END;

    MERGE dbo.SystemConfig AS target
    USING
    (
        VALUES
            (N'DemoSeedVersion', N'2026.05.200-residents', N'Phiên bản seed demo chung cư thật-like'),
            (N'DemoResidentCount', N'200', N'Số cư dân active được seed trong bộ demo'),
            (N'DemoSeededAt', CONVERT(NVARCHAR(30), @Now, 126), N'Thời điểm seed demo gần nhất'),
            (N'ApartmentNameFormat', N'{building} / {block} / {code}', N'Định dạng hiển thị căn hộ')
    ) AS source(ConfigKey, ConfigValue, Description)
    ON target.ConfigKey = source.ConfigKey
    WHEN MATCHED THEN
        UPDATE SET ConfigValue = source.ConfigValue,
                   Description = source.Description,
                   UpdatedAt = @Now,
                   UpdatedBy = @SuperAdminUserID
    WHEN NOT MATCHED THEN
        INSERT (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
        VALUES (source.ConfigKey, source.ConfigValue, source.Description, @Now, @SuperAdminUserID);

    DECLARE @ActiveResidentCount INT;
    SELECT @ActiveResidentCount = COUNT(*) FROM dbo.Residents WHERE Status = N'Active';
    IF @ActiveResidentCount <> 200
    BEGIN
        THROW 51003, 'Demo seed must create exactly 200 active residents.', 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.Residents r LEFT JOIN dbo.Apartments a ON a.ApartmentID = r.ApartmentID WHERE a.ApartmentID IS NULL)
        THROW 51004, 'Resident orphan rows detected.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Invoices i LEFT JOIN dbo.Apartments a ON a.ApartmentID = i.ApartmentID WHERE a.ApartmentID IS NULL)
        THROW 51005, 'Invoice orphan rows detected.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Payments p LEFT JOIN dbo.Invoices i ON i.InvoiceID = p.InvoiceID WHERE i.InvoiceID IS NULL)
        THROW 51006, 'Payment orphan rows detected.', 1;

    IF EXISTS (SELECT 1 FROM dbo.MaintenanceSchedules m LEFT JOIN dbo.Assets a ON a.AssetID = m.AssetID WHERE a.AssetID IS NULL)
        THROW 51007, 'Asset maintenance orphan rows detected.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Invoices WHERE RemainingAmount <> CASE WHEN TotalAmount - PaidAmount < 0 THEN 0 ELSE TotalAmount - PaidAmount END)
        THROW 51008, 'Invoice remaining amount mismatch detected.', 1;

    COMMIT TRANSACTION;

    PRINT N'========== REALISTIC DEMO DATA SEEDED SUCCESSFULLY: 200 ACTIVE RESIDENTS ==========';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT N'ERROR DURING DEMO DATA SEEDING:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;
GO
