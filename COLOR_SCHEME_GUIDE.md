# Dashboard Color Scheme Reference

## Official Color Palette

### Primary Colors

**Blue (Primary)**
- Name: ModernUi.Blue
- Usage: Main action buttons, primary dashboards, important metrics
- Common use: Stat cards, navigation highlights, primary charts
- Contrast: High on white/light backgrounds

**Green (Success/Positive)**
- RGB: (34, 197, 94)
- Hex: #22C55E
- Usage: Revenue metrics, active residents, successful operations, completion status
- Admin Dashboard: Revenue card
- Manager Dashboard: New complaints (actionable), occupancy completion
- Resident Dashboard: Apartment info, payment status

**Orange (Warning/Caution)**
- RGB: (249, 115, 22)
- Hex: #F97316
- Usage: Backup warnings, invoice editing, maintenance alerts
- Admin Dashboard: Backup alerts, invoice action buttons
- Quick Actions: Edit data button
- System alerts for attention-needed items

**Red (Alert/Critical)**
- RGB: (239, 68, 68)
- Hex: #EF4444
- Usage: Unpaid invoices, overdue items, critical alerts
- All Dashboards: Unpaid invoice indicator
- Quick Actions: Delete operations
- Error states and critical status

**Cyan (Secondary/Accent)**
- RGB: (6, 182, 212)
- Hex: #06B6D4
- Usage: Occupancy rates, availability metrics, scheduling
- Admin Dashboard: Occupancy stat card
- Manager Dashboard: Maintenance schedules
- Resident Dashboard: Complaint tracking

### Supporting Colors

**Gray/Slate (Neutral/Disabled)**
- RGB: (100, 116, 139) - #64748B
- Usage: Neutral actions, refresh buttons, cancel buttons
- Quick Actions: Refresh/reload button

**Dark Slate (Secondary Actions)**
- RGB: (51, 65, 85) - #334155
- Usage: Reports, less critical actions
- Quick Actions: Report/export button

**Muted Text**
- RGB: (148, 163, 184) - #94A3B8
- Usage: Secondary text, hints, group headers
- Sidebar: Group header labels
- Table headers: Secondary information

## Usage Guidelines by Section

### Admin Dashboard

#### Stat Cards (Row 1)
1. **Total Accounts** → Blue (primary metric)
2. **Total Residents** → Blue (primary metric)
3. **Total Apartments** → Blue (primary metric)
4. **Occupancy Rate** → Cyan (availability metric)
5. **Monthly Revenue** → Green (positive/monetized)
6. **Unpaid Invoices** → Red (alert state)

#### Charts Section
- **Revenue Chart** → Blue bars (primary chart)
- **Occupancy Donut** → Cyan (complementary)
- **System Alert** → Orange (backup) or Green (normal status)

#### Quick Actions (2x3 Grid)
1. Add → Green (#22C55E) - Creation action
2. Edit → Orange (#F97316) - Modification action
3. Delete → Red (#EF4444) - Destructive action
4. Save → Blue (ModernUi.Blue) - Primary action
5. Refresh → Gray (#64748B) - Neutral action
6. Reports → Dark Slate (#334155) - Secondary action

### Manager Dashboard

#### Stat Cards (Row 1)
1. **Current Residents** → Blue
2. **New Complaints** → Green
3. **Unpaid Invoices** → Red
4. **Upcoming Maintenance** → Cyan
5. **Today's Visitors** → Blue
6. **Occupancy Rate** → Green

#### Charts Section
- **Complaint Types** → Blue bars
- **New Complaints Grid** → Text with status indicators
- **Maintenance Schedule** → Cyan accent
- **Notifications** → Blue icons

### Resident Dashboard

#### Stat Cards (Row 1)
1. **Personal Info** → Blue (#0066CC)
2. **Apartment Info** → Green (#22C55E)
3. **Latest Invoice** → Orange (#F97316)
4. **Payment Status** → Green (#22C55E)
5. **Unread Notifications** → Blue
6. **Active Complaints** → Cyan (#06B6D4)

#### Sections Below
- **Invoices Table** → Text styling with Green for paid status
- **Notifications** → Blue accent markers
- **Complaint Timeline** → Status-based colors
- **QR Code Section** → Blue badge

### Sidebar Navigation

#### Group Headers
- Color: Muted (#94A3B8)
- Font Size: 8.4f Bold
- Text: "Quản lý", "Vận hành", "Hệ thống"

#### Menu Buttons
- Normal: Dark background (0, 46, 99)
- Text: Light gray (226, 232, 240)
- Hover: Darker shade (15, 23, 42)
- Active: Blue highlight (ModernUi.Blue)
- Font: Regular 9.8f (reduced from Bold 10.5f)

## Semantic Color Usage

### Data Status Indicators

**New/Pending**
- Preferred: Gray/Muted (#94A3B8)
- Alternative: Light gray
- Not recommended: Yellow (hard to read)

**In Progress/Processing**
- Preferred: Blue (ModernUi.Blue)
- Alternative: Cyan (#06B6D4)
- Represents: Active action, ongoing

**Completed/Resolved**
- Preferred: Green (#22C55E)
- Alternative: Cyan
- Represents: Success, completed state

**Problem/Overdue**
- Preferred: Red (#EF4444)
- Alternative: Orange (for caution)
- Represents: Attention needed, critical

**Blocked/Maintenance**
- Preferred: Orange (#F97316)
- Alternative: Gray
- Represents: Temporary unavailability

## Color Combination Matrix

### Recommended Combinations
- Blue + Green: Primary/Secondary actions ✅
- Blue + Red: Important with alert ✅
- Green + Red: Success/Failure contrast ✅
- Blue + Orange: Primary/Warning ✅
- Green + Orange: Success/Caution ✅

### Avoid These Combinations
- Red + Green: Can cause colorblind issues ❌
- Orange + Red: Too similar, hard to distinguish ❌
- Too many colors in one section ❌

## Accessibility Considerations

### Contrast Ratios (WCAG AA Standard: 4.5:1 for text)

- Blue text on white: ✅ 2.4:1 (for large UI elements)
- Green text on white: ✅ 2.7:1
- Red text on white: ✅ 2.1:1 (borderline, use sparingly for text)
- Orange text on white: ✅ 2.2:1

### For Colorblind Users

**Red-Green Colorblind (Deuteranopia)**
- Use Blue + Cyan instead of Green + Red for critical info ✅
- Add text labels to all color-coded items ✅
- Avoid red-green as sole differentiator ✅

**Blue-Yellow Colorblind (Tritanopia)**
- Blue (#0066CC) and Orange (#F97316) are distinguishable ✅
- Use blue-green combinations ✅

## Implementation in Code

### Colors by Location

**File**: `ApartmentManager\GUI\Forms\FrmMainDashboard.cs`

**Admin Dashboard Colors**:
```csharp
Line 1105-1110: Stat card colors
- ModernUi.Blue, ModernUi.Blue, ModernUi.Blue, 
  Color.FromArgb(6, 182, 212), // Cyan
  Color.FromArgb(34, 197, 94), // Green
  ModernUi.Red
```

**Quick Action Colors**:
```csharp
Line 1243-1258:
- Color.FromArgb(34, 197, 94)   // Green - Add
- Color.FromArgb(249, 115, 22)  // Orange - Edit
- Color.FromArgb(239, 68, 68)   // Red - Delete
- ModernUi.Blue                 // Blue - Save
- Color.FromArgb(100, 116, 139) // Gray - Refresh
- Color.FromArgb(51, 65, 85)    // Slate - Reports
```

**Sidebar Colors**:
```csharp
Line 247-250: Menu items
- Color.FromArgb(226, 232, 240) // Light gray text
- Color.FromArgb(15, 23, 42)    // Dark hover
- Color.FromArgb(148, 163, 184) // Muted headers
```

## Testing Color Consistency

### Checklist for Manual Testing

- [ ] All blue elements use ModernUi.Blue or consistent #0066CC
- [ ] Revenue/success metrics show green (#22C55E)
- [ ] All unpaid invoices display red (#EF4444)
- [ ] All occupancy/availability shows cyan (#06B6D4)
- [ ] Backup warnings show orange (#F97316)
- [ ] No random colors outside palette
- [ ] Hover states work on all buttons
- [ ] Text contrast meets WCAG AA standard
- [ ] Colors remain consistent across zoom levels
- [ ] Print preview maintains color scheme

## Future Color Enhancements

**Complaint Status Colors** (for next phase):
- New: Gray (#94A3B8)
- In Progress: Blue (#0066CC)
- Resolved: Green (#22C55E)

**Payment Status Colors** (for next phase):
- Pending: Orange (#F97316)
- Partial: Yellow (#EAB308) - requires testing
- Paid: Green (#22C55E)
- Overdue: Red (#EF4444)

**Asset Status Colors** (for next phase):
- Available: Green (#22C55E)
- In Use: Blue (#0066CC)
- Maintenance: Orange (#F97316)
- Unavailable: Red (#EF4444)
