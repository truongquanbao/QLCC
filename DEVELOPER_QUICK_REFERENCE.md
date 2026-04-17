# DEVELOPER QUICK REFERENCE - Phase 2

## Creating a New Management Form

### Template Pattern
```csharp
using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public class FrmNewModule : Form
{
    private DataGridView dgvData = new DataGridView();
    private TextBox txtSearch = new TextBox();
    private Button btnCreate = new Button();
    private Button btnEdit = new Button();
    private Button btnDelete = new Button();

    public FrmNewModule()
    {
        InitializeComponent();
        LoadData();
        AuditLogDAL.LogAction(SessionManager.GetSession().UserID, "Form opened", "FrmNewModule");
    }

    private void LoadData()
    {
        try
        {
            // Call BLL method
            var data = YourBLL.GetAllItems();
            
            // Bind to DataGridView
            dgvData.DataSource = data;
            
            // Customize columns as needed
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                           MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnCreate_Click(object sender, EventArgs e)
    {
        try
        {
            // Get input values
            var result = YourBLL.CreateItem(param1, param2, param3);
            
            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", 
                               MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     "Item created", "FrmNewModule");
                LoadData();
            }
            else
            {
                MessageBox.Show(result.Message, "Error", 
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating item: {ex.Message}", "Error");
        }
    }
}
```

---

## Common DAL Patterns

### Reading Data
```csharp
// Single record
var item = ModuleDAL.GetItemByID(id);

// Multiple records
var items = ModuleDAL.GetAllItems();

// Filtered results
var items = ModuleDAL.GetItemsByStatus("Active");
```

### Creating Data
```csharp
try
{
    int newID = ModuleDAL.CreateItem(param1, param2, param3);
    // Success - use newID
}
catch (Exception ex)
{
    // DAL logs the error, handle in BLL
}
```

### Updating Data
```csharp
bool success = ModuleDAL.UpdateItem(id, newValue1, newValue2);
if (success)
{
    // Item updated
}
```

### Deleting Data
```csharp
bool success = ModuleDAL.DeleteItem(id);
if (success)
{
    // Item deleted
}
```

---

## Common BLL Patterns

### Input Validation
```csharp
if (string.IsNullOrWhiteSpace(input))
    return (false, "Field is required");

if (!ValidationHelper.IsValidEmail(email))
    return (false, "Invalid email format");

if (amount <= 0 || amount > maxLimit)
    return (false, "Amount must be between 0 and {maxLimit}");
```

### Create Operation Pattern
```csharp
public static (bool Success, string Message, int ID) CreateItem(...)
{
    try
    {
        // 1. Validate all inputs
        if (!IsValid(inputs))
            return (false, errorMessage, 0);

        // 2. Check for duplicates
        if (DAL.ItemExists(uniqueField))
            return (false, "Item already exists", 0);

        // 3. Call DAL to create
        int id = ItemDAL.CreateItem(params);

        // 4. Log operation
        Log.Information("Item created: {ID}", id);

        return (true, "Item created successfully", id);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "BLL Error creating item");
        return (false, $"Error: {ex.Message}", 0);
    }
}
```

### Update Operation Pattern
```csharp
public static (bool Success, string Message) UpdateItem(...)
{
    try
    {
        // 1. Validate ID
        if (id <= 0)
            return (false, "Invalid ID");

        // 2. Validate inputs
        if (!IsValid(inputs))
            return (false, errorMessage);

        // 3. Call DAL
        bool success = ItemDAL.UpdateItem(id, params);

        if (success)
        {
            Log.Information("Item updated: {ID}", id);
            return (true, "Item updated successfully");
        }

        return (false, "Failed to update item");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "BLL Error updating item: {ID}", id);
        return (false, $"Error: {ex.Message}");
    }
}
```

### Statistics Pattern
```csharp
public static dynamic GetStatistics()
{
    try
    {
        var items = ItemDAL.GetAllItems();

        var stats = new
        {
            Total = items.Count,
            Active = items.Count(x => x.Status == "Active"),
            Inactive = items.Count(x => x.Status == "Inactive"),
            Rate = items.Count > 0 ? 
                   ((items.Count(x => x.Status == "Active") * 100.0) / items.Count)
                       .ToString("F2") + "%" : "0%"
        };

        return stats;
    }
    catch (Exception ex)
    {
        Log.Error(ex, "BLL Error getting statistics");
        return null;
    }
}
```

---

## Validation Helper Usage

```csharp
// Phone validation (Vietnamese format)
if (!ValidationHelper.IsValidPhone(phone))
    return (false, "Phone must be in format 09xxxxxxxxx");

// Email validation
if (!ValidationHelper.IsValidEmail(email))
    return (false, "Invalid email format");

// CCCD validation (9 or 12 digits)
if (!ValidationHelper.IsValidCCCD(cccd))
    return (false, "CCCD must be 9 or 12 digits");

// Birth date validation (18+ years)
if (!ValidationHelper.IsValidBirthDate(dob))
    return (false, "Resident must be at least 18 years old");
```

---

## Logging Best Practices

```csharp
// Information: Successful operations
Log.Information("Apartment created: {ApartmentCode} (ID: {ApartmentID})", 
               apartmentCode, apartmentID);

// Warning: Invalid inputs, not errors
Log.Warning("Invalid apartment ID: {ApartmentID}", apartmentID);

// Error: Exceptions, include full context
Log.Error(ex, "Error creating apartment: {ApartmentCode}", apartmentCode);

// Audit logging for user actions
AuditLogDAL.LogAction(userId, "Created apartment", "Apartment_" + apartmentID);
```

---

## Session Management

```csharp
// Get current user session
var session = SessionManager.GetSession();

// Use session data
int userID = session.UserID;
string username = session.Username;
string role = session.Role;

// Check permissions
if (SessionManager.HasPermission("CreateApartment"))
{
    // Allow operation
}

// Check role
if (SessionManager.IsSuperAdmin())
{
    // Show admin features
}
else if (SessionManager.IsManager())
{
    // Show manager features
}
else if (SessionManager.IsResident())
{
    // Show resident features
}

// Logout
SessionManager.ClearSession();
```

---

## Error Handling Pattern

### In Forms
```csharp
try
{
    var result = BLL.CreateItem(...);
    
    if (result.Success)
    {
        MessageBox.Show(result.Message, "Success",
                       MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadData(); // Refresh UI
    }
    else
    {
        MessageBox.Show(result.Message, "Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
catch (Exception ex)
{
    MessageBox.Show($"Unexpected error: {ex.Message}", "Error",
                   MessageBoxButtons.OK, MessageBoxIcon.Error);
}
```

### In BLL
```csharp
try
{
    // Business logic here
    return (true, "Success message");
}
catch (Exception ex)
{
    Log.Error(ex, "Context about the operation");
    return (false, "User-friendly error message");
}
```

### In DAL
```csharp
try
{
    // Database operation
    return result;
}
catch (Exception ex)
{
    Log.Error(ex, "Error message with context: {Parameter}", param);
    throw; // DAL throws, BLL catches
}
```

---

## Unit Testing Template

```csharp
[TestClass]
public class ApartmentBLLTests
{
    [TestMethod]
    public void CreateApartment_ValidInput_ReturnsSuccess()
    {
        // Arrange
        string code = "A101";
        int floorID = 1;
        decimal area = 50;
        string type = "1BR";
        int maxResidents = 4;

        // Act
        var result = ApartmentBLL.CreateApartment(code, floorID, area, type, maxResidents);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.ApartmentID > 0);
        Assert.IsTrue(ApartmentDAL.ApartmentCodeExists(code));
    }

    [TestMethod]
    public void CreateApartment_DuplicateCode_ReturnsFalse()
    {
        // Arrange
        string code = "A101";
        // First creation succeeds
        ApartmentBLL.CreateApartment(code, 1, 50, "1BR", 4);

        // Act
        var result = ApartmentBLL.CreateApartment(code, 1, 50, "1BR", 4);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Apartment code already exists", result.Message);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void CreateApartment_InvalidArea_ThrowsException()
    {
        // Act
        ApartmentBLL.CreateApartment("A102", 1, -50, "1BR", 4);
    }
}
```

---

## Commonly Used BLL Classes

| Class | Methods | Use Case |
|-------|---------|----------|
| ApartmentBLL | Create, Update, Delete, GetStats | Building management |
| ResidentBLL | Create, Update, MoveOut, GetStats | Occupancy management |
| InvoiceBLL | Create, RecordPayment, GetStats | Finance tracking |
| AuthenticationBLL | Login, Register, ChangePassword | User management |
| UserBLL | GetUser, ApproveUser, LockUser | Access control |
| AuditLogDAL | LogAction | Activity tracking |

---

## Database Connection Troubleshooting

```csharp
// Test connection
bool canConnect = DatabaseHelper.TestConnection();
if (!canConnect)
{
    MessageBox.Show("Cannot connect to database. Check app.config settings.", "Error");
}

// Check if database is initialized
bool isInitialized = DatabaseHelper.IsDatabaseInitialized();
if (!isInitialized)
{
    // Run FrmDatabaseSetup
}

// Get connection string
string connString = DatabaseHelper.GetConnectionString();
```

---

## Form Development Checklist

- [ ] Inherit from `Form`
- [ ] Create required controls (TextBox, DataGridView, etc.)
- [ ] Implement `InitializeComponent()` method
- [ ] Load initial data in constructor
- [ ] Add Create button → call BLL → refresh UI
- [ ] Add Edit button → populate controls → update
- [ ] Add Delete button → confirm → call BLL
- [ ] Add validation for user input
- [ ] Handle errors with try-catch
- [ ] Log actions via AuditLogDAL
- [ ] Check permissions via SessionManager
- [ ] Format data for display (currency, dates, etc.)
- [ ] Test with sample data

---

## Next Module to Build: FrmApartmentManagement

See PHASE2_COMPLETION.md for the priority order of remaining forms.

**Ready to build your first management form? Start with FrmApartmentManagement!**
