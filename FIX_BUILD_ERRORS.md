# 🔧 Hướng Dẫn Fix Lỗi Build và Chạy Dự Án .NET 9

## 📋 Vấn Đề Gặp Phải

Khi build dự án, bạn sẽ gặp lỗi:
```
error CS0246: The type or namespace name 'List<>' could not be found
error CS0246: The type or namespace name 'Form' could not be found
error CS0723: Cannot declare a variable of static type 'SessionManager'
```

## ✅ Giải Pháp

### Cách 1: Sử dụng Visual Studio 2022 (Khuyên Dùng)

Visual Studio 2022 sẽ **tự động thêm missing using statements**:

1. **Mở Project**:
   ```
   File → Open → Project/Solution
   C:\Users\dung0\source\repos\QLCC\ApartmentManager.sln
   ```

2. **Chờ IntelliSense load xong** (khoảng 30-60 giây)

3. **Build Solution**:
   - Nhấn: `Ctrl + Shift + B`
   - Hoặc: **Build** → **Build Solution**

4. **VS sẽ tự động fix missing using directives**:
   - Nếu có lỗi đỏ, nhấn chuột phải → **Quick Actions**
   - Chọn: **Add using [namespace]**

5. **Hoặc Auto-Fix Tất Cả**:
   - **Edit** → **IntelliSense** → **Fix All Usings**

6. **Build lại**:
   ```
   dotnet build --force
   ```

### Cách 2: Sửa Lỗi SessionManager (Code-level)

Vấn đề: Một số file đang khai báo biến `SessionManager`:
```csharp
SessionManager session = SessionManager.GetSession(); // ❌ WRONG - SessionManager là static
```

**Fix:** Sửa thành:
```csharp
var session = SessionManager.GetSession(); // ✅ CORRECT
```

**Để tìm file lỗi:**
```powershell
cd c:\Users\dung0\source\repos\QLCC
grep -r "SessionManager session" ApartmentManager/GUI/Forms/
```

Các file cần fix:
- `FrmFeeTypeManagement.cs`
- `FrmNotificationManagement.cs`
- `FrmVisitorManagement.cs`
- `FrmContractManagement.cs`

### Cách 3: Chạy AutoFix Script

Tôi sẽ tạo script để tự động thêm using statements. Chạy PowerShell:

```powershell
cd c:\Users\dung0\source\repos\QLCC

# Add using System to all .cs files that don't have it
$files = Get-ChildItem -Path "ApartmentManager" -Recurse -Filter "*.cs"

foreach ($file in $files) {
    $content = Get-Content $file.FullName
    
    # Check if file already has "using System;"
    if ($content -notlike '*using System;*') {
        # Add "using System;" at the beginning
        $newContent = "using System;`n" + $content
        Set-Content -Path $file.FullName -Value $newContent
        Write-Host "Fixed: $($file.Name)"
    }
}

Write-Host "Done! All files now have using System;"
```

## 🚀 Bước Tiếp Theo

### 1. Cleanup và Rebuild

```powershell
cd c:\Users\dung0\source\repos\QLCC

# Xóa build cache
Remove-Item -Path "ApartmentManager\bin", "ApartmentManager\obj", "ApartmentManager.Tests\bin", "ApartmentManager.Tests\obj" -Recurse -Force

# Restore + Build
dotnet restore --force
dotnet build --force
```

### 2. Chạy Project

**Option A: Visual Studio (Recommended)**
```
F5 hoặc Click "Run"
```

**Option B: Command Line**
```powershell
dotnet run --project ApartmentManager --configuration Debug
```

## 🎯 Checklist Trước Khi Chạy

- [ ] `.NET 9 SDK` đã cài đặt
- [ ] `SQL Server` đã chạy
- [ ] Database `ApartmentManagerDB` đã được tạo
- [ ] `Database\01_CreateTables.sql` đã chạy
- [ ] `Database\02_SeedData.sql` đã chạy
- [ ] `app.config` có connection string đúng
- [ ] `Build` thành công (0 lỗi)
- [ ] Có thể start debugging (`F5`)

## 📝 Nếu Vẫn Gặp Lỗi

### Lỗi: "Build failed with error"

**Giải pháp:**
```powershell
# Xem chi tiết lỗi
dotnet build --verbosity detailed

# Nếu vẫn lỗi, xóa hết cache
Remove-Item -Path "$env:USERPROFILE\.nuget\packages" -Recurse -Force
dotnet nuget locals all --clear
dotnet restore --force
dotnet build --force
```

### Lỗi: "SessionManager cannot be instantiated"

**Đây là static class, không thể `new`:**
```csharp
// ❌ WRONG
var session = new SessionManager();

// ✅ CORRECT
var session = SessionManager.GetSession();
```

### Lỗi: "Database connection failed"

**Kiểm tra connection string:**
```powershell
# Kiểm tra LocalDB đang chạy
sqllocaldb info mssqllocaldb

# Hoặc kết nối trực tiếp
sqlcmd -S "(localdb)\mssqllocaldb"
```

## 🔍 Verify Build Success

Sau khi build thành công, bạn sẽ thấy:

```
Build succeeded with 0 error(s) and X warning(s)
```

**Warnings nhỏ là OK:**
```
- NU1603: Package version not found (resolved to newer)
- NU1903: Security vulnerability (low risk)
```

---

## 📞 Nếu Cần Hỗ Trợ Thêm

1. **Xem Build Output Chi Tiết**:
   ```powershell
   dotnet build 2>&1 | Out-File build.log
   # Mở file build.log để xem chi tiết
   ```

2. **Kiểm tra lỗi từng folder**:
   ```powershell
   dotnet build ApartmentManager --verbosity detailed
   ```

3. **Rebuild từ đầu**:
   ```powershell
   dotnet clean
   dotnet build --force --verbosity detailed
   ```

---

**Good Luck! You're almost there! 🚀**
