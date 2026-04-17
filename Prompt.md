1. Mục tiêu tổng quát

Hãy thiết kế và xây dựng một phần mềm quản lý khu chung cư hoàn chỉnh bằng:

Ngôn ngữ: C#
Framework giao diện: WinForms
Cơ sở dữ liệu: SQL Server
Mô hình kiến trúc: 3-layer architecture hoặc MVC-like for WinForms
Presentation Layer
Business Logic Layer (BLL)
Data Access Layer (DAL)
Kết nối CSDL: ADO.NET hoặc Entity Framework nếu cần, ưu tiên ADO.NET rõ ràng
Bảo mật đăng nhập: hash mật khẩu, phân quyền theo role
Thiết kế giao diện hiện đại, dễ dùng, trực quan
Hệ thống phải có đăng ký, đăng nhập, đăng xuất
Có 3 phân quyền chính:
Super Admin
Quản lý khu chung cư
Cư dân

Yêu cầu hệ thống phải có đầy đủ:

giao diện desktop chuyên nghiệp
phân hệ quản trị
phân hệ vận hành
phân hệ cư dân
báo cáo
thông báo
tra cứu
kiểm thử test case đầy đủ và logic phải chạy đúng
2. Mô tả dự án

Phần mềm phục vụ cho việc quản lý toàn bộ hoạt động của một khu chung cư, bao gồm:

quản lý tòa nhà, block, tầng, căn hộ
quản lý cư dân
quản lý nhân sự ban quản lý
quản lý phí dịch vụ
quản lý hóa đơn
quản lý phương tiện
quản lý phản ánh / khiếu nại
quản lý thông báo
quản lý hợp đồng thuê/mua
quản lý khách ra vào
quản lý tài sản chung
thống kê, báo cáo
đăng ký/đăng nhập và phân quyền chặt chẽ

Phần mềm phải hướng tới tính thực tiễn, dễ mở rộng, dễ bảo trì, có khả năng chạy ổn định trên Windows.

3. Yêu cầu giao diện tổng thể

Thiết kế giao diện WinForms theo phong cách hiện đại, sạch sẽ, chuyên nghiệp.

3.1. Bố cục giao diện chính
Form đăng nhập
Form đăng ký
Form quên mật khẩu
Main Dashboard sau đăng nhập
Menu trái hoặc Ribbon menu trên cùng
Khu vực hiển thị nội dung ở trung tâm
Thanh trạng thái phía dưới hiển thị:
tên người dùng
vai trò
ngày giờ hệ thống
trạng thái kết nối
3.2. Màu sắc gợi ý
Tông màu chính: xanh dương đậm, trắng, xám nhạt
Nút chức năng nổi bật:
Thêm: xanh lá
Sửa: vàng/cam
Xóa: đỏ
Lưu: xanh dương
Hủy: xám
3.3. Thành phần giao diện nên có
DataGridView hiển thị danh sách
GroupBox cho khu nhập liệu
TabControl cho các nhóm chức năng
ComboBox lọc dữ liệu
DateTimePicker
PictureBox cho avatar cư dân
Chart cho thống kê
Notification popup hoặc panel thông báo
3.4. Tiêu chuẩn UI/UX
Form rõ ràng, căn chỉnh đẹp
Các nút có icon minh họa
Có validate dữ liệu trước khi lưu
Có thông báo lỗi/thành công
Có xác nhận khi xóa dữ liệu
Có ô tìm kiếm nhanh
Có lọc theo nhiều điều kiện
4. Chức năng đăng ký / đăng nhập / xác thực
4.1. Đăng ký tài khoản


Cho phép tạo tài khoản mới với các thông tin:

tên đăng nhập
mật khẩu
xác nhận mật khẩu
họ tên
email
số điện thoại
CCCD/CMND
mã căn hộ hoặc căn hộ đang liên kết
chọn loại tài khoản:
cư dân
quản lý (do Super Admin tạo hoặc phê duyệt)
super admin (không cho đăng ký công khai, chỉ seed dữ liệu hoặc tạo thủ công)
4.2. Quy tắc đăng ký
username không được trùng
email không được trùng
số điện thoại hợp lệ
mật khẩu tối thiểu 8 ký tự
mật khẩu phải có chữ hoa, chữ thường, số và ký tự đặc biệt
xác nhận mật khẩu phải khớp
cư dân đăng ký cần chờ xác minh hoặc phê duyệt bởi quản lý / super admin
tài khoản quản lý không cho tự đăng ký công khai nếu không được phê duyệt
4.3. Đăng nhập
đăng nhập bằng username hoặc email
nhập mật khẩu
checkbox “Nhớ đăng nhập”
phân quyền điều hướng đúng dashboard theo role
khóa tài khoản tạm thời sau nhiều lần nhập sai
ghi log đăng nhập thất bại/thành công
4.4. Quên mật khẩu
nhập email / số điện thoại
xác minh thông tin
reset mật khẩu
mật khẩu mới phải đạt chuẩn bảo mật
4.5. Bảo mật
mật khẩu lưu dưới dạng hash
không lưu plain text password
role-based authorization
kiểm soát truy cập từng chức năng theo role
5. Phân quyền chi tiết
5.1. Super Admin có quyền gì?

Super Admin là người có quyền cao nhất toàn hệ thống.

Quyền quản trị hệ thống
quản lý toàn bộ tài khoản
tạo, sửa, xóa, khóa/mở khóa tài khoản
reset mật khẩu cho mọi tài khoản
gán role cho tài khoản
quản lý phân quyền chi tiết
xem log hệ thống
sao lưu và phục hồi dữ liệu
cấu hình hệ thống
quản lý danh mục chung
Quyền quản lý khu chung cư
thêm/sửa/xóa tòa nhà
thêm/sửa/xóa block
thêm/sửa/xóa tầng
thêm/sửa/xóa căn hộ
cấu hình trạng thái căn hộ:
trống
đang ở
đang cho thuê
bảo trì
khóa
Quyền quản lý nhân sự
tạo tài khoản quản lý khu chung cư
gán khu vực phụ trách
phân công nhiệm vụ
theo dõi hiệu suất xử lý công việc
Quyền tài chính
cấu hình loại phí
cấu hình công thức tính phí
xem toàn bộ hóa đơn
xem công nợ toàn hệ thống
duyệt miễn giảm phí
xem thống kê doanh thu
Quyền báo cáo
xem mọi báo cáo
xuất Excel/PDF
xem dashboard tổng hợp toàn khu hoặc nhiều khu
Quyền kiểm soát
theo dõi lịch sử thao tác người dùng
xem log truy cập
kiểm tra lịch sử chỉnh sửa dữ liệu
khóa tính năng với user vi phạm
5.2. Quản lý khu chung cư có quyền gì?

Đây là người quản lý trực tiếp hoạt động vận hành của khu chung cư.

Quyền cư dân
xem danh sách cư dân
thêm/sửa thông tin cư dân


phê duyệt tài khoản cư dân đăng ký
gán cư dân vào căn hộ
cập nhật trạng thái cư trú
quản lý hợp đồng thuê/mua
quản lý số lượng người ở trong căn hộ
Quyền căn hộ
xem danh sách căn hộ
cập nhật trạng thái căn hộ
quản lý chủ hộ / người thuê
tra cứu lịch sử cư trú
Quyền hóa đơn và phí
tạo hóa đơn hàng tháng
cập nhật thanh toán
ghi nhận đã thanh toán / chưa thanh toán
in hóa đơn
gửi nhắc phí cho cư dân
theo dõi công nợ
lọc theo tháng, quý, năm
Quyền phản ánh / khiếu nại
tiếp nhận phản ánh
phân loại phản ánh
cập nhật trạng thái xử lý
trả lời cư dân
đánh dấu hoàn tất
xem lịch sử phản ánh
Quyền thông báo
tạo thông báo toàn khu
tạo thông báo theo block/tầng
gửi thông báo khẩn
cập nhật lịch bảo trì/cúp điện/cúp nước
Quyền phương tiện
quản lý xe cư dân
cấp mã xe/thẻ xe
kiểm tra biển số
thống kê số lượng xe
Quyền khách ra vào
duyệt khách thăm
quản lý lịch sử khách đến
kiểm tra đăng ký khách
hỗ trợ bảo vệ tra cứu
Quyền tài sản chung
quản lý thiết bị
lập lịch bảo trì
cập nhật tình trạng thiết bị
ghi nhận sửa chữa
Quyền báo cáo
báo cáo cư dân
báo cáo căn hộ
báo cáo thu phí
báo cáo công nợ
báo cáo phản ánh
báo cáo tỷ lệ lấp đầy căn hộ
Hạn chế
không có quyền xóa dữ liệu hệ thống quan trọng nếu không được cấp
không có quyền thay đổi cấu hình bảo mật cấp cao
không có quyền xem/khôi phục backup nếu không được phép
5.3. Cư dân có quyền gì / dùng tính năng gì?

Cư dân là người sử dụng phần mềm để tra cứu, thanh toán, gửi yêu cầu, nhận thông báo.

Tài khoản cá nhân
đăng ký tài khoản
đăng nhập
đổi mật khẩu
cập nhật hồ sơ cá nhân
cập nhật số điện thoại, email, avatar
Xem thông tin căn hộ
xem thông tin căn hộ đang ở
xem số người đăng ký cư trú
xem diện tích, loại căn hộ
xem hợp đồng liên quan nếu được phép
Xem phí và hóa đơn
xem hóa đơn hàng tháng
xem chi tiết các loại phí
xem trạng thái thanh toán
xem lịch sử thanh toán
in biên lai hoặc xem biên lai điện tử
Gửi phản ánh / khiếu nại
tạo phản ánh mới
chọn loại phản ánh
nhập nội dung chi tiết
đính kèm hình ảnh minh họa nếu có
theo dõi tiến độ xử lý
đánh giá sau khi hoàn tất
Nhận thông báo
xem thông báo từ ban quản lý
xác nhận đã đọc
lọc thông báo theo loại:
khẩn cấp
bảo trì
nhắc thanh toán
thông báo chung
Quản lý phương tiện
khai báo xe
cập nhật thông tin xe
xem danh sách xe đã đăng ký
xem trạng thái thẻ xe
Đăng ký khách đến thăm
tạo yêu cầu đăng ký khách
nhập họ tên khách, số điện thoại, thời gian đến
theo dõi trạng thái duyệt


Gửi yêu cầu dịch vụ
yêu cầu hỗ trợ kỹ thuật
yêu cầu sửa chữa
yêu cầu xác nhận cư trú
yêu cầu đăng ký chuyển đồ
yêu cầu đăng ký sửa chữa căn hộ
Hạn chế
không được xem dữ liệu cư dân khác
không được chỉnh sửa hóa đơn
không được duyệt tài khoản khác
không được thay đổi dữ liệu hệ thống
6. Các module chức năng chi tiết
6.1. Module quản lý tài khoản

Chức năng:

thêm tài khoản
sửa tài khoản
xóa tài khoản
khóa / mở khóa tài khoản
reset mật khẩu
tìm kiếm tài khoản theo:
username
role
trạng thái
phân quyền user
ghi log hoạt động

Form đề xuất:

FormDanhSachTaiKhoan
FormThemSuaTaiKhoan
FormPhanQuyen
6.2. Module quản lý tòa nhà / block / tầng / căn hộ

Chức năng:

thêm/sửa/xóa tòa nhà
thêm/sửa/xóa block
thêm/sửa/xóa tầng
thêm/sửa/xóa căn hộ
gán cư dân vào căn hộ
cập nhật trạng thái căn hộ

Thông tin căn hộ gồm:

mã căn hộ
tòa nhà
block
tầng
diện tích
loại căn hộ
trạng thái
số người tối đa
ghi chú
6.3. Module quản lý cư dân

Thông tin cư dân:

mã cư dân
họ tên
ngày sinh
giới tính
CCCD
số điện thoại
email
địa chỉ thường trú
ảnh đại diện
tình trạng cư trú
ngày vào ở
căn hộ liên kết
vai trò trong căn hộ: chủ hộ / người thuê / thành viên

Chức năng:

thêm/sửa/xóa cư dân
tìm kiếm cư dân
lọc theo căn hộ/tòa nhà/trạng thái
xem lịch sử cư trú
in danh sách cư dân
6.4. Module quản lý phí dịch vụ và hóa đơn

Các loại phí:

phí quản lý
phí gửi xe
phí vệ sinh
phí điện
phí nước
phí internet/cáp nếu cần
phí bảo trì
phí phát sinh khác

Chức năng:

cấu hình loại phí
tạo hóa đơn theo tháng
tính phí tự động theo công thức
chỉnh sửa hóa đơn
xác nhận thanh toán
in/xuất hóa đơn
thống kê công nợ
lọc hóa đơn theo thời gian/căn hộ/trạng thái

Thông tin hóa đơn:

mã hóa đơn
tháng/năm
căn hộ
chủ hộ
danh sách khoản phí
tổng tiền
trạng thái thanh toán
ngày thanh toán
người xác nhận
6.5. Module phản ánh / khiếu nại / yêu cầu hỗ trợ

Thông tin:

mã phản ánh
người gửi
căn hộ
loại phản ánh
tiêu đề
nội dung
ngày gửi
mức độ ưu tiên
trạng thái xử lý
người phụ trách
ngày hoàn tất
phản hồi xử lý

Chức năng:

tạo yêu cầu
sửa yêu cầu khi chưa duyệt
hủy yêu cầu khi chưa xử lý
tiếp nhận yêu cầu
chuyển xử lý
cập nhật tiến độ
kết thúc yêu cầu
đánh giá mức hài lòng
6.6. Module quản lý thông báo

Chức năng:

tạo thông báo mới
phân loại thông báo
chọn đối tượng nhận
đăng thông báo
chỉnh sửa thông báo
xóa thông báo
đánh dấu thông báo khẩn
cư dân xem và xác nhận đã đọc

Loại thông báo:

thông báo chung
thông báo khẩn
nhắc thanh toán


lịch bảo trì
cúp điện / nước
nội quy / chính sách mới
6.7. Module quản lý phương tiện

Chức năng:

đăng ký xe
sửa thông tin xe
xóa xe
kiểm tra biển số trùng
quản lý thẻ xe
thống kê xe theo căn hộ

Thông tin xe:

mã xe
loại xe
biển số
hãng xe
màu xe
chủ sở hữu
căn hộ
ngày đăng ký
trạng thái
6.8. Module quản lý khách ra vào

Chức năng:

cư dân đăng ký khách
quản lý duyệt khách
tra cứu khách theo ngày
quản lý lịch sử ra vào

Thông tin:

mã đăng ký
họ tên khách
số điện thoại
căn hộ đến
thời gian đến
thời gian rời đi
trạng thái duyệt
ghi chú
6.9. Module quản lý tài sản chung và bảo trì

Chức năng:

thêm thiết bị
cập nhật tình trạng
lập lịch bảo trì
ghi nhận sửa chữa
cảnh báo thiết bị đến hạn bảo trì

Thông tin:

mã tài sản
tên tài sản
loại tài sản
vị trí
ngày mua
tình trạng
ngày bảo trì gần nhất
ngày bảo trì kế tiếp
chi phí sửa chữa
6.10. Module báo cáo và thống kê

Các báo cáo:

báo cáo số lượng cư dân
báo cáo căn hộ đang ở / trống
báo cáo doanh thu theo tháng
báo cáo công nợ
báo cáo phản ánh
báo cáo phương tiện
báo cáo khách ra vào
báo cáo tài sản cần bảo trì

Biểu đồ:

doanh thu theo tháng
tỷ lệ thanh toán
số phản ánh theo loại
tỷ lệ lấp đầy căn hộ
7. Danh sách form đề xuất
Form hệ thống
FrmLogin
FrmRegister
FrmForgotPassword
FrmMainDashboard
FrmChangePassword
FrmProfile
Form quản trị
FrmAccountManagement
FrmRolePermission
FrmSystemConfig
FrmAuditLog
Form nghiệp vụ
FrmBuildingManagement
FrmApartmentManagement
FrmResidentManagement
FrmVehicleManagement
FrmInvoiceManagement
FrmFeeManagement
FrmComplaintManagement
FrmNotificationManagement
FrmVisitorManagement
FrmAssetManagement
FrmContractManagement
Form báo cáo
FrmRevenueReport
FrmResidentReport
FrmDebtReport
FrmComplaintReport
FrmDashboardStatistics
8. Thiết kế database SQL Server

Hãy thiết kế database chuẩn hóa, có khóa chính, khóa ngoại rõ ràng.

Các bảng chính đề xuất
Users
UserID
Username
PasswordHash
FullName
Email
Phone
RoleID
Status
CreatedAt
UpdatedAt
Roles
RoleID
RoleName
Description
Permissions
PermissionID
PermissionName
Description
RolePermissions
RoleID
PermissionID
Buildings
BuildingID
BuildingName
Address
Description
Blocks
BlockID
BlockName
BuildingID
Floors
FloorID
FloorNumber
BlockID
Apartments
ApartmentID
ApartmentCode
FloorID
Area
ApartmentType
Status
MaxResidents
Note
Residents
ResidentID
UserID
FullName
DOB
Gender
CCCD
Phone
Email
Address
AvatarPath
ResidentType
MoveInDate
MoveOutDate
ApartmentID
Status
Contracts
ContractID
ApartmentID


ResidentID
ContractType
StartDate
EndDate
DepositAmount
MonthlyRent
Status
FeeTypes
FeeTypeID
FeeName
Unit
DefaultAmount
Description
Invoices
InvoiceID
ApartmentID
ResidentID
Month
Year
TotalAmount
PaymentStatus
CreatedDate
PaidDate
InvoiceDetails
InvoiceDetailID
InvoiceID
FeeTypeID
Quantity
UnitPrice
Amount
Vehicles
VehicleID
ResidentID
ApartmentID
VehicleType
LicensePlate
Brand
Color
RegisterDate
Status
Complaints
ComplaintID
ResidentID
ApartmentID
Title
Content
ComplaintType
PriorityLevel
Status
CreatedDate
AssignedTo
ResolvedDate
ResponseNote
Notifications
NotificationID
Title
Content
NotificationType
CreatedBy
CreatedDate
TargetRole
IsUrgent
Visitors
VisitorID
ResidentID
ApartmentID
VisitorName
Phone
VisitDate
LeaveDate
ApprovalStatus
Note
Assets
AssetID
AssetName
AssetType
Location
PurchaseDate
Status
LastMaintenanceDate
NextMaintenanceDate
RepairCost
AuditLogs
LogID
UserID
Action
EntityName
EntityID
Timestamp
Description
9. Quy tắc nghiệp vụ bắt buộc
9.1. Tài khoản
username là duy nhất
email là duy nhất
tài khoản bị khóa thì không được đăng nhập
role quyết định menu hiển thị
9.2. Cư dân
một cư dân phải gắn với ít nhất một căn hộ nếu trạng thái là đang cư trú
không được thêm cư dân trùng CCCD nếu hệ thống quy định duy nhất
ngày chuyển ra phải lớn hơn ngày chuyển vào
9.3. Căn hộ
mã căn hộ là duy nhất
số người cư trú không vượt quá số tối đa nếu có quy định
căn hộ trống thì không có chủ hộ đang hoạt động
9.4. Hóa đơn
không được tạo trùng hóa đơn cho cùng căn hộ cùng tháng/năm
tổng tiền hóa đơn = tổng các dòng chi tiết
khi đã thanh toán thì chỉ admin hoặc quản lý có quyền sửa
9.5. Phản ánh
cư dân chỉ sửa được phản ánh khi trạng thái là “mới tạo”
phản ánh đã hoàn tất thì không cho chỉnh sửa nội dung
9.6. Phương tiện
biển số xe không được trùng nếu đang hoạt động
một xe phải gắn với cư dân hoặc căn hộ hợp lệ
10. Yêu cầu kỹ thuật code

Hãy viết phần mềm với cấu trúc sạch, dễ bảo trì.

10.1. Kiến trúc thư mục
GUI / Forms
DTO / Models
DAL
BLL
Utilities
Database Scripts
10.2. Coding standard
đặt tên class, method, biến rõ ràng
tách riêng logic nghiệp vụ khỏi giao diện
dùng try-catch hợp lý
ghi log lỗi
validate đầu vào đầy đủ
comment cho các đoạn logic phức tạp
10.3. Các yêu cầu xử lý
chống SQL injection
dùng parameterized query
kiểm soát null và lỗi format
xử lý transaction cho nghiệp vụ quan trọng
thông báo lỗi thân thiện người dùng


11. Yêu cầu hiển thị menu theo phân quyền
Super Admin nhìn thấy:
Dashboard
Quản lý tài khoản
Phân quyền
Tòa nhà / block / tầng / căn hộ
Cư dân
Hóa đơn / phí
Phản ánh
Phương tiện
Khách ra vào
Tài sản
Báo cáo
Log hệ thống
Cấu hình hệ thống
Quản lý khu chung cư nhìn thấy:
Dashboard
Căn hộ
Cư dân
Hóa đơn / phí
Phản ánh
Phương tiện
Khách ra vào
Tài sản
Thông báo
Báo cáo
Hồ sơ cá nhân
Cư dân nhìn thấy:
Dashboard cá nhân
Hồ sơ cá nhân
Thông tin căn hộ
Hóa đơn của tôi
Thanh toán / lịch sử thanh toán
Gửi phản ánh
Thông báo
Xe của tôi
Khách của tôi
Đổi mật khẩu
12. Dashboard theo từng vai trò
12.1. Dashboard Super Admin

Hiển thị:

tổng số tài khoản
số cư dân
số căn hộ
số căn hộ đang ở / còn trống
tổng doanh thu tháng
số hóa đơn chưa thanh toán
số phản ánh đang xử lý
biểu đồ doanh thu
12.2. Dashboard Quản lý

Hiển thị:

số cư dân hiện tại
số phản ánh mới
số hóa đơn chưa thanh toán
lịch bảo trì sắp tới
số khách đăng ký hôm nay
top loại phản ánh nhiều nhất
12.3. Dashboard Cư dân

Hiển thị:

thông tin cá nhân
thông tin căn hộ
hóa đơn mới nhất
thông báo gần đây
phản ánh gần đây
trạng thái thanh toán
13. Validation chi tiết
Đăng ký / đăng nhập
không cho bỏ trống username
không cho bỏ trống mật khẩu
email đúng định dạng
mật khẩu đủ mạnh
xác nhận mật khẩu phải khớp
Cư dân
CCCD chỉ gồm số, đủ độ dài hợp lệ
số điện thoại đúng định dạng
email hợp lệ
ngày sinh không vượt ngày hiện tại
Căn hộ
diện tích > 0
số người tối đa > 0
mã căn hộ không trùng
Hóa đơn
tháng từ 1 đến 12
năm hợp lệ
số tiền >= 0
Xe
biển số đúng định dạng
không trùng biển số đang hoạt động
14. Test case bắt buộc phải đạt

Hãy thiết kế test case đầy đủ cho từng phân hệ. Dưới đây là danh sách tối thiểu.

14.1. Test case đăng ký
Đăng ký thành công với dữ liệu hợp lệ
Đăng ký thất bại khi username trùng
Đăng ký thất bại khi email trùng
Đăng ký thất bại khi mật khẩu yếu
Đăng ký thất bại khi xác nhận mật khẩu không khớp
Đăng ký thất bại khi bỏ trống trường bắt buộc
14.2. Test case đăng nhập
Đăng nhập thành công với Super Admin
Đăng nhập thành công với Quản lý
Đăng nhập thành công với Cư dân
Đăng nhập thất bại khi sai mật khẩu
Đăng nhập thất bại khi tài khoản bị khóa
Đăng nhập thất bại khi tài khoản chưa được phê duyệt
Đăng nhập đúng role thì hiển thị đúng menu
14.3. Test case phân quyền
Super Admin truy cập toàn bộ module thành công


Quản lý không truy cập được module phân quyền hệ thống nếu không có quyền
Cư dân không truy cập được form quản lý tài khoản
Cư dân chỉ xem được dữ liệu của chính mình
Role thay đổi thì menu thay đổi tương ứng sau đăng nhập lại
14.4. Test case quản lý căn hộ
Thêm căn hộ thành công
Không cho thêm căn hộ trùng mã
Sửa căn hộ thành công
Xóa căn hộ khi chưa có cư dân
Không cho xóa căn hộ đang có dữ liệu liên quan
14.5. Test case quản lý cư dân
Thêm cư dân thành công
Không cho thêm cư dân trùng CCCD
Gán cư dân vào căn hộ thành công
Cập nhật trạng thái chuyển ra thành công
Tìm kiếm cư dân theo tên / căn hộ chính xác
14.6. Test case hóa đơn
Tạo hóa đơn tháng thành công
Không tạo trùng hóa đơn cùng căn hộ cùng tháng năm
Tổng tiền hóa đơn tính đúng
Cập nhật thanh toán thành công
Lọc hóa đơn chưa thanh toán chính xác
14.7. Test case phản ánh
Cư dân gửi phản ánh thành công
Quản lý tiếp nhận phản ánh thành công
Quản lý cập nhật trạng thái thành công
Cư dân xem đúng tiến độ xử lý
Không cho cư dân sửa phản ánh đã xử lý
14.8. Test case thông báo
Quản lý tạo thông báo thành công
Cư dân nhìn thấy đúng thông báo được gửi
Cư dân xác nhận đã đọc thành công
14.9. Test case phương tiện
Cư dân đăng ký xe thành công
Không cho trùng biển số
Quản lý tra cứu xe theo biển số thành công
14.10. Test case bảo mật
Không truy cập form nếu chưa đăng nhập
Không truy cập trái phép qua thao tác trực tiếp
SQL Injection bị chặn
Mật khẩu lưu hash không lưu plain text
15. Tiêu chí nghiệm thu

Phần mềm được coi là đạt khi:

đăng ký, đăng nhập hoạt động đúng
phân quyền đúng 100%
CRUD các module chính hoạt động ổn định
dữ liệu lưu và truy xuất đúng từ SQL Server
validate đầy đủ
không crash khi thao tác sai
test case chính đều pass
giao diện đẹp, dễ sử dụng
báo cáo hiển thị hợp lý
code rõ ràng, chia lớp hợp lý
16. Yêu cầu đầu ra từ AI

Hãy tạo đầy đủ các phần sau:

Phân tích yêu cầu hệ thống
Sơ đồ use case cho 3 vai trò
Danh sách chức năng chi tiết
Thiết kế cơ sở dữ liệu SQL Server
Mô tả quan hệ bảng
Thiết kế giao diện WinForms từng màn hình
Sơ đồ điều hướng màn hình
Cấu trúc project C# WinForms
Mã nguồn mẫu cho đăng nhập, phân quyền, CRUD
Stored procedures / script SQL tạo database
Test case chi tiết

17. Môi trường phát triển bắt buộc
Ngôn ngữ & Runtime:

C# với .NET 10 (Windows Forms trên .NET 10)
Visual Studio 2022 trở lên (hỗ trợ .NET 10)
Target framework: net10.0-windows

Cơ sở dữ liệu:

SQL Server 2022 (có thể dùng SQL Server 2022 Express nếu môi trường dev)
SQL Server Management Studio (SSMS) 19+
Connection string lưu trong app.config, mục connectionStrings, có thể mã hóa bằng DPAPI nếu cần bảo mật

NuGet packages bắt buộc:
BCrypt.Net-Next          // hash mật khẩu
ClosedXML                // xuất Excel (.xlsx)
iTextSharp hoặc QuestPDF // xuất PDF
Microsoft.Data.SqlClient // ADO.NET kết nối SQL Server 2022
NuGet packages tùy chọn:
Serilog                  // ghi log lỗi ra file
FontAwesome.Sharp        // icon cho nút bấm WinForms
LiveCharts.WinForms      // biểu đồ dashboard (thay thế MS Chart nếu cần đẹp hơn)

18. Cấu hình kết nối database lần đầu

Khi khởi động lần đầu, nếu không tìm thấy connection string hợp lệ hoặc không kết nối được, hiển thị FrmDatabaseSetup để người dùng nhập:

Server name
Database name
Authentication (Windows Auth hoặc SQL Auth)
Username / Password (nếu SQL Auth)
Nút "Test kết nối" trước khi lưu


Sau khi lưu, ghi vào app.config và tự động tạo database nếu chưa tồn tại bằng cách chạy script SQL khởi tạo


19. Seed data và tài khoản mặc định
Khi database được tạo lần đầu, hệ thống phải tự động seed dữ liệu mẫu bao gồm:
Tài khoản mặc định:
Super Admin:
  Username : superadmin
  Password : Admin@123456
  Email    : superadmin@system.local
  Role     : Super Admin
  Status   : Active
Dữ liệu mẫu tối thiểu:

1 tòa nhà mẫu → 2 block → mỗi block 5 tầng → mỗi tầng 4 căn hộ
3 loại phí mẫu: Phí quản lý, Phí gửi xe, Phí vệ sinh
1 tài khoản Quản lý mẫu (username: manager1, password: Manager@123)
1 tài khoản Cư dân mẫu (username: resident1, password: Resident@123)
2-3 thông báo mẫu
2-3 phản ánh mẫu ở các trạng thái khác nhau
3-5 hóa đơn mẫu (có thanh toán và chưa thanh toán)


Seed data phải được viết trong stored procedure sp_SeedInitialData và chỉ chạy một lần khi database mới tạo, kiểm tra bằng flag trong bảng SystemConfig


20. Bổ sung bảng database còn thiếu
Bổ sung thêm các cột và bảng sau vào thiết kế database ở mục 8:
Bổ sung cột cho bảng Users:
sqlAvatarPath        NVARCHAR(500)   NULL
LastLoginAt       DATETIME        NULL
FailedLoginCount  INT             DEFAULT 0
LockedUntil       DATETIME        NULL
IsApproved        BIT             DEFAULT 0
ApprovedBy        INT             NULL  -- FK -> Users.UserID
ApprovedAt        DATETIME        NULL


Bổ sung cột cho bảng Complaints:
sqlImageAttachmentPath  NVARCHAR(500)  NULL
SatisfactionRating   INT            NULL  -- 1 đến 5 sao
Bổ sung cột cho bảng Invoices:
sqlNote         NVARCHAR(500)  NULL
ConfirmedBy  INT            NULL  -- FK -> Users.UserID
Bổ sung cột cho bảng Contracts:
sqlDocumentPath  NVARCHAR(500)  NULL  -- đường dẫn file hợp đồng scan
Bảng mới: NotificationReads — theo dõi cư dân đã đọc thông báo chưa:
sqlNotificationReadID  INT           PK IDENTITY
NotificationID      INT           FK -> Notifications.NotificationID
UserID              INT           FK -> Users.UserID
ReadAt              DATETIME      DEFAULT GETDATE()
Bảng mới: SystemConfig — lưu cấu hình hệ thống:
sqlConfigID     INT            PK IDENTITY
ConfigKey    NVARCHAR(100)  UNIQUE NOT NULL
ConfigValue  NVARCHAR(500)  NULL
Description  NVARCHAR(300)  NULL
UpdatedAt    DATETIME       NULL
UpdatedBy    INT            NULL  -- FK -> Users.UserID
```

Dữ liệu mẫu bắt buộc trong `SystemConfig`:
```
IsSeeded         = "true"     -- đánh dấu seed data đã chạy
ApartmentNameFormat = "Block {block} - Tầng {floor} - Căn {code}"
MaxLoginAttempts = "5"        -- số lần sai tối đa trước khi khóa
LockDurationMinutes = "15"    -- thời gian khóa tài khoản (phút)
AppVersion       = "1.0.0"
```

---

## 21. Cơ chế phê duyệt tài khoản cư dân

Quy trình đăng ký và phê duyệt tài khoản cư dân phải tuân theo flow sau:
```
Cư dân đăng ký
      ↓
Trạng thái: Pending (IsApproved = 0, Status = 'Pending')
      ↓
Quản lý / Super Admin thấy danh sách tài khoản chờ duyệt
      ↓
      ├─ Duyệt → IsApproved = 1, Status = 'Active'
      │          ghi ApprovedBy, ApprovedAt
      │          hiển thị thông báo trong app khi cư dân đăng nhập lần sau
      │
      └─ Từ chối → Status = 'Rejected'
                   ghi lý do từ chối vào AuditLog
                   cư dân đăng nhập thấy thông báo lý do bị từ chối

Tài khoản Pending đăng nhập sẽ thấy màn hình thông báo "Tài khoản chờ phê duyệt" thay vì dashboard
Tài khoản Rejected đăng nhập thấy lý do từ chối và nút "Liên hệ hỗ trợ"
Tài khoản Quản lý chỉ do Super Admin tạo trực tiếp, không qua form đăng ký công khai


22. Cơ chế khóa tài khoản sau đăng nhập sai

Đọc MaxLoginAttempts và LockDurationMinutes từ bảng SystemConfig
Sau mỗi lần sai: tăng FailedLoginCount thêm 1
Khi FailedLoginCount >= MaxLoginAttempts: set LockedUntil = GETDATE() + LockDurationMinutes
Khi đăng nhập thành công: reset FailedLoginCount = 0, LockedUntil = NULL, cập nhật LastLoginAt


Hiển thị thông báo rõ ràng: "Tài khoản bị khóa đến HH:mm ngày DD/MM/YYYY"


23. Cơ chế "Nhớ đăng nhập"

Khi checkbox "Nhớ đăng nhập" được tích, lưu thông tin vào Windows Registry tại:
HKEY_CURRENT_USER\Software\ApartmentManager\RememberMe
Lưu: Username (plaintext) + RememberToken (GUID hash, không lưu password)
Thời hạn nhớ: 30 ngày, sau đó tự xóa
Khi mở app: kiểm tra token còn hạn thì tự điền username, focus vào ô password
Không tự đăng nhập hoàn toàn mà vẫn yêu cầu nhập password (bảo mật)
Nút "Đăng xuất" phải xóa token khỏi Registry


24. Cơ chế thông báo trong app

Thông báo không real-time, sử dụng polling: mỗi 60 giây app tự động kiểm tra thông báo mới từ DB
Hiển thị badge số đỏ trên icon thông báo ở thanh menu nếu có thông báo chưa đọc
Khi cư dân click "Đã đọc": ghi vào bảng NotificationReads
Thông báo chỉ hiển thị cho đúng đối tượng dựa trên TargetRole hoặc TargetApartmentID
Thông báo khẩn (IsUrgent = 1): tự động popup khi cư dân mở app hoặc khi polling phát hiện thông báo mới


25. Xuất báo cáo Excel và PDF
Xuất Excel:

Dùng thư viện ClosedXML
Mỗi báo cáo xuất ra file .xlsx với:

Header có logo khu chung cư (nếu có) và tiêu đề báo cáo
Dòng tiêu đề cột in đậm, nền màu xanh dương nhạt
Dữ liệu có border đầy đủ
Dòng tổng cộng ở cuối (nếu có)
Tên file theo format: TenBaoCao_YYYYMMDD_HHmmss.xlsx



Xuất PDF:

Dùng thư viện QuestPDF
Layout A4, có header và footer mỗi trang
Footer ghi: tên phần mềm, ngày xuất, số trang
Tên file theo format: TenBaoCao_YYYYMMDD_HHmmss.pdf

Nơi lưu file xuất:

Mặc định lưu vào thư mục Documents\ApartmentManager\Reports\
Sau khi xuất, hỏi người dùng có muốn mở file ngay không


26. Module quản lý hợp đồng (bổ sung form còn thiếu)
Bổ sung vào danh sách form ở mục 7:
Form: FrmContractManagement
Chức năng:

xem danh sách hợp đồng
thêm hợp đồng mới (thuê / mua)
sửa hợp đồng còn hiệu lực
gia hạn hợp đồng
kết thúc / thanh lý hợp đồng
đính kèm file hợp đồng scan (PDF/ảnh)
cảnh báo hợp đồng sắp hết hạn trong 30 ngày

Quy tắc nghiệp vụ hợp đồng:

một căn hộ chỉ có một hợp đồng đang hoạt động tại một thời điểm
ngày kết thúc phải lớn hơn ngày bắt đầu
không được xóa hợp đồng đã có hóa đơn liên quan, chỉ được kết thúc/thanh lý

Test case hợp đồng:

Thêm hợp đồng mới thành công
Không cho thêm hợp đồng thứ 2 cho căn hộ đang có hợp đồng active
Gia hạn hợp đồng thành công
Kết thúc hợp đồng cập nhật trạng thái căn hộ về "Trống"


Cảnh báo hiển thị đúng với hợp đồng sắp hết hạn


27. Localization và định dạng hiển thị

Toàn bộ giao diện chỉ dùng tiếng Việt
Định dạng ngày tháng: dd/MM/yyyy
Định dạng giờ: HH:mm:ss
Định dạng số tiền: #,##0 VNĐ (ví dụ: 1.500.000 VNĐ)
Dấu phân cách hàng nghìn: dấu chấm (.)
Dấu phân cách thập phân: dấu phẩy (,)
Múi giờ: UTC+7 (Việt Nam)
Encoding: UTF-8 toàn bộ


28. Cơ chế backup dữ liệu (dành cho Super Admin)
Form: FrmBackupRestore
Chức năng:

Backup: chạy lệnh BACKUP DATABASE của SQL Server, lưu file .bak vào thư mục do người dùng chọn
Restore: chọn file .bak và chạy RESTORE DATABASE
Hiển thị lịch sử backup (lưu vào bảng SystemConfig với key dạng LastBackupAt, LastBackupPath)
Cảnh báo nếu chưa backup quá 7 ngày (hiển thị trên dashboard Super Admin)


29. Bổ sung test case còn thiếu
Test case hợp đồng (đã nêu ở mục 26)
Test case cấu hình kết nối:

Kết nối thành công với thông tin đúng
Hiển thị lỗi rõ ràng khi sai server/database
Nút "Test kết nối" phản hồi đúng

Test case backup/restore:

Backup thành công tạo file .bak
Restore thành công từ file .bak hợp lệ
Hiển thị lỗi khi file .bak không hợp lệ

Test case thông báo nâng cao:

Badge số cập nhật đúng khi có thông báo mới
Thông báo khẩn popup tự động đúng
Polling không gây giật lag giao diện

Test case hợp đồng sắp hết hạn:

Cảnh báo hiển thị đúng với hợp đồng còn 30 ngày
Không cảnh báo hợp đồng đã kết thúc


//tạo một file set up môi trường để khi tôi cài đặt máy khác có thể chạy được 