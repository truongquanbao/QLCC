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
    private void RenderAdminDashboard()
    {
        var page = BeginPage("Dashboard", "Trang chủ / Tổng quan hệ thống");
        const int margin = 20;
        const int gap = 14;
        const int statCardHeight = 118;
        const int chartRowHeight = 268;
        const int bottomRowHeight = 312;
        const int topOffset = 86;

        var users = UserDAL.GetAllUsers();
        var residents = ResidentDAL.GetAllResidents();
        var apartments = ApartmentDAL.GetAllApartments();
        var invoices = InvoiceDAL.GetAllInvoices();
        var complaintsData = ComplaintDAL.GetAllComplaints();
        var configs = GetSystemConfigs();

        int occupied = apartments.Count(a => ViStatus(a.Status) == "Đang sử dụng");
        int vacant = apartments.Count(a => ViStatus(a.Status) == "Đang trống");
        int occupancyRate = apartments.Count == 0 ? 0 : (int)Math.Round(occupied * 100m / apartments.Count);
        int vacancyRate = Math.Max(0, 100 - occupancyRate);

        var latestInvoice = invoices
            .OrderByDescending(i => i.Year)
            .ThenByDescending(i => i.Month)
            .FirstOrDefault();

        var periodInvoices = latestInvoice == null
            ? new List<InvoiceDTO>()
            : invoices.Where(i => i.Year == latestInvoice.Year && i.Month == latestInvoice.Month).ToList();

        decimal monthRevenue = periodInvoices.Sum(i => i.PaidAmount);
        var unpaidInvoices = invoices.Where(i => ViStatus(i.PaymentStatus) != "Đã thanh toán").ToList();
        decimal unpaidAmount = unpaidInvoices.Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount));

        int backupWarningDays = int.TryParse(ConfigValue(configs, "BackupWarningDays", "7"), out var parsedWarningDays)
            ? Math.Max(1, parsedWarningDays)
            : 7;
        DateTime? lastBackupAt = ParseDashboardDate(ConfigValue(configs, "LastBackupAt", ""));
        bool backupOverdue = IsBackupOverdue(lastBackupAt, backupWarningDays);
        string backupText = lastBackupAt.HasValue ? DateTimeText(lastBackupAt.Value) : "Chưa có dữ liệu";

        var statCards = new[]
        {
            DashboardUi.CreateStatCard("Số tài khoản", users.Count.ToString("N0"), "Tài khoản", ModernUi.Blue, "●", $"{users.Count(u => IsActiveStatus(u.Status))} hoạt động", 300, statCardHeight),
            DashboardUi.CreateStatCard("Số cư dân", residents.Count.ToString("N0"), "Người", ModernUi.Blue, "◉", $"{residents.Count(r => IsActiveStatus(r.Status))} đang cư trú", 300, statCardHeight),
            DashboardUi.CreateStatCard("Số căn hộ", apartments.Count.ToString("N0"), "Căn hộ", ModernUi.Blue, "▦", $"{occupied:N0} đang ở", 300, statCardHeight),
            DashboardUi.CreateStatCard("Lấp đầy", $"{occupancyRate}%", "Đơn vị", ModernUi.Teal, "◔", $"{occupied:N0} / {apartments.Count:N0}", 300, statCardHeight),
            DashboardUi.CreateStatCard("Doanh thu", Money(monthRevenue), "VNĐ", ModernUi.Green, "$", latestInvoice == null ? "Chưa có dữ liệu" : $"Kỳ {latestInvoice.Month:00}/{latestInvoice.Year}", 300, statCardHeight),
            DashboardUi.CreateStatCard("Nợ chưa thu", Money(unpaidAmount), "VNĐ", ModernUi.Red, "▤", $"{unpaidInvoices.Count:N0} hóa đơn", 300, statCardHeight)
        };
        foreach (var statCard in statCards)
        {
            page.Controls.Add(statCard);
        }

        var revenue = ModernUi.Section("Doanh thu theo tháng (VNĐ)", 620, chartRowHeight);
        var revenueChart = new DashboardBarChartPanel
        {
            SeriesLabel = "Doanh thu (VNĐ)",
            EmptyMessage = "Chưa có dữ liệu doanh thu",
            BarColor = ModernUi.Orange
        };
        revenue.Controls.Add(revenueChart);
        page.Controls.Add(revenue);

        var monthlyRevenue = invoices
            .GroupBy(i => new DateTime(i.Year, i.Month, 1))
            .OrderBy(g => g.Key)
            .TakeLast(12)
            .Select(g => (Label: $"T{g.Key.Month}", Value: g.Sum(i => i.PaidAmount)))
            .ToList();
        revenueChart.AxisMax = monthlyRevenue.Count == 0 ? 0 : Math.Max(1m, monthlyRevenue.Max(m => m.Value) * 1.2m);
        revenueChart.Bars.AddRange(monthlyRevenue);

        var occupancy = ModernUi.Section("Tỷ lệ lấp đầy căn hộ", 360, chartRowHeight);
        var occupancyChart = new DashboardDonutChartPanel
        {
            Percent = occupancyRate,
            AccentColor = ModernUi.Green,
            CenterText = $"{occupancyRate}%",
            SubText = "Đang ở",
            PrimaryLabel = "Đang ở",
            PrimaryValue = $"{occupied:N0} ({occupancyRate}%)",
            SecondaryLabel = "Còn trống",
            SecondaryValue = $"{vacant:N0} ({vacancyRate}%)"
        };
        occupancy.Controls.Add(occupancyChart);
        page.Controls.Add(occupancy);

        var alert = ModernUi.Section("Cảnh báo hệ thống", 260, bottomRowHeight);
        alert.BackColor = backupOverdue ? Color.FromArgb(255, 249, 235) : Color.FromArgb(241, 251, 245);
        alert.BorderColor = backupOverdue ? Color.FromArgb(241, 213, 153) : Color.FromArgb(193, 230, 208);

        Control alertIcon = backupOverdue
            ? new CircleLabel
            {
                Text = "!",
                CircleColor = ModernUi.Orange,
                ForeColor = Color.White,
                Font = ModernUi.Font(26f, FontStyle.Bold),
                Size = new Size(76, 76)
            }
            : new CircleLabel
            {
                Text = "✓",
                CircleColor = ModernUi.Green,
                ForeColor = Color.White,
                Font = ModernUi.Font(26f, FontStyle.Bold),
                Size = new Size(76, 76)
            };
        alert.Controls.Add(alertIcon);

        var alertHeadline = ModernUi.Label(
            backupOverdue ? $"CHƯA BACKUP QUÁ {backupWarningDays} NGÀY" : "TRẠNG THÁI BACKUP ỔN ĐỊNH",
            10.1f,
            FontStyle.Bold,
            backupOverdue ? Color.FromArgb(180, 88, 10) : Color.FromArgb(22, 101, 52));
        alertHeadline.TextAlign = ContentAlignment.MiddleCenter;
        alertHeadline.AutoEllipsis = true;
        alert.Controls.Add(alertHeadline);

        var alertSub = ModernUi.Label($"Lần backup gần nhất: {backupText}", 8.9f, FontStyle.Regular, ModernUi.Text);
        alertSub.TextAlign = ContentAlignment.MiddleCenter;
        alertSub.AutoEllipsis = true;
        alert.Controls.Add(alertSub);

        var backup = new RoundedButton
        {
            Text = "Backup ngay",
            BackColor = ModernUi.Orange,
            HoverBackColor = ControlPaint.Light(ModernUi.Orange, 0.08f),
            Size = new Size(150, 38),
            CornerRadius = 10
        };
        backup.Click += (_, _) => RunDatabaseBackup();
        alert.Controls.Add(backup);
        page.Controls.Add(alert);

        var complaints = ModernUi.Section("Phản ánh đang xử lý", 520, bottomRowHeight);
        var openComplaints = complaintsData
            .Where(c => ViStatus(c.Status) is not ("Đã xử lý" or "Đã đóng"))
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .ToList();

        var complaintsGrid = CreateGrid(
            new[] { "STT", "Mã phản ánh", "Nội dung", "Cư dân", "Căn hộ" },
            RowsOrEmpty(openComplaints, 5, (c, i) => new object[]
            {
                i + 1,
                $"PA{c.CreatedAt:yyMMdd}-{c.ComplaintID:000}",
                c.Title,
                c.ResidentName,
                c.ApartmentCode
            }));
        DashboardUi.ApplySummaryGridStyle(complaintsGrid);
        complaintsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        complaintsGrid.ScrollBars = ScrollBars.Vertical;
        complaintsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        complaintsGrid.ColumnHeadersDefaultCellStyle.Font = ModernUi.Font(7.8f, FontStyle.Bold);
        complaintsGrid.DefaultCellStyle.Font = ModernUi.Font(7.8f);
        complaints.Controls.Add(complaintsGrid);

        var allComplaints = ModernUi.OutlineButton("Xem tất cả phản ánh →", 174, 30);
        allComplaints.Click += (_, _) => Navigate("complaints");
        complaints.Controls.Add(allComplaints);
        page.Controls.Add(complaints);

        var actions = ModernUi.Section("Thao tác nhanh", 320, bottomRowHeight);
        page.Controls.Add(actions);
        AddAdminQuickActions(actions);

        void LayoutDashboard()
        {
            int viewportWidth = Math.Max(540, page.ClientSize.Width - (page.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
            int availableWidth = Math.Max(480, viewportWidth - margin * 2);

            int statWidth = Math.Max(150, (availableWidth - gap * 2) / 3);
            int statBlockWidth = statWidth * 3 + gap * 2;
            int statLeft = margin + Math.Max(0, (availableWidth - statBlockWidth) / 2);

            int row1Y = topOffset;
            int row2Y = row1Y + statCardHeight + gap;
            int chartsY = row2Y + statCardHeight + 18;
            int bottomY = chartsY + chartRowHeight + 18;

            for (int i = 0; i < statCards.Length; i++)
            {
                int col = i % 3;
                int row = i / 3;
                int x = statLeft + col * (statWidth + gap);
                int y = row == 0 ? row1Y : row2Y;
                statCards[i].SetBounds(x, y, statWidth, statCardHeight);
            }

            int donutWidth = Math.Clamp((int)Math.Round(availableWidth * 0.38), 330, 410);
            int revenueWidth = Math.Max(420, availableWidth - donutWidth - gap);
            if (revenueWidth + donutWidth + gap > availableWidth)
            {
                donutWidth = Math.Max(300, availableWidth - revenueWidth - gap);
            }
            int chartBlockWidth = revenueWidth + donutWidth + gap;
            int chartLeft = margin + Math.Max(0, (availableWidth - chartBlockWidth) / 2);

            revenue.SetBounds(chartLeft, chartsY, revenueWidth, chartRowHeight);
            revenueChart.SetBounds(16, 46, revenue.Width - 32, revenue.Height - 60);

            occupancy.SetBounds(revenue.Right + gap, chartsY, donutWidth, chartRowHeight);
            occupancyChart.SetBounds(12, 40, occupancy.Width - 24, occupancy.Height - 50);

            int alertWidth = availableWidth >= 1220 ? 280 : 260;
            int actionsWidth = availableWidth >= 1220 ? 320 : 300;
            int complaintsWidth = availableWidth - alertWidth - actionsWidth - gap * 2;

            int bottomBlockWidth = alertWidth + complaintsWidth + actionsWidth + gap * 2;
            int bottomLeft = margin + Math.Max(0, (availableWidth - bottomBlockWidth) / 2);

            alert.SetBounds(bottomLeft, bottomY, alertWidth, bottomRowHeight);
            alertIcon.Location = new Point((alert.Width - alertIcon.Width) / 2, 48);
            alertHeadline.SetBounds(18, 136, alert.Width - 36, 44);
            alertSub.SetBounds(18, 182, alert.Width - 36, 42);
            backup.Location = new Point((alert.Width - backup.Width) / 2, alert.Height - backup.Height - 14);

            complaints.SetBounds(alert.Right + gap, bottomY, complaintsWidth, bottomRowHeight);
            complaintsGrid.SetBounds(12, 46, complaints.Width - 24, complaints.Height - 94);
            allComplaints.Location = new Point(16, complaints.Height - 42);

            if (complaintsGrid.Columns.Count == 5)
            {
                int fixedWidth = 50 + 120 + 150 + 90;
                int contentWidth = Math.Max(180, complaintsGrid.ClientSize.Width - fixedWidth - 8);
                complaintsGrid.Columns[0].Width = 50;
                complaintsGrid.Columns[1].Width = 120;
                complaintsGrid.Columns[2].Width = contentWidth;
                complaintsGrid.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                complaintsGrid.Columns[3].Width = 150;
                complaintsGrid.Columns[4].Width = 90;
            }

            actions.SetBounds(complaints.Right + gap, bottomY, actionsWidth, bottomRowHeight);
            AddAdminQuickActions(actions);

            page.AutoScrollMinSize = new Size(0, bottomY + bottomRowHeight + 24);
        }

        page.Resize += (_, _) => LayoutDashboard();
        LayoutDashboard();
    }

    private void AddAdminQuickActions(Control actions)
    {
        foreach (Control control in actions.Controls.Cast<Control>().ToList())
        {
            if (control is Label)
            {
                continue;
            }

            actions.Controls.Remove(control);
            control.Dispose();
        }

        const int columns = 2;
        const int tileH = 78;
        const int padX = 14;
        const int gapX = 10;
        const int gapY = 10;
        const int top = 46;
        int tileW = Math.Max(96, (actions.ClientSize.Width - padX * 2 - gapX) / columns);
        int startX = padX;

        Point TilePosition(int index)
        {
            int col = index % columns;
            int row = index / columns;
            return new Point(startX + col * (tileW + gapX), top + row * (tileH + gapY));
        }

        var addPos = TilePosition(0);
        var addTile = AddActionTile(actions, "➕", "Thêm mới", string.Empty, Color.FromArgb(34, 197, 94), addPos.X, addPos.Y, width: tileW, height: tileH);
        BindTileClick(addTile, (_, _) => ShowQuickActionMenu(addTile,
            ("Thêm tài khoản", () => NavigateWithQuickAction("accounts", "add")),
            ("Thêm căn hộ", () => NavigateWithQuickAction("apartments", "add")),
            ("Thêm phương tiện", () => NavigateWithQuickAction("vehicles", "add")),
            ("Đăng ký khách", () => NavigateWithQuickAction("visitors", "add")),
            ("Thêm tài sản", () => NavigateWithQuickAction("assets", "add")),
            ("Mở trang cư dân", () => Navigate("residents")),
            ("Mở trang hóa đơn", () => Navigate("invoices")),
            ("Mở trang phản ánh", () => Navigate("complaints"))));

        var editPos = TilePosition(1);
        var editTile = AddActionTile(actions, "✏", "Sửa dữ liệu", string.Empty, Color.FromArgb(249, 115, 22), editPos.X, editPos.Y, width: tileW, height: tileH);
        BindTileClick(editTile, (_, _) => ShowQuickActionMenu(editTile,
            ("Tài khoản", () => Navigate("accounts")),
            ("Cư dân", () => Navigate("residents")),
            ("Căn hộ", () => Navigate("apartments")),
            ("Hóa đơn", () => Navigate("invoices")),
            ("Phản ánh", () => Navigate("complaints")),
            ("Phương tiện", () => Navigate("vehicles")),
            ("Khách ra vào", () => Navigate("visitors")),
            ("Tài sản", () => Navigate("assets"))));

        var deletePos = TilePosition(2);
        var deleteTile = AddActionTile(actions, "🗑", "Xóa dữ liệu", string.Empty, Color.FromArgb(239, 68, 68), deletePos.X, deletePos.Y, width: tileW, height: tileH);
        BindTileClick(deleteTile, (_, _) => ShowQuickActionMenu(deleteTile,
            ("Tài khoản", () => Navigate("accounts")),
            ("Cư dân", () => Navigate("residents")),
            ("Căn hộ", () => Navigate("apartments")),
            ("Hóa đơn", () => Navigate("invoices")),
            ("Phản ánh", () => Navigate("complaints")),
            ("Phương tiện", () => Navigate("vehicles")),
            ("Khách ra vào", () => Navigate("visitors")),
            ("Tài sản", () => Navigate("assets"))));

        var savePos = TilePosition(3);
        var saveTile = AddActionTile(actions, "💾", "Lưu dữ liệu", string.Empty, ModernUi.Blue, savePos.X, savePos.Y, width: tileW, height: tileH);
        BindTileClick(saveTile, (_, _) => SaveDashboardSnapshot());

        var refreshPos = TilePosition(4);
        var refreshTile = AddActionTile(actions, "↻", "Làm mới", string.Empty, Color.FromArgb(100, 116, 139), refreshPos.X, refreshPos.Y, width: tileW, height: tileH);
        BindTileClick(refreshTile, (_, _) => ReloadCurrentPage());

        var reportPos = TilePosition(5);
        var reportTile = AddActionTile(actions, "📊", "Báo cáo", string.Empty, Color.FromArgb(51, 65, 85), reportPos.X, reportPos.Y, width: tileW, height: tileH);
        BindTileClick(reportTile, (_, _) => ShowQuickActionMenu(reportTile,
            ("Xuất báo cáo lấp đầy (.xlsx)", () => SaveGeneratedFile(
                ReportsBLL.GenerateOccupancyReport(),
                "Excel Workbook (*.xlsx)|*.xlsx",
                "QuickOccupancyReport")),
            ("Xuất danh sách cư dân (.csv)", () => SaveGeneratedFile(
                ReportsBLL.ExportDataToCSV("residents"),
                "CSV (*.csv)|*.csv",
                "QuickResidentsExport")),
            ("Xuất danh sách hóa đơn (.csv)", () => SaveGeneratedFile(
                ReportsBLL.ExportDataToCSV("invoices"),
                "CSV (*.csv)|*.csv",
                "QuickInvoicesExport")),
            ("Mở trang báo cáo", () => Navigate("reports"))));
    }

    private void AddInvoiceQuickActions(
        Control actions,
        Func<InvoiceDTO?>? selectedInvoiceProvider = null,
        Action<int?>? reloadInvoices = null)
    {
        actions.Controls.Clear();

        const int pad = 16;
        const int gap = 10;
        const int startY = 42;

        int availableW = Math.Max(260, actions.Width - pad * 2);
        int tileW = Math.Max(120, (availableW - gap) / 2);

        int availableH = Math.Max(180, actions.Height - startY - pad);
        int tileH = Math.Max(48, Math.Min(58, (availableH - gap * 2) / 3));

        int x1 = pad;
        int x2 = pad + tileW + gap;

        int y1 = startY;
        int y2 = y1 + tileH + gap;
        int y3 = y2 + tileH + gap;

        var btnCreate = AddActionTile(actions, "+", "Tạo hóa đơn", "Theo tháng", ModernUi.Blue, x1, y1, width: tileW, height: tileH);
        BindTileClick(btnCreate, (_, _) =>
        {
            CreateInvoiceQuickAction(reloadInvoices);
        });

        var btnCalculate = AddActionTile(actions, "=", "Tính phí", "Tự động", ModernUi.Green, x2, y1, width: tileW, height: tileH);
        BindTileClick(btnCalculate, (_, _) =>
        {
            GenerateMonthlyInvoicesQuickAction(reloadInvoices);
        });

        var btnUpdate = AddActionTile(actions, "✓", "Cập nhật", "Thanh toán", ModernUi.Orange, x1, y2, width: tileW, height: tileH);
        BindTileClick(btnUpdate, (_, _) =>
        {
            RecordInvoicePaymentQuickAction(selectedInvoiceProvider, reloadInvoices);
        });

        var btnPrint = AddActionTile(actions, "P", "In hóa đơn", "Bản in", ModernUi.Purple, x2, y2, width: tileW, height: tileH);
        BindTileClick(btnPrint, (_, _) =>
        {
            PrintInvoiceQuickAction(selectedInvoiceProvider);
        });

        var btnExportExcel = AddActionTile(actions, "X", "Xuất Excel", "File Excel", ModernUi.Green, x1, y3, width: tileW, height: tileH);
        BindTileClick(btnExportExcel, (_, _) =>
        {
            SaveGeneratedFile(
                ReportsBLL.ExportDataToCSV("invoices"),
                "CSV (*.csv)|*.csv",
                "InvoiceExportCSV");
        });

        var btnExportPdf = AddActionTile(actions, "PDF", "Xuất PDF", "File PDF", ModernUi.Red, x2, y3, width: tileW, height: tileH);
        BindTileClick(btnExportPdf, (_, _) =>
        {
            SaveGeneratedFile(
                ReportsBLL.ExportDataToCSV("invoices"),
                "CSV (*.csv)|*.csv",
                "InvoiceExportForPDF");
        });
    }

    private void CreateInvoiceQuickAction(Action<int?>? reloadInvoices)
    {
        var apartments = ApartmentDAL.GetAllApartments()
            .Where(a => a.ApartmentID > 0)
            .OrderBy(a => Display(a.ApartmentCode, ""))
            .ToList();

        if (apartments.Count == 0)
        {
            MessageBox.Show(this, "Chưa có căn hộ để tạo hóa đơn.", "Tạo hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = CreateAssetDialog("Tạo hóa đơn", 480, 350);
        var title = ModernUi.Label("Tạo hóa đơn phí dịch vụ", 10f, FontStyle.Bold, ModernUi.Navy);
        title.SetBounds(18, 16, dialog.ClientSize.Width - 36, 24);
        dialog.Controls.Add(title);

        var apartmentInput = AddAssetValueCombo(
            dialog,
            "Căn hộ",
            apartments.Select(a => (
                Text: $"{Display(a.ApartmentCode, $"Căn hộ {a.ApartmentID}")} - {Display(a.BuildingName, "Tòa nhà")}",
                Value: a.ApartmentID.ToString(CultureInfo.InvariantCulture))),
            apartments[0].ApartmentID.ToString(CultureInfo.InvariantCulture),
            18,
            54,
            210);

        var monthInput = AddAssetValueCombo(
            dialog,
            "Tháng",
            Enumerable.Range(1, 12).Select(m => (Text: $"{m:00}", Value: m.ToString(CultureInfo.InvariantCulture))),
            DateTime.Today.Month.ToString(CultureInfo.InvariantCulture),
            246,
            54,
            82);

        var yearInput = AddAssetInput(dialog, "Năm", DateTime.Today.Year.ToString(CultureInfo.InvariantCulture), 346, 54, 96);
        var dueInput = AddAssetInput(dialog, "Hạn thanh toán", DateText(DateTime.Today.AddDays(14)), 18, 112, 210);
        var amountInput = AddAssetInput(dialog, "Số tiền", BuildCalculatedInvoiceAmount(apartments[0]).ToString("N0", CultureInfo.InvariantCulture), 246, 112, 196);
        var noteInput = AddAssetInput(dialog, "Ghi chú", $"Phí dịch vụ tháng {DateTime.Today.Month:00}/{DateTime.Today.Year}", 18, 170, 424);

        apartmentInput.SelectedIndexChanged += (_, _) =>
        {
            int apartmentId = ComboBoxHelper.GetSelectedValueInt(apartmentInput);
            var apartment = apartments.FirstOrDefault(a => a.ApartmentID == apartmentId);
            if (apartment != null)
            {
                amountInput.Text = BuildCalculatedInvoiceAmount(apartment).ToString("N0", CultureInfo.InvariantCulture);
            }
        };

        var cancel = ModernUi.OutlineButton("Hủy", 96, 34);
        var save = ModernUi.Button("Tạo hóa đơn", ModernUi.Blue, 126, 34);
        cancel.Location = new Point(dialog.ClientSize.Width - 244, 276);
        save.Location = new Point(dialog.ClientSize.Width - 140, 276);
        dialog.Controls.Add(cancel);
        dialog.Controls.Add(save);

        cancel.Click += (_, _) => dialog.DialogResult = DialogResult.Cancel;
        save.Click += (_, _) =>
        {
            int apartmentId = ComboBoxHelper.GetSelectedValueInt(apartmentInput);
            int month = ComboBoxHelper.GetSelectedValueInt(monthInput);
            bool validYear = int.TryParse(yearInput.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int year);
            DateTime? dueDate = ParseAssetDate(dueInput.Text);
            bool validAmount = TryParseInvoiceMoney(amountInput.Text, out decimal amount);

            if (apartmentId <= 0 || month is < 1 or > 12 || !validYear || !dueDate.HasValue || !validAmount || amount <= 0)
            {
                MessageBox.Show(dialog, "Vui lòng nhập đầy đủ căn hộ, kỳ hóa đơn, hạn thanh toán và số tiền hợp lệ.", "Tạo hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = InvoiceBLL.CreateInvoice(apartmentId, month, year, dueDate.Value.Date, amount, noteInput.Text.Trim());
            if (!result.Success)
            {
                MessageBox.Show(dialog, result.Message, "Tạo hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AuditLogDAL.LogAction(_session?.UserID, "Create_Invoice_Quick", "Invoice", result.InvoiceID, $"Tạo hóa đơn nhanh {month:00}/{year}");
            dialog.Tag = result.InvoiceID;
            dialog.DialogResult = DialogResult.OK;
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            int invoiceId = dialog.Tag is int id ? id : 0;
            MessageBox.Show(this, "Đã tạo hóa đơn.", "Tạo hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
            reloadInvoices?.Invoke(invoiceId > 0 ? invoiceId : null);
        }
    }

    private void GenerateMonthlyInvoicesQuickAction(Action<int?>? reloadInvoices)
    {
        var allApartments = ApartmentDAL.GetAllApartments()
            .Where(a => a.ApartmentID > 0)
            .OrderBy(a => Display(a.ApartmentCode, ""))
            .ToList();

        if (allApartments.Count == 0)
        {
            MessageBox.Show(this, "Chưa có căn hộ để tính phí.", "Tính phí", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = CreateAssetDialog("Tính phí tự động", 450, 308);
        var title = ModernUi.Label("Tạo hóa đơn phí dịch vụ theo tháng", 10f, FontStyle.Bold, ModernUi.Navy);
        title.SetBounds(18, 16, dialog.ClientSize.Width - 36, 24);
        dialog.Controls.Add(title);

        var monthInput = AddAssetValueCombo(
            dialog,
            "Tháng",
            Enumerable.Range(1, 12).Select(m => (Text: $"{m:00}", Value: m.ToString(CultureInfo.InvariantCulture))),
            DateTime.Today.Month.ToString(CultureInfo.InvariantCulture),
            18,
            54,
            110);
        var yearInput = AddAssetInput(dialog, "Năm", DateTime.Today.Year.ToString(CultureInfo.InvariantCulture), 146, 54, 110);
        var dueInput = AddAssetInput(dialog, "Hạn thanh toán", DateText(DateTime.Today.AddDays(14)), 274, 54, 140);
        var noteInput = AddAssetInput(dialog, "Ghi chú", "Phí dịch vụ tự động", 18, 112, 396);

        var help = ModernUi.Label("Áp dụng cho căn hộ đang sử dụng/đang thuê. Căn hộ đã có hóa đơn cùng kỳ sẽ được bỏ qua.", 8.8f, FontStyle.Regular, ModernUi.Muted);
        help.SetBounds(18, 170, 396, 42);
        dialog.Controls.Add(help);

        var cancel = ModernUi.OutlineButton("Hủy", 96, 34);
        var save = ModernUi.Button("Tạo hàng loạt", ModernUi.Green, 126, 34);
        cancel.Location = new Point(dialog.ClientSize.Width - 244, 234);
        save.Location = new Point(dialog.ClientSize.Width - 140, 234);
        dialog.Controls.Add(cancel);
        dialog.Controls.Add(save);

        cancel.Click += (_, _) => dialog.DialogResult = DialogResult.Cancel;
        save.Click += (_, _) =>
        {
            int month = ComboBoxHelper.GetSelectedValueInt(monthInput);
            bool validYear = int.TryParse(yearInput.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int year);
            DateTime? dueDate = ParseAssetDate(dueInput.Text);

            if (month is < 1 or > 12 || !validYear || !dueDate.HasValue)
            {
                MessageBox.Show(dialog, "Vui lòng nhập tháng, năm và hạn thanh toán hợp lệ.", "Tính phí", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            dialog.Tag = (month, year, dueDate.Value.Date, noteInput.Text.Trim());
            dialog.DialogResult = DialogResult.OK;
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Tag is not ValueTuple<int, int, DateTime, string> values)
        {
            return;
        }

        var billableApartments = allApartments.Where(IsInvoiceBillableApartment).ToList();
        if (billableApartments.Count == 0)
        {
            billableApartments = allApartments;
        }

        int created = 0;
        int skipped = 0;
        int failed = 0;
        int firstInvoiceId = 0;
        string firstError = string.Empty;

        foreach (var apartment in billableApartments)
        {
            decimal amount = BuildCalculatedInvoiceAmount(apartment);
            string note = string.IsNullOrWhiteSpace(values.Item4)
                ? $"Phí dịch vụ tháng {values.Item1:00}/{values.Item2}"
                : values.Item4;
            var result = InvoiceBLL.CreateInvoice(apartment.ApartmentID, values.Item1, values.Item2, values.Item3, amount, note);
            if (result.Success)
            {
                created++;
                if (firstInvoiceId <= 0)
                {
                    firstInvoiceId = result.InvoiceID;
                }
            }
            else if (result.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                skipped++;
            }
            else
            {
                failed++;
                if (string.IsNullOrWhiteSpace(firstError))
                {
                    firstError = result.Message;
                }
            }
        }

        AuditLogDAL.LogAction(_session?.UserID, "Generate_Invoices_Quick", "Invoice", description: $"Tạo {created} hóa đơn kỳ {values.Item1:00}/{values.Item2}");

        string summary = $"Đã tạo {created:N0} hóa đơn kỳ {values.Item1:00}/{values.Item2}.\nBỏ qua {skipped:N0} hóa đơn đã tồn tại.";
        if (failed > 0)
        {
            summary += $"\nLỗi {failed:N0} căn hộ" + (string.IsNullOrWhiteSpace(firstError) ? "." : $": {firstError}");
        }

        MessageBox.Show(this, summary, "Tính phí", MessageBoxButtons.OK, failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        reloadInvoices?.Invoke(firstInvoiceId > 0 ? firstInvoiceId : null);
    }

    private void RecordInvoicePaymentQuickAction(Func<InvoiceDTO?>? selectedInvoiceProvider, Action<int?>? reloadInvoices)
    {
        var selectedInvoice = selectedInvoiceProvider?.Invoke();
        if (selectedInvoice == null)
        {
            MessageBox.Show(this, "Vui lòng chọn một hóa đơn trong danh sách trước khi cập nhật thanh toán.", "Cập nhật thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var invoice = InvoiceDAL.GetInvoiceByID(selectedInvoice.InvoiceID) ?? selectedInvoice;
        decimal remaining = invoice.RemainingAmount > 0
            ? invoice.RemainingAmount
            : Math.Max(0m, invoice.TotalAmount - invoice.PaidAmount);

        if (remaining <= 0)
        {
            MessageBox.Show(this, "Hóa đơn này đã thanh toán đủ.", "Cập nhật thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = CreateAssetDialog("Cập nhật thanh toán", 450, 326);
        var title = ModernUi.Label($"{InvoiceCode(invoice)} - Căn hộ {Display(invoice.ApartmentCode)}", 10f, FontStyle.Bold, ModernUi.Navy);
        title.SetBounds(18, 16, dialog.ClientSize.Width - 36, 24);
        dialog.Controls.Add(title);

        var amountInput = AddAssetInput(dialog, "Số tiền thanh toán", remaining.ToString("N0", CultureInfo.InvariantCulture), 18, 58, 190);
        var dateInput = AddAssetInput(dialog, "Ngày ghi nhận", DateText(DateTime.Today), 226, 58, 188);
        var methodInput = AddAssetValueCombo(
            dialog,
            "Phương thức",
            new[]
            {
                ("Tiền mặt", "Cash"),
                ("Chuyển khoản", "Transfer"),
                ("Quẹt thẻ", "Card")
            },
            "Cash",
            18,
            116,
            190);
        var note = ModernUi.Label($"Còn phải thu: {Money(remaining)} VNĐ", 9f, FontStyle.Bold, ModernUi.Red);
        note.SetBounds(226, 132, 188, 28);
        dialog.Controls.Add(note);

        var cancel = ModernUi.OutlineButton("Hủy", 96, 34);
        var save = ModernUi.Button("Ghi nhận", ModernUi.Orange, 112, 34);
        cancel.Location = new Point(dialog.ClientSize.Width - 226, 252);
        save.Location = new Point(dialog.ClientSize.Width - 122, 252);
        dialog.Controls.Add(cancel);
        dialog.Controls.Add(save);

        cancel.Click += (_, _) => dialog.DialogResult = DialogResult.Cancel;
        save.Click += (_, _) =>
        {
            DateTime? paymentDate = ParseAssetDate(dateInput.Text);
            if (!paymentDate.HasValue || !TryParseInvoiceMoney(amountInput.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show(dialog, "Vui lòng nhập ngày ghi nhận và số tiền hợp lệ.", "Cập nhật thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (amount > remaining)
            {
                MessageBox.Show(dialog, "Số tiền thanh toán không được vượt quá số còn phải thu.", "Cập nhật thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = InvoiceBLL.RecordPayment(invoice.InvoiceID, amount, paymentDate.Value, ComboBoxHelper.GetSelectedValueString(methodInput));
            if (!result.Success)
            {
                MessageBox.Show(dialog, result.Message, "Cập nhật thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AuditLogDAL.LogAction(_session?.UserID, "Record_Invoice_Payment_Quick", "Invoice", invoice.InvoiceID, $"Ghi nhận thanh toán {amount:N0}");
            dialog.DialogResult = DialogResult.OK;
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            MessageBox.Show(this, "Đã cập nhật thanh toán.", "Cập nhật thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Information);
            reloadInvoices?.Invoke(invoice.InvoiceID);
        }
    }

    private void PrintInvoiceQuickAction(Func<InvoiceDTO?>? selectedInvoiceProvider)
    {
        var selectedInvoice = selectedInvoiceProvider?.Invoke();
        if (selectedInvoice == null)
        {
            MessageBox.Show(this, "Vui lòng chọn một hóa đơn trong danh sách trước khi in.", "In hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var invoice = InvoiceDAL.GetInvoiceByID(selectedInvoice.InvoiceID) ?? selectedInvoice;
        var resident = FindInvoiceResident(invoice);

        using var dialog = new SaveFileDialog
        {
            Title = "Lưu bản in hóa đơn",
            Filter = "Text file (*.txt)|*.txt",
            FileName = $"hoa-don-{InvoiceCode(invoice)}.txt",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            RestoreDirectory = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        File.WriteAllText(dialog.FileName, BuildInvoicePrintContent(invoice, resident), new System.Text.UTF8Encoding(true));
        AuditLogDAL.LogAction(_session?.UserID, "Print_Invoice_Quick", "Invoice", invoice.InvoiceID, $"Tạo bản in hóa đơn: {dialog.FileName}");
        MessageBox.Show(this, "Đã tạo bản in hóa đơn.", "In hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static bool IsInvoiceBillableApartment(ApartmentDTO apartment)
    {
        string status = ViStatus(apartment.Status);
        return status is "Đang sử dụng" or "Đang thuê";
    }

    private static decimal BuildCalculatedInvoiceAmount(ApartmentDTO apartment)
    {
        decimal area = apartment.Area > 0 ? apartment.Area : 60m;
        decimal managementFee = area * 12000m;
        decimal serviceFee = 250000m;
        decimal hygieneFee = 80000m;
        decimal sharedUtilityFee = 120000m;
        decimal total = managementFee + serviceFee + hygieneFee + sharedUtilityFee;
        return Math.Ceiling(total / 1000m) * 1000m;
    }

    private static bool TryParseInvoiceMoney(string value, out decimal amount)
    {
        string text = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            amount = 0;
            return false;
        }

        string normalized = text
            .Replace(" ", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("VNĐ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("VND", "", StringComparison.OrdinalIgnoreCase)
            .Replace("đ", "", StringComparison.OrdinalIgnoreCase);

        if (normalized.All(char.IsDigit) &&
            decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            return true;
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out amount) ||
               decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    private static ResidentDTO? FindInvoiceResident(InvoiceDTO invoice)
    {
        try
        {
            if (invoice.ResidentID.HasValue && invoice.ResidentID.Value > 0)
            {
                var resident = ResidentDAL.GetResidentByID(invoice.ResidentID.Value);
                if (resident != null)
                {
                    return resident;
                }
            }

            return ResidentDAL.GetAllResidents()
                .FirstOrDefault(r => r.ApartmentID == invoice.ApartmentID);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildInvoicePrintContent(InvoiceDTO invoice, ResidentDTO? resident)
    {
        decimal remaining = invoice.RemainingAmount > 0
            ? invoice.RemainingAmount
            : Math.Max(0m, invoice.TotalAmount - invoice.PaidAmount);

        var lines = new List<string>
        {
            "PHIẾU THU / HÓA ĐƠN PHÍ CHUNG CƯ",
            "----------------------------------",
            $"Mã hóa đơn: {InvoiceCode(invoice)}",
            $"Căn hộ: {Display(invoice.ApartmentCode)}",
            $"Chủ hộ / cư dân: {Display(resident?.FullName)}",
            $"Điện thoại: {Display(resident?.Phone)}",
            $"Kỳ phí: {invoice.Month:00}/{invoice.Year}",
            $"Hạn thanh toán: {DateText(invoice.DueDate)}",
            "",
            $"Tổng tiền: {Money(invoice.TotalAmount)} VNĐ",
            $"Đã thanh toán: {Money(invoice.PaidAmount)} VNĐ",
            $"Còn phải thu: {Money(remaining)} VNĐ",
            $"Trạng thái: {ViStatus(invoice.PaymentStatus)}",
            "",
            $"Ghi chú: {Display(invoice.Note)}",
            $"Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm}",
            "",
            "Người lập phiếu                         Người nộp tiền",
            "",
            "",
            "__________________                      __________________"
        };

        return string.Join(Environment.NewLine, lines);
    }

    private void RenderManagerDashboard()
    {
        var page = BeginPage("Dashboard", "Trang chủ / Dashboard");
        int x = 14;
        int w = Math.Max(1150, _content.ClientSize.Width - 28);
        int y = 83;
        int gap = 16;
        int cardW = (w - gap * 5) / 6;
        var residents = ResidentDAL.GetAllResidents();
        var apartments = ApartmentDAL.GetAllApartments();
        var invoices = InvoiceDAL.GetAllInvoices();
        var complaints = ComplaintDAL.GetAllComplaints();
        var visitors = VisitorDAL.GetAllVisitors();
        var schedules = AssetDAL.GetMaintenanceSchedules();
        var notifications = NotificationDAL.GetAllNotifications();
        int occupied = apartments.Count(a => ViStatus(a.Status) == "Đang sử dụng");
        int occupancyRate = apartments.Count == 0 ? 0 : (int)Math.Round(occupied * 100m / apartments.Count);
        int unpaidInvoices = invoices.Count(i => ViStatus(i.PaymentStatus) != "Đã thanh toán");
        int todayVisitors = visitors.Count(v => ((DateTime)v.ArrivalTime).Date == DateTime.Today);

        decimal debtAmount = invoices.Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount));
        bool hasDebt = unpaidInvoices > 0 && debtAmount > 0;

        AddRowAt(page, x, y, gap,
            ModernUi.StatCard("Số cư dân hiện tại", residents.Count.ToString("N0"), "Người", ModernUi.Blue, "●●", $"{residents.Count(r => IsActiveStatus(r.Status))} đang cư trú", cardW, 162),
            ModernUi.StatCard("Phản ánh mới", complaints.Count(c => ViStatus(c.Status) == "Mới").ToString("N0"), "Phản ánh", Color.FromArgb(34, 197, 94), "▤", $"{complaints.Count:N0} tổng phiếu", cardW, 162),
            CreateDebtCard(unpaidInvoices, debtAmount, cardW, hasDebt),
            ModernUi.StatCard("Bảo trì sắp tới", schedules.Count(s => s.ScheduledDate <= DateTime.Today.AddDays(7)).ToString("N0"), "Lịch", Color.FromArgb(6, 182, 212), "⚒", "Trong 7 ngày tới", cardW, 162),
            ModernUi.StatCard("Khách hôm nay", todayVisitors.ToString("N0"), "Khách", ModernUi.Blue, "●", $"{visitors.Count:N0} tổng lượt", cardW, 162),
            ModernUi.StatCard("Lấp đầy căn hộ", $"{occupancyRate}%", "Đơn vị", Color.FromArgb(34, 197, 94), "◔", $"{occupied:N0}/{apartments.Count:N0}", cardW, 162));

        y += 176;

        int leftW = (int)((w - gap) * 0.42);
        int rightW = w - leftW - gap;
        int topPanelH = 272;

        var chartPanel = ModernUi.Section("Top loại phản ánh nhiều nhất", leftW, topPanelH);
        chartPanel.Location = new Point(x, y);
        AddSectionDots(chartPanel);
        var axisTitle = ModernUi.Label("Số lượng", 8.6f, FontStyle.Regular, ModernUi.Text);
        axisTitle.Location = new Point(16, 32);
        axisTitle.Size = new Size(90, 20);
        chartPanel.Controls.Add(axisTitle);
        var chart = new BarChartPanel
        {
            Location = new Point(12, 52),
            Size = new Size(chartPanel.Width - 24, 194),
            BarColor = ModernUi.Orange,
            AxisMax = 50,
            GridSteps = 5,
            ShowValueLabels = true,
            SeriesLabel = "Số lượng phản ánh"
        };
        var complaintGroups = complaints
            .GroupBy(c => (string)Display(c.Category, "Khác"))
            .OrderByDescending(g => g.Count())
            .Take(6)
            .Select(g => (Label: g.Key.Length > 10 ? g.Key[..10] : g.Key, Value: g.Count()))
            .ToList();
        chart.AxisMax = Math.Max(1, complaintGroups.Count == 0 ? 1 : (int)(complaintGroups.Max(g => g.Value) * 1.2m));
        chart.Bars.AddRange(complaintGroups);
        chartPanel.Controls.Add(chart);
        page.Controls.Add(chartPanel);

        var newComplaints = ModernUi.Section("Phản ánh mới cần xử lý", rightW, topPanelH);
        newComplaints.Location = new Point(chartPanel.Right + gap, y);
        AddSectionDots(newComplaints);
        var grid = CreateGrid(
            new[] { "STT", "Mã phản ánh", "Nội dung", "Căn hộ", "Người gửi", "Thời gian", "Ưu tiên", "Trạng thái" },
            RowsOrEmpty(complaints.Where(c => ViStatus(c.Status) == "Mới").Take(5), 8, (c, i) => new object[]
            {
                i + 1,
                $"PA{c.CreatedAt:yyMMdd}-{c.ComplaintID:000}",
                c.Title,
                c.ApartmentCode,
                c.ResidentName,
                DateTimeText(c.CreatedAt),
                ViStatus(c.Priority),
                ViStatus(c.Status)
            }));
        grid.Location = new Point(12, 38);
        grid.Size = new Size(newComplaints.Width - 24, 192);
        newComplaints.Controls.Add(grid);
        var link = ModernUi.OutlineButton("Xem tất cả phản ánh mới  →", 210, 30);
        link.Location = new Point(newComplaints.Width - 232, 231);
        link.Click += (_, _) => Navigate("complaints");
        newComplaints.Controls.Add(link);
        page.Controls.Add(newComplaints);

        y += 285;

        int bottomPanelH = 255;
        var schedule = ModernUi.Section("Lịch bảo trì 7 ngày tới", leftW, bottomPanelH);
        schedule.Location = new Point(x, y);
        AddSectionDots(schedule);
        var scheduleGrid = CreateGrid(
            new[] { "Ngày", "Hạng mục", "Khu vực", "Nội dung", "Trạng thái" },
            RowsOrEmpty(schedules.Take(5), 5, (s, _) => new object[]
            {
                DateText(s.ScheduledDate),
                s.Category,
                s.Location,
                Display(s.Note, s.AssetName),
                ViStatus(s.Status)
            }, "Không có lịch bảo trì"));
        scheduleGrid.Location = new Point(12, 42);
        scheduleGrid.Size = new Size(schedule.Width - 24, 194);
        schedule.Controls.Add(scheduleGrid);
        page.Controls.Add(schedule);

        var notice = ModernUi.Section("Thông báo nhanh", rightW, bottomPanelH);
        notice.Location = new Point(schedule.Right + gap, y);
        var latestNotices = notifications.Take(5).ToList();
        if (latestNotices.Count == 0)
        {
            AddNoticeRow(notice, 0, "Không có thông báo", "");
        }
        for (int i = 0; i < latestNotices.Count; i++)
        {
            AddNoticeRow(notice, i, Display(latestNotices[i].Title, Display(latestNotices[i].Message)), DateTimeText(latestNotices[i].CreatedAt));
        }
        page.Controls.Add(notice);

        static void AddRowAt(Control parent, int startX, int top, int spacing, params Control[] controls)
        {
            int cx = startX;
            foreach (var control in controls)
            {
                control.Location = new Point(cx, top);
                parent.Controls.Add(control);
                cx += control.Width + spacing;
            }
        }

        static void AddSectionDots(Control parent)
        {
            var dots = ModernUi.OutlineButton("...", 28, 24);
            dots.Location = new Point(parent.Width - 40, 11);
            parent.Controls.Add(dots);
        }

        static void AddNoticeRow(Control parent, int index, string message, string time)
        {
            int y = 44 + index * 37;
            var icon = ModernUi.Label("▰", 11f, FontStyle.Bold, ModernUi.Blue);
            icon.Location = new Point(24, y);
            icon.Size = new Size(20, 24);
            icon.TextAlign = ContentAlignment.MiddleCenter;
            parent.Controls.Add(icon);

            var text = ModernUi.Label(message, 9f, FontStyle.Regular, ModernUi.Text);
            text.Location = new Point(58, y);
            text.Size = new Size(parent.Width - 250, 24);
            parent.Controls.Add(text);

            var date = ModernUi.Label(time, 8.7f, FontStyle.Regular, Color.FromArgb(148, 163, 184));
            date.Location = new Point(parent.Width - 150, y);
            date.Size = new Size(130, 24);
            date.TextAlign = ContentAlignment.MiddleRight;
            parent.Controls.Add(date);

            if (index < 4)
            {
                var line = new Panel
                {
                    BackColor = Color.FromArgb(232, 238, 246),
                    Location = new Point(12, y + 30),
                    Size = new Size(parent.Width - 24, 1)
                };
                parent.Controls.Add(line);
            }
        }
    }

    private void RenderResidentDashboard()
    {
        var page = BeginPage("Dashboard cá nhân", $"Xin chào, {CurrentDisplayName()}! Chúc bạn một ngày tốt lành.");

        page.AutoScroll = true;
        page.AutoScrollMinSize = Size.Empty;

        int x = 18;
        int y = 76;
        int gap = 14;
        int scrollbar = SystemInformation.VerticalScrollBarWidth;
        int w = Math.Max(860, page.ClientSize.Width - x * 2 - scrollbar);

        ResidentDTO? resident = _session?.UserID > 0
            ? ResidentDAL.GetResidentByUserID(_session.UserID)
            : null;

        resident ??= ResidentDAL.GetAllResidents()
            .FirstOrDefault(r => string.Equals(r.Username, CurrentUsername(), StringComparison.OrdinalIgnoreCase));

        var apartment = resident == null ? null : ApartmentDAL.GetApartmentByID(resident.ApartmentID);
        var residentInvoices = resident == null ? new List<InvoiceDTO>() : InvoiceDAL.GetInvoicesByResident(resident.ResidentID);
        var latestInvoice = residentInvoices.FirstOrDefault();

        var residentNotifications = _session?.UserID > 0
            ? NotificationDAL.GetUserNotifications(_session.UserID)
            : new List<NotificationDTO>();

        var residentComplaints = resident == null
            ? new List<dynamic>()
            : ComplaintDAL.GetComplaintsByResident(resident.ResidentID);

        int unreadCount = residentNotifications.Count(n => !n.IsRead);
        int openComplaintCount = residentComplaints.Count(c => ViStatus(c.Status) is not ("Đã xử lý" or "Đã đóng"));

        int cardW = Math.Max(260, (w - gap * 2) / 3);

        string apartmentDetail = apartment == null
            ? "Chưa có dữ liệu\r\n-"
            : $"{Display(apartment.BuildingName)}\r\nDiện tích: {apartment.Area.ToString("N1", CultureInfo.InvariantCulture)} m²";

        var card1 = ResidentCard(
            "Thông tin cá nhân",
            Display(resident?.FullName, CurrentDisplayName()),
            $"{Display(resident?.Phone)}\r\n{Display(resident?.Email)}",
            ModernUi.Blue,
            "●",
            "Xem chi tiết",
            cardW);

        var card2 = ResidentCard(
            "Căn hộ đang ở",
            Display(resident?.ApartmentCode),
            apartmentDetail,
            Color.FromArgb(34, 197, 94),
            "⌂",
            "Xem chi tiết",
            cardW);

        var card3 = ResidentCard(
            "Hóa đơn mới nhất",
            latestInvoice == null ? "Chưa có" : $"Tháng {latestInvoice.Month:00}/{latestInvoice.Year}",
            latestInvoice == null
                ? "Không có dữ liệu\r\n-"
                : $"{Money(latestInvoice.TotalAmount)} VNĐ\r\nNgày phát hành: {DateText(latestInvoice.CreatedAt)}",
            Color.FromArgb(249, 115, 22),
            "▤",
            "Xem hóa đơn",
            cardW);

        var card4 = ResidentCard(
            "Trạng thái thanh toán",
            latestInvoice == null ? "-" : ViStatus(latestInvoice.PaymentStatus),
            latestInvoice == null
                ? "Không có hóa đơn\r\n-"
                : $"Đã thu: {Money(latestInvoice.PaidAmount)} VNĐ\r\nHạn: {DateText(latestInvoice.DueDate)}",
            Color.FromArgb(34, 197, 94),
            "✓",
            "Xem lịch sử",
            cardW);

        var card5 = ResidentCard(
            "Thông báo chưa đọc",
            unreadCount.ToString("N0"),
            "Thông báo mới",
            ModernUi.Blue,
            "◆",
            "Xem tất cả",
            cardW);

        var card6 = ResidentCard(
            "Phản ánh đang xử lý",
            openComplaintCount.ToString("N0"),
            "Phản ánh đang xử lý",
            Color.FromArgb(6, 182, 212),
            "■",
            "Xem chi tiết",
            cardW);

        card1.Location = new Point(x, y);
        card2.Location = new Point(x + cardW + gap, y);
        card3.Location = new Point(x + (cardW + gap) * 2, y);

        y += 140;

        card4.Location = new Point(x, y);
        card5.Location = new Point(x + cardW + gap, y);
        card6.Location = new Point(x + (cardW + gap) * 2, y);

        page.Controls.Add(card1);
        page.Controls.Add(card2);
        page.Controls.Add(card3);
        page.Controls.Add(card4);
        page.Controls.Add(card5);
        page.Controls.Add(card6);

        y += 154;

        int leftW = Math.Max(520, (int)(w * 0.58));
        int rightW = w - leftW - gap;

        var invoices = ModernUi.Section("Hóa đơn gần đây", leftW, 276);
        invoices.Location = new Point(x, y);

        var grid = CreateGrid(
            new[] { "Kỳ", "Ngày phát hành", "Hạn thanh toán", "Số tiền", "Trạng thái", "Hành động" },
            RowsOrEmpty(residentInvoices.Take(6), 6, (invoice, _) => new object[]
            {
            $"{invoice.Month:00}/{invoice.Year}",
            DateText(invoice.CreatedAt),
            DateText(invoice.DueDate),
            Money(invoice.TotalAmount),
            ViStatus(invoice.PaymentStatus),
            "Xem"
            }));

        grid.Location = new Point(12, 46);
        grid.Size = new Size(invoices.Width - 24, 196);
        grid.ScrollBars = ScrollBars.Vertical;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        invoices.Controls.Add(grid);
        page.Controls.Add(invoices);

        var notices = ModernUi.Section("Thông báo gần đây", rightW, 276);
        notices.Location = new Point(invoices.Right + gap, y);

        var recentNotifications = residentNotifications.Take(5).ToList();
        if (recentNotifications.Count == 0)
        {
            recentNotifications.Add(new NotificationDTO
            {
                Title = "Không có thông báo mới",
                CreatedAt = DateTime.MinValue
            });
        }

        for (int i = 0; i < recentNotifications.Count; i++)
        {
            string title = Display(recentNotifications[i].Title, Display(recentNotifications[i].Message));
            string time = DateTimeText(recentNotifications[i].CreatedAt);

            var row = ModernUi.Label("•  " + title, 9.4f, FontStyle.Regular, ModernUi.Text);
            row.Location = new Point(18, 54 + i * 36);
            row.Size = new Size(notices.Width - 150, 28);
            row.AutoEllipsis = true;
            notices.Controls.Add(row);

            var date = ModernUi.Label(time, 8.4f, FontStyle.Regular, ModernUi.Muted);
            date.Location = new Point(notices.Width - 128, 54 + i * 36);
            date.Size = new Size(108, 28);
            date.TextAlign = ContentAlignment.MiddleRight;
            date.AutoEllipsis = true;
            notices.Controls.Add(date);
        }

        page.Controls.Add(notices);

        y += 292;

        int qrW = Math.Max(560, (int)(w * 0.62));
        int timelineW = w - qrW - gap;

        var qr = ModernUi.Section("Thanh toán nhanh qua QR Code", qrW, 250);
        qr.Location = new Point(x, y);

        string[] qrImageCandidates =
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "qr_payment_test.jpg"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "qr_payment_test.jpg"))
        };
        string? qrImagePath = qrImageCandidates.FirstOrDefault(File.Exists);

        if (!string.IsNullOrWhiteSpace(qrImagePath))
        {
            using var qrImage = Image.FromFile(qrImagePath);
            var qrPicture = new PictureBox
            {
                Location = new Point(24, 50),
                Size = new Size(170, 170),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Image = new Bitmap(qrImage)
            };
            qr.Controls.Add(qrPicture);
        }
        else
        {
            var missingQr = ModernUi.Label("Không tìm thấy ảnh QR", 10f, FontStyle.Bold, ModernUi.Muted);
            missingQr.Location = new Point(24, 112);
            missingQr.Size = new Size(170, 28);
            missingQr.TextAlign = ContentAlignment.MiddleCenter;
            qr.Controls.Add(missingQr);
        }

        string qrInvoiceText = latestInvoice == null
            ? "Quét QR để thanh toán hóa đơn mới nhất\r\n\r\nHóa đơn:   Chưa có hóa đơn\r\nSố tiền:   0 VNĐ\r\nNội dung CK: -"
            : $"Quét QR để thanh toán hóa đơn mới nhất\r\n\r\nHóa đơn:   Tháng {latestInvoice.Month:00}/{latestInvoice.Year}\r\nSố tiền:   {Money(Math.Max(0, latestInvoice.TotalAmount - latestInvoice.PaidAmount))} VNĐ\r\nNội dung CK: {Display(resident?.ApartmentCode)}-{latestInvoice.Month:00}{latestInvoice.Year}-{Display(resident?.FullName, CurrentUsername()).Replace(" ", "")}";

        var qrText = ModernUi.Label(qrInvoiceText, 10f, FontStyle.Regular, ModernUi.Text);
        qrText.Location = new Point(218, 58);
        qrText.Size = new Size(qr.Width - 242, 116);
        qr.Controls.Add(qrText);

        var note = ModernUi.Badge("Sau khi thanh toán, hệ thống sẽ tự động cập nhật trong vòng 5-10 phút.", ModernUi.Blue);
        note.Location = new Point(218, 188);
        note.Size = new Size(qr.Width - 242, 28);
        qr.Controls.Add(note);

        page.Controls.Add(qr);

        var progress = ModernUi.Section("Tiến độ phản ánh của tôi", timelineW, 250);
        progress.Location = new Point(qr.Right + gap, y);
        AddTimeline(progress);
        page.Controls.Add(progress);

        page.AutoScrollMinSize = new Size(0, y + 300);
    }

    private RoundedPanel CreateDebtCard(int unpaidCount, decimal debtAmount, int width, bool hasDebt)
    {
        var card = ModernUi.CardPanel();
        card.Size = new Size(width, 126);

        Color accentColor = hasDebt ? ModernUi.Red : Color.FromArgb(249, 115, 22);
        var titleLabel = ModernUi.Label("NỢ CHƯA THU", 8.5f, FontStyle.Bold, accentColor);
        titleLabel.Location = new Point(14, 12);
        titleLabel.Size = new Size(width - 28, 22);
        titleLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(titleLabel);

        var circle = new CircleLabel
        {
            Text = "!",
            CircleColor = accentColor,
            ForeColor = Color.White,
            Font = ModernUi.Font(22f, FontStyle.Bold),
            Size = new Size(58, 58),
            Location = new Point(18, 44)
        };
        card.Controls.Add(circle);

        // Add warning icon/badge if there's debt
        if (hasDebt)
        {
            var badge = new CircleLabel
            {
                Text = "⚠",
                CircleColor = ModernUi.Red,
                ForeColor = Color.White,
                Font = ModernUi.Font(12f, FontStyle.Bold),
                Size = new Size(28, 28),
                Location = new Point(62, 42)
            };
            card.Controls.Add(badge);
        }

        var valueLabel = ModernUi.Label(unpaidCount.ToString("N0"), 13f, FontStyle.Bold, ModernUi.Navy);
        valueLabel.Location = new Point(86, 40);
        valueLabel.Size = new Size(width - 98, 30);
        valueLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(valueLabel);

        var detailLabel = ModernUi.Label($"{Money(debtAmount)} VNĐ\r\n{unpaidCount} Hóa đơn", 8.4f, FontStyle.Regular, ModernUi.Text);
        detailLabel.Location = new Point(86, 70);
        detailLabel.Size = new Size(width - 98, 40);
        detailLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(detailLabel);

        Color badgeColor = hasDebt ? ModernUi.Red : Color.FromArgb(249, 115, 22);
        var actionLabel = ModernUi.Badge("Cần xử lý", badgeColor);
        actionLabel.Location = new Point(14, 92);
        actionLabel.Size = new Size(width - 28, 24);
        card.Controls.Add(actionLabel);

        return card;
    }

    private static ComboBox AddInvoiceStatusFilter(Control parent, string label, string selected, int x, int y = 8)
    {
        var lbl = ModernUi.Label(label, 8.7f, FontStyle.Bold, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(170, 18);
        parent.Controls.Add(lbl);
        var combo = ModernUi.ComboBox(new[]
        {
            selected,
            "Tất cả",
            "Đã thanh toán",
            "Chưa thanh toán",
            "Thanh toán một phần",
            "Quá hạn"
        }, 210);
        combo.Location = new Point(x, y + 22);
        parent.Controls.Add(combo);
        return combo;
    }
}
