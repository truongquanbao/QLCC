using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public partial class FrmMainDashboard
{
    private void RenderReports()
    {
        var page = BeginPage("Báo cáo & thống kê", "Dashboard / Báo cáo");
        int w = PageWorkWidth();
        int y = 72;
        var residents = ResidentDAL.GetAllResidents();
        var apartments = ApartmentDAL.GetAllApartments();
        var invoices = InvoiceDAL.GetAllInvoices();
        var complaints = ComplaintDAL.GetAllComplaints();
        var vehicles = VehicleDAL.GetAllVehicles();
        var visitors = VisitorDAL.GetAllVisitors();
        int occupied = apartments.Count(a => ViStatus(a.Status) == "Đang sử dụng");
        int occupancyRate = apartments.Count == 0 ? 0 : (int)Math.Round(occupied * 100m / apartments.Count);
        decimal revenueTotal = invoices.Sum(i => i.PaidAmount);
        decimal debtTotal = invoices.Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount));
        int unpaidCount = invoices.Count(i => ViStatus(i.PaymentStatus) != "Đã thanh toán");

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 96);
        AddFilter(filters, "Kỳ báo cáo", "Tháng 05/2024", 14);
        AddFilter(filters, "Từ ngày", "01/05/2024", 238);
        AddFilter(filters, "Đến ngày", "17/05/2024", 462);
        AddFilter(filters, "Tòa nhà", "Tất cả", 686);
        var excel = ModernUi.Button("▥  Excel", ModernUi.Green, 100, 32);
        excel.Location = new Point(w - 330, 60);
        filters.Controls.Add(excel);
        var pdf = ModernUi.Button("PDF", ModernUi.Red, 88, 32);
        pdf.Location = new Point(w - 216, 60);
        filters.Controls.Add(pdf);
        var refresh = ModernUi.Button("⟳  Làm mới", ModernUi.Blue, 112, 32);
        refresh.Location = new Point(w - 116, 60);
        filters.Controls.Add(refresh);
        page.Controls.Add(filters);

        y += 112;
        int cardW = (w - 36 - 12 * 5) / 6;
        AddRow(page, y, 12,
            ModernUi.StatCard("Cư dân", residents.Count.ToString("N0"), "Người", ModernUi.Blue, "●●", $"{residents.Count(r => IsActiveStatus(r.Status))} hoạt động", cardW, 124),
            ModernUi.StatCard("Lấp đầy", $"{occupancyRate}%", "Căn hộ", ModernUi.Green, "◔", $"{occupied:N0}/{apartments.Count:N0}", cardW, 124),
            ModernUi.StatCard("Doanh thu", MoneyShort(revenueTotal), "VNĐ", ModernUi.Orange, "$", "Đã thu", cardW, 124),
            ModernUi.StatCard("Công nợ", MoneyShort(debtTotal), "VNĐ", ModernUi.Red, "!", $"{unpaidCount:N0} hóa đơn", cardW, 124),
            ModernUi.StatCard("Phản ánh", complaints.Count.ToString("N0"), "Phiếu", ModernUi.Purple, "▤", $"{complaints.Count(c => ViStatus(c.Status) == "Mới")} mới", cardW, 124),
            ModernUi.StatCard("Phương tiện", vehicles.Count.ToString("N0"), "Xe", ModernUi.Teal, "▣", $"{vehicles.Count(v => IsActiveStatus(v.Status))} hoạt động", cardW, 124));

        y += 140;
        int chartW = (w - 12) / 2;
        var revenue = ModernUi.Section("Doanh thu theo tháng (VNĐ)", chartW, 264);
        revenue.Location = new Point(18, y);
        var chart = new BarChartPanel
        {
            BarColor = ModernUi.Orange,
            AxisMax = 1,
            SeriesLabel = "Doanh thu (VNĐ)",
            Location = new Point(12, 42),
            Size = new Size(revenue.Width - 24, 188)
        };
        var monthly = invoices
            .GroupBy(i => new DateTime(i.Year, i.Month, 1))
            .OrderBy(g => g.Key)
            .TakeLast(12)
            .Select(g => (Label: $"T{g.Key.Month}", Value: ChartValue(g.Sum(i => i.PaidAmount))))
            .ToList();
        chart.AxisMax = Math.Max(1, monthly.Count == 0 ? 1 : (int)(monthly.Max(m => m.Value) * 1.2m));
        foreach (var item in monthly)
        {
            chart.Bars.Add(item);
        }
        revenue.Controls.Add(chart);
        page.Controls.Add(revenue);

        var operations = ModernUi.Section("Vận hành tòa nhà", w - chartW - 12, 264);
        operations.Location = new Point(revenue.Right + 12, y);
        int donutW = Math.Min(270, Math.Max(220, operations.Width - 280));
        var occupancy = new DonutChartPanel
        {
            Percent = occupancyRate,
            CenterText = $"{occupancyRate}%",
            SubText = "Lấp đầy",
            AccentColor = ModernUi.Green,
            Location = new Point(16, 48),
            Size = new Size(donutW, 168)
        };
        operations.Controls.Add(occupancy);
        var reportText = ModernUi.Label($"Phản ánh đã xử lý: {complaints.Count(c => ViStatus(c.Status) == "Đã xử lý"):N0} / {complaints.Count:N0}\r\nKhách ra vào hôm nay: {visitors.Count(v => ((DateTime)v.ArrivalTime).Date == DateTime.Today):N0} lượt\r\nCăn hộ bảo trì: {apartments.Count(a => ViStatus(a.Status) == "Bảo trì"):N0}\r\nPhương tiện hoạt động: {vehicles.Count(v => IsActiveStatus(v.Status)):N0} / {vehicles.Count:N0}",
            9.5f, FontStyle.Regular, ModernUi.Text);
        reportText.Location = new Point(donutW + 38, 64);
        reportText.Size = new Size(operations.Width - donutW - 56, 116);
        operations.Controls.Add(reportText);
        page.Controls.Add(operations);

        y += 280;
        int complaintW = (int)(w * 0.42);
        var complaintChart = ModernUi.Section("Phản ánh theo loại", complaintW, 234);
        complaintChart.Location = new Point(18, y);
        var complaintBars = new BarChartPanel
        {
            BarColor = ModernUi.Orange,
            AxisMax = 1,
            ShowValueLabels = true,
            SeriesLabel = "Số lượng phản ánh",
            Location = new Point(12, 42),
            Size = new Size(complaintChart.Width - 24, 160)
        };
        var complaintGroups = complaints
            .GroupBy(c => (string)Display(c.Category, "Khác"))
            .OrderByDescending(g => g.Count())
            .Take(6)
            .Select(g => (Label: g.Key.Length > 10 ? g.Key[..10] : g.Key, Value: g.Count()))
            .ToList();
        complaintBars.AxisMax = Math.Max(1, complaintGroups.Count == 0 ? 1 : (int)(complaintGroups.Max(g => g.Value) * 1.2m));
        foreach (var item in complaintGroups)
        {
            complaintBars.Bars.Add(item);
        }
        complaintChart.Controls.Add(complaintBars);
        page.Controls.Add(complaintChart);

        var saved = ModernUi.Section("Danh sách báo cáo đã tạo", w - complaintW - 12, 234);
        saved.Location = new Point(complaintChart.Right + 12, y);
        var savedGrid = CreateGrid(
            new[] { "Báo cáo", "Kỳ", "Người tạo", "Ngày tạo", "Định dạng", "Trạng thái" },
            new object[][]
            {
                new object[] { "Doanh thu tháng", "05/2024", "ketoan01", "17/05/2024 15:10", "Excel", "Thành công" },
                new object[] { "Công nợ cư dân", "05/2024", "manager1", "17/05/2024 14:40", "PDF", "Thành công" },
                new object[] { "Phản ánh vận hành", "Tuần 20", "manager1", "16/05/2024 18:20", "Excel", "Thành công" },
                new object[] { "Tài sản bảo trì", "05/2024", "admin1", "16/05/2024 16:05", "PDF", "Chờ xử lý" }
            });
        savedGrid.Location = new Point(12, 44);
        savedGrid.Size = new Size(saved.Width - 24, 150);
        saved.Controls.Add(savedGrid);
        page.Controls.Add(saved);
    }

    private void RenderSystemLogs()
    {
        var page = BeginPage("Log hệ thống", "Dashboard / Log hệ thống");
        int w = PageWorkWidth();
        int y = 72;
        var logs = AuditLogDAL.GetAuditLogs(limit: 100);
        var userByName = UserDAL.GetAllUsers()
            .Where(u => !string.IsNullOrWhiteSpace(u.Username))
            .GroupBy(u => u.Username!)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 96);
        AddFilter(filters, "Người dùng", "Tất cả", 14);
        AddFilter(filters, "Hành động", "Tất cả", 238);
        AddFilter(filters, "Mức độ", "Tất cả", 462);
        AddFilter(filters, "Từ ngày", "01/05/2024", 686);
        var search = ModernUi.TextBox("Tìm module, nội dung, IP...", Math.Min(340, w - 420));
        search.Location = new Point(14, 62);
        filters.Controls.Add(search);
        var export = ModernUi.Button("▥  Xuất log", ModernUi.Green, 120, 32);
        export.Location = new Point(w - 272, 60);
        filters.Controls.Add(export);
        var clear = ModernUi.Button("×  Xóa log cũ", ModernUi.Red, 132, 32);
        clear.Location = new Point(w - 138, 60);
        filters.Controls.Add(clear);
        page.Controls.Add(filters);

        y += 112;
        int leftW = (int)(w * 0.68);
        int rightW = w - leftW - 12;

        var list = ModernUi.Section("Nhật ký hoạt động", leftW, 460);
        list.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "Thời gian", "Người dùng", "Vai trò", "Hành động", "Module", "Nội dung", "IP", "Mức độ" },
            RowsOrEmpty(logs.Take(50), 8, (l, _) =>
            {
                string username = Display(l.Username, "system");
                userByName.TryGetValue(username, out var user);
                return new object[]
                {
                    DateTimeText(l.Timestamp),
                    username,
                    Display(user?.RoleName),
                    Display(l.Action),
                    Display(l.EntityName),
                    Display(l.Description, $"{l.Action} {l.EntityName}"),
                    Display(l.IPAddress),
                    AuditLevel(l.Action)
                };
            }));
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 356);
        list.Controls.Add(grid);
        var pager = ModernUi.Label($"Hiển thị 1 - {Math.Min(50, logs.Count)} / {logs.Count} dòng log", 9f, FontStyle.Regular, ModernUi.Text);
        pager.Location = new Point(18, 412);
        pager.Size = new Size(list.Width - 36, 28);
        list.Controls.Add(pager);
        page.Controls.Add(list);

        var detail = ModernUi.Section("Chi tiết log", rightW, 460);
        detail.Location = new Point(list.Right + 12, y);
        dynamic? selectedLog = logs.FirstOrDefault();
        string selectedUsername = selectedLog == null ? "" : Display(selectedLog.Username, "system");
        userByName.TryGetValue(selectedUsername, out var selectedUser);
        AddDetailField(detail, "Thời gian", selectedLog == null ? "" : DateTimeText(selectedLog.Timestamp), 48);
        AddDetailField(detail, "Người dùng", selectedUsername, 88);
        AddDetailField(detail, "Vai trò", Display(selectedUser?.RoleName), 128);
        AddDetailField(detail, "Module", selectedLog == null ? "" : Display(selectedLog.EntityName), 168);
        AddDetailField(detail, "Hành động", selectedLog == null ? "" : Display(selectedLog.Action), 208);
        AddDetailField(detail, "IP", selectedLog == null ? "" : Display(selectedLog.IPAddress), 248);
        AddDetailField(detail, "Mức độ", selectedLog == null ? "" : AuditLevel(selectedLog.Action), 288);
        var content = new TextBox
        {
            Text = selectedLog == null ? "" : Display(selectedLog.Description, $"{selectedLog.Action} {selectedLog.EntityName}"),
            Location = new Point(120, 328),
            Size = new Size(detail.Width - 150, 74),
            Multiline = true,
            Font = ModernUi.Font(9f)
        };
        detail.Controls.Add(content);
        page.Controls.Add(detail);

        y += 476;
        var security = ModernUi.Section("Cảnh báo bảo mật gần đây", w, 190);
        security.Location = new Point(18, y);
        var securityLogs = logs
            .Where(l => AuditLevel(l.Action) != "Thông tin")
            .Take(20)
            .ToList();
        var securityGrid = CreateGrid(
            new[] { "Thời gian", "Nguồn", "Sự kiện", "Mức độ", "Trạng thái" },
            RowsOrEmpty(securityLogs, 5, (l, _) => new object[]
            {
                DateTimeText(l.Timestamp),
                Display(l.IPAddress),
                Display(l.Description, $"{l.Action} {l.EntityName}"),
                AuditLevel(l.Action),
                "Đã ghi nhận"
            }, "Không có cảnh báo bảo mật"));
        securityGrid.Location = new Point(12, 44);
        securityGrid.Size = new Size(security.Width - 24, 108);
        security.Controls.Add(securityGrid);
        page.Controls.Add(security);
    }

    private void RenderSystemSettings()
    {
        var page = BeginPage("Cấu hình hệ thống", "Dashboard / Cấu hình hệ thống");
        int w = PageWorkWidth();
        int y = 72;
        int gap = 12;
        int colW = (w - gap) / 2;
        var systemConfigs = GetSystemConfigs();
        var connectionBuilder = new SqlConnectionStringBuilder(DatabaseHelper.GetConnectionString());
        int backupWarningDays = int.TryParse(ConfigValue(systemConfigs, "BackupWarningDays", "7"), out var parsedWarningDays) ? Math.Max(1, parsedWarningDays) : 7;
        string lastBackupRaw = ConfigValue(systemConfigs, "LastBackupAt", "");
        DateTime? lastBackupAt = ParseDashboardDate(lastBackupRaw);
        bool backupOverdue = IsBackupOverdue(lastBackupAt, backupWarningDays);
        string backupStatusText = lastBackupAt.HasValue ? $"Đã backup: {DateTimeText(lastBackupAt.Value)}" : "Chưa có dữ liệu backup";

        var general = ModernUi.Section("Cấu hình chung", colW, 284);
        general.Location = new Point(18, y);
        AddDetailField(general, "Tên hệ thống", ConfigValue(systemConfigs, "AppName", ConfigurationHelper.GetAppSetting("AppName")), 46);
        AddDetailField(general, "Múi giờ", ConfigValue(systemConfigs, "TimeZone", TimeZoneInfo.Local.Id), 86);
        AddDetailField(general, "Ngôn ngữ", ConfigValue(systemConfigs, "Language", "Tiếng Việt"), 126);
        AddDetailField(general, "Định dạng ngày", ConfigValue(systemConfigs, "DateTimeFormat", "dd/MM/yyyy HH:mm"), 166);
        AddDetailField(general, "Phiên bản", ConfigValue(systemConfigs, "AppVersion", ConfigurationHelper.GetAppSetting("AppVersion")), 206);
        AddResponsiveFormActions(general, 244);
        page.Controls.Add(general);

        var security = ModernUi.Section("Bảo mật đăng nhập", colW, 284);
        security.Location = new Point(general.Right + gap, y);
        AddDetailField(security, "Mật khẩu mạnh", ConfigValue(systemConfigs, "RequireStrongPassword", "Bắt buộc"), 46);
        AddDetailField(security, "Sai tối đa", $"{ConfigValue(systemConfigs, "MaxLoginAttempts", ConfigurationHelper.GetAppSetting("MaxLoginAttempts"))} lần", 86);
        AddDetailField(security, "Khóa tài khoản", $"{ConfigValue(systemConfigs, "LockDurationMinutes", ConfigurationHelper.GetAppSetting("LockDurationMinutes"))} phút", 126);
        AddDetailField(security, "Hết hạn mật khẩu", $"{ConfigurationHelper.GetAppSetting("PasswordExpirationDays")} ngày", 166);
        AddDetailField(security, "Phiên làm việc", $"{ConfigurationHelper.GetAppSetting("SessionTimeoutMinutes")} phút", 206);
        var saveSecurity = ModernUi.Button("▣  Lưu bảo mật", ModernUi.Blue, 138, 32);
        saveSecurity.Location = new Point(18, 244);
        security.Controls.Add(saveSecurity);
        page.Controls.Add(security);

        y += 300;
        var database = ModernUi.Section("Kết nối & dữ liệu", colW, 300);
        database.Location = new Point(18, y);
        AddDetailField(database, "SQL Server", connectionBuilder.DataSource, 46);
        AddDetailField(database, "Database", connectionBuilder.InitialCatalog, 86);
        AddDetailField(database, "Xác thực", connectionBuilder.IntegratedSecurity ? "Windows" : "SQL Login", 126);
        AddDetailField(database, "Backup gần nhất", BackupDisplayText(lastBackupRaw), 166);
        AddDetailField(database, "Thư mục backup", ConfigValue(systemConfigs, "BackupPath", ".\\backups"), 206);
        var test = ModernUi.Button("✓  Test kết nối", ModernUi.Green, 138, 32);
        test.Location = new Point(18, 252);
        database.Controls.Add(test);
        var save = ModernUi.Button("▣  Lưu cấu hình", ModernUi.Blue, 138, 32);
        save.Location = new Point(168, 252);
        database.Controls.Add(save);
        page.Controls.Add(database);

        var backup = ModernUi.Section("Backup / Restore", colW, 300);
        backup.Location = new Point(database.Right + gap, y);
        var backupNow = ModernUi.Button("▤  Backup ngay", ModernUi.Orange, Math.Max(150, (backup.Width - 48) / 2), 48);
        backupNow.Location = new Point(18, 56);
        backupNow.Click += (_, _) => RunDatabaseBackup();
        backup.Controls.Add(backupNow);
        var restore = ModernUi.Button("↥  Restore từ file", ModernUi.Blue, Math.Max(150, (backup.Width - 48) / 2), 48);
        restore.Location = new Point(backupNow.Right + 12, 56);
        backup.Controls.Add(restore);
        var autoBackup = ModernUi.Label(
            $"Tự động backup: {ConfigValue(systemConfigs, "AutoBackupTime", "02:00")} hằng ngày\r\n" +
            $"Giữ bản sao lưu: {ConfigValue(systemConfigs, "BackupRetentionDays", "30")} ngày\r\n" +
            $"Kiểm tra toàn vẹn dữ liệu: {ConfigValue(systemConfigs, "VerifyBackup", "Đã bật")}\r\n" +
            $"Cảnh báo backup quá hạn: {ConfigValue(systemConfigs, "BackupWarningDays", "7")} ngày",
            9.5f, FontStyle.Regular, ModernUi.Text);
        autoBackup.Location = new Point(22, 126);
        autoBackup.Size = new Size(backup.Width - 44, 112);
        backup.Controls.Add(autoBackup);
        string backupState = backupOverdue ? $"Cần backup (quá {backupWarningDays} ngày)" : backupStatusText;
        var backupStatus = ModernUi.Badge($"Trạng thái: {backupState}", backupOverdue ? ModernUi.Orange : ModernUi.Green);
        backupStatus.Location = new Point(22, 244);
        backupStatus.Size = new Size(backup.Width - 44, 32);
        backup.Controls.Add(backupStatus);
        page.Controls.Add(backup);

        y += 316;
        var configs = ModernUi.Section("Bảng cấu hình hệ thống", w, 226);
        configs.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "ConfigKey", "ConfigValue", "Mô tả", "Cập nhật lúc", "Cập nhật bởi" },
            RowsOrEmpty(systemConfigs, 5, (c, _) => new object[] { c.ConfigKey, c.ConfigValue, Display(c.Description), DateTimeText(c.UpdatedAt), Display(c.UpdatedBy) }));
        grid.Location = new Point(12, 44);
        grid.Size = new Size(configs.Width - 24, 146);
        configs.Controls.Add(grid);
        page.Controls.Add(configs);
    }
}
