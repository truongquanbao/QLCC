# 📚 Developer Reference - Apartment Manager

## 🎯 Quick Commands

### Build & Run
```bash
# Restore packages
dotnet restore

# Clean
dotnet clean

# Build
dotnet build

# Run
dotnet run

# Run with Debug
dotnet run -- --debug
```

### Database

```bash
# Run in SSMS (SQL Server Management Studio):

-- Create database & tables
-- Open: Database/01_CreateTables.sql
-- Press: F5

-- Seed initial data
-- Open: Database/02_SeedData.sql
-- Press: F5

-- Verify setup
-- Open: Database/03_VerifySetup.sql
-- Press: F5
```

---

## 🏗️ Project Structure

```
ApartmentManager/
├── GUI/Forms/          → WinForms .cs files
├── DTO/                → Data Transfer Objects
├── DAL/                → Database Access (UserDAL, ApartmentDAL...)
├── BLL/                → Business Logic (AuthenticationBLL, UserBLL...)
├── Utilities/          → Helpers (PasswordHasher, DatabaseHelper...)
├── Program.cs          → Entry point
└── app.config          → Configuration
```

---

## 🔑 Key Classes & Methods

### Authentication (AuthenticationBLL)
```csharp
// Login
var (success, message, session) = AuthenticationBLL.Login(username, password);

// Register
var (success, msg, userID) = AuthenticationBLL.RegisterResident(
    username, password, passwordConfirm, fullName, email, phone, cccd);

// Change Password
var (success, msg) = AuthenticationBLL.ChangePassword(
    userID, currentPassword, newPassword, confirmPassword);
```

### Session Management (SessionManager)
```csharp
// Get current session
var session = SessionManager.GetSession();

// Check if logged in
bool isLoggedIn = SessionManager.IsLoggedIn();

// Get current user ID
int userID = SessionManager.GetCurrentUserID();

// Check permission
bool hasPermission = SessionManager.HasPermission("Manage_Account");

// Check role
bool isSuperAdmin = SessionManager.IsSuperAdmin();
bool isManager = SessionManager.IsManager();
bool isResident = SessionManager.IsResident();

// Logout
SessionManager.ClearSession();
```

### User Management (UserBLL)
```csharp
// Get user
var user = UserBLL.GetUserByID(userID);

// Get all users
var users = UserBLL.GetAllUsers();

// Approve user
var (success, msg) = UserBLL.ApproveUser(userID, approvedBy);

// Check permission
bool hasPermission = UserBLL.UserHasPermission(userID, "Manage_Account");

// Create manager
var (success, msg, userID) = UserBLL.CreateManagerAccount(
    username, fullName, email, phone, tempPassword);
```

### Password Hashing (PasswordHasher)
```csharp
// Hash password
string hash = PasswordHasher.HashPassword("Admin@123456");

// Verify password
bool isValid = PasswordHasher.VerifyPassword("Admin@123456", hash);

// Validate strength
var result = PasswordHasher.ValidatePasswordStrength(password);
if (!result.IsValid)
    MessageBox.Show(result.Message);
```

### Validation (ValidationHelper)
```csharp
// Email
bool isValid = ValidationHelper.IsValidEmail("test@example.com");

// Phone (Vietnamese format 09xxxxxxxxx)
bool isValid = ValidationHelper.IsValidPhone("0901234567");

// CCCD (9 or 12 digits)
bool isValid = ValidationHelper.IsValidCCCD("123456789");

// Username
bool isValid = ValidationHelper.IsValidUsername("user_123");

// Birth date (must be 18+)
bool isValid = ValidationHelper.IsValidBirthDate(dateTime);
```

### Database (DatabaseHelper)
```csharp
// Get connection string
string connStr = DatabaseHelper.GetConnectionString();

// Test connection
bool isConnected = DatabaseHelper.TestConnection();
bool isConnected = DatabaseHelper.TestConnection(customConnStr);

// Check if DB is initialized
bool isInit = DatabaseHelper.IsDatabaseInitialized();

// Create new connection
using (var conn = DatabaseHelper.CreateConnection()) { ... }
```

### Configuration (ConfigurationHelper)
```csharp
// Initialize (call once at startup)
ConfigurationHelper.Initialize();

// Get string value
string value = ConfigurationHelper.GetValue("AppVersion");
string value = ConfigurationHelper.GetValue("AppVersion", "1.0.0");

// Get int value
int maxAttempts = ConfigurationHelper.GetIntValue("MaxLoginAttempts", 5);

// Get bool value
bool isSeeded = ConfigurationHelper.GetBoolValue("IsSeeded", false);

// Set value
ConfigurationHelper.SetValue("IsSeeded", "true", userID);
```

### Audit Logging (AuditLogDAL)
```csharp
// Log action
AuditLogDAL.LogAction(userID, "Create", "Apartment", apartmentID, 
    "Tạo căn hộ mới");

// Log login
AuditLogDAL.LogLogin(userID, true);
AuditLogDAL.LogLogin(userID, false, "Sai mật khẩu");

// Log logout
AuditLogDAL.LogLogout(userID);

// Get audit logs
var logs = AuditLogDAL.GetAuditLogs(
    userID: 1,
    action: "Login_Success",
    limit: 100);
```

---

## 📝 Adding a New Feature

### Step 1: Create DTO (if needed)
File: `DTO/MyFeatureDTO.cs`
```csharp
namespace ApartmentManager.DTO;

public class MyFeatureDTO
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    // ... other properties
}
```

### Step 2: Create DAL
File: `DAL/MyFeatureDAL.cs`
```csharp
namespace ApartmentManager.DAL;

public class MyFeatureDAL
{
    public static MyFeatureDTO? GetByID(int id)
    {
        try
        {
            const string query = "SELECT * FROM MyFeature WHERE ID = @ID";
            using (var conn = DatabaseHelper.CreateConnection())
            {
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return new MyFeatureDTO { ID = reader.GetInt32(0), ... };
                    }
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting MyFeature");
            return null;
        }
    }
}
```

### Step 3: Create BLL
File: `BLL/MyFeatureBLL.cs`
```csharp
namespace ApartmentManager.BLL;

public class MyFeatureBLL
{
    public static MyFeatureDTO? GetByID(int id)
    {
        return MyFeatureDAL.GetByID(id);
    }
}
```

### Step 4: Create Form
File: `GUI/Forms/FrmMyFeature.cs`
```csharp
namespace ApartmentManager.GUI.Forms;

public partial class FrmMyFeature : Form
{
    public FrmMyFeature()
    {
        InitializeComponent();
    }

    private void FrmMyFeature_Load(object sender, EventArgs e)
    {
        ConfigureUI();
        LoadData();
    }

    private void ConfigureUI()
    {
        // Design UI here
    }

    private void LoadData()
    {
        // Load data using BLL
        var data = MyFeatureBLL.GetByID(1);
    }
}
```

---

## 🐛 Debugging Tips

### 1. Check Logs
```
Open: Logs/ folder
File: log-YYYY-MM-DD.txt
```

### 2. Debug Output
```csharp
// Add to Program.cs for verbose logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()  // Show all logs
    .WriteTo.Console()     // Also write to console
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

### 3. Database Check
```sql
-- In SSMS, check if data exists
SELECT * FROM Users;
SELECT * FROM Apartments;
SELECT * FROM Invoices;

-- Check recent audit logs
SELECT TOP 20 * FROM AuditLogs ORDER BY LogID DESC;
```

### 4. Session Check
```csharp
// In any form, verify session is active
if (!SessionManager.IsLoggedIn())
{
    MessageBox.Show("Session expired");
    return;
}

var session = SessionManager.GetSession();
Console.WriteLine($"User: {session?.Username}, Role: {session?.RoleName}");
```

---

## ✅ Testing Checklist

Before committing:
- [ ] No compiler errors
- [ ] Logs are clean (no ERROR level)
- [ ] Database connection works
- [ ] Can login with default account
- [ ] Session persists across forms
- [ ] Logout clears session
- [ ] Permissions are enforced

---

## 📦 NuGet Packages

Current packages:
- **BCrypt.Net-Next** (4.0.3) - Password hashing
- **Microsoft.Data.SqlClient** (5.1.5) - Database access
- **ClosedXML** (0.102.1) - Excel export
- **QuestPDF** (2024.12.0) - PDF export
- **Serilog** (3.1.1) - Logging
- **FontAwesome.Sharp** (6.3.0) - Icons

To update:
```bash
dotnet add package PackageName --version 1.0.0
dotnet restore
```

---

## 🔒 Security Reminders

- ✅ **Never** store plain text passwords
- ✅ Always use **Parameterized Queries** to prevent SQL Injection
- ✅ **Validate** all user inputs
- ✅ **Log** important actions (login, create, delete)
- ✅ **Check** permissions before sensitive operations
- ✅ Use **try-catch** for error handling
- ✅ Clear **sensitive data** when session ends

---

## 📞 Common Issues

### "Connection timeout"
→ Check SQL Server is running
→ Verify server name in app.config

### "Database does not exist"
→ Run Database/01_CreateTables.sql
→ Run Database/02_SeedData.sql

### "NuGet packages missing"
→ Run: `dotnet restore`
→ Delete bin/ and obj/ folders
→ Rebuild solution

### "Login fails"
→ Check if superadmin user exists
→ Verify password hash in database
→ Check Logs/ folder for details

---

## 📚 Resources

- **Microsoft Docs**: https://docs.microsoft.com/dotnet
- **SQL Server Docs**: https://docs.microsoft.com/sql
- **WinForms Docs**: https://docs.microsoft.com/dotnet/desktop/winforms
- **Serilog**: https://serilog.net
- **BCrypt**: https://github.com/BcryptNet/bcrypt.net

---

**Keep this file handy during development! 📖**
