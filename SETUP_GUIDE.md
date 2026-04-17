# 🏢 Hướng Dẫn Setup Dự Án Quản Lý Khu Chung Cư

## 📋 Yêu Cầu Môi Trường

- **OS**: Windows 10/11
- **Visual Studio**: 2022 trở lên (hỗ trợ .NET 10)
- **SQL Server**: 2022 (hoặc 2022 Express)
- **SQL Server Management Studio (SSMS)**: 19+
- **.NET**: 10.0

## 🚀 Hướng Dẫn Cài Đặt

### Bước 1: Chuẩn Bị Môi Trường

#### 1.1 Cài Đặt SQL Server 2022
```
https://www.microsoft.com/sql-server/sql-server-downloads
→ Chọn SQL Server 2022 Express (hoặc Developer Edition)
→ Cài đặt đầy đủ
→ Cấu hình: Hỗ trợ TCP/IP
```

#### 1.2 Cài Đặt SSMS
```
https://learn.microsoft.com/sql/ssms/
→ Download SQL Server Management Studio
→ Cài đặt và khởi động
```

#### 1.3 Cài Đặt .NET 10 SDK
```
https://dotnet.microsoft.com/download/dotnet/10.0
→ Download .NET 10 SDK
→ Cài đặt hoàn toàn
```

#### 1.4 Cài Đặt Visual Studio 2022
```
https://visualstudio.microsoft.com/downloads/
→ Download Visual Studio Community 2022
→ Trong Installation chọn:
  - Desktop development with C#
  - Windows Forms
  - .NET 10 workload
```

### Bước 2: Tạo Database

#### 2.1 Mở SSMS
```
→ Connect đến SQL Server (thường là localhost\SQLEXPRESS)
```

#### 2.2 Tạo Database
```sql
-- Copy toàn bộ nội dung file: Database/01_CreateTables.sql
-- Paste vào SSMS
-- Nhấn F5 để chạy
```

#### 2.3 Seed Dữ Liệu
```sql
-- Copy toàn bộ nội dung file: Database/02_SeedData.sql
-- Paste vào SSMS
-- Nhấn F5 để chạy
```

#### 2.4 Xác Nhận Database Được Tạo
```sql
-- Chạy lệnh này trong SSMS:
USE ApartmentManagerDB;
GO
SELECT COUNT(*) AS TableCount FROM INFORMATION_SCHEMA.TABLES;
GO
-- Kết quả phải >= 21 tables
```

### Bước 3: Clone / Copy Project

```bash
# Tạo thư mục project
mkdir C:\Projects\ApartmentManager
cd C:\Projects\ApartmentManager

# Copy toàn bộ file code từ /ApartmentManager folder tại đây
```

### Bước 4: Mở Project trong Visual Studio

```
1. Mở Visual Studio 2022
2. File → Open → Folder → Chọn C:\Projects\ApartmentManager
3. Hoặc File → Open Project/Solution → ApartmentManager.csproj
```

### Bước 5: Restore NuGet Packages

```bash
# Terminal hoặc Package Manager Console trong VS
dotnet restore

# Hoặc sử dụng Package Manager Console:
# Tools → NuGet Package Manager → Package Manager Console
# PM> Update-Package
```

### Bước 6: Cấu Hình Connection String

#### 6.1 Chỉnh Sửa app.config
Mở file: `ApartmentManager/app.config`

```xml
<!-- Tìm dòng này: -->
<add name="ApartmentManagerDB" 
     connectionString="Server=localhost;Database=ApartmentManagerDB;Integrated Security=true;Encrypt=false;" 
     providerName="Microsoft.Data.SqlClient" />

<!-- Nếu dùng SQL Server Express (SQLEXPRESS), đổi thành: -->
<add name="ApartmentManagerDB" 
     connectionString="Server=localhost\SQLEXPRESS;Database=ApartmentManagerDB;Integrated Security=true;Encrypt=false;" 
     providerName="Microsoft.Data.SqlClient" />

<!-- Nếu dùng SQL Server Authentication (Username/Password): -->
<add name="ApartmentManagerDB" 
     connectionString="Server=localhost;Database=ApartmentManagerDB;User Id=sa;Password=YourPassword;Encrypt=false;" 
     providerName="Microsoft.Data.SqlClient" />
```

**Lưu file!**

### Bước 7: Build Project

```bash
# Terminal hoặc VS
dotnet build

# Hoặc trong VS:
# Build → Build Solution (Ctrl + Shift + B)
```

**Nếu có lỗi NuGet, hãy:**
```bash
dotnet restore
dotnet clean
dotnet build
```

### Bước 8: Chạy Ứng Dụng

```bash
# Terminal
dotnet run

# Hoặc trong VS:
# Nhấn F5 hoặc Ctrl + F5
```

## ✅ Xác Nhận Cài Đặt Thành Công

### Login Test:
```
Username: superadmin
Password: Admin@123456
```

→ Nếu đăng nhập thành công → Setup OK! ✓

### Tài khoản Mẫu:
```
Manager:
  Username: manager1
  Password: Manager@123

Resident:
  Username: resident1
  Password: Resident@123
```

## 🐛 Troubleshooting

### Lỗi: "Connection timeout"
```
→ Kiểm tra SQL Server đang chạy:
   Services → SQL Server (MSSQLSERVER hoặc SQLEXPRESS)
→ Kiểm tra Connection String trong app.config
→ Nếu dùng Express: Server=localhost\SQLEXPRESS
```

### Lỗi: "Database does not exist"
```
→ Chạy lại script: Database/01_CreateTables.sql
→ Chạy lại script: Database/02_SeedData.sql
```

### Lỗi: "NuGet packages not found"
```
→ Xóa thư mục: bin/ và obj/
→ Chạy: dotnet restore
→ Build lại
```

### Lỗi: "A type initializer threw an exception"
```
→ Xóa file: bin/ và obj/
→ Chạy Clean solution
→ Rebuild solution
```

## 📁 Cấu Trúc Thư Mục

```
ApartmentManager/
├── ApartmentManager/
│   ├── GUI/
│   │   └── Forms/
│   │       ├── FrmLogin.cs
│   │       ├── FrmRegister.cs
│   │       ├── FrmDatabaseSetup.cs
│   │       └── FrmMainDashboard.cs
│   ├── DTO/
│   │   ├── UserDTO.cs
│   │   ├── RoleDTO.cs
│   │   ├── ApartmentDTO.cs
│   │   └── ...
│   ├── DAL/
│   │   ├── UserDAL.cs
│   │   ├── RolePermissionDAL.cs
│   │   └── AuditLogDAL.cs
│   ├── BLL/
│   │   ├── AuthenticationBLL.cs
│   │   └── UserBLL.cs
│   ├── Utilities/
│   │   ├── PasswordHasher.cs
│   │   ├── DatabaseHelper.cs
│   │   ├── SessionManager.cs
│   │   ├── ValidationHelper.cs
│   │   └── ConfigurationHelper.cs
│   ├── Resources/
│   ├── Logs/
│   ├── Program.cs
│   ├── app.config
│   └── ApartmentManager.csproj
├── Database/
│   ├── 01_CreateTables.sql
│   └── 02_SeedData.sql
├── SETUP_GUIDE.md
└── README.md
```

## 📝 Ghi Chú

- **Mật khẩu mặc định được hash bằng BCrypt**
- **Không bao giờ lưu plain text password**
- **Log được lưu trong thư mục: Logs/**
- **Để debug, bật: Logs/ folder để xem file log**

## 🔐 Bảo Mật

- ✓ Mật khẩu được hash bằng BCrypt
- ✓ SQL Injection được chặn (Parameterized Queries)
- ✓ Phân quyền dựa trên Role
- ✓ Audit log mọi hành động quan trọng
- ✓ Session management
- ✓ Khóa tài khoản sau X lần sai mật khẩu

## 📞 Hỗ Trợ

Nếu gặp vấn đề, hãy:
1. Kiểm tra file Logs/ để xem chi tiết lỗi
2. Xem troubleshooting ở trên
3. Kiểm tra SSMS xem database có tồn tại không

---

**Chúc bạn cài đặt thành công! 🎉**
