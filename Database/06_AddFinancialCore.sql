USE ApartmentManagerDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Funds', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Funds
    (
        FundID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Funds PRIMARY KEY,
        FundName NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        InitialBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Funds_InitialBalance DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_Funds_IsActive DEFAULT 1,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Funds_CreatedAt DEFAULT GETDATE()
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Funds_FundName' AND object_id = OBJECT_ID(N'dbo.Funds'))
BEGIN
    CREATE UNIQUE INDEX UX_Funds_FundName ON dbo.Funds(FundName);
END;
GO

IF OBJECT_ID(N'dbo.PaymentAccounts', N'U') IS NULL
BEGIN
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
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PaymentAccounts_CreatedAt DEFAULT GETDATE()
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PaymentAccounts_AccountType')
BEGIN
    ALTER TABLE dbo.PaymentAccounts
    ADD CONSTRAINT CK_PaymentAccounts_AccountType
    CHECK (AccountType IN (N'Cash', N'Bank', N'QR', N'EWallet'));
END;
GO

IF OBJECT_ID(N'dbo.ExpenseCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExpenseCategories
    (
        ExpenseCategoryID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExpenseCategories PRIMARY KEY,
        CategoryName NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ExpenseCategories_IsActive DEFAULT 1,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ExpenseCategories_CreatedAt DEFAULT GETDATE()
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ExpenseCategories_CategoryName' AND object_id = OBJECT_ID(N'dbo.ExpenseCategories'))
BEGIN
    CREATE UNIQUE INDEX UX_ExpenseCategories_CategoryName ON dbo.ExpenseCategories(CategoryName);
END;
GO

IF OBJECT_ID(N'dbo.Vendors', N'U') IS NULL
BEGIN
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
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Vendors_VendorName' AND object_id = OBJECT_ID(N'dbo.Vendors'))
BEGIN
    CREATE UNIQUE INDEX UX_Vendors_VendorName ON dbo.Vendors(VendorName);
END;
GO

IF COL_LENGTH(N'dbo.Invoices', N'RemainingAmount') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
    ADD RemainingAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_RemainingAmount DEFAULT 0;
END;
GO

IF COL_LENGTH(N'dbo.Invoices', N'CancelReason') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
    ADD CancelReason NVARCHAR(500) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Invoices', N'AdjustmentNote') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
    ADD AdjustmentNote NVARCHAR(500) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Invoices', N'PaidAt') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
    ADD PaidAt DATETIME2(0) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Invoices', N'ConfirmedBy') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
    ADD ConfirmedBy INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Invoices_ConfirmedBy')
BEGIN
    ALTER TABLE dbo.Invoices
    ADD CONSTRAINT FK_Invoices_ConfirmedBy
        FOREIGN KEY (ConfirmedBy) REFERENCES dbo.Users(UserID);
END;
GO

UPDATE dbo.Invoices
SET PaymentStatus = N'PartiallyPaid'
WHERE PaymentStatus = N'Partial';
GO

UPDATE dbo.Invoices
SET RemainingAmount = CASE
    WHEN TotalAmount - PaidAmount < 0 THEN 0
    ELSE TotalAmount - PaidAmount
END,
    PaidAt = CASE WHEN PaidAmount >= TotalAmount AND TotalAmount > 0 AND PaidAt IS NULL THEN GETDATE() ELSE PaidAt END
WHERE RemainingAmount <> CASE
    WHEN TotalAmount - PaidAmount < 0 THEN 0
    ELSE TotalAmount - PaidAmount
END
   OR PaidAt IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Invoices_PaymentStatus')
BEGIN
    ALTER TABLE dbo.Invoices
    ADD CONSTRAINT CK_Invoices_PaymentStatus
    CHECK (PaymentStatus IN (N'Unpaid', N'Pending', N'PartiallyPaid', N'Paid', N'Overdue', N'Cancelled'));
END;
GO

IF COL_LENGTH(N'dbo.FeeTypes', N'CalculationType') IS NULL
BEGIN
    ALTER TABLE dbo.FeeTypes
    ADD CalculationType NVARCHAR(50) NOT NULL CONSTRAINT DF_FeeTypes_CalculationType DEFAULT N'Fixed';
END;
GO

IF COL_LENGTH(N'dbo.FeeTypes', N'FundID') IS NULL
BEGIN
    ALTER TABLE dbo.FeeTypes
    ADD FundID INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.FeeTypes', N'IsActive') IS NULL
BEGIN
    ALTER TABLE dbo.FeeTypes
    ADD IsActive BIT NOT NULL CONSTRAINT DF_FeeTypes_IsActive DEFAULT 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FeeTypes_Funds')
BEGIN
    ALTER TABLE dbo.FeeTypes
    ADD CONSTRAINT FK_FeeTypes_Funds
        FOREIGN KEY (FundID) REFERENCES dbo.Funds(FundID);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_FeeTypes_CalculationType')
BEGIN
    ALTER TABLE dbo.FeeTypes
    ADD CONSTRAINT CK_FeeTypes_CalculationType
    CHECK (CalculationType IN (N'Fixed', N'PerArea', N'PerVehicle', N'Manual'));
END;
GO

IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
BEGIN
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
        CONSTRAINT FK_Payments_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserID)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Payments_PaymentCode' AND object_id = OBJECT_ID(N'dbo.Payments'))
BEGIN
    CREATE UNIQUE INDEX UX_Payments_PaymentCode ON dbo.Payments(PaymentCode);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Payments_InvoiceID_Status' AND object_id = OBJECT_ID(N'dbo.Payments'))
BEGIN
    CREATE INDEX IX_Payments_InvoiceID_Status ON dbo.Payments(InvoiceID, PaymentStatus);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Payments_ResidentID' AND object_id = OBJECT_ID(N'dbo.Payments'))
BEGIN
    CREATE INDEX IX_Payments_ResidentID ON dbo.Payments(ResidentID, PaymentDate DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Payments_PaymentMethod')
BEGIN
    ALTER TABLE dbo.Payments
    ADD CONSTRAINT CK_Payments_PaymentMethod
    CHECK (PaymentMethod IN (N'Cash', N'BankTransfer', N'QR', N'EWallet', N'Other'));
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Payments_PaymentStatus')
BEGIN
    ALTER TABLE dbo.Payments
    ADD CONSTRAINT CK_Payments_PaymentStatus
    CHECK (PaymentStatus IN (N'Pending', N'Confirmed', N'Rejected', N'Cancelled', N'Refunded'));
END;
GO

IF OBJECT_ID(N'dbo.Receipts', N'U') IS NULL
BEGIN
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
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Receipts_ReceiptCode' AND object_id = OBJECT_ID(N'dbo.Receipts'))
BEGIN
    CREATE UNIQUE INDEX UX_Receipts_ReceiptCode ON dbo.Receipts(ReceiptCode);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Receipts_PaymentID' AND object_id = OBJECT_ID(N'dbo.Receipts'))
BEGIN
    CREATE UNIQUE INDEX UX_Receipts_PaymentID ON dbo.Receipts(PaymentID);
END;
GO

IF OBJECT_ID(N'dbo.Expenses', N'U') IS NULL
BEGIN
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
        CONSTRAINT FK_Expenses_PaidBy FOREIGN KEY (PaidBy) REFERENCES dbo.Users(UserID)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Expenses_ExpenseCode' AND object_id = OBJECT_ID(N'dbo.Expenses'))
BEGIN
    CREATE UNIQUE INDEX UX_Expenses_ExpenseCode ON dbo.Expenses(ExpenseCode);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Expenses_PaymentMethod')
BEGIN
    ALTER TABLE dbo.Expenses
    ADD CONSTRAINT CK_Expenses_PaymentMethod
    CHECK (PaymentMethod IN (N'Cash', N'BankTransfer', N'Other'));
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Expenses_Status')
BEGIN
    ALTER TABLE dbo.Expenses
    ADD CONSTRAINT CK_Expenses_Status
    CHECK (ExpenseStatus IN (N'Draft', N'PendingApproval', N'Approved', N'Rejected', N'Paid', N'Cancelled'));
END;
GO

IF OBJECT_ID(N'dbo.PaymentVouchers', N'U') IS NULL
BEGIN
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
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PaymentVouchers_VoucherCode' AND object_id = OBJECT_ID(N'dbo.PaymentVouchers'))
BEGIN
    CREATE UNIQUE INDEX UX_PaymentVouchers_VoucherCode ON dbo.PaymentVouchers(VoucherCode);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PaymentVouchers_ExpenseID' AND object_id = OBJECT_ID(N'dbo.PaymentVouchers'))
BEGIN
    CREATE UNIQUE INDEX UX_PaymentVouchers_ExpenseID ON dbo.PaymentVouchers(ExpenseID);
END;
GO

IF OBJECT_ID(N'dbo.FundTransactions', N'U') IS NULL
BEGIN
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
        CONSTRAINT FK_FundTransactions_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserID)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FundTransactions_FundID_TransactionDate' AND object_id = OBJECT_ID(N'dbo.FundTransactions'))
BEGIN
    CREATE INDEX IX_FundTransactions_FundID_TransactionDate ON dbo.FundTransactions(FundID, TransactionDate DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_FundTransactions_TransactionType')
BEGIN
    ALTER TABLE dbo.FundTransactions
    ADD CONSTRAINT CK_FundTransactions_TransactionType
    CHECK (TransactionType IN (N'Income', N'Expense', N'Adjustment'));
END;
GO

IF OBJECT_ID(N'dbo.FinancialPeriods', N'U') IS NULL
BEGIN
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
        CONSTRAINT FK_FinancialPeriods_ClosedBy FOREIGN KEY (ClosedBy) REFERENCES dbo.Users(UserID)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FinancialPeriods_Month_Year' AND object_id = OBJECT_ID(N'dbo.FinancialPeriods'))
BEGIN
    CREATE UNIQUE INDEX UX_FinancialPeriods_Month_Year ON dbo.FinancialPeriods([Month], [Year]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_FinancialPeriods_Status')
BEGIN
    ALTER TABLE dbo.FinancialPeriods
    ADD CONSTRAINT CK_FinancialPeriods_Status
    CHECK (Status IN (N'Open', N'Closed'));
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Funds WHERE FundName = N'Quỹ vận hành')
BEGIN
    INSERT INTO dbo.Funds (FundName, Description, InitialBalance, IsActive)
    VALUES (N'Quỹ vận hành', N'Quỹ phục vụ thu chi vận hành thường xuyên', 0, 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Funds WHERE FundName = N'Quỹ bảo trì')
BEGIN
    INSERT INTO dbo.Funds (FundName, Description, InitialBalance, IsActive)
    VALUES (N'Quỹ bảo trì', N'Quỹ dành cho bảo trì tài sản và hạ tầng chung', 0, 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PaymentAccounts WHERE AccountName = N'Tiền mặt quầy thu')
BEGIN
    INSERT INTO dbo.PaymentAccounts (AccountName, AccountType, IsActive)
    VALUES (N'Tiền mặt quầy thu', N'Cash', 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PaymentAccounts WHERE AccountName = N'Tài khoản ngân hàng chính')
BEGIN
    INSERT INTO dbo.PaymentAccounts (AccountName, AccountType, BankName, AccountNumber, AccountHolder, IsActive)
    VALUES (N'Tài khoản ngân hàng chính', N'Bank', N'Vietcombank', N'0123456789', N'Ban quản lý chung cư', 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PaymentAccounts WHERE AccountName = N'Tài khoản QR chính')
BEGIN
    INSERT INTO dbo.PaymentAccounts (AccountName, AccountType, BankName, AccountNumber, AccountHolder, IsActive)
    VALUES (N'Tài khoản QR chính', N'QR', N'Vietcombank', N'0123456789', N'Ban quản lý chung cư', 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ExpenseCategories WHERE CategoryName = N'Lương bảo vệ')
BEGIN
    INSERT INTO dbo.ExpenseCategories (CategoryName, Description, IsActive)
    VALUES
        (N'Lương bảo vệ', N'Chi trả lương đội bảo vệ', 1),
        (N'Lương vệ sinh', N'Chi trả lương nhân viên vệ sinh', 1),
        (N'Sửa chữa bảo trì', N'Chi phí sửa chữa và bảo trì', 1),
        (N'Điện khu chung', N'Điện sử dụng cho khu vực chung', 1),
        (N'Nước khu chung', N'Nước sử dụng cho khu vực chung', 1),
        (N'Văn phòng phẩm', N'Chi phí văn phòng phẩm', 1),
        (N'Phí dịch vụ bên ngoài', N'Chi cho đơn vị dịch vụ thuê ngoài', 1),
        (N'Chi phí phát sinh khác', N'Các khoản chi phát sinh khác', 1);
END;
GO

DECLARE @OperatingFundID INT;
SELECT @OperatingFundID = FundID FROM dbo.Funds WHERE FundName = N'Quỹ vận hành';

UPDATE dbo.FeeTypes
SET FundID = ISNULL(FundID, @OperatingFundID),
    CalculationType = ISNULL(NULLIF(CalculationType, N''), N'Fixed'),
    IsActive = CASE WHEN Status = N'Active' THEN 1 ELSE IsActive END
WHERE @OperatingFundID IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SystemConfig WHERE ConfigKey = N'AllowNegativeFund')
BEGIN
    INSERT INTO dbo.SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
    VALUES (N'AllowNegativeFund', N'false', N'Cho phép quỹ âm khi ghi nhận chi', GETDATE(), NULL);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SystemConfig WHERE ConfigKey = N'DefaultPaymentProofFolder')
BEGIN
    INSERT INTO dbo.SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
    VALUES (N'DefaultPaymentProofFolder', N'Documents\\ApartmentManager\\PaymentProofs', N'Thư mục mặc định lưu minh chứng thanh toán', GETDATE(), NULL);
END;
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
    SET @NewRemainingAmount = CASE
        WHEN @InvoiceTotal - @NewPaidAmount < 0 THEN 0
        ELSE @InvoiceTotal - @NewPaidAmount
    END;

    SET @NewInvoiceStatus = CASE
        WHEN @NewRemainingAmount = 0 THEN N'Paid'
        WHEN @NewPaidAmount > 0 THEN N'PartiallyPaid'
        WHEN @InvoiceDueDate < CAST(GETDATE() AS DATE) THEN N'Overdue'
        ELSE N'Unpaid'
    END;

    SELECT TOP (1) @FundID = COALESCE(ft.FundID, @FundID)
    FROM dbo.InvoiceDetails id
    LEFT JOIN dbo.FeeTypes ft ON id.FeeTypeID = ft.FeeTypeID
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
        (
            ReceiptCode, PaymentID, InvoiceID, ApartmentID, PayerName, Amount, ReceiptDate, CreatedBy, Note
        )
        VALUES
        (
            @ReceiptCode, @PaymentID, @InvoiceID, @ApartmentID, @PayerName, @Amount, GETDATE(), @ConfirmedBy, @ConfirmationNote
        );
    END;

    IF @FundID IS NOT NULL
    BEGIN
        INSERT INTO dbo.FundTransactions
        (
            FundID, TransactionType, ReferenceType, ReferenceID, Amount, TransactionDate, CreatedBy, Note
        )
        VALUES
        (
            @FundID, N'Income', N'Payment', @PaymentID, @Amount, GETDATE(), @ConfirmedBy, COALESCE(@ConfirmationNote, CONCAT(N'Confirmed payment ', @PaymentID))
        );
    END;

    COMMIT TRANSACTION;
END;
GO
