USE ApartmentManagerDB;
GO

SET NOCOUNT ON;
GO

DECLARE @Now DATETIME2(0) = GETDATE();

PRINT N'Extending building, block, floor and apartment demo data...';

IF EXISTS (SELECT 1 FROM dbo.Buildings WHERE BuildingName = N'Tòa Nhà A')
   AND NOT EXISTS (SELECT 1 FROM dbo.Buildings WHERE BuildingName = N'Tòa A')
BEGIN
    UPDATE dbo.Buildings
    SET BuildingName = N'Tòa A',
        Address = N'01 Nguyễn Hữu Cảnh, Bình Thạnh, TP.HCM',
        Description = N'Tòa căn hộ A - khu chính',
        UpdatedAt = @Now
    WHERE BuildingName = N'Tòa Nhà A';
END;

DECLARE @BuildingSeed TABLE
(
    BuildingName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL
);

INSERT INTO @BuildingSeed (BuildingName, Address, Description)
VALUES
    (N'Tòa A', N'01 Nguyễn Hữu Cảnh, Bình Thạnh, TP.HCM', N'Tòa căn hộ A - khu chính'),
    (N'Tòa B', N'03 Nguyễn Hữu Cảnh, Bình Thạnh, TP.HCM', N'Tòa căn hộ B - khu gia đình'),
    (N'Tòa C', N'05 Nguyễn Hữu Cảnh, Bình Thạnh, TP.HCM', N'Tòa căn hộ C - khu dịch vụ');

INSERT INTO dbo.Buildings (BuildingName, Address, Description, CreatedAt, UpdatedAt)
SELECT s.BuildingName, s.Address, s.Description, @Now, @Now
FROM @BuildingSeed s
WHERE NOT EXISTS (SELECT 1 FROM dbo.Buildings b WHERE b.BuildingName = s.BuildingName);

DECLARE @BlockFloorSeed TABLE
(
    BuildingName NVARCHAR(100) NOT NULL,
    BlockName NVARCHAR(100) NOT NULL,
    MaxFloor INT NOT NULL,
    CodePrefix NVARCHAR(4) NOT NULL,
    PRIMARY KEY (BuildingName, BlockName)
);

INSERT INTO @BlockFloorSeed (BuildingName, BlockName, MaxFloor, CodePrefix)
VALUES
    (N'Tòa A', N'Block A', 12, N'A'),
    (N'Tòa A', N'Block B', 12, N'B'),
    (N'Tòa A', N'Block C', 12, N'C'),
    (N'Tòa B', N'Block A', 10, N'D'),
    (N'Tòa B', N'Block B', 10, N'E'),
    (N'Tòa C', N'Block A', 8,  N'F');

INSERT INTO dbo.Blocks (BlockName, BuildingID, CreatedAt, UpdatedAt)
SELECT bfs.BlockName, b.BuildingID, @Now, @Now
FROM @BlockFloorSeed bfs
INNER JOIN dbo.Buildings b ON b.BuildingName = bfs.BuildingName
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Blocks bl
    WHERE bl.BuildingID = b.BuildingID
      AND bl.BlockName = bfs.BlockName
);

DECLARE @SeedBuildingName NVARCHAR(100);
DECLARE @SeedBlockName NVARCHAR(100);
DECLARE @SeedMaxFloor INT;
DECLARE @SeedPrefix NVARCHAR(4);
DECLARE @SeedBlockID INT;
DECLARE @SeedFloorNumber INT;
DECLARE @SeedFloorID INT;
DECLARE @SeedUnitNumber INT;
DECLARE @SeedApartmentCode NVARCHAR(20);
DECLARE @SeedApartmentType NVARCHAR(100);
DECLARE @SeedArea DECIMAL(10,2);
DECLARE @SeedMaxResidents INT;
DECLARE @SeedStatus NVARCHAR(20);

DECLARE block_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT BuildingName, BlockName, MaxFloor, CodePrefix
    FROM @BlockFloorSeed
    ORDER BY BuildingName, BlockName;

OPEN block_cursor;
FETCH NEXT FROM block_cursor INTO @SeedBuildingName, @SeedBlockName, @SeedMaxFloor, @SeedPrefix;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @SeedBlockID = bl.BlockID
    FROM dbo.Blocks bl
    INNER JOIN dbo.Buildings b ON b.BuildingID = bl.BuildingID
    WHERE b.BuildingName = @SeedBuildingName
      AND bl.BlockName = @SeedBlockName;

    SET @SeedFloorNumber = 1;
    WHILE @SeedFloorNumber <= @SeedMaxFloor
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Floors WHERE BlockID = @SeedBlockID AND FloorNumber = @SeedFloorNumber)
        BEGIN
            INSERT INTO dbo.Floors (FloorNumber, BlockID, CreatedAt, UpdatedAt)
            VALUES (@SeedFloorNumber, @SeedBlockID, @Now, @Now);
        END;

        SELECT @SeedFloorID = FloorID
        FROM dbo.Floors
        WHERE BlockID = @SeedBlockID
          AND FloorNumber = @SeedFloorNumber;

        SET @SeedUnitNumber = 1;
        WHILE @SeedUnitNumber <= 5
        BEGIN
            SET @SeedApartmentCode = CONCAT(
                @SeedPrefix,
                N'-',
                RIGHT(CONCAT(N'0', CONVERT(NVARCHAR(2), @SeedFloorNumber)), 2),
                RIGHT(CONCAT(N'0', CONVERT(NVARCHAR(2), @SeedUnitNumber)), 2)
            );

            SET @SeedApartmentType = CASE @SeedUnitNumber
                WHEN 1 THEN N'1BR'
                WHEN 2 THEN N'2BR'
                WHEN 3 THEN N'2BR'
                WHEN 4 THEN N'3BR'
                ELSE N'3BR'
            END;

            SET @SeedArea = CASE @SeedUnitNumber
                WHEN 1 THEN 54.20
                WHEN 2 THEN 68.50
                WHEN 3 THEN 76.30
                WHEN 4 THEN 89.10
                ELSE 92.00
            END;

            SET @SeedMaxResidents = CASE @SeedUnitNumber
                WHEN 1 THEN 2
                WHEN 2 THEN 4
                WHEN 3 THEN 4
                ELSE 6
            END;

            SET @SeedStatus = CASE
                WHEN @SeedUnitNumber = 3 AND @SeedFloorNumber % 7 = 0 THEN N'Locked'
                WHEN @SeedUnitNumber = 4 AND @SeedFloorNumber % 5 = 0 THEN N'Maintenance'
                WHEN (@SeedFloorNumber + @SeedUnitNumber) % 4 = 0 THEN N'Occupied'
                ELSE N'Empty'
            END;

            IF NOT EXISTS (SELECT 1 FROM dbo.Apartments WHERE ApartmentCode = @SeedApartmentCode)
            BEGIN
                INSERT INTO dbo.Apartments
                    (ApartmentCode, FloorID, Area, ApartmentType, Status, MaxResidents, Note, CreatedAt, UpdatedAt)
                VALUES
                    (@SeedApartmentCode, @SeedFloorID, @SeedArea, @SeedApartmentType, @SeedStatus, @SeedMaxResidents,
                     N'Dữ liệu demo căn hộ theo sơ đồ tòa/block/tầng', @Now, @Now);
            END;

            SET @SeedUnitNumber += 1;
        END;

        SET @SeedFloorNumber += 1;
    END;

    FETCH NEXT FROM block_cursor INTO @SeedBuildingName, @SeedBlockName, @SeedMaxFloor, @SeedPrefix;
END;

CLOSE block_cursor;
DEALLOCATE block_cursor;
GO

DECLARE @Now DATETIME2(0) = GETDATE();
DECLARE @ResidentRoleID INT;
DECLARE @SuperAdminUserID INT;
DECLARE @ManagerUserID INT;
DECLARE @DefaultResidentPassword NVARCHAR(255);

SELECT @ResidentRoleID = RoleID FROM dbo.Roles WHERE RoleName = N'Resident';
SELECT @SuperAdminUserID = UserID FROM dbo.Users WHERE Username = N'superadmin';
SELECT @ManagerUserID = UserID FROM dbo.Users WHERE Username = N'manager1';
SELECT @DefaultResidentPassword = PasswordHash FROM dbo.Users WHERE Username = N'resident1';

PRINT N'Adding richer fee types...';

DECLARE @MoreFeeTypes TABLE
(
    FeeTypeName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    UnitOfMeasurement NVARCHAR(50) NOT NULL
);

INSERT INTO @MoreFeeTypes (FeeTypeName, Description, UnitOfMeasurement)
VALUES
    (N'Phí Điện', N'Tiền điện sinh hoạt theo đồng hồ căn hộ', N'kWh'),
    (N'Phí Nước', N'Tiền nước sinh hoạt theo đồng hồ căn hộ', N'm3'),
    (N'Phí Bảo Trì', N'Phí bảo trì hạ tầng và thiết bị chung', N'VND'),
    (N'Phí Internet', N'Gói internet nội khu theo tháng', N'VND');

INSERT INTO dbo.FeeTypes (FeeTypeName, Description, UnitOfMeasurement, Status, CreatedAt, UpdatedAt)
SELECT s.FeeTypeName, s.Description, s.UnitOfMeasurement, N'Active', @Now, @Now
FROM @MoreFeeTypes s
WHERE NOT EXISTS (SELECT 1 FROM dbo.FeeTypes f WHERE f.FeeTypeName = s.FeeTypeName);

PRINT N'Adding resident accounts and resident profiles...';

DECLARE @ResidentSeed TABLE
(
    Username NVARCHAR(50) NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    CCCD NVARCHAR(20) NOT NULL,
    DOB DATE NOT NULL,
    Gender NVARCHAR(10) NOT NULL,
    AddressRegistration NVARCHAR(255) NOT NULL,
    ApartmentCode NVARCHAR(20) NOT NULL,
    RelationshipWithOwner NVARCHAR(50) NOT NULL,
    ResidentStatus NVARCHAR(20) NOT NULL,
    MoveInDate DATETIME2(0) NOT NULL,
    Note NVARCHAR(MAX) NOT NULL
);

INSERT INTO @ResidentSeed
    (Username, FullName, Phone, Email, CCCD, DOB, Gender, AddressRegistration, ApartmentCode, RelationshipWithOwner, ResidentStatus, MoveInDate, Note)
VALUES
    (N'resident2',  N'Trần Thị Bình',      N'0912345678', N'resident2@system.local',   N'079190000002', '1990-02-14', N'Nữ',  N'Quận 3, TP.HCM',       N'A-1205', N'Chủ hộ',      N'Đang ở',    DATEADD(MONTH, -18, @Now), N'Chủ hộ căn góc hướng Đông Nam'),
    (N'resident03', N'Lê Văn Cường',       N'0903456789', N'cuong.le@chungcu.local',   N'079185000003', '1985-07-21', N'Nam', N'Thủ Đức, TP.HCM',      N'A-1204', N'Thành viên',  N'Đang ở',    DATEADD(MONTH, -17, @Now), N'Sống cùng gia đình'),
    (N'resident04', N'Phạm Thị Dung',      N'0934567890', N'dung.pham@chungcu.local',  N'079192000004', '1992-11-02', N'Nữ',  N'Quận 7, TP.HCM',       N'A-1102', N'Chủ hộ',      N'Đang ở',    DATEADD(MONTH, -16, @Now), N'Đã đăng ký 1 ô tô'),
    (N'resident05', N'Hoàng Minh Đức',     N'0945678901', N'duc.hoang@chungcu.local',  N'079188000005', '1988-04-10', N'Nam', N'Bình Thạnh, TP.HCM',   N'A-1001', N'Khách thuê',  N'Đang thuê', DATEADD(MONTH, -12, @Now), N'Hợp đồng thuê 12 tháng'),
    (N'resident06', N'Nguyễn Thị Hoa',     N'0956789012', N'hoa.nguyen@chungcu.local', N'079195000006', '1995-08-25', N'Nữ',  N'Gò Vấp, TP.HCM',       N'A-0903', N'Chủ hộ',      N'Đang ở',    DATEADD(MONTH, -14, @Now), N'Đăng ký khách thường xuyên'),
    (N'resident07', N'Đặng Văn Tùng',      N'0967890123', N'tung.dang@chungcu.local',  N'079186000007', '1986-12-18', N'Nam', N'Quận 10, TP.HCM',      N'A-0802', N'Chủ hộ',      N'Đang ở',    DATEADD(MONTH, -20, @Now), N'Có 2 xe máy'),
    (N'resident08', N'Võ Thị Lan',         N'0978901234', N'lan.vo@chungcu.local',     N'079193000008', '1993-03-30', N'Nữ',  N'Tân Bình, TP.HCM',     N'A-0605', N'Chủ hộ',      N'Đang ở',    DATEADD(MONTH, -9, @Now),  N'Cư dân mới chuyển vào'),
    (N'resident09', N'Bùi Quốc Huy',       N'0989012345', N'huy.bui@chungcu.local',    N'079184000009', '1984-05-19', N'Nam', N'Quận 5, TP.HCM',       N'A-0502', N'Khách thuê',  N'Đang thuê', DATEADD(MONTH, -6, @Now),  N'Hợp đồng đang hiệu lực'),
    (N'resident10', N'Phan Ngọc Anh',      N'0890123456', N'anh.phan@chungcu.local',   N'079199000010', '1999-09-07', N'Nữ',  N'Nhà Bè, TP.HCM',       N'B-0701', N'Thành viên',  N'Đang ở',    DATEADD(MONTH, -5, @Now),  N'Liên hệ qua email'),
    (N'resident11', N'Đỗ Minh Khang',      N'0887654321', N'khang.do@chungcu.local',   N'079187000011', '1987-01-12', N'Nam', N'Quận 2, TP.HCM',       N'B-0504', N'Chủ hộ',      N'Đang ở',    DATEADD(MONTH, -24, @Now), N'Cư dân lâu năm'),
    (N'resident12', N'Ngô Thanh Mai',      N'0876543210', N'mai.ngo@chungcu.local',    N'079191000012', '1991-10-05', N'Nữ',  N'Quận 4, TP.HCM',       N'C-0301', N'Chủ hộ',      N'Đang ở',    DATEADD(MONTH, -11, @Now), N'Có đăng ký thú cưng'),
    (N'resident13', N'Trương Gia Bảo',     N'0865432109', N'bao.truong@chungcu.local', N'079189000013', '1989-06-23', N'Nam', N'Quận 8, TP.HCM',       N'C-1005', N'Khách thuê',  N'Đang thuê', DATEADD(MONTH, -4, @Now),  N'Khách thuê dài hạn'),
    (N'resident14', N'Vũ Mỹ Linh',         N'0854321098', N'linh.vu@chungcu.local',    N'079196000014', '1996-12-09', N'Nữ',  N'Phú Nhuận, TP.HCM',    N'D-0702', N'Chủ hộ',      N'Đang ở',    DATEADD(MONTH, -8, @Now),  N'Tòa B - Block A'),
    (N'resident15', N'Phạm Quang Minh',    N'0843210987', N'minh.pham@chungcu.local',  N'079183000015', '1983-04-01', N'Nam', N'Quận 6, TP.HCM',       N'E-0405', N'Chủ hộ',      N'Đang ở',    DATEADD(MONTH, -15, @Now), N'Tòa B - Block B'),
    (N'resident16', N'Lý Thu Hằng',        N'0832109876', N'hang.ly@chungcu.local',    N'079197000016', '1997-07-17', N'Nữ',  N'Bình Chánh, TP.HCM',   N'F-0301', N'Khách thuê',  N'Đang thuê', DATEADD(MONTH, -3, @Now),  N'Tòa C - khu dịch vụ');

DECLARE @Username NVARCHAR(50);
DECLARE @FullName NVARCHAR(150);
DECLARE @Phone NVARCHAR(20);
DECLARE @Email NVARCHAR(150);
DECLARE @CCCD NVARCHAR(20);
DECLARE @DOB DATE;
DECLARE @Gender NVARCHAR(10);
DECLARE @Address NVARCHAR(255);
DECLARE @ApartmentCode NVARCHAR(20);
DECLARE @Relationship NVARCHAR(50);
DECLARE @ResidentStatus NVARCHAR(20);
DECLARE @MoveInDate DATETIME2(0);
DECLARE @Note NVARCHAR(MAX);
DECLARE @UserID INT;
DECLARE @ApartmentID INT;
DECLARE @ResidentID INT;

DECLARE resident_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT Username, FullName, Phone, Email, CCCD, DOB, Gender, AddressRegistration,
           ApartmentCode, RelationshipWithOwner, ResidentStatus, MoveInDate, Note
    FROM @ResidentSeed
    ORDER BY ApartmentCode, Username;

OPEN resident_cursor;
FETCH NEXT FROM resident_cursor INTO @Username, @FullName, @Phone, @Email, @CCCD, @DOB, @Gender, @Address,
    @ApartmentCode, @Relationship, @ResidentStatus, @MoveInDate, @Note;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username)
    BEGIN
        INSERT INTO dbo.Users
            (Username, PasswordHash, FullName, Email, Phone, RoleID, Status, IsApproved, ApprovedAt, ApprovedBy, CreatedAt, UpdatedAt)
        VALUES
            (@Username, @DefaultResidentPassword, @FullName, @Email, @Phone, @ResidentRoleID, N'Active', 1, @Now, @SuperAdminUserID, @Now, @Now);
    END
    ELSE
    BEGIN
        UPDATE dbo.Users
        SET FullName = @FullName,
            Email = @Email,
            Phone = @Phone,
            Status = N'Active',
            IsApproved = 1,
            ApprovedAt = COALESCE(ApprovedAt, @Now),
            ApprovedBy = COALESCE(ApprovedBy, @SuperAdminUserID),
            UpdatedAt = @Now
        WHERE Username = @Username;
    END;

    SELECT @UserID = UserID FROM dbo.Users WHERE Username = @Username;
    SELECT @ApartmentID = ApartmentID FROM dbo.Apartments WHERE ApartmentCode = @ApartmentCode;
    SELECT @ResidentID = ResidentID FROM dbo.Residents WHERE CCCD = @CCCD;

    IF @ApartmentID IS NOT NULL AND @ResidentID IS NULL
    BEGIN
        INSERT INTO dbo.Residents
            (UserID, FullName, Phone, Email, CCCD, DOB, Gender, AddressRegistration,
             ApartmentID, RelationshipWithOwner, Status, ResidentStatus, MoveInDate, StartDate, Note, CreatedAt, UpdatedAt)
        VALUES
            (@UserID, @FullName, @Phone, @Email, @CCCD, @DOB, @Gender, @Address,
             @ApartmentID, @Relationship, N'Active', @ResidentStatus, @MoveInDate, @MoveInDate, @Note, @Now, @Now);

        SELECT @ResidentID = SCOPE_IDENTITY();
    END;

    IF @ApartmentID IS NOT NULL
    BEGIN
        UPDATE dbo.Apartments
        SET Status = CASE WHEN @ResidentStatus = N'Đang thuê' THEN N'Renting' ELSE N'Occupied' END,
            Note = @Note,
            UpdatedAt = @Now
        WHERE ApartmentID = @ApartmentID;
    END;

    IF @ApartmentID IS NOT NULL
       AND @ResidentID IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Contracts WHERE ApartmentID = @ApartmentID AND ResidentID = @ResidentID)
    BEGIN
        INSERT INTO dbo.Contracts
            (ApartmentID, ResidentID, StartDate, EndDate, RentAmount, DepositAmount, Status, Note, CreatedAt, UpdatedAt)
        VALUES
            (@ApartmentID, @ResidentID, CONVERT(DATE, @MoveInDate), CONVERT(DATE, DATEADD(YEAR, 1, @MoveInDate)),
             CASE WHEN @ResidentStatus = N'Đang thuê' THEN 12500000 ELSE 0 END,
             CASE WHEN @ResidentStatus = N'Đang thuê' THEN 25000000 ELSE 0 END,
             N'Active', CONCAT(N'Hợp đồng demo cho căn hộ ', @ApartmentCode), @Now, @Now);
    END;

    FETCH NEXT FROM resident_cursor INTO @Username, @FullName, @Phone, @Email, @CCCD, @DOB, @Gender, @Address,
        @ApartmentCode, @Relationship, @ResidentStatus, @MoveInDate, @Note;
END;

CLOSE resident_cursor;
DEALLOCATE resident_cursor;

PRINT N'Backfilling residents for occupied / renting apartments without active residents...';

DECLARE @AutoResidentSeed TABLE
(
    ApartmentID INT NOT NULL,
    ApartmentCode NVARCHAR(20) NOT NULL,
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

;WITH CandidateApartments AS
(
    SELECT a.ApartmentID,
           a.ApartmentCode,
           a.Status,
           a.MaxResidents,
           b.BuildingName,
           bl.BlockName,
           f.FloorNumber,
           ROW_NUMBER() OVER (ORDER BY a.ApartmentCode) AS Seq
    FROM dbo.Apartments a
    INNER JOIN dbo.Floors f ON a.FloorID = f.FloorID
    INNER JOIN dbo.Blocks bl ON f.BlockID = bl.BlockID
    INNER JOIN dbo.Buildings b ON bl.BuildingID = b.BuildingID
    WHERE a.Status IN (N'Occupied', N'Renting')
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.Residents r
          WHERE r.ApartmentID = a.ApartmentID
            AND r.Status = N'Active'
      )
)
INSERT INTO @AutoResidentSeed
    (ApartmentID, ApartmentCode, Username, FullName, Phone, Email, CCCD, DOB, Gender, AddressRegistration,
     RelationshipWithOwner, ResidentStatus, MoveInDate, Note)
SELECT c.ApartmentID,
       c.ApartmentCode,
       CONCAT(N'resident_', LOWER(REPLACE(c.ApartmentCode, N'-', N''))),
       CASE (c.Seq - 1) % 24
           WHEN 0 THEN N'Nguyễn Minh An'
           WHEN 1 THEN N'Trần Thu Hà'
           WHEN 2 THEN N'Lê Quốc Bảo'
           WHEN 3 THEN N'Phạm Gia Hân'
           WHEN 4 THEN N'Hoàng Đức Long'
           WHEN 5 THEN N'Võ Khánh Linh'
           WHEN 6 THEN N'Đặng Minh Khoa'
           WHEN 7 THEN N'Bùi Thanh Mai'
           WHEN 8 THEN N'Đỗ Gia Bảo'
           WHEN 9 THEN N'Ngô Phương Anh'
           WHEN 10 THEN N'Phan Anh Tú'
           WHEN 11 THEN N'Dương Hải Yến'
           WHEN 12 THEN N'Vũ Quang Huy'
           WHEN 13 THEN N'Lý Mỹ Duyên'
           WHEN 14 THEN N'Hồ Nhật Nam'
           WHEN 15 THEN N'Tạ Bảo Trâm'
           WHEN 16 THEN N'Cao Minh Khôi'
           WHEN 17 THEN N'Chu Ngọc Lan'
           WHEN 18 THEN N'Trương Phúc Thịnh'
           WHEN 19 THEN N'Đinh Hoài Thương'
           WHEN 20 THEN N'Nguyễn Khánh Vy'
           WHEN 21 THEN N'Trần Đức Anh'
           WHEN 22 THEN N'Lâm Gia Bảo'
           ELSE N'Phùng Hải Đăng'
       END,
       CONCAT(N'0907', RIGHT(CONCAT(N'000000', CONVERT(NVARCHAR(6), c.Seq)), 6)),
       CONCAT(N'resident.', LOWER(REPLACE(c.ApartmentCode, N'-', N'')), N'@system.local'),
       CONCAT(N'0794', RIGHT(CONCAT(N'00000000', CONVERT(NVARCHAR(8), c.Seq)), 8)),
       DATEADD(YEAR, -(24 + (c.Seq % 18)), CONVERT(DATE, @Now)),
       CASE WHEN c.Seq % 2 = 0 THEN N'Nam' ELSE N'Nữ' END,
       CONCAT(c.BuildingName, N' / ', c.BlockName, N' / Tầng ', RIGHT(CONCAT(N'0', CONVERT(NVARCHAR(2), c.FloorNumber)), 2)),
       CASE WHEN c.Status = N'Renting' THEN N'Khách thuê' ELSE N'Chủ hộ' END,
       CASE WHEN c.Status = N'Renting' THEN N'Đang thuê' ELSE N'Đang ở' END,
       DATEADD(MONTH, -(2 + (c.Seq % 16)), @Now),
       CONCAT(N'Cư dân tự động sinh cho căn ', c.ApartmentCode, N' đang ', CASE WHEN c.Status = N'Renting' THEN N'cho thuê' ELSE N'sử dụng' END)
FROM CandidateApartments c;

INSERT INTO dbo.Users
    (Username, PasswordHash, FullName, Email, Phone, RoleID, Status, IsApproved, ApprovedAt, ApprovedBy, CreatedAt, UpdatedAt)
SELECT s.Username, @DefaultResidentPassword, s.FullName, s.Email, s.Phone,
       @ResidentRoleID, N'Active', 1, @Now, @SuperAdminUserID, @Now, @Now
FROM @AutoResidentSeed s
WHERE NOT EXISTS (SELECT 1 FROM dbo.Users u WHERE u.Username = s.Username);

INSERT INTO dbo.Residents
    (UserID, FullName, Phone, Email, CCCD, DOB, Gender, AddressRegistration,
     ApartmentID, RelationshipWithOwner, Status, ResidentStatus, MoveInDate, StartDate, Note, CreatedAt, UpdatedAt)
SELECT u.UserID,
       s.FullName,
       s.Phone,
       s.Email,
       s.CCCD,
       s.DOB,
       s.Gender,
       s.AddressRegistration,
       s.ApartmentID,
       s.RelationshipWithOwner,
       N'Active',
       s.ResidentStatus,
       s.MoveInDate,
       s.MoveInDate,
       s.Note,
       @Now,
       @Now
FROM @AutoResidentSeed s
INNER JOIN dbo.Users u ON u.Username = s.Username
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Residents r
    WHERE r.ApartmentID = s.ApartmentID
      AND r.Status = N'Active'
);

INSERT INTO dbo.Contracts
    (ApartmentID, ResidentID, StartDate, EndDate, RentAmount, DepositAmount, Status, Note, CreatedAt, UpdatedAt)
SELECT s.ApartmentID,
       r.ResidentID,
       CONVERT(DATE, s.MoveInDate),
       CONVERT(DATE, DATEADD(YEAR, 1, s.MoveInDate)),
       CASE WHEN s.ResidentStatus = N'Đang thuê' THEN 9800000 ELSE 0 END,
       CASE WHEN s.ResidentStatus = N'Đang thuê' THEN 19600000 ELSE 0 END,
       N'Active',
       CONCAT(N'Hợp đồng tự động cho căn ', s.ApartmentCode),
       @Now,
       @Now
FROM @AutoResidentSeed s
INNER JOIN dbo.Residents r ON r.CCCD = s.CCCD
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Contracts c
    WHERE c.ApartmentID = s.ApartmentID
      AND c.ResidentID = r.ResidentID
);
GO

DECLARE @Now DATETIME2(0) = GETDATE();
DECLARE @ManagerUserID INT;
DECLARE @SuperAdminUserID INT;
SELECT @ManagerUserID = UserID FROM dbo.Users WHERE Username = N'manager1';
SELECT @SuperAdminUserID = UserID FROM dbo.Users WHERE Username = N'superadmin';

PRINT N'Adding demo complaints / feedback...';

DECLARE @ComplaintSeed TABLE
(
    CCCD NVARCHAR(20) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    ComplaintType NVARCHAR(50) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Priority NVARCHAR(20) NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    ResolutionNotes NVARCHAR(MAX) NULL,
    SatisfactionRating INT NULL,
    DaysAgo INT NOT NULL
);

INSERT INTO @ComplaintSeed
    (CCCD, Category, ComplaintType, Title, Description, Priority, Status, ResolutionNotes, SatisfactionRating, DaysAgo)
VALUES
    (N'079190000002', N'Elevator',    N'Maintenance', N'Thang máy tòa A bị kẹt',          N'Thang máy block A dừng giữa tầng 8 và 9 khoảng 5 phút.',              N'High',   N'InProgress', N'Kỹ thuật đã kiểm tra cảm biến cửa, đang đặt lịch thay linh kiện.', NULL, 1),
    (N'079185000003', N'Water',       N'Maintenance', N'Rò rỉ nước tại ban công',         N'Nước chảy từ trần ban công sau mưa lớn.',                              N'Medium', N'New',        NULL, NULL, 2),
    (N'079192000004', N'Electrical',  N'Maintenance', N'Đèn hành lang không sáng',        N'Đèn hành lang tầng 11 bị chập chờn vào buổi tối.',                      N'Medium', N'Resolved',   N'Đã thay bóng và kiểm tra aptomat tầng.', 5, 4),
    (N'079188000005', N'Noise',       N'General',     N'Tiếng ồn sau 22 giờ',             N'Căn bên cạnh thường xuyên kéo ghế và mở nhạc lớn sau 22 giờ.',           N'Low',    N'Closed',     N'Ban quản lý đã nhắc nhở cư dân liên quan.', 4, 6),
    (N'079195000006', N'Security',    N'Security',    N'Khách lạ đi vào tầng 9',          N'Có khách lạ đi vào khu căn hộ mà không đăng ký tại quầy lễ tân.',        N'High',   N'InProgress', N'Bảo vệ đang trích xuất camera.', NULL, 0),
    (N'079186000007', N'Cleaning',    N'General',     N'Rác tồn ở phòng rác tầng 8',      N'Phòng rác có mùi và chưa được dọn đúng lịch.',                          N'Medium', N'Resolved',   N'Đã điều phối nhân viên vệ sinh xử lý trong ngày.', 4, 5),
    (N'079193000008', N'Parking',     N'General',     N'Xe đỗ sai vị trí',                N'Một xe máy thường xuyên đỗ vào ô đã đăng ký của cư dân.',                N'Medium', N'New',        NULL, NULL, 1),
    (N'079184000009', N'Facility',    N'Maintenance', N'Cửa kính sảnh bị lỏng bản lề',    N'Cửa kính sảnh block A đóng mở khó và có tiếng kêu.',                     N'High',   N'InProgress', N'Đã báo nhà thầu nhôm kính kiểm tra.', NULL, 3),
    (N'079199000010', N'Garden',      N'General',     N'Cây xanh khô héo',                N'Mảng cây xanh gần lối vào block B thiếu nước.',                         N'Low',    N'Resolved',   N'Đã bổ sung lịch tưới tự động.', 5, 8),
    (N'079187000011', N'Payment',     N'Billing',     N'Sai số tiền phí gửi xe',          N'Hóa đơn tháng này tính thêm một xe máy không sử dụng.',                  N'Medium', N'Closed',     N'Đã điều chỉnh hóa đơn và gửi lại thông báo.', 5, 7),
    (N'079191000012', N'Internet',    N'Facility',    N'Wifi khu sinh hoạt chung yếu',    N'Tín hiệu wifi tại phòng sinh hoạt chung tầng 3 rất yếu.',                N'Low',    N'New',        NULL, NULL, 2),
    (N'079197000016', N'AccessCard',  N'Security',    N'Thẻ từ không mở được cửa tầng 3', N'Thẻ cư dân quét được thang máy nhưng không mở được cửa hành lang tầng.', N'Medium', N'Resolved',   N'Đã cấp lại quyền truy cập thẻ từ.', 4, 3);

INSERT INTO dbo.Complaints
    (ResidentID, ApartmentID, Category, ComplaintType, Title, Description, Priority, Status,
     AssignedToUserID, ResolutionNotes, SatisfactionRating, CreatedAt, UpdatedAt)
SELECT r.ResidentID,
       r.ApartmentID,
       s.Category,
       s.ComplaintType,
       s.Title,
       s.Description,
       s.Priority,
       s.Status,
       CASE WHEN s.Status IN (N'InProgress', N'Resolved', N'Closed') THEN @ManagerUserID ELSE NULL END,
       s.ResolutionNotes,
       s.SatisfactionRating,
       DATEADD(DAY, -s.DaysAgo, @Now),
       DATEADD(DAY, -CASE WHEN s.Status IN (N'Resolved', N'Closed') THEN s.DaysAgo - 1 ELSE 0 END, @Now)
FROM @ComplaintSeed s
INNER JOIN dbo.Residents r ON r.CCCD = s.CCCD
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Complaints c
    WHERE c.ResidentID = r.ResidentID
      AND c.Title = s.Title
);

PRINT N'Adding demo invoices and invoice details...';

DECLARE @FeeManagementID INT;
DECLARE @FeeParkingID INT;
DECLARE @FeeCleaningID INT;
DECLARE @FeeElectricID INT;
DECLARE @FeeWaterID INT;
SELECT @FeeManagementID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Quản Lý';
SELECT @FeeParkingID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Gửi Xe';
SELECT @FeeCleaningID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Vệ Sinh';
SELECT @FeeElectricID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Điện';
SELECT @FeeWaterID = FeeTypeID FROM dbo.FeeTypes WHERE FeeTypeName = N'Phí Nước';

DECLARE @InvoiceApartmentID INT;
DECLARE @InvoiceArea DECIMAL(10,2);
DECLARE @InvoiceMaxResidents INT;
DECLARE @InvoiceIndex INT = 0;
DECLARE @InvoiceID INT;
DECLARE @InvoiceTotal DECIMAL(18,2);
DECLARE @InvoicePaid DECIMAL(18,2);
DECLARE @InvoiceStatus NVARCHAR(20);
DECLARE @InvoiceMonth INT;
DECLARE @InvoiceYear INT;
DECLARE @InvoiceDue DATE;

DECLARE invoice_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT TOP (20) a.ApartmentID, a.Area, a.MaxResidents
    FROM dbo.Apartments a
    INNER JOIN dbo.Residents r ON r.ApartmentID = a.ApartmentID
    ORDER BY a.ApartmentID;

OPEN invoice_cursor;
FETCH NEXT FROM invoice_cursor INTO @InvoiceApartmentID, @InvoiceArea, @InvoiceMaxResidents;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @InvoiceIndex += 1;
    SET @InvoiceTotal = ROUND(@InvoiceArea * 12000, 0) + 180000 + (@InvoiceMaxResidents * 45000) + 350000 + 120000;
    SET @InvoiceStatus = CASE @InvoiceIndex % 4
        WHEN 0 THEN N'Paid'
        WHEN 1 THEN N'Unpaid'
        WHEN 2 THEN N'Partial'
        ELSE N'Overdue'
    END;
    SET @InvoicePaid = CASE @InvoiceStatus
        WHEN N'Paid' THEN @InvoiceTotal
        WHEN N'Partial' THEN ROUND(@InvoiceTotal * 0.45, 0)
        ELSE 0
    END;
    SET @InvoiceMonth = MONTH(@Now);
    SET @InvoiceYear = YEAR(@Now);
    SET @InvoiceDue = EOMONTH(@Now);

    IF NOT EXISTS (SELECT 1 FROM dbo.Invoices WHERE ApartmentID = @InvoiceApartmentID AND [Month] = @InvoiceMonth AND [Year] = @InvoiceYear)
    BEGIN
        INSERT INTO dbo.Invoices (ApartmentID, [Month], [Year], DueDate, PaymentStatus, TotalAmount, PaidAmount, Note, CreatedAt, UpdatedAt)
        VALUES (@InvoiceApartmentID, @InvoiceMonth, @InvoiceYear, @InvoiceDue, @InvoiceStatus, @InvoiceTotal, @InvoicePaid,
                N'Hóa đơn demo tháng hiện tại', @Now, @Now);
        SET @InvoiceID = SCOPE_IDENTITY();

        INSERT INTO dbo.InvoiceDetails (InvoiceID, FeeTypeID, Amount)
        VALUES
            (@InvoiceID, @FeeManagementID, ROUND(@InvoiceArea * 12000, 0)),
            (@InvoiceID, @FeeCleaningID, @InvoiceMaxResidents * 45000),
            (@InvoiceID, @FeeParkingID, 350000),
            (@InvoiceID, @FeeElectricID, 180000),
            (@InvoiceID, @FeeWaterID, 120000);
    END;

    SET @InvoiceMonth = MONTH(DATEADD(MONTH, -1, @Now));
    SET @InvoiceYear = YEAR(DATEADD(MONTH, -1, @Now));
    SET @InvoiceDue = EOMONTH(DATEADD(MONTH, -1, @Now));

    IF NOT EXISTS (SELECT 1 FROM dbo.Invoices WHERE ApartmentID = @InvoiceApartmentID AND [Month] = @InvoiceMonth AND [Year] = @InvoiceYear)
    BEGIN
        INSERT INTO dbo.Invoices (ApartmentID, [Month], [Year], DueDate, PaymentStatus, TotalAmount, PaidAmount, Note, CreatedAt, UpdatedAt)
        VALUES (@InvoiceApartmentID, @InvoiceMonth, @InvoiceYear, @InvoiceDue, N'Paid', @InvoiceTotal - 50000, @InvoiceTotal - 50000,
                N'Hóa đơn demo tháng trước', DATEADD(MONTH, -1, @Now), DATEADD(MONTH, -1, @Now));
        SET @InvoiceID = SCOPE_IDENTITY();

        INSERT INTO dbo.InvoiceDetails (InvoiceID, FeeTypeID, Amount)
        VALUES
            (@InvoiceID, @FeeManagementID, ROUND(@InvoiceArea * 12000, 0)),
            (@InvoiceID, @FeeCleaningID, @InvoiceMaxResidents * 45000),
            (@InvoiceID, @FeeParkingID, 300000),
            (@InvoiceID, @FeeElectricID, 150000),
            (@InvoiceID, @FeeWaterID, 70000);
    END;

    FETCH NEXT FROM invoice_cursor INTO @InvoiceApartmentID, @InvoiceArea, @InvoiceMaxResidents;
END;

CLOSE invoice_cursor;
DEALLOCATE invoice_cursor;

PRINT N'Adding demo vehicles...';

DECLARE @VehicleSeed TABLE
(
    CCCD NVARCHAR(20) NOT NULL,
    VehicleType NVARCHAR(50) NOT NULL,
    LicensePlate NVARCHAR(20) NOT NULL,
    Color NVARCHAR(50) NOT NULL,
    Brand NVARCHAR(100) NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    Note NVARCHAR(MAX) NOT NULL
);

INSERT INTO @VehicleSeed (CCCD, VehicleType, LicensePlate, Color, Brand, Status, Note)
VALUES
    (N'079190000002', N'Car',        N'51G-248.68', N'Trắng', N'Mazda',   N'Active',  N'MODEL=CX-5;YEAR=2022;NOTE=Ô tô gia đình'),
    (N'079190000002', N'Motorcycle', N'59X2-120.05', N'Đen',  N'Honda',   N'Active',  N'MODEL=SH Mode;YEAR=2023;NOTE=Xe máy cá nhân'),
    (N'079192000004', N'Motorcycle', N'59X3-110.02', N'Xanh', N'Yamaha',  N'Active',  N'MODEL=Grande;YEAR=2021;NOTE=Đăng ký giữ xe tháng'),
    (N'079188000005', N'Car',        N'51A-886.10', N'Đỏ',    N'Kia',     N'Pending', N'MODEL=Seltos;YEAR=2024;NOTE=Chờ duyệt thẻ xe'),
    (N'079186000007', N'Motorcycle', N'59X1-806.02', N'Bạc',  N'VinFast', N'Active',  N'MODEL=Feliz;YEAR=2022;NOTE=Xe điện'),
    (N'079184000009', N'Motorcycle', N'59X1-502.09', N'Đen',  N'Honda',   N'Active',  N'MODEL=Vision;YEAR=2020;NOTE=Khách thuê'),
    (N'079187000011', N'Car',        N'51H-050.04', N'Xám',   N'Toyota',  N'Active',  N'MODEL=Corolla Cross;YEAR=2023;NOTE=Ô B2-14'),
    (N'079197000016', N'Bicycle',    N'BIKE-F301',  N'Trắng', N'Giant',   N'Active',  N'MODEL=Escape 3;YEAR=2022;NOTE=Gửi khu xe đạp');

INSERT INTO dbo.Vehicles (ResidentID, VehicleType, LicensePlate, Color, Brand, Status, Note, CreatedAt, UpdatedAt)
SELECT r.ResidentID, s.VehicleType, s.LicensePlate, s.Color, s.Brand, s.Status, s.Note, @Now, @Now
FROM @VehicleSeed s
INNER JOIN dbo.Residents r ON r.CCCD = s.CCCD
WHERE NOT EXISTS (SELECT 1 FROM dbo.Vehicles v WHERE v.LicensePlate = s.LicensePlate);

PRINT N'Adding demo visitors...';

DECLARE @VisitorSeed TABLE
(
    CCCD NVARCHAR(20) NOT NULL,
    VisitorName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    IDNumber NVARCHAR(50) NOT NULL,
    Purpose NVARCHAR(255) NOT NULL,
    ArrivalOffsetHour INT NOT NULL,
    StayHours INT NULL,
    Status NVARCHAR(20) NOT NULL,
    Note NVARCHAR(MAX) NOT NULL
);

INSERT INTO @VisitorSeed
    (CCCD, VisitorName, Phone, Email, IDNumber, Purpose, ArrivalOffsetHour, StayHours, Status, Note)
VALUES
    (N'079190000002', N'Nguyễn Văn An',  N'0908123456', N'an.nguyen@example.com',   N'079200001001', N'Thăm gia đình',       -2,  NULL, N'Pending',  N'Khách thân'),
    (N'079192000004', N'Lê Hoàng Nam',   N'0908234567', N'nam.le@example.com',      N'079200001002', N'Sửa internet',        -4,  2,    N'Approved', N'Nhà thầu kỹ thuật'),
    (N'079195000006', N'Phạm Thu Trang', N'0908345678', N'trang.pham@example.com',  N'079200001003', N'Giao hàng',           -1,  NULL, N'Pending',  N'Shipper'),
    (N'079186000007', N'Trần Minh Quân', N'0908456789', N'quan.tran@example.com',   N'079200001004', N'Thăm bạn',            -8,  3,    N'CheckedOut', N'Đã rời khỏi tòa'),
    (N'079187000011', N'Vũ Hải Yến',     N'0908567890', N'yen.vu@example.com',      N'079200001005', N'Họp cư dân',          3,   NULL, N'Approved', N'Khách đăng ký trước'),
    (N'079197000016', N'Bùi Tấn Phát',   N'0908678901', N'phat.bui@example.com',    N'079200001006', N'Lắp thiết bị bếp',    5,   NULL, N'Pending',  N'Nhà cung cấp');

INSERT INTO dbo.Visitors
    (ResidentID, VisitorName, Phone, Email, IDNumber, Purpose, ArrivalTime, DepartureTime,
     Status, ApprovedByUserID, Note, CreatedAt, UpdatedAt)
SELECT r.ResidentID,
       s.VisitorName,
       s.Phone,
       s.Email,
       s.IDNumber,
       s.Purpose,
       DATEADD(HOUR, s.ArrivalOffsetHour, @Now),
       CASE WHEN s.StayHours IS NULL THEN NULL ELSE DATEADD(HOUR, s.ArrivalOffsetHour + s.StayHours, @Now) END,
       s.Status,
       CASE WHEN s.Status IN (N'Approved', N'CheckedOut') THEN @ManagerUserID ELSE NULL END,
       s.Note,
       @Now,
       @Now
FROM @VisitorSeed s
INNER JOIN dbo.Residents r ON r.CCCD = s.CCCD
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Visitors v
    WHERE v.ResidentID = r.ResidentID
      AND v.VisitorName = s.VisitorName
      AND CONVERT(DATE, v.ArrivalTime) = CONVERT(DATE, DATEADD(HOUR, s.ArrivalOffsetHour, @Now))
);

PRINT N'Adding demo notifications and audit logs...';

INSERT INTO dbo.Notifications
    (UserID, ResidentID, Title, Subject, Message, Body, NotificationType, Priority, IsRead, ReadAt, Status, SentDate, CreatedAt, UpdatedAt)
SELECT NULL, NULL, s.Title, s.Title, s.Message, s.Message, s.NotificationType, s.Priority, 0, NULL, N'Sent', @Now, @Now, @Now
FROM
(
    VALUES
        (N'Lịch bảo trì thang máy Block A', N'Thang máy Block A sẽ bảo trì từ 08:00 đến 12:00 ngày mai.', N'Maintenance', N'High'),
        (N'Thông báo thu phí tháng hiện tại', N'Quý cư dân vui lòng thanh toán các khoản phí trước ngày 25 hàng tháng.', N'Payment', N'Medium'),
        (N'Diễn tập PCCC định kỳ', N'Buổi diễn tập PCCC toàn khu sẽ diễn ra vào sáng thứ Bảy tuần này.', N'Security', N'Medium'),
        (N'Cập nhật quy định khách ra vào', N'Khách đến thăm cần đăng ký CCCD/hộ chiếu tại quầy lễ tân.', N'General', N'Low')
) AS s(Title, Message, NotificationType, Priority)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Notifications n WHERE n.Title = s.Title);

INSERT INTO dbo.AuditLogs
    (UserID, Action, EntityName, EntityID, OldValue, NewValue, [Timestamp], Description, IPAddress)
VALUES
    (@SuperAdminUserID, N'ImportDemoData', N'Database', NULL, NULL, N'05_SeedDemoData.sql', @Now, N'Khởi tạo bộ dữ liệu demo mở rộng', N'127.0.0.1'),
    (@ManagerUserID, N'ApproveVisitor', N'Visitors', NULL, N'Pending', N'Approved', DATEADD(MINUTE, -35, @Now), N'Duyệt khách ra vào demo', N'127.0.0.1'),
    (@ManagerUserID, N'UpdateComplaint', N'Complaints', NULL, N'New', N'InProgress', DATEADD(MINUTE, -20, @Now), N'Cập nhật trạng thái phản ánh demo', N'127.0.0.1');
GO
