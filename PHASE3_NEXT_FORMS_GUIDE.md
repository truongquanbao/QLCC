# Phase 3 Part 3 - Next Forms to Build

## Overview
This document outlines the next 4-5 forms to be created in Phase 3, with architectural patterns and specifications.

## Forms to Build (in recommended order)

### 1. FrmVehicleManagement (~400 lines)
**Purpose**: Register and manage resident vehicles with license plate tracking

**Key Features**:
- Vehicle CRUD operations (Create, Read, Update, Delete)
- License plate format validation
- Vehicle type dropdown (Car, Motorcycle, Truck, etc.)
- Filter by owner/resident
- Search by license plate
- Vehicle status (Active, Inactive, Sold)

**Main Controls**:
- ComboBox: Filter by resident
- TextBox: License plate search
- DataGridView: Vehicle list (7 columns)
- TextBox/ComboBox: Vehicle details (LicensePlate, Type, Color, Brand, Model, YearMade, Note)
- Buttons: Create, Edit, Delete, Statistics, Refresh, Close

**BLL Methods Needed**:
- `CreateVehicle(residentID, licensePlate, type, color, brand, model, yearMade, note)`
- `UpdateVehicle(vehicleID, licensePlate, color, brand, model, note)`
- `DeleteVehicle(vehicleID)`
- `GetVehiclesByResident(residentID)`
- `GetVehicleStatistics()`

**DAL Methods** (likely exist):
- GetVehicleByID()
- GetAllVehicles()
- GetVehiclesByResident()
- CreateVehicle()
- UpdateVehicle()
- DeleteVehicle()
- ValidateLicensePlate()

**Validation Rules**:
- License plate format: Standard Vietnamese format (XX-0000-XX or similar)
- Year made: 1980 to current year
- Type: Must be from valid list (Car, Motorcycle, Truck, Bus, Bicycle, Scooter)
- Prevent duplicate license plates

---

### 2. FrmComplaintManagement (~450 lines)
**Purpose**: Manage resident complaints with priority, assignment, and resolution tracking

**Key Features**:
- Complaint CRUD operations
- Priority levels (Low, Medium, High, Critical)
- Status tracking (New, Assigned, In-Progress, Resolved, Closed)
- Staff assignment for complaint handling
- Resolution tracking with dates
- Filter by status, priority, assignee
- Complaint history/timeline

**Main Controls**:
- ComboBox: Filter by status, priority, assignee
- ComboBox: Resident/apartment selection
- TextBox: Description, resolution notes
- DateTimePicker: Report date, completion date
- DataGridView: Complaint list (8 columns)
- Buttons: Create, Edit, Assign, Resolve, Delete, Statistics, Close

**BLL Methods Needed**:
- `CreateComplaint(residentID, title, description, priority)`
- `UpdateComplaint(complaintID, title, description, priority)`
- `AssignComplaint(complaintID, assignedToUserID)`
- `ResolveComplaint(complaintID, resolutionNote)`
- `DeleteComplaint(complaintID)`
- `GetComplaintsByStatus(status)`
- `GetComplaintStatistics()`

**Validation Rules**:
- Title: 10-200 characters
- Description: 20-2000 characters
- Priority: One of (Low, Medium, High, Critical)
- Status workflow: New → Assigned → In-Progress → Resolved → Closed
- Cannot delete resolved/closed complaints without special permission

---

### 3. FrmContractManagement (~500 lines)
**Purpose**: Manage lease contracts with terms, dates, and renewal tracking

**Key Features**:
- Contract CRUD operations
- Contract type (Lease, Purchase, Service, Maintenance)
- Term duration tracking (monthly, yearly, custom)
- Auto-renewal settings
- Contract status (Active, Expired, Terminated, Pending)
- Renewal reminders (days before expiry)
- Filter by status, type, apartment

**Main Controls**:
- ComboBox: Filter by status, type
- ComboBox: Apartment/resident selection
- DateTimePicker: Start date, end date
- TextBox: Contract number, notes
- NumericUpDown: Renewal reminder days
- DataGridView: Contract list (8 columns)
- CheckBox: Auto-renewal
- Buttons: Create, Edit, Renew, Terminate, Delete, Close

**BLL Methods Needed**:
- `CreateContract(apartmentID, contractType, startDate, endDate, autoRenewal, note)`
- `UpdateContract(contractID, startDate, endDate, autoRenewal, note)`
- `RenewContract(contractID, newStartDate, newEndDate)`
- `TerminateContract(contractID, terminationDate, reason)`
- `DeleteContract(contractID)`
- `GetContractsByStatus(status)`
- `GetExpiringContracts(daysUntilExpiry)`
- `GetContractStatistics()`

**Validation Rules**:
- End date > Start date
- Contract number unique
- Cannot create contracts for inactive apartments
- Status workflow: Pending → Active → Expired (or Terminated)

---

### 4. FrmVisitorManagement (~400 lines)
**Purpose**: Track visitor check-ins/check-outs with apartment assignment

**Key Features**:
- Visitor check-in/check-out logging
- Visitor type (Guest, Delivery, Service, Family, Other)
- Purpose tracking
- Phone verification (optional)
- Multiple visits per resident
- Duration calculation
- Filter by status (Checked-in, Checked-out), type

**Main Controls**:
- ComboBox: Filter by type, apartment
- TextBox: Visitor name, phone, ID number
- ComboBox: Visitor type, purpose
- DateTimePicker: Check-in date/time
- DataGridView: Visitor log (8 columns)
- Button: Check-in, Check-out, Delete, Statistics, Close

**BLL Methods Needed**:
- `CheckInVisitor(residentID, visitorName, phone, type, purpose, idNumber)`
- `CheckOutVisitor(visitorID)`
- `DeleteVisitorLog(visitorID)`
- `GetActiveVisitors()`
- `GetVisitorsByResident(residentID)`
- `GetVisitorStatistics()`

**Validation Rules**:
- Visitor name: 3-100 characters
- Phone: Valid format if provided
- Cannot check-out visitor not checked in
- Type: One of (Guest, Delivery, Service, Family, Other)
- Duration calculated: CheckOut - CheckIn

---

### 5. FrmFeeTypeManagement (~300 lines) - Optional/Easy
**Purpose**: Manage different types of fees (Water, Electric, Maintenance, Parking, etc.)

**Key Features**:
- Fee type CRUD
- Monthly fee amounts
- Fee description
- Applicable apartments selection
- Status (Active, Inactive)

**Main Controls**:
- TextBox: Fee name, description
- TextBox: Monthly amount
- DataGridView: Fee types (6 columns)
- Button: Create, Edit, Delete, Close

**BLL Methods Needed**:
- `CreateFeeType(feeName, description, monthlyAmount)`
- `UpdateFeeType(feeTypeID, feeName, monthlyAmount, description)`
- `DeleteFeeType(feeTypeID)`
- `GetAllFeeTypes()`

---

## Form Template Code Structure

```csharp
public class FrmXxxManagement : Form
{
    // Constants
    private const int PRIMARY_COLOR = 0x215689;
    
    // Controls grouped by panel
    private Panel pnlFilter;
    private Panel pnlDetails;
    private Panel pnlGrid;
    private Panel pnlButtons;
    private Panel pnlStatus;
    
    // Selected item tracking
    private int _selectedXxxID = 0;
    
    // Constructor with permission check
    public FrmXxxManagement()
    {
        if (!SessionManager.HasPermission("ManageXxx"))
            { Close(); return; }
        
        InitializeComponent();
        LoadData();
        AuditLogDAL.LogAction(...);
    }
    
    // Layout methods
    private void InitializeComponent() { }
    private Panel CreateFilterPanel() { }
    private Panel CreateDetailsPanel() { }
    private Panel CreateGridPanel() { }
    private Panel CreateButtonPanel() { }
    private Panel CreateStatusPanel() { }
    
    // Data loading
    private void LoadData() { }
    private void DgvXxx_CellClick(...) { }
    
    // Button handlers
    private void BtnCreate_Click(...) { }
    private void BtnEdit_Click(...) { }
    private void BtnDelete_Click(...) { }
    private void BtnSearch_Click(...) { }
    private void BtnStatistics_Click(...) { }
    
    // Helper method
    private void ClearForm() { }
}
```

## Common Patterns to Use

### Validation in BLL
```csharp
public static (bool Success, string Message, int ID) CreateXxx(params)
{
    try {
        // Input validation
        if (string.IsNullOrWhiteSpace(param))
            return (false, "Error message", 0);
        
        // Business rule checks
        if (DuplicateCheck)
            return (false, "Duplicate error", 0);
        
        // DAL call
        int id = XxxDAL.CreateXxx(params);
        
        Log.Information("Created xxx");
        return (true, "Created successfully", id);
    }
    catch (Exception ex) {
        Log.Error(ex, "Error creating xxx");
        return (false, $"Error: {ex.Message}", 0);
    }
}
```

### DataGridView Setup
```csharp
dgvXxx.AllowUserToAddRows = false;
dgvXxx.ReadOnly = true;
dgvXxx.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
dgvXxx.MultiSelect = false;

dgvXxx.Columns.Add(new DataGridViewTextBoxColumn { 
    HeaderText = "ID", 
    DataPropertyName = "XxxID", 
    Width = 50, 
    Visible = false 
});
```

### Permission Checking
```csharp
if (!SessionManager.HasPermission("ManageXxx"))
{
    MessageBox.Show("Access Denied", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    this.Close();
    return;
}
```

### Audit Logging
```csharp
AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                     "Action description", "FormName");
```

## BLL Implementation Checklist

For each form, implement BLL class with:
- [ ] GetByID() - Retrieve single record
- [ ] GetAll() - Retrieve all records
- [ ] Create() - Insert with validation (return (bool, string, int))
- [ ] Update() - Modify with validation (return (bool, string))
- [ ] Delete() - Remove with checks (return (bool, string))
- [ ] GetFiltered() - Return filtered list
- [ ] GetStatistics() - Return summary data

## Testing Priority

1. **Critical Path** (test first):
   - Create with validation
   - Delete with confirmation
   - Edit existing records
   - Duplicate prevention

2. **Business Rules** (test second):
   - Status workflows
   - Date validations
   - Amount validations
   - Permission checks

3. **UI/UX** (test third):
   - Filter operations
   - Search functionality
   - Grid display
   - Error messages

## Estimated Completion Time

| Form | Lines | Hours | Notes |
|------|-------|-------|-------|
| Vehicle | 400 | 3-4 | Straightforward CRUD |
| Complaint | 450 | 4-5 | Status workflow complexity |
| Contract | 500 | 5-6 | Date calculations, renewals |
| Visitor | 400 | 3-4 | Check-in/out logic |
| FeeType | 300 | 2-3 | Simple configuration form |
| **Total** | **2,050** | **17-22** | **~5 business days** |

## Notes for Next Developer

1. **Reuse Code**: Copy FrmResidentManagement as template - it has all patterns
2. **Test as You Build**: Don't wait until end to compile/test
3. **Validation First**: Implement BLL validation before touching UI
4. **Grid Binding**: Use anonymous types with Cast<dynamic>()
5. **Error Handling**: Always include try-catch with user feedback
6. **Logging**: Every operation should have audit log entry
7. **Permissions**: Check session permissions at form load
8. **Database**: Ensure DAL methods exist before using in form

## Database Considerations

- All tables already exist (21 total)
- All DAL classes likely exist but might need enhancements
- Composite keys where needed (e.g., Visitor + ResidentID)
- Check foreign key constraints before deleting
- Null handling for optional fields

---

**Ready to proceed?** Answer "yes" to continue with Vehicle Management form.

