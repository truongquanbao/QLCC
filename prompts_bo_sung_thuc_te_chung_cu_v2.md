# Prompt bổ sung phần còn thiếu cho phần mềm quản lý chung cư — Bản V2 thực tế hơn

## Đánh giá nhanh bản cũ

Bản prompt cũ đã xử lý đúng các phần còn thiếu chính:

- Thu tiền qua bảng `Payments`
- Chi tiền qua bảng `Expenses`
- Công nợ và nhắc nợ
- Dashboard thu - chi - tồn quỹ
- Báo cáo tài chính
- Cư dân upload minh chứng thanh toán
- Tạo hóa đơn hàng tháng
- Phân quyền tài chính
- Audit log
- Test case nghiệm thu

Tuy nhiên nếu đặt mình là một công ty làm phần mềm cho khách hàng thật, bản cũ vẫn chưa đủ mạnh để gọi là sát thực tế/doanh nghiệp. Còn thiếu các phần sau:

1. Phạm vi dữ liệu theo từng chung cư/tòa nhà chưa chặt.
2. Chưa có tài khoản/quỹ thu tiền rõ ràng.
3. Chưa có phiếu thu, phiếu chi đúng nghiệp vụ.
4. Chưa có đối soát thanh toán chuyển khoản.
5. Chưa có hoàn tiền, hủy giao dịch, điều chỉnh hóa đơn.
6. Chưa có ngân sách/quỹ vận hành/quỹ bảo trì.
7. Chưa có quy trình phê duyệt khoản chi nhiều bước.
8. Chưa có nhà cung cấp/đơn vị dịch vụ nhận tiền chi.
9. Chưa có khóa kỳ tài chính chặt chẽ.
10. Chưa đủ tiêu chuẩn để đưa ra thị trường thật, nhưng đủ nâng cấp thành đồ án rất sát thực tế.

File này là bản prompt chia nhỏ để bổ sung đầy đủ hơn.

---

# PHẦN A — DATABASE TÀI CHÍNH THỰC TẾ

## Prompt 1 — Bổ sung lõi tài chính: Payments, Expenses, Receipts, Vouchers, Funds

Bạn là Senior Business Analyst + Senior C# WinForms Developer + SQL Server Database Designer.  
Tôi đang có phần mềm quản lý chung cư bằng C# WinForms, SQL Server, kiến trúc 3-layer GUI/BLL/DAL.

Mục tiêu: bổ sung hệ thống tài chính thực tế, không chỉ lưu trạng thái hóa đơn đã đóng/chưa đóng.

### Bảng cần bổ sung

#### 1. PaymentAccounts — tài khoản/quỹ nhận tiền
Lưu nơi nhận tiền của chung cư.

Các cột:
- PaymentAccountID INT IDENTITY PRIMARY KEY
- AccountName NVARCHAR(150) NOT NULL
- AccountType NVARCHAR(50) NOT NULL -- Cash, Bank, QR, EWallet
- BankName NVARCHAR(150) NULL
- AccountNumber NVARCHAR(100) NULL
- AccountHolder NVARCHAR(150) NULL
- QRImagePath NVARCHAR(500) NULL
- IsActive BIT DEFAULT 1
- CreatedAt DATETIME DEFAULT GETDATE()

#### 2. Payments — giao dịch thu tiền
- PaymentID INT IDENTITY PRIMARY KEY
- PaymentCode NVARCHAR(50) UNIQUE NOT NULL
- InvoiceID INT NOT NULL FK -> Invoices.InvoiceID
- ApartmentID INT NOT NULL FK -> Apartments.ApartmentID
- ResidentID INT NULL FK -> Residents.ResidentID
- PaymentAccountID INT NULL FK -> PaymentAccounts.PaymentAccountID
- Amount DECIMAL(18,2) NOT NULL
- PaymentMethod NVARCHAR(50) NOT NULL -- Cash, BankTransfer, QR, EWallet, Other
- PaymentDate DATETIME NOT NULL DEFAULT GETDATE()
- TransactionCode NVARCHAR(100) NULL
- ProofImagePath NVARCHAR(500) NULL
- PaymentStatus NVARCHAR(50) DEFAULT 'Pending' -- Pending, Confirmed, Rejected, Cancelled, Refunded
- ConfirmedBy INT NULL FK -> Users.UserID
- ConfirmedAt DATETIME NULL
- RejectedReason NVARCHAR(500) NULL
- CancelReason NVARCHAR(500) NULL
- Note NVARCHAR(500) NULL
- CreatedBy INT NULL FK -> Users.UserID
- CreatedAt DATETIME DEFAULT GETDATE()

#### 3. Receipts — phiếu thu
Sau khi payment được xác nhận, hệ thống tạo phiếu thu.

Các cột:
- ReceiptID INT IDENTITY PRIMARY KEY
- ReceiptCode NVARCHAR(50) UNIQUE NOT NULL
- PaymentID INT NOT NULL FK -> Payments.PaymentID
- InvoiceID INT NOT NULL FK -> Invoices.InvoiceID
- ApartmentID INT NOT NULL FK -> Apartments.ApartmentID
- PayerName NVARCHAR(150) NOT NULL
- Amount DECIMAL(18,2) NOT NULL
- ReceiptDate DATETIME DEFAULT GETDATE()
- CreatedBy INT FK -> Users.UserID
- Note NVARCHAR(500) NULL

#### 4. ExpenseCategories — danh mục chi
- ExpenseCategoryID INT IDENTITY PRIMARY KEY
- CategoryName NVARCHAR(150) NOT NULL
- Description NVARCHAR(500) NULL
- IsActive BIT DEFAULT 1

Seed danh mục:
- Lương bảo vệ
- Lương vệ sinh
- Sửa chữa bảo trì
- Điện khu chung
- Nước khu chung
- Văn phòng phẩm
- Phí dịch vụ bên ngoài
- Chi phí phát sinh khác

#### 5. Vendors — nhà cung cấp/người nhận chi
- VendorID INT IDENTITY PRIMARY KEY
- VendorName NVARCHAR(200) NOT NULL
- Phone NVARCHAR(30) NULL
- Email NVARCHAR(150) NULL
- Address NVARCHAR(300) NULL
- TaxCode NVARCHAR(50) NULL
- BankAccount NVARCHAR(100) NULL
- BankName NVARCHAR(150) NULL
- IsActive BIT DEFAULT 1

#### 6. Expenses — đề nghị/khoản chi
- ExpenseID INT IDENTITY PRIMARY KEY
- ExpenseCode NVARCHAR(50) UNIQUE NOT NULL
- ExpenseCategoryID INT NOT NULL FK -> ExpenseCategories.ExpenseCategoryID
- VendorID INT NULL FK -> Vendors.VendorID
- Title NVARCHAR(200) NOT NULL
- Description NVARCHAR(500) NULL
- Amount DECIMAL(18,2) NOT NULL
- ExpenseDate DATETIME NOT NULL
- PaymentMethod NVARCHAR(50) NOT NULL -- Cash, BankTransfer, Other
- ReceiverName NVARCHAR(150) NULL
- ReceiptImagePath NVARCHAR(500) NULL
- ExpenseStatus NVARCHAR(50) DEFAULT 'Draft' -- Draft, PendingApproval, Approved, Rejected, Paid, Cancelled
- CreatedBy INT NOT NULL FK -> Users.UserID
- ApprovedBy INT NULL FK -> Users.UserID
- ApprovedAt DATETIME NULL
- PaidBy INT NULL FK -> Users.UserID
- PaidAt DATETIME NULL
- RejectedReason NVARCHAR(500) NULL
- CreatedAt DATETIME DEFAULT GETDATE()

#### 7. PaymentVouchers — phiếu chi
Khi khoản chi chuyển sang Paid thì tạo phiếu chi.

Các cột:
- VoucherID INT IDENTITY PRIMARY KEY
- VoucherCode NVARCHAR(50) UNIQUE NOT NULL
- ExpenseID INT NOT NULL FK -> Expenses.ExpenseID
- PayeeName NVARCHAR(150) NOT NULL
- Amount DECIMAL(18,2) NOT NULL
- VoucherDate DATETIME DEFAULT GETDATE()
- CreatedBy INT FK -> Users.UserID
- Note NVARCHAR(500) NULL

#### 8. Funds — quỹ tiền
Quản lý tiền theo quỹ: quỹ vận hành, quỹ bảo trì.

Các cột:
- FundID INT IDENTITY PRIMARY KEY
- FundName NVARCHAR(150) NOT NULL -- Quỹ vận hành, Quỹ bảo trì
- Description NVARCHAR(500) NULL
- InitialBalance DECIMAL(18,2) DEFAULT 0
- IsActive BIT DEFAULT 1

#### 9. FundTransactions — biến động quỹ
- FundTransactionID INT IDENTITY PRIMARY KEY
- FundID INT NOT NULL FK -> Funds.FundID
- TransactionType NVARCHAR(50) NOT NULL -- Income, Expense, Adjustment
- ReferenceType NVARCHAR(50) NULL -- Payment, Expense, ManualAdjustment
- ReferenceID INT NULL
- Amount DECIMAL(18,2) NOT NULL
- TransactionDate DATETIME DEFAULT GETDATE()
- CreatedBy INT FK -> Users.UserID
- Note NVARCHAR(500) NULL

#### 10. FinancialPeriods — kỳ tài chính
- PeriodID INT IDENTITY PRIMARY KEY
- Month INT NOT NULL
- Year INT NOT NULL
- StartDate DATE NOT NULL
- EndDate DATE NOT NULL
- Status NVARCHAR(50) DEFAULT 'Open' -- Open, Closed
- ClosedBy INT NULL FK -> Users.UserID
- ClosedAt DATETIME NULL
- Note NVARCHAR(500) NULL

### Bổ sung vào Invoices
Nếu chưa có thì thêm:
- DueDate DATETIME NULL
- PaidAmount DECIMAL(18,2) DEFAULT 0
- RemainingAmount DECIMAL(18,2) DEFAULT 0
- PaymentStatus NVARCHAR(50) DEFAULT 'Unpaid' -- Unpaid, PartiallyPaid, Paid, Overdue, Cancelled
- CancelReason NVARCHAR(500) NULL
- AdjustmentNote NVARCHAR(500) NULL

### Yêu cầu kỹ thuật
- Viết SQL migration an toàn: dùng IF NOT EXISTS, không làm mất dữ liệu cũ.
- Tạo FK, CHECK constraint, UNIQUE constraint, INDEX cần thiết.
- Viết stored procedure hoặc transaction cập nhật invoice khi xác nhận payment.
- Viết seed data cho ExpenseCategories, Funds, PaymentAccounts.
- Tất cả số tiền dùng DECIMAL(18,2), không dùng FLOAT.

### Đầu ra bắt buộc
1. SQL migration script hoàn chỉnh.
2. Giải thích quan hệ bảng.
3. Rule nghiệp vụ tài chính.
4. Test case database.

---

# PHẦN B — THU TIỀN THỰC TẾ

## Prompt 2 — Module cư dân thanh toán và upload minh chứng

Bạn là Senior C# WinForms Developer.

Hãy bổ sung module cư dân thanh toán hóa đơn.

### Form cần tạo/cập nhật
- FrmMyInvoices
- FrmSubmitPayment
- FrmMyPaymentHistory

### FrmMyInvoices
Hiển thị:
- Mã hóa đơn
- Tháng/năm
- Tổng tiền
- Đã thanh toán
- Còn lại
- Hạn thanh toán
- Trạng thái
- Nút Thanh toán
- Nút Xem chi tiết

### FrmSubmitPayment
Cư dân nhập:
- Hóa đơn cần thanh toán
- Số tiền thanh toán
- Phương thức: BankTransfer, QR, EWallet, Other
- Tài khoản/quỹ nhận tiền
- Mã giao dịch
- Upload ảnh minh chứng
- Ghi chú

Sau khi gửi:
- Tạo bản ghi Payments với PaymentStatus = Pending.
- Không cập nhật PaidAmount của hóa đơn ngay.
- Gửi notification cho quản lý: “Có thanh toán mới chờ xác nhận”.

### Nghiệp vụ bắt buộc
1. Cư dân chỉ xem hóa đơn của căn hộ mình.
2. Không cho gửi số tiền <= 0.
3. Không cho gửi số tiền lớn hơn RemainingAmount.
4. Chuyển khoản/QR/EWallet bắt buộc có mã giao dịch hoặc ảnh minh chứng.
5. Nếu hóa đơn đã Paid thì ẩn nút thanh toán.
6. Payment Pending không được tính vào doanh thu.
7. Payment Rejected hiển thị lý do và cho gửi lại.
8. Upload ảnh lưu vào thư mục Documents\ApartmentManager\PaymentProofs\.

### Đầu ra
- DTO/DAL/BLL.
- Code WinForms.
- Validation.
- Notification.
- Test case.

---

## Prompt 3 — Module quản lý xác nhận thanh toán

Bạn là Senior C# WinForms Developer.

Hãy tạo module quản lý xác nhận thanh toán cho Super Admin và Quản lý chung cư.

### Form cần tạo
FrmPaymentManagement

### Giao diện
Bộ lọc:
- Từ ngày / đến ngày
- Căn hộ
- Cư dân
- Trạng thái payment
- Phương thức thanh toán
- Tài khoản nhận tiền
- Mã giao dịch

DataGridView:
- Mã thanh toán
- Mã hóa đơn
- Căn hộ
- Cư dân
- Số tiền
- Phương thức
- Tài khoản nhận
- Mã giao dịch
- Trạng thái
- Ngày gửi
- Người xác nhận

Nút chức năng:
- Xem minh chứng
- Xác nhận thanh toán
- Từ chối thanh toán
- Hủy thanh toán
- In phiếu thu
- Xuất Excel

### Nghiệp vụ xác nhận
1. Khi Confirmed:
   - Cập nhật Payments.PaymentStatus = Confirmed.
   - Cộng Amount vào Invoices.PaidAmount.
   - Tính lại RemainingAmount.
   - Cập nhật PaymentStatus của hóa đơn: Unpaid / PartiallyPaid / Paid.
   - Tạo Receipts.
   - Tạo FundTransactions loại Income.
   - Ghi AuditLog.
2. Khi Rejected:
   - Không cộng tiền.
   - Bắt buộc nhập lý do từ chối.
   - Gửi notification cho cư dân.
   - Ghi AuditLog.
3. Không cho xác nhận payment nếu kỳ tài chính đã Closed.
4. Không cho xác nhận payment đã Confirmed/Rejected/Cancelled.
5. Không cho xác nhận số tiền vượt RemainingAmount.
6. Dùng SQL transaction cho toàn bộ quá trình.

### Phân quyền
- Super Admin: xem/xử lý toàn bộ.
- Quản lý chung cư: chỉ xử lý chung cư được phân công.
- Cư dân: không vào form này.

### Đầu ra
- DTO/DAL/BLL.
- Stored procedure hoặc transaction trong DAL.
- Code WinForms.
- Phiếu thu.
- Test case.

---

## Prompt 4 — Module đối soát thanh toán ngân hàng/QR

Bạn là Senior Business Analyst + C# WinForms Developer.

Hãy bổ sung chức năng đối soát thanh toán để hệ thống thực tế hơn.

### Form cần tạo
FrmPaymentReconciliation

### Mục tiêu
Cho phép quản lý nhập danh sách giao dịch ngân hàng từ file Excel/CSV và đối chiếu với Payments Pending.

### Giao diện
- Import file Excel/CSV giao dịch ngân hàng
- DataGridView giao dịch ngân hàng
- DataGridView Payments Pending
- Nút tự động gợi ý khớp
- Nút xác nhận khớp thủ công
- Nút bỏ qua giao dịch

### Rule khớp giao dịch
Một payment được gợi ý khớp nếu:
- Số tiền bằng nhau
- Mã giao dịch giống hoặc nội dung chuyển khoản chứa mã hóa đơn/căn hộ
- Ngày giao dịch gần ngày payment gửi

### Database bổ sung
BankTransactions:
- BankTransactionID INT IDENTITY PRIMARY KEY
- TransactionDate DATETIME NOT NULL
- Amount DECIMAL(18,2) NOT NULL
- Description NVARCHAR(500) NULL
- TransactionCode NVARCHAR(100) NULL
- ImportedFileName NVARCHAR(255) NULL
- MatchedPaymentID INT NULL FK -> Payments.PaymentID
- MatchStatus NVARCHAR(50) DEFAULT 'Unmatched' -- Unmatched, Suggested, Matched, Ignored
- ImportedBy INT FK -> Users.UserID
- ImportedAt DATETIME DEFAULT GETDATE()

### Đầu ra
- SQL table.
- Import Excel/CSV.
- Logic gợi ý khớp.
- Test case.

---

# PHẦN C — CHI TIỀN THỰC TẾ

## Prompt 5 — Module đề nghị chi và phê duyệt khoản chi

Bạn là Senior C# WinForms Developer có kinh nghiệm nghiệp vụ tài chính.

Hãy bổ sung module đề nghị chi và phê duyệt khoản chi vận hành chung cư.

### Form cần tạo
FrmExpenseManagement

### Giao diện
Bộ lọc:
- Từ ngày / đến ngày
- Danh mục chi
- Nhà cung cấp/người nhận
- Trạng thái
- Người tạo

DataGridView:
- Mã khoản chi
- Danh mục
- Tiêu đề
- Nhà cung cấp/người nhận
- Số tiền
- Ngày chi
- Trạng thái
- Người tạo
- Người duyệt

Khu nhập liệu:
- Danh mục chi
- Nhà cung cấp
- Tiêu đề
- Mô tả
- Số tiền
- Ngày chi
- Phương thức chi
- Người nhận
- Upload hóa đơn/biên nhận

Nút:
- Tạo nháp
- Gửi duyệt
- Duyệt
- Từ chối
- Đánh dấu đã chi tiền
- Hủy
- In phiếu chi
- Xuất Excel

### Nghiệp vụ
1. Khoản chi mới là Draft.
2. Manager có thể tạo Draft và gửi duyệt.
3. Super Admin hoặc người có quyền EXPENSE_APPROVE mới được duyệt.
4. Sau khi Approved mới được đánh dấu Paid.
5. Khi Paid:
   - Tạo PaymentVouchers.
   - Tạo FundTransactions loại Expense.
   - Dashboard tăng tổng chi.
   - Ghi AuditLog.
6. Không cho sửa khoản chi đã Paid.
7. Từ chối bắt buộc nhập lý do.
8. Không cho thao tác nếu kỳ tài chính đã Closed.

### Đầu ra
- DTO/DAL/BLL.
- Transaction.
- Code form.
- Phiếu chi.
- Test case.

---

## Prompt 6 — Module nhà cung cấp và hợp đồng dịch vụ

Bạn là Senior C# WinForms Developer.

Hãy bổ sung module quản lý nhà cung cấp/đơn vị dịch vụ để các khoản chi có nguồn gốc rõ ràng.

### Form cần tạo
- FrmVendorManagement
- FrmServiceContractManagement

### Vendor
Quản lý:
- Tên nhà cung cấp
- SĐT
- Email
- Địa chỉ
- Mã số thuế
- Tài khoản ngân hàng
- Trạng thái

### ServiceContracts
Bảng mới:
- ServiceContractID INT IDENTITY PRIMARY KEY
- VendorID INT FK -> Vendors.VendorID
- ContractCode NVARCHAR(50) UNIQUE NOT NULL
- ServiceName NVARCHAR(200) NOT NULL
- StartDate DATE NOT NULL
- EndDate DATE NULL
- MonthlyAmount DECIMAL(18,2) NULL
- ContractFilePath NVARCHAR(500) NULL
- Status NVARCHAR(50) DEFAULT 'Active'
- CreatedAt DATETIME DEFAULT GETDATE()

### Nghiệp vụ
- Cảnh báo hợp đồng dịch vụ sắp hết hạn 30 ngày.
- Khoản chi có thể liên kết với nhà cung cấp/hợp đồng.
- Không xóa vendor nếu đã có expenses.

### Đầu ra
- SQL.
- Form.
- DAL/BLL.
- Test case.

---

# PHẦN D — CÔNG NỢ, NHẮC NỢ, ĐIỀU CHỈNH

## Prompt 7 — Module công nợ và nhắc nợ

Bạn là Senior C# WinForms Developer.

Hãy bổ sung module quản lý công nợ.

### Form
FrmDebtManagement

### Hiển thị
- Mã hóa đơn
- Căn hộ
- Chủ hộ
- Tổng tiền
- Đã thanh toán
- Còn nợ
- Hạn đóng
- Số ngày quá hạn
- Trạng thái
- Số lần nhắc nợ

### Bảng bổ sung
DebtReminders:
- ReminderID INT IDENTITY PRIMARY KEY
- InvoiceID INT FK
- ApartmentID INT FK
- ResidentID INT NULL FK
- ReminderContent NVARCHAR(1000)
- SentBy INT FK -> Users.UserID
- SentAt DATETIME DEFAULT GETDATE()
- ReminderMethod NVARCHAR(50) DEFAULT 'InApp'

### Nghiệp vụ
1. DueDate < Today và RemainingAmount > 0 thì Overdue.
2. Gửi nhắc nợ tạo Notification.
3. Không nhắc hóa đơn Paid.
4. Cư dân chỉ xem công nợ của mình.
5. Quản lý chỉ xem công nợ thuộc chung cư mình.

### Đầu ra
- DAL/BLL/Form.
- Notification.
- Test case.

---

## Prompt 8 — Module điều chỉnh hóa đơn, hủy hóa đơn, hoàn tiền

Bạn là Senior Business Analyst + C# Developer.

Hãy bổ sung các nghiệp vụ tài chính thực tế: điều chỉnh hóa đơn, hủy hóa đơn, hoàn tiền.

### Database bổ sung
InvoiceAdjustments:
- AdjustmentID INT IDENTITY PRIMARY KEY
- InvoiceID INT FK -> Invoices.InvoiceID
- AdjustmentType NVARCHAR(50) NOT NULL -- Increase, Decrease
- Amount DECIMAL(18,2) NOT NULL
- Reason NVARCHAR(500) NOT NULL
- CreatedBy INT FK -> Users.UserID
- ApprovedBy INT NULL FK -> Users.UserID
- Status NVARCHAR(50) DEFAULT 'Pending' -- Pending, Approved, Rejected
- CreatedAt DATETIME DEFAULT GETDATE()

Refunds:
- RefundID INT IDENTITY PRIMARY KEY
- PaymentID INT FK -> Payments.PaymentID
- Amount DECIMAL(18,2) NOT NULL
- Reason NVARCHAR(500) NOT NULL
- RefundStatus NVARCHAR(50) DEFAULT 'Pending' -- Pending, Approved, Paid, Rejected
- CreatedBy INT FK -> Users.UserID
- ApprovedBy INT NULL FK -> Users.UserID
- PaidAt DATETIME NULL
- CreatedAt DATETIME DEFAULT GETDATE()

### Form
- FrmInvoiceAdjustment
- FrmRefundManagement

### Nghiệp vụ
1. Hóa đơn đã Paid vẫn có thể tạo điều chỉnh nhưng phải qua duyệt.
2. Điều chỉnh Approved cập nhật TotalAmount và RemainingAmount.
3. Hủy hóa đơn chỉ cho phép khi chưa có payment confirmed; nếu đã có payment phải dùng refund/adjustment.
4. Hoàn tiền phải liên kết với payment confirmed.
5. Hoàn tiền Paid tạo FundTransactions loại Expense hoặc Adjustment.
6. Mọi thao tác ghi AuditLog.

### Đầu ra
- SQL.
- Form.
- DAL/BLL.
- Test case.

---

# PHẦN E — TẠO HÓA ĐƠN TỰ ĐỘNG VÀ QUỸ

## Prompt 9 — Tạo hóa đơn hàng tháng tự động theo công thức phí

Bạn là Senior C# WinForms Developer.

Hãy bổ sung module tạo hóa đơn hàng tháng tự động.

### Form
FrmGenerateMonthlyInvoices

### Nghiệp vụ
1. Không tạo trùng hóa đơn cùng căn hộ/tháng/năm.
2. Chỉ tạo cho căn hộ đang ở/đang thuê.
3. Phí quản lý có thể tính cố định hoặc theo diện tích.
4. Phí gửi xe tính theo số xe active.
5. Phí nước/điện có thể nhập thủ công nếu chưa có module chỉ số.
6. Preview trước khi tạo.
7. Tạo hàng loạt phải dùng transaction.
8. Ghi AuditLog.

### Bổ sung FeeTypes nếu thiếu
- CalculationType NVARCHAR(50) -- Fixed, PerArea, PerVehicle, Manual
- FundID INT NULL FK -> Funds.FundID
- IsActive BIT DEFAULT 1

### Đầu ra
- SQL.
- Form preview.
- Stored procedure/DAL transaction.
- Test case.

---

## Prompt 10 — Module quản lý ngân sách/quỹ vận hành

Bạn là Senior Business Analyst + Developer.

Hãy bổ sung module quản lý ngân sách/quỹ để khách hàng thấy rõ tiền thu dùng vào đâu.

### Form
FrmFundManagement

### Hiển thị
- Quỹ vận hành
- Quỹ bảo trì
- Tổng thu vào quỹ
- Tổng chi từ quỹ
- Số dư hiện tại
- Lịch sử biến động quỹ

### Nghiệp vụ
1. Payment confirmed tạo Income cho quỹ tương ứng.
2. Expense paid tạo Expense từ quỹ tương ứng.
3. Cho phép điều chỉnh số dư nhưng phải nhập lý do và chỉ Super Admin được làm.
4. Không cho chi nếu quỹ âm, trừ khi SystemConfig.AllowNegativeFund = true.
5. Kỳ tài chính Closed thì không cho sửa giao dịch quỹ.

### Đầu ra
- DAL/BLL/Form.
- Test case.

---

# PHẦN F — BÁO CÁO, DASHBOARD, KHÓA KỲ

## Prompt 11 — Dashboard tài chính 3 quyền

Bạn là Senior C# WinForms Developer chuyên dashboard.

Hãy nâng cấp dashboard theo 3 quyền.

### Super Admin
- Tổng thu tháng
- Tổng chi tháng
- Tồn quỹ
- Công nợ
- Số hóa đơn quá hạn
- Số payment pending
- Số expense pending approval
- Biểu đồ thu/chi 6 tháng
- Top căn hộ/chung cư nợ cao

### Quản lý chung cư
- Tổng thu thuộc khu mình
- Tổng chi thuộc khu mình
- Công nợ
- Payment chờ xác nhận
- Khoản chi chờ duyệt/trạng thái
- Phản ánh mới
- Căn hộ nợ nhiều nhất

### Cư dân
- Hóa đơn mới nhất
- Số tiền còn nợ
- Payment gần đây
- Nhắc nợ/thông báo
- Phản ánh gần đây

### Quy tắc tính
- Thu = Payments Confirmed.
- Chi = Expenses Paid + Refunds Paid nếu có.
- Tồn quỹ = Income - Expense trong FundTransactions.
- Công nợ = RemainingAmount của invoices chưa Paid/Cancelled.

### Đầu ra
- DTO/DAL/BLL.
- Query tối ưu.
- WinForms UI.
- Test case.

---

## Prompt 12 — Báo cáo tài chính chuyên nghiệp

Bạn là Senior C# WinForms Developer.

Hãy tạo module báo cáo tài chính.

### Form
FrmFinancialReport

### Báo cáo cần có
1. Báo cáo tổng thu.
2. Báo cáo tổng chi.
3. Báo cáo thu - chi - tồn quỹ.
4. Báo cáo công nợ căn hộ.
5. Báo cáo payment pending/rejected.
6. Báo cáo chi phí theo danh mục.
7. Báo cáo biến động quỹ.
8. Báo cáo phiếu thu/phiếu chi.

### Xuất file
- Excel dùng ClosedXML.
- PDF dùng QuestPDF hoặc iTextSharp.
- Lưu mặc định vào Documents\ApartmentManager\Reports\.
- File name: TenBaoCao_yyyyMMdd_HHmmss.

### Đầu ra
- DTO/DAL/BLL.
- Code export Excel/PDF.
- Test case.

---

## Prompt 13 — Khóa kỳ tài chính cuối tháng

Bạn là Senior Business Analyst + Developer.

Hãy bổ sung chức năng khóa kỳ tài chính.

### Form
FrmFinancialPeriodClosing

### Nghiệp vụ
1. Super Admin hoặc người có quyền mới được khóa kỳ.
2. Trước khi khóa kỳ phải kiểm tra:
   - Không còn payment pending trong kỳ.
   - Không còn expense pending/approved chưa paid trong kỳ nếu cấu hình bắt buộc.
   - Báo cáo thu/chi tạo được.
3. Khi kỳ Closed:
   - Không cho sửa/xóa payment, expense, invoice thuộc kỳ.
   - Chỉ được điều chỉnh bằng InvoiceAdjustment/Refund ở kỳ sau.
4. Có thể mở lại kỳ chỉ với quyền Super Admin và phải ghi lý do.
5. Ghi AuditLog.

### Đầu ra
- SQL update FinancialPeriods.
- DAL/BLL/Form.
- Test case.

---

# PHẦN G — PHÂN QUYỀN, AUDIT, NGHIỆM THU

## Prompt 14 — Phân quyền tài chính theo chức năng

Bạn là Senior Developer.

Hãy bổ sung permission chi tiết.

### Permissions
- PAYMENT_VIEW
- PAYMENT_SUBMIT
- PAYMENT_CONFIRM
- PAYMENT_REJECT
- PAYMENT_CANCEL
- PAYMENT_RECONCILE
- RECEIPT_PRINT
- EXPENSE_VIEW
- EXPENSE_CREATE
- EXPENSE_SUBMIT_APPROVAL
- EXPENSE_APPROVE
- EXPENSE_REJECT
- EXPENSE_MARK_PAID
- VOUCHER_PRINT
- DEBT_VIEW
- DEBT_SEND_REMINDER
- FUND_VIEW
- FUND_ADJUST
- FINANCIAL_REPORT_VIEW
- FINANCIAL_REPORT_EXPORT
- FINANCIAL_PERIOD_CLOSE
- INVOICE_ADJUST
- REFUND_MANAGE

### Yêu cầu
- Menu ẩn/hiện theo permission.
- Button enable/disable theo permission.
- BLL bắt buộc kiểm tra lại quyền, không chỉ chặn UI.
- Cư dân chỉ xem dữ liệu của chính họ.
- Quản lý chỉ xem dữ liệu chung cư được phân công.
- Super Admin xem toàn bộ.

### Đầu ra
- SQL seed permissions.
- Update RolePermissions.
- Code PermissionService.
- Test case.

---

## Prompt 15 — Audit log tài chính chi tiết

Bạn là Senior Developer.

Hãy bổ sung audit log cho mọi nghiệp vụ tiền.

### Cần log
- Tạo/sửa/hủy hóa đơn.
- Tạo/xác nhận/từ chối/hủy payment.
- In phiếu thu.
- Tạo/gửi duyệt/duyệt/từ chối/paid expense.
- In phiếu chi.
- Điều chỉnh hóa đơn.
- Hoàn tiền.
- Khóa/mở kỳ tài chính.
- Điều chỉnh quỹ.
- Xuất báo cáo.
- Truy cập trái phép.

### Bổ sung AuditLogs nếu thiếu
- OldValue NVARCHAR(MAX)
- NewValue NVARCHAR(MAX)
- Module NVARCHAR(100)
- Action NVARCHAR(100)
- IpAddress NVARCHAR(50)
- MachineName NVARCHAR(100)
- Severity NVARCHAR(50)

### Form
FrmAuditLog

### Đầu ra
- AuditLogService.
- Tích hợp vào các module.
- Test case.

---

## Prompt 16 — Nghiệm thu cuối theo góc nhìn công ty phần mềm

Bạn là QA Lead + Product Owner cho phần mềm quản lý chung cư.

Hãy kiểm thử toàn bộ hệ thống sau khi bổ sung các module trên.

### Flow bắt buộc phải pass

#### Flow 1: Thu tiền chuyển khoản
1. Manager tạo hóa đơn tháng.
2. Resident xem hóa đơn.
3. Resident gửi payment QR/chuyển khoản và upload ảnh.
4. Manager xác nhận.
5. Hệ thống cập nhật invoice.
6. Tạo phiếu thu.
7. Tăng quỹ.
8. Dashboard cập nhật.
9. AuditLog ghi đầy đủ.

#### Flow 2: Thanh toán một phần
1. Hóa đơn 2.000.000 VNĐ.
2. Resident thanh toán 1.000.000 VNĐ.
3. Invoice thành PartiallyPaid.
4. RemainingAmount = 1.000.000 VNĐ.

#### Flow 3: Công nợ quá hạn
1. Hóa đơn quá hạn.
2. Dashboard hiển thị Overdue.
3. Manager gửi nhắc nợ.
4. Resident nhận notification.

#### Flow 4: Chi tiền vận hành
1. Manager tạo đề nghị chi sửa thang máy.
2. Super Admin duyệt.
3. Người có quyền đánh dấu Paid.
4. Tạo phiếu chi.
5. Giảm quỹ.
6. Dashboard và báo cáo cập nhật.

#### Flow 5: Điều chỉnh/hoàn tiền
1. Tạo điều chỉnh giảm hóa đơn.
2. Duyệt điều chỉnh.
3. Hóa đơn cập nhật đúng.
4. Nếu cần hoàn tiền, tạo refund.
5. Quỹ cập nhật đúng.

#### Flow 6: Khóa kỳ tài chính
1. Hết tháng, Super Admin khóa kỳ.
2. Không sửa được payment/expense/invoice trong kỳ đã khóa.
3. Mọi thay đổi sau khóa phải đi qua adjustment/refund.

#### Flow 7: Phân quyền
1. Cư dân không mở được quản lý khoản chi.
2. Quản lý không duyệt khoản chi nếu không có quyền.
3. Super Admin thấy toàn bộ.
4. BLL chặn thao tác trái phép.

### Bảng test case cần xuất
- TestCaseID
- Module
- Mục tiêu
- Dữ liệu đầu vào
- Các bước thực hiện
- Kết quả mong đợi
- Kết quả thực tế
- Pass/Fail
- Ghi chú

### Kết luận cần có
- Phần nào đã đạt mức đồ án.
- Phần nào gần thực tế doanh nghiệp.
- Phần nào chưa đủ để thương mại hóa.
- Danh sách lỗi cần sửa.
- Ưu tiên sửa lỗi theo mức độ: Critical, High, Medium, Low.

---

# Thứ tự chạy prompt đề xuất

Nên chạy theo thứ tự sau để ít lỗi:

1. Prompt 1 — Database tài chính thực tế.
2. Prompt 14 — Phân quyền tài chính.
3. Prompt 9 — Tạo hóa đơn hàng tháng.
4. Prompt 2 — Cư dân thanh toán.
5. Prompt 3 — Xác nhận thanh toán.
6. Prompt 7 — Công nợ và nhắc nợ.
7. Prompt 5 — Đề nghị chi và phê duyệt chi.
8. Prompt 10 — Quản lý quỹ.
9. Prompt 11 — Dashboard.
10. Prompt 12 — Báo cáo.
11. Prompt 15 — Audit log.
12. Prompt 13 — Khóa kỳ tài chính.
13. Prompt 4 — Đối soát ngân hàng/QR.
14. Prompt 8 — Điều chỉnh hóa đơn/hoàn tiền.
15. Prompt 6 — Nhà cung cấp/hợp đồng dịch vụ.
16. Prompt 16 — Nghiệm thu cuối.

---

# Checklist bản V2

## Thu tiền
- [ ] Có Payments.
- [ ] Có PaymentAccounts.
- [ ] Có Receipts/phiếu thu.
- [ ] Cư dân upload minh chứng.
- [ ] Quản lý xác nhận/từ chối.
- [ ] Có đối soát ngân hàng/QR.

## Chi tiền
- [ ] Có Expenses.
- [ ] Có ExpenseCategories.
- [ ] Có Vendors.
- [ ] Có PaymentVouchers/phiếu chi.
- [ ] Có duyệt khoản chi.
- [ ] Có trạng thái Draft/Pending/Approved/Paid.

## Quỹ và tài chính
- [ ] Có Funds.
- [ ] Có FundTransactions.
- [ ] Có báo cáo thu - chi - tồn quỹ.
- [ ] Có công nợ.
- [ ] Có khóa kỳ tài chính.
- [ ] Có điều chỉnh hóa đơn.
- [ ] Có hoàn tiền.

## Phân quyền và bảo mật
- [ ] GUI chặn theo quyền.
- [ ] BLL chặn theo quyền.
- [ ] Cư dân chỉ xem dữ liệu của mình.
- [ ] Quản lý chỉ xem khu được phân công.
- [ ] Super Admin toàn quyền.

## Nghiệm thu
- [ ] Flow thu tiền pass.
- [ ] Flow thanh toán một phần pass.
- [ ] Flow công nợ pass.
- [ ] Flow chi tiền pass.
- [ ] Flow hoàn tiền/điều chỉnh pass.
- [ ] Flow khóa kỳ pass.
- [ ] Audit log đầy đủ.

---

# Ghi chú thực tế

Bản V2 này đã đủ tốt để biến đồ án thành một hệ thống có tư duy sản phẩm thực tế. Tuy nhiên, nếu muốn đưa ra thị trường thật, vẫn cần thêm:

- Bảo mật nâng cao.
- Sao lưu tự động.
- Phân quyền theo từng chung cư/tòa/block thật chặt.
- Kiểm thử tải dữ liệu lớn.
- Mã hóa file upload.
- Ký số/chữ ký điện tử cho phiếu thu/phiếu chi nếu cần.
- Tích hợp cổng thanh toán thật.
- Hệ thống cloud/web/mobile thay vì chỉ WinForms desktop.

