# Dashboard Redesign - QA & Testing Checklist

## Pre-Launch Verification

### Build & Compilation ✅
- [x] Solution builds without errors
- [x] No compiler warnings in FrmMainDashboard.cs
- [x] All dependencies resolved
- [x] No missing method references
- [x] Timer and form disposal logic intact

### Code Review ✅
- [x] Sidebar grouping logic correct
- [x] Auto-refresh timer implementation safe
- [x] Color values consistent
- [x] No breaking changes to public API
- [x] All three dashboard types updated

---

## Visual Testing Checklist

### Admin Dashboard
- [ ] **Stat Cards Display Correctly**
  - [ ] Row 1: 6 cards visible and aligned
  - [ ] Colors match spec: Blue (4), Green (1), Red (1)
  - [ ] Text readable (high contrast)
  - [ ] Card heights consistent (all 162px)
  - [ ] No overlapping or misalignment

- [ ] **Charts Section**
  - [ ] Revenue bar chart renders
  - [ ] Blue bars display correctly
  - [ ] Occupancy donut chart visible
  - [ ] System alert icon/text clear
  - [ ] Orange alert visible if backup overdue
  - [ ] Green checkmark for normal status

- [ ] **Complaints Section**
  - [ ] Data grid displays recent complaints
  - [ ] Table columns aligned properly
  - [ ] Scrolling works if content exceeds area
  - [ ] "View all" button clickable

- [ ] **Quick Actions Section**
  - [ ] 6 tiles visible in 2 rows (3 columns each)
  - [ ] All tiles same size
  - [ ] Color: Green (add), Orange (edit), Red (delete)
  - [ ] Color: Blue (save), Gray (refresh), Slate (reports)
  - [ ] Icons visible (#⊕, ✎, ▥, ▣, ×, ▤)
  - [ ] Hover effects work on all buttons
  - [ ] Click handlers fire correctly

### Manager Dashboard
- [ ] **Stat Cards (6 cards)**
  - [ ] Colors: Blue (2), Green (2), Red (1), Cyan (1)
  - [ ] Values update from database
  - [ ] Card labels clear and readable
  - [ ] Layout consistent with admin view

- [ ] **Charts Section**
  - [ ] Complaint types bar chart renders
  - [ ] New complaints grid displays
  - [ ] "View all complaints" button works
  - [ ] Layout balanced (left/right split)

- [ ] **Maintenance Schedule**
  - [ ] 7-day schedule table visible
  - [ ] Dates formatted correctly (dd/MM/yyyy)
  - [ ] Status indicators working
  - [ ] No data display issues

- [ ] **Notifications Section**
  - [ ] Recent notifications list shows (5 max)
  - [ ] Timestamps displayed
  - [ ] Scrolling works if more than 5

### Resident Dashboard
- [ ] **Personal Info Cards (6 cards)**
  - [ ] Personal info card: Blue background
  - [ ] Apartment info card: Green background
  - [ ] Invoice card: Orange background
  - [ ] Payment status: Green background
  - [ ] Notifications card: Blue background
  - [ ] Complaints card: Cyan background
  - [ ] All text readable

- [ ] **Invoice Section**
  - [ ] Table displays recent invoices
  - [ ] Status column shows paid/unpaid
  - [ ] Dates formatted correctly
  - [ ] Amounts formatted with thousand separators

- [ ] **Notifications Section**
  - [ ] Recent notifications display (5 max)
  - [ ] Each shows title and timestamp
  - [ ] Text truncated if too long

- [ ] **Complaint Timeline**
  - [ ] Timeline displays correctly
  - [ ] Status indicators visible
  - [ ] Progress indicators working

- [ ] **QR Code Section**
  - [ ] QR code placeholder visible
  - [ ] Payment info displays below QR
  - [ ] Blue badge shows info text
  - [ ] Layout responsive

---

## Sidebar Navigation Testing

- [ ] **Group Headers Display**
  - [ ] "Quản lý" header visible
  - [ ] "Vận hành" header visible
  - [ ] "Hệ thống" header visible (admin only)
  - [ ] Headers have muted text color
  - [ ] Headers don't interfere with menu items

- [ ] **Menu Items Organized**
  - [ ] Items under correct group headers
  - [ ] No items orphaned above first group
  - [ ] All items accessible (not cut off)

- [ ] **Menu Navigation**
  - [ ] Click each menu item navigates correctly
  - [ ] Active item highlights in blue
  - [ ] Hover effects work (darker background)
  - [ ] Icons display properly
  - [ ] No visual glitches on hover

- [ ] **User Role Variants**
  - [ ] Admin sees all 13 items + 3 groups
  - [ ] Manager sees 11 items + 3 groups
  - [ ] Resident sees 10 items + 3 groups
  - [ ] Wrong roles don't see each other's items

- [ ] **Scrolling**
  - [ ] Sidebar scrolls if content exceeds height
  - [ ] Can scroll to all items on small screens
  - [ ] Smooth scroll (no stuttering)

---

## Auto-Refresh Functionality

- [ ] **Timer Initialization**
  - [ ] Timer starts after form is shown
  - [ ] No errors in console/debug output
  - [ ] Timer interval is 10 seconds (or configured value)

- [ ] **Dashboard Reloads**
  - [ ] Admin dashboard data updates every 10 seconds
  - [ ] Manager dashboard data updates every 10 seconds
  - [ ] Resident dashboard data updates every 10 seconds
  - [ ] No visible flicker or UI jumping

- [ ] **Form Closure**
  - [ ] Timer stops when form closes
  - [ ] No memory leaks on close
  - [ ] No exceptions in Output window
  - [ ] Focus/selection not lost on refresh

- [ ] **Data Updates**
  - [ ] New complaints appear after adding
  - [ ] Invoice counts update correctly
  - [ ] Statistics refresh with new data
  - [ ] Charts update with latest values

---

## Color Consistency Verification

### Colors by Dashboard
- [ ] **Admin Dashboard**
  - [ ] 4 stat cards use blue
  - [ ] 1 stat card uses green (revenue)
  - [ ] 1 stat card uses red (unpaid)
  - [ ] Revenue chart bars are blue
  - [ ] Occupancy chart is cyan/blue

- [ ] **Manager Dashboard**
  - [ ] Residents stat card: Blue
  - [ ] New complaints stat card: Green
  - [ ] Unpaid invoices stat card: Red
  - [ ] Maintenance stat card: Cyan
  - [ ] Visitors stat card: Blue
  - [ ] Occupancy stat card: Green

- [ ] **Resident Dashboard**
  - [ ] Personal info card: Blue
  - [ ] Apartment info card: Green
  - [ ] Invoice card: Orange
  - [ ] Payment status card: Green
  - [ ] Notifications card: Blue
  - [ ] Complaints card: Cyan

### Quick Action Buttons
- [ ] Add button: Green (#22C55E)
- [ ] Edit button: Orange (#F97316)
- [ ] Delete button: Red (#EF4444)
- [ ] Save button: Blue (ModernUi.Blue)
- [ ] Refresh button: Gray (#64748B)
- [ ] Report button: Dark slate (#334155)

### Text Colors
- [ ] Group headers: Muted gray (#94A3B8)
- [ ] Menu text: Light gray (226, 232, 240)
- [ ] Button text: Appropriate contrast
- [ ] No white text on very light backgrounds
- [ ] No black text on dark backgrounds

---

## Cross-Browser/Platform Testing

### Windows 10/11 Display
- [ ] Colors display correctly at 100% scale
- [ ] Colors display correctly at 125% scale
- [ ] Colors display correctly at 150% scale
- [ ] No color banding or artifacting

### Screen Resolutions
- [ ] 1024x768 (minimum): Layout doesn't break
- [ ] 1366x768 (typical): Optimal display
- [ ] 1920x1080 (HD): Spacing looks good
- [ ] 2560x1440 (4K): Text readable

### DPI/Scaling
- [ ] Normal DPI (96): Perfect rendering
- [ ] High DPI (144+): Text sharp and readable
- [ ] Mixed DPI setup: No visual issues

---

## Accessibility Testing

- [ ] **Keyboard Navigation**
  - [ ] Tab through all menu items works
  - [ ] Enter/Space activates menu items
  - [ ] Sidebar scrollable with keyboard
  - [ ] All buttons reachable via keyboard

- [ ] **Color Contrast**
  - [ ] All text meets WCAG AA (4.5:1 for body text)
  - [ ] All UI elements distinguishable
  - [ ] No red-green only indicators
  - [ ] Color-blind users can distinguish elements

- [ ] **Screen Reader**
  - [ ] Menu items read correctly
  - [ ] Stat card labels announced
  - [ ] Data table headers read properly
  - [ ] Button labels announced

---

## Performance Testing

- [ ] **CPU Usage**
  - [ ] Dashboard at rest: <5% CPU
  - [ ] During auto-refresh: <10% CPU
  - [ ] Multiple dashboards open: <15% CPU
  - [ ] No memory leaks after 1 hour running

- [ ] **Memory Usage**
  - [ ] Initial load: Baseline
  - [ ] After 1 hour: No significant increase
  - [ ] After closing/reopening tabs: Memory freed

- [ ] **Responsiveness**
  - [ ] Clicking menu items: <100ms response
  - [ ] Dashboard loads: <500ms
  - [ ] Auto-refresh doesn't cause UI lag
  - [ ] Scrolling is smooth

---

## Data Integrity Testing

- [ ] **Numbers Display Correctly**
  - [ ] Stat card values match database
  - [ ] Sum calculations correct (e.g., total accounts)
  - [ ] Percentages calculated correctly (e.g., occupancy rate)
  - [ ] Currency formatted with thousand separators

- [ ] **Date/Time Display**
  - [ ] Dates formatted as dd/MM/yyyy
  - [ ] Times formatted as HH:mm
  - [ ] Timezones handled correctly
  - [ ] Timestamps update with refresh

- [ ] **Status Indicators**
  - [ ] "Mới" status displays correctly
  - [ ] "Đang xử lý" status displays correctly
  - [ ] "Đã xử lý" status displays correctly
  - [ ] Color-coded status indicators accurate

---

## Error Handling

- [ ] **Network Errors**
  - [ ] Dashboard degrades gracefully if DB unavailable
  - [ ] Error message displayed (not blank dashboard)
  - [ ] Auto-refresh doesn't crash on network error
  - [ ] User can retry/reload

- [ ] **Database Errors**
  - [ ] No unhandled exceptions shown to users
  - [ ] Errors logged to appropriate location
  - [ ] Application remains stable
  - [ ] Sidebar still functional

- [ ] **Form Lifecycle**
  - [ ] Closing form stops all timers
  - [ ] Reopening form creates fresh timer
  - [ ] No resource leaks detected
  - [ ] Multiple form instances work independently

---

## User Acceptance Testing (UAT)

### Admin Users
- [ ] Navigation to all admin features works
- [ ] Dashboard data trust (numbers make sense)
- [ ] Quick action buttons perform intended actions
- [ ] Auto-refresh helpful (not annoying)
- [ ] Overall UI feels professional

### Manager Users
- [ ] Can navigate manager-specific items
- [ ] Complaint data displays correctly
- [ ] Maintenance schedule useful
- [ ] Notifications section helpful
- [ ] No confusion with admin features

### Resident Users
- [ ] Can see their information correctly
- [ ] Invoice history complete
- [ ] Payment status clear
- [ ] Complaint tracking helpful
- [ ] Interface not overwhelming

---

## Sign-Off Checklist

| Component | Status | Tester | Date | Notes |
|-----------|--------|--------|------|-------|
| Build & Compilation | ✅ | Auto | - | No errors |
| Visual Layout | ⬜ | Manual | - | - |
| Colors | ⬜ | Manual | - | - |
| Sidebar | ⬜ | Manual | - | - |
| Auto-Refresh | ⬜ | Manual | - | - |
| Performance | ⬜ | Manual | - | - |
| Accessibility | ⬜ | Manual | - | - |
| Data Accuracy | ⬜ | Manual | - | - |
| UAT | ⬜ | Users | - | - |

---

## Known Issues & Workarounds

### Issue: Colors appear different on different monitors
**Cause**: Monitor calibration differences
**Workaround**: Use provided RGB/Hex values as reference, not visual match
**Status**: Not a code issue

### Issue: Auto-refresh loses focus on text input
**Cause**: Form reload clears focused control
**Workaround**: Users should complete input before 10-second mark
**Fix**: (Future enhancement) Preserve focus state

### Issue: Sidebar scrolls on small screens
**Cause**: Menu longer than sidebar height
**Current behavior**: Scrollbar appears
**Status**: Working as designed

---

## Deployment Notes

1. **No Database Migration Required**
   - Changes are UI/styling only
   - No schema changes
   - No data migration needed

2. **No Configuration Changes**
   - Default auto-refresh: 10 seconds
   - Can be adjusted via code constant
   - No config file updates needed

3. **Backward Compatibility**
   - All existing features still work
   - No breaking changes to navigation
   - Users don't need retraining

4. **Rollback Plan**
   - Simple if needed: revert single file
   - No database cleanup required
   - Users can continue working during rollback

---

## Post-Launch Monitoring

- [ ] Monitor for crash reports related to FrmMainDashboard
- [ ] Track performance metrics (CPU, memory)
- [ ] Gather user feedback on new UI
- [ ] Monitor error logs for exceptions
- [ ] Verify auto-refresh working for all users

**Support Period**: 2 weeks post-launch for critical issues

---

## Testing Sign-Off

**QA Lead**: _________________ **Date**: _______

**Product Owner**: _________________ **Date**: _______

**Development Lead**: _________________ **Date**: _______
