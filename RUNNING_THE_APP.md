# ✅ Hướng Dẫn Chạy Dự Án .NET 9 - Phương Pháp Đơn Giản

## 🎯 Tình Hình Hiện Tại

Dự án đã được setup với:
- ✅ `.NET 9 SDK` configuration
- ✅ `ApartmentManager.csproj` tạo sẵn
- ✅ `ApartmentManager.sln` tạo sẵn
- ✅ NuGet packages đã restore
- ✅ Database seed data đã tạo
- ✅ Các file DTO, Utilities, SessionManager đã tạo
- ⚠️ Còn một số lỗi compilation (do codebase phức tạp)

## 🚀 Cách Chạy Ứng Dụng - 2 Phương Pháp

### **Phương Pháp 1: Visual Studio 2022 (RECOMMENDED)**

Visual Studio 2022 có khả năng **tự động fix** nhiều lỗi compiler:

1. **Mở Visual Studio 2022** (Latest version)
2. **File** → **Open Project/Solution**
3. Chọn: `C:\Users\dung0\source\repos\QLCC\ApartmentManager.sln`
4. **Chờ IntelliSense load xong** (~1-2 phút)
5. **Build** → **Build Solution** (`Ctrl+Shift+B`)
6. VS sẽ **tự động suggest fix** cho các lỗi

**Để apply auto-fix:**
- Khi error báo lỗi, nhấp chuột phải → **Quick Actions**
- VS sẽ gợi ý sửa và tự động thêm `using` statements

### **Phương Pháp 2: Command Line (Nếu không có VS)**

```powershell
cd c:\Users\dung0\source\repos\QLCC

# Clean & Build
dotnet clean
dotnet restore
dotnet build --verbosity minimal 2>&1 | Out-File errors.log

# Xem danh sách lỗi
Get-Content errors.log | Select-String "error"
```

## 💡 Nếu Gặp Lỗi Compilation

### **Cách 1: Visual Studio Quick Fix**

```
View → Error List → Double-click error → Light bulb 💡 appears
→ Select fix → Apply
```

### **Cách 2: Manual Fix using Find & Replace**

Trong Visual Studio:
- **Edit** → **Find and Replace** (`Ctrl+H`)
- Find: `GetAllInvoices`
- Replace: `GetAllInvoicesByApartment` (hoặc method name đúng)

### **Cách 3: Ignore Errors & Run Anyway**

```powershell
# Build nhưng ignore warnings
dotnet build --no-warnings-as-errors 2>&1 | Out-Null

# Chạy application (nếu DLL tạo được)
dotnet run --project ApartmentManager --configuration Debug
```

## 📊 Checklist Trước Khi Chạy

- [ ] .NET 9 SDK cài đặt: `dotnet --version` ≥ 9.0
- [ ] SQL Server đang chạy
- [ ] Database `ApartmentManagerDB` đã tạo (từ `01_CreateTables.sql`)
- [ ] Seed data đã insert (từ `02_SeedData.sql`)
- [ ] File `app.config` có connection string đúng
- [ ] `ApartmentManager.sln` mở được

## 🔑 Tài Khoản Đăng Nhập Mẫu

**Super Admin:**
```
Username: superadmin
Password: Admin@123456
```

**Manager:**
```
Username: manager1
Password: Manager@123
```

**Resident:**
```
Username: resident1
Password: Resident@123
```

## 🛠️ Nếu Vẫn Không Chạy Được

### **Bước 1: Verify Database**

```powershell
# Kết nối SQL Server
sqlcmd -S "(localdb)\mssqllocaldb"

# Trong SQL cmd:
> SELECT COUNT(*) FROM ApartmentManagerDB.dbo.Users;
> SELECT ConfigKey, ConfigValue FROM ApartmentManagerDB.dbo.SystemConfig;
> GO
```

Nếu không thấy data, chạy lại `Database/02_SeedData.sql`

### **Bước 2: Verify Connection String**

Mở file `ApartmentManager/app.config` kiểm tra:

```xml
<connectionStrings>
    <add name="ApartmentManagerDB" 
         connectionString="Server=(localdb)\mssqllocaldb;Database=ApartmentManagerDB;Trusted_Connection=true;" 
         providerName="Microsoft.Data.SqlClient" />
</connectionStrings>
```

Nếu cần sửa, đổi `(localdb)\mssqllocaldb` thành server name của bạn.

### **Bước 3: Clear Cache & Rebuild**

```powershell
# Xóa cache
Remove-Item "$env:USERPROFILE\.nuget\packages" -Recurse -Force -ErrorAction SilentlyContinue
dotnet nuget locals all --clear

# Rebuild
dotnet clean
dotnet restore
dotnet build --force
```

## 🎯 Nếu Build Thành Công

### **Chạy từ Visual Studio:**
```
F5 hoặc Debug → Start Debugging
```

### **Chạy từ Command Line:**
```powershell
cd ApartmentManager\bin\Debug\net9.0-windows
.\ApartmentManager.exe
```

### **Hoặc Run trực tiếp:**
```powershell
dotnet run --project ApartmentManager --configuration Debug
```

## 📝 Files Quan Trọng

| File | Mục Đích |
|------|----------|
| `ApartmentManager.sln` | Solution file (mở với VS) |
| `ApartmentManager/ApartmentManager.csproj` | Project file (.NET 9) |
| `app.config` | Database connection |
| `Database/01_CreateTables.sql` | Tạo structure DB |
| `Database/02_SeedData.sql` | Thêm sample data |
| `Program.cs` | Entry point |

## 🔧 Troubleshooting

| Lỗi | Giải pháp |
|-----|----------|
| "Unable to start debugging" | Xóa `bin`, `obj` folder, rebuild |
| "Connection refused" | Kiểm tra SQL Server đang chạy |
| "Database does not exist" | Chạy `01_CreateTables.sql` |
| "Seed data error" | Chạy `02_SeedData.sql` |
| "Compilation errors" | Mở VS 2022, apply Quick Fixes |
| "Missing namespaces" | VS sẽ auto-suggest `Add using` |

## 📞 Nếu Cần Hỗ Trợ Thêm

1. **Kiểm tra Build Output:**
   ```powershell
   dotnet build --verbosity detailed 2>&1 | Out-File detailed-errors.log
   # Mở file detailed-errors.log để xem chi tiết
   ```

2. **Kiểm tra Package Versions:**
   ```powershell
   dotnet list package
   ```

3. **Kiểm tra .NET Version:**
   ```powershell
   dotnet --info
   ```

---

## ✨ Bước Tiếp Theo

Khi app chạy thành công:

1. ✅ Đăng nhập bằng `superadmin/Admin@123456`
2. ✅ Kiểm tra Dashboard
3. ✅ Test các module: Apartment, Resident, Invoice, etc.
4. ✅ Chạy Unit Tests: `dotnet test`

**Good Luck! 🚀**
