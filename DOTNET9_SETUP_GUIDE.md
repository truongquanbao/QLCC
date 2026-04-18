# 🚀 Hướng Dẫn Chạy Dự Án với .NET 9

## 1. Yêu Cầu Hệ Thống

- ✅ **.NET 9 SDK** - [Download tại đây](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- ✅ **Visual Studio 2022** (Latest) hoặc **VS Code**
- ✅ **SQL Server 2022** / **SQL Server Express** / **LocalDB**
- ✅ **Windows 10/11** 64-bit

## 2. Kiểm Tra Cài Đặt .NET 9

Mở **PowerShell** và chạy:

```powershell
dotnet --version
```

Kết quả phải là `9.0.0` hoặc cao hơn.

Kiểm tra SDK có sẵn:
```powershell
dotnet --list-sdks
```

## 3. Chuẩn Bị Database

### 3.1 Tạo Database (Cách 1: Sử dụng SQL Server Management Studio)

1. Mở **SQL Server Management Studio**
2. Kết nối đến server SQL của bạn
3. Chạy script tạo table: `Database/01_CreateTables.sql`
4. Chạy script seed data: `Database/02_SeedData.sql`

### 3.2 Tạo Database (Cách 2: Sử dụng PowerShell)

```powershell
cd c:\Users\dung0\source\repos\QLCC

# Kết nối và chạy script
sqlcmd -S "(localdb)\mssqllocaldb" -i "Database\01_CreateTables.sql"
sqlcmd -S "(localdb)\mssqllocaldb" -i "Database\02_SeedData.sql"
```

### 3.3 Cấu Hình Connection String

Sửa file `ApartmentManager/app.config`:

**Nếu dùng LocalDB:**
```xml
<add name="ApartmentManagerDB" 
     connectionString="Server=(localdb)\mssqllocaldb;Database=ApartmentManagerDB;Trusted_Connection=true;" 
     providerName="Microsoft.Data.SqlClient" />
```

**Nếu dùng SQL Server Express:**
```xml
<add name="ApartmentManagerDB" 
     connectionString="Server=.\SQLEXPRESS;Database=ApartmentManagerDB;Trusted_Connection=true;" 
     providerName="Microsoft.Data.SqlClient" />
```

**Nếu dùng SQL Server Instance cụ thể:**
```xml
<add name="ApartmentManagerDB" 
     connectionString="Server=YOUR_SERVER\YOUR_INSTANCE;Database=ApartmentManagerDB;Trusted_Connection=true;" 
     providerName="Microsoft.Data.SqlClient" />
```

## 4. Mở Dự Án với Visual Studio

### 4.1 Sử dụng Visual Studio 2022

1. **File** → **Open** → **Project/Solution**
2. Chọn: `c:\Users\dung0\source\repos\QLCC\ApartmentManager.sln`
3. Chờ **IntelliSense** load xong
4. **Build** → **Build Solution** (Ctrl + Shift + B)
5. Kiểm tra không có lỗi compile

### 4.2 Sử dụng Command Line

```powershell
cd c:\Users\dung0\source\repos\QLCC

# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run project
dotnet run --project ApartmentManager
```

## 5. Chạy Ứng Dụng

### 5.1 Từ Visual Studio

1. **Set Startup Project**: Right-click **ApartmentManager** → **Set as Startup Project**
2. Nhấn **F5** hoặc **Run** button
3. Chọn **Debug** mode

### 5.2 Từ Command Line

```powershell
cd c:\Users\dung0\source\repos\QLCC

# Chạy ở Debug mode
dotnet run --project ApartmentManager --configuration Debug

# Hoặc Publish + chạy Release
dotnet publish -c Release
cd ApartmentManager\bin\Release\net9.0-windows\publish
.\ApartmentManager.exe
```

## 6. Đăng Nhập Ứng Dụng

Sau khi ứng dụng khởi động, đăng nhập bằng tài khoản mẫu:

### Tài Khoản Super Admin
```
Username: superadmin
Password: Admin@123456
```

### Tài Khoản Manager
```
Username: manager1
Password: Manager@123
```

### Tài Khoản Resident
```
Username: resident1
Password: Resident@123
```

## 7. Khắc Phục Sự Cố

### Lỗi: "Unable to start debugging. The startup project could not be launched"

**Giải pháp:**
1. Xóa folder `bin` và `obj`:
```powershell
cd ApartmentManager
Remove-Item -Path "bin", "obj" -Recurse -Force
cd ..
```

2. Clean và rebuild:
```powershell
dotnet clean
dotnet restore
dotnet build
```

3. Đảm bảo `StartupObject` đúng trong `.csproj`:
```xml
<StartupObject>ApartmentManager.Program</StartupObject>
```

### Lỗi: "Connection string not found"

**Giải pháp:**
- Kiểm tra file `app.config` có tồn tại không
- Kiểm tra `connectionStrings` section
- Chắc chắn connection string là đúng

### Lỗi: "Database does not exist"

**Giải pháp:**
1. Chạy lại script tạo database:
```powershell
sqlcmd -S "(localdb)\mssqllocaldb" -i "Database\01_CreateTables.sql"
```

2. Kiểm tra database có được tạo:
```powershell
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT name FROM sys.databases WHERE name='ApartmentManagerDB'"
```

### Lỗi: "Missing NuGet packages"

**Giải pháp:**
```powershell
cd c:\Users\dung0\source\repos\QLCC
dotnet restore
```

### Lỗi: "Compilation Error: The type or namespace ... does not exist"

**Giải pháp:**
1. Kiểm tra project structure có đúng không
2. Chắc chắn tất cả files `.cs` nằm đúng folder
3. Rebuild project: `dotnet build --force`

## 8. Chạy Unit Tests

```powershell
cd c:\Users\dung0\source\repos\QLCC

# Chạy tất cả tests
dotnet test

# Chạy test cụ thể
dotnet test --filter "NameofTest"

# Xem chi tiết output
dotnet test --verbosity detailed
```

## 9. Publish Ứng Dụng (Tạo EXE)

```powershell
cd c:\Users\dung0\source\repos\QLCC

# Publish to Release folder
dotnet publish -c Release -o ".\publish"

# File EXE sẽ ở: .\publish\ApartmentManager.exe
```

## 10. Kiểm Tra Project Đã Sẵn Sàng

Chạy verification script để kiểm tra tất cả:

```powershell
# Kiểm tra .NET version
dotnet --version

# Kiểm tra SDK
dotnet --info

# Kiểm tra cấu trúc project
dotnet new list

# Build test
dotnet build --no-restore
```

---

## 📋 Checklist Trước Khi Chạy

- [ ] .NET 9 SDK đã cài đặt
- [ ] SQL Server đã cài đặt và running
- [ ] Database `ApartmentManagerDB` đã được tạo
- [ ] Script `01_CreateTables.sql` đã chạy
- [ ] Script `02_SeedData.sql` đã chạy
- [ ] File `app.config` đã cấu hình connection string
- [ ] `ApartmentManager.sln` có thể mở được
- [ ] `dotnet build` không có lỗi
- [ ] `F5` hoặc `dotnet run` khởi động được app

---

## 📞 Hỗ Trợ Thêm

Nếu vẫn gặp vấn đề:

1. **Kiểm tra Event Viewer** (Windows):
   - Mở Event Viewer → Windows Logs → Application
   - Tìm error messages từ ứng dụng

2. **Kiểm tra Output Window** (Visual Studio):
   - View → Output → Chọn "Debug"
   - Xem chi tiết lỗi compile

3. **Kiểm tra Log Files**:
   - File logs lưu tại: `ApartmentManager\bin\Debug\net9.0-windows\logs\`

---

**Happy Coding! 🎉**
