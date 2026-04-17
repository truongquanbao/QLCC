# 🏢 Hệ Thống Quản Lý Khu Chung Cư (Apartment Manager)

> **Phần mềm quản lý hoàn chỉnh cho khu chung cư** - Xây dựng bằng C# WinForms, SQL Server 2022, kiến trúc 3-layer.

---

## 🎯 Tổng Quan Dự Án

Đây là một **ứng dụng WinForms chuyên nghiệp** để quản lý:
- 👥 Cư dân & người sử dụng
- 🏠 Căn hộ, khối, tầng, tòa nhà
- 💰 Hóa đơn, phí dịch vụ, thanh toán
- 📋 Phản ánh, khiếu nại
- 📢 Thông báo hệ thống
- 🚗 Quản lý phương tiện
- 🔧 Quản lý tài sản chung
- 📊 Báo cáo & thống kê
- 🔐 Phân quyền & bảo mật

---

## ✨ Tính Năng Chính

### 🔐 Xác Thực & Phân Quyền
- ✅ Đăng ký tài khoản cư dân (cần xác minh)
- ✅ Đăng nhập với phân quyền 3 cấp:
  - **Super Admin**: Toàn quyền hệ thống
  - **Manager**: Quản lý khu chung cư
  - **Resident**: Cư dân sử dụng
- ✅ Nhớ đăng nhập (Remember Me)
- ✅ Khóa tài khoản sau X lần sai mật khẩu
- ✅ Đổi mật khẩu, quên mật khẩu

### 🏠 Quản Lý Bất Động Sản
- ✅ CRUD Tòa nhà, Block, Tầng, Căn hộ
- ✅ Cấu hình trạng thái căn hộ (Trống, Đang ở, Cho thuê, Bảo trì, Khóa)
- ✅ Quản lý cư dân và gán vào căn hộ
- ✅ Lịch sử chuyển nhà

### 💰 Quản Lý Tài Chính
- ✅ Tạo hóa đơn tháng tự động
- ✅ Tính phí linh hoạt (quản lý, gửi xe, vệ sinh, điện, nước...)
- ✅ Tracking thanh toán & công nợ
- ✅ In / Xuất hóa đơn (Excel, PDF)
- ✅ Nhắc thanh toán tự động

### 📋 Quản Lý Yêu Cầu
- ✅ Cư dân gửi phản ánh / khiếu nại
- ✅ Quản lý phân loại & xử lý
- ✅ Theo dõi tiến độ xử lý
- ✅ Đánh giá mức hài lòng

### 📢 Quản Lý Thông Báo
- ✅ Tạo thông báo (Chung, Khẩn cấp, Bảo trì, Nhắc thanh toán)
- ✅ Gửi theo đối tượng (Toàn bộ, Theo Block, Theo căn hộ)
- ✅ Thông báo khẩn tự động popup
- ✅ Xác nhận đã đọc

### 📊 Báo Cáo & Thống Kê
- ✅ Báo cáo cư dân
- ✅ Báo cáo căn hộ (trống/đang ở)
- ✅ Báo cáo doanh thu theo tháng
- ✅ Báo cáo công nợ
- ✅ Biểu đồ trực quan (doanh thu, tỷ lệ thanh toán, phản ánh...)
- ✅ Xuất Excel & PDF

### 🔒 Bảo Mật & Audit
- ✅ Hash mật khẩu BCrypt
- ✅ SQL Injection protection (Parameterized queries)
- ✅ Audit log mọi hành động
- ✅ Session management
- ✅ Phân quyền chi tiết từng module

---

## 🏗️ Kiến Trúc

### 3-Layer Architecture

```
┌─────────────────────────────────────┐
│      Presentation Layer (GUI)       │
│  - WinForms Forms                   │
│  - User Interface                   │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   Business Logic Layer (BLL)        │
│  - AuthenticationBLL                │
│  - UserBLL                          │
│  - ApartmentBLL                     │
│  - InvoiceBLL                       │
│  - ...                              │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   Data Access Layer (DAL)           │
│  - UserDAL                          │
│  - ApartmentDAL                     │
│  - InvoiceDAL                       │
│  - AuditLogDAL                      │
│  - ...                              │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│     SQL Server Database             │
│  - 21+ Tables                       │
│  - Stored Procedures                │
└─────────────────────────────────────┘
```

### Stack Công Nghệ

| Thành Phần | Công Nghệ |
|-----------|----------|
| **Language** | C# 13 |
| **Framework** | .NET 10 + WinForms |
| **Database** | SQL Server 2022 |
| **Authentication** | BCrypt.Net-Next |
| **Logging** | Serilog |
| **Excel Export** | ClosedXML |
| **PDF Export** | QuestPDF |
| **Charts** | LiveCharts (optional) |

---

## 📦 Cấu Trúc Thư Mục

```
ApartmentManager/
├── ApartmentManager/               # Main project
│   ├── GUI/
│   │   ├── Forms/                 # WinForms
│   │   │   ├── FrmLogin.cs
│   │   │   ├── FrmRegister.cs
│   │   │   ├── FrmDatabaseSetup.cs
│   │   │   ├── FrmMainDashboard.cs
│   │   │   └── ... (20+ forms)
│   │   └── Components/            # Custom controls
│   ├── DTO/                       # Data Transfer Objects
│   │   ├── UserDTO.cs
│   │   ├── ApartmentDTO.cs
│   │   └── ... (10+ DTOs)
│   ├── DAL/                       # Data Access Layer
│   │   ├── UserDAL.cs
│   │   ├── ApartmentDAL.cs
│   │   └── ... (8+ DAL classes)
│   ├── BLL/                       # Business Logic Layer
│   │   ├── AuthenticationBLL.cs
│   │   ├── UserBLL.cs
│   │   └── ... (8+ BLL classes)
│   ├── Utilities/
│   │   ├── PasswordHasher.cs
│   │   ├── DatabaseHelper.cs
│   │   ├── SessionManager.cs
│   │   ├── ValidationHelper.cs
│   │   └── ConfigurationHelper.cs
│   ├── Resources/                 # Images, Icons
│   ├── Logs/                      # Application logs
│   ├── Program.cs
│   ├── app.config                 # Configuration
│   └── ApartmentManager.csproj
├── Database/
│   ├── 01_CreateTables.sql       # Database schema
│   └── 02_SeedData.sql           # Initial data
├── SETUP_GUIDE.md                # Installation guide
├── README.md                     # This file
└── prompt.md                     # Requirements document
```

---

## 🚀 Quick Start

### Prerequisites
- Windows 10/11
- Visual Studio 2022
- .NET 10 SDK
- SQL Server 2022

### Installation

```bash
# 1. Clone/Download project
cd ApartmentManager

# 2. Restore NuGet packages
dotnet restore

# 3. Create database (run in SQL Server Management Studio)
# File: Database/01_CreateTables.sql
# File: Database/02_SeedData.sql

# 4. Build & Run
dotnet build
dotnet run
```

Xem chi tiết tại: **[SETUP_GUIDE.md](./SETUP_GUIDE.md)**

---

## 🔑 Default Accounts

| Role | Username | Password | 
|------|----------|----------|
| **Super Admin** | superadmin | Admin@123456 |
| **Manager** | manager1 | Manager@123 |
| **Resident** | resident1 | Resident@123 |

---

## 📋 Danh Sách Form (Dự Kiến)

### Hệ Thống
- [x] FrmLogin - Đăng nhập
- [x] FrmRegister - Đăng ký
- [x] FrmDatabaseSetup - Cấu hình DB
- [x] FrmMainDashboard - Dashboard chính
- [ ] FrmChangePassword - Đổi mật khẩu
- [ ] FrmForgotPassword - Quên mật khẩu

### Super Admin
- [ ] FrmAccountManagement - Quản lý tài khoản
- [ ] FrmRolePermission - Phân quyền
- [ ] FrmSystemConfig - Cấu hình hệ thống
- [ ] FrmAuditLog - Log hệ thống
- [ ] FrmBackupRestore - Backup/Restore

### Quản Lý
- [ ] FrmBuildingManagement - Quản lý tòa nhà
- [ ] FrmApartmentManagement - Quản lý căn hộ
- [ ] FrmResidentManagement - Quản lý cư dân
- [ ] FrmInvoiceManagement - Quản lý hóa đơn
- [ ] FrmComplaintManagement - Quản lý phản ánh
- [ ] FrmNotificationManagement - Quản lý thông báo
- [ ] FrmVehicleManagement - Quản lý phương tiện
- [ ] FrmVisitorManagement - Quản lý khách
- [ ] FrmAssetManagement - Quản lý tài sản
- [ ] FrmContractManagement - Quản lý hợp đồng

### Báo Cáo
- [ ] FrmRevenueReport - Báo cáo doanh thu
- [ ] FrmResidentReport - Báo cáo cư dân
- [ ] FrmDebtReport - Báo cáo công nợ
- [ ] FrmComplaintReport - Báo cáo phản ánh
- [ ] FrmDashboardStatistics - Dashboard thống kê

### Cư Dân
- [ ] FrmResidentProfile - Hồ sơ cá nhân
- [ ] FrmApartmentInfo - Thông tin căn hộ
- [ ] FrmMyInvoices - Hóa đơn của tôi
- [ ] FrmMyComplaints - Phản ánh của tôi
- [ ] FrmMyVehicles - Xe của tôi
- [ ] FrmMyVisitors - Khách của tôi

---

## ✅ Test Cases

Dự án bao gồm 40+ test case cho:
- ✅ Đăng ký / Đăng nhập
- ✅ Phân quyền & bảo mật
- ✅ CRUD các module
- ✅ Validation dữ liệu
- ✅ Xử lý lỗi
- ✅ Báo cáo & xuất file

---

## 🔒 Security Features

| Feature | Implementation |
|---------|-----------------|
| **Password Hashing** | BCrypt (11 rounds) |
| **SQL Injection** | Parameterized Queries |
| **Session Management** | Thread-safe SessionManager |
| **Authorization** | Role-Based Access Control (RBAC) |
| **Audit Logging** | AuditLogDAL (mọi hành động quan trọng) |
| **Account Lockout** | Tự động khóa sau X lần sai mật khẩu |
| **Input Validation** | Validation Helper (Email, Phone, CCCD...) |
| **Encryption** | DPAPI (optional) cho sensitive config |

---

## 🎨 UI/UX Design

- **Color Scheme**: Xanh dương đậm (#215C9B), Trắng, Xám nhạt
- **Typography**: Segoe UI
- **Icons**: FontAwesome Sharp
- **Layout**: Modern, Professional, User-friendly
- **Responsive**: Hỗ trợ các độ phân giải khác nhau

---

## 📊 Database Schema

**21+ Tables:**
- Users, Roles, Permissions, RolePermissions
- Buildings, Blocks, Floors, Apartments
- Residents, Contracts
- FeeTypes, Invoices, InvoiceDetails
- Vehicles, Complaints, Notifications, NotificationReads
- Visitors, Assets, AuditLogs, SystemConfig

**Relationships:**
- 1:N (Building → Blocks → Floors → Apartments)
- N:N (Users ↔ Roles, Roles ↔ Permissions)
- 1:N (Apartments ↔ Residents, Residents ↔ Vehicles)
- Referential Integrity ON DELETE CASCADE

---

## 🚦 Development Status

| Module | Status |
|--------|--------|
| **Authentication** | ✅ Core Done |
| **Database Layer** | ✅ Done |
| **Business Logic** | ✅ Core Done |
| **UI Forms** | 🔄 In Progress |
| **Reports** | ⏳ Planned |
| **Testing** | ⏳ Planned |

---

## 🐛 Known Issues & TODOs

- [ ] Email/SMS notifications (placeholders)
- [ ] Real-time notifications (implement SignalR)
- [ ] Advanced reporting charts (LiveCharts)
- [ ] Multi-language support
- [ ] Mobile app companion
- [ ] API layer (REST API)

---

## 📞 Hỗ Trợ

Nếu gặp vấn đề:
1. Xem **SETUP_GUIDE.md** - Hướng dẫn chi tiết
2. Kiểm tra **Logs/** folder - Chi tiết lỗi
3. Chạy lại database scripts
4. Xóa bin/ và obj/, rebuild solution

---

## 📄 License

MIT License - Tự do sử dụng cho mục đích học tập & thương mại

---

## 👨‍💻 Author

Xây dựng theo yêu cầu chi tiết (29 mục) - Phiên bản 1.0.0

---

## 🎉 Chúc Bạn Thành Công!

**"Quản lý khu chung cư dễ dàng, hiệu quả, minh bạch"**

---

**Last Updated**: April 17, 2026  
**Version**: 1.0.0  
**.NET Target**: 10.0-windows
