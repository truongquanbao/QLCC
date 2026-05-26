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
        int w = Math.Max(980, _content.ClientSize.Width - 28 - SystemInformation.VerticalScrollBarWidth);
        int y = 83;
        int gap = 12;
        var residents = ResidentDAL.GetAllResidents();
        var apartments = ApartmentDAL.GetAllApartments();
        var invoices = InvoiceDAL.GetAllInvoices();
        var complaints = ComplaintDAL.GetAllComplaints();
        var visitors = VisitorDAL.GetAllVisitors();
        var vehicles = VehicleDAL.GetAllVehicles();
        var schedules = AssetDAL.GetMaintenanceSchedules();
        var pendingPayments = PaymentDAL.GetPendingPayments();
        var notifications = _session?.UserID > 0
            ? NotificationDAL.GetUserNotifications(_session.UserID)
            : new List<NotificationDTO>();
        int currentResidents = residents.Count(IsCurrentResident);
        int newComplaintCount = complaints.Count(c => ViStatus(c.Status) == "Mới");
        int occupied = apartments.Count(a => ViStatus(a.Status) == "Đang sử dụng");
        int occupancyRate = apartments.Count == 0 ? 0 : (int)Math.Round(occupied * 100m / apartments.Count);
        var unpaidInvoiceItems = invoices
            .Where(i => InvoiceRemaining(i) > 0m || ViStatus(i.PaymentStatus) != "Đã thanh toán")
            .ToList();
        int unpaidInvoices = unpaidInvoiceItems.Count;
        DateTime today = DateTime.Today;
        DateTime weekEnd = today.AddDays(7);
        var upcomingSchedules = schedules
            .Where(s => s.ScheduledDate.Date >= today && s.ScheduledDate.Date <= weekEnd)
            .OrderBy(s => s.ScheduledDate)
            .ToList();
        int todayVisitors = visitors.Count(v => ((DateTime)v.ArrivalTime).Date == today);

        decimal debtAmount = unpaidInvoiceItems.Sum(InvoiceRemaining);
        bool hasDebt = unpaidInvoices > 0 && debtAmount > 0;
        string residentTrend = residents.Count == 0 ? "Chưa có dữ liệu" : $"{residents.Count:N0} hồ sơ cư dân";
        string complaintTrend = complaints.Count == 0 ? "Chưa có dữ liệu" : $"{complaints.Count:N0} tổng phiếu";
        string occupancyTrend = apartments.Count == 0 ? "Chưa có dữ liệu" : $"{occupancyRate}% lấp đầy";

        const int kpiCardWidth = 190;
        const int kpiCardHeight = 132;
        int kpiCardsPerRow = w >= kpiCardWidth * 6 + gap * 5 ? 6 : 3;
        int kpiRows = kpiCardsPerRow == 6 ? 1 : 2;
        var kpiPanel = new FlowLayoutPanel
        {
            Location = new Point(x, y),
            Size = new Size(w, kpiRows * kpiCardHeight + (kpiRows - 1) * gap + 2),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = false,
            BackColor = ModernUi.Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        var kpiCards = new Control[]
        {
            ModernUi.StatCard("Số cư dân hiện tại", currentResidents.ToString("N0"), "Người", ModernUi.Blue, "●●", residentTrend, kpiCardWidth, kpiCardHeight),
            ModernUi.StatCard("Phản ánh mới", newComplaintCount.ToString("N0"), "Phản ánh", Color.FromArgb(34, 197, 94), "▤", complaintTrend, kpiCardWidth, kpiCardHeight),
            CreateDebtCard(unpaidInvoices, debtAmount, kpiCardWidth, hasDebt, kpiCardHeight),
            ModernUi.StatCard("Bảo trì sắp tới", upcomingSchedules.Count.ToString("N0"), "Lịch", Color.FromArgb(6, 182, 212), "⚒", "Trong 7 ngày tới", kpiCardWidth, kpiCardHeight),
            ModernUi.StatCard("Khách hôm nay", todayVisitors.ToString("N0"), "Khách", ModernUi.Blue, "●", $"{visitors.Count:N0} tổng lượt", kpiCardWidth, kpiCardHeight),
            ModernUi.StatCard("Lấp đầy căn hộ", $"{occupied:N0}/{apartments.Count:N0}", "Căn hộ", Color.FromArgb(34, 197, 94), "◔", occupancyTrend, kpiCardWidth, kpiCardHeight)
        };

        for (int i = 0; i < kpiCards.Length; i++)
        {
            bool isLastInRow = (i + 1) % kpiCardsPerRow == 0;
            kpiCards[i].Margin = new Padding(0, 0, isLastInRow ? 0 : gap, i < kpiCards.Length - kpiCardsPerRow ? gap : 0);
            kpiPanel.Controls.Add(kpiCards[i]);

            if (isLastInRow && i < kpiCards.Length - 1)
            {
                kpiPanel.SetFlowBreak(kpiCards[i], true);
            }
        }

        page.Controls.Add(kpiPanel);
        y = kpiPanel.Bottom + 16;

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
        var complaintGroups = complaints
            .GroupBy(c => (string)Display(c.ComplaintType, Display(c.Category, "Khác")))
            .OrderByDescending(g => g.Count())
            .Take(6)
            .Select(g => (Label: ShortChartLabel(g.Key), Value: g.Count()))
            .ToList();
        if (complaintGroups.Count == 0)
        {
            var emptyChart = ModernUi.Label("Chưa có dữ liệu phản ánh.", 10f, FontStyle.Regular, ModernUi.Muted);
            emptyChart.Location = new Point(20, 108);
            emptyChart.Size = new Size(chartPanel.Width - 40, 36);
            emptyChart.TextAlign = ContentAlignment.MiddleCenter;
            chartPanel.Controls.Add(emptyChart);
        }
        else
        {
            int maxCount = complaintGroups.Max(g => g.Value);
            var chart = new BarChartPanel
            {
                Location = new Point(12, 54),
                Size = new Size(chartPanel.Width - 24, 190),
                BarColor = ModernUi.Orange,
                AxisMax = Math.Max(5, maxCount + 1),
                GridSteps = 5,
                ShowValueLabels = true,
                SeriesLabel = "Số lượng phản ánh"
            };
            chart.Bars.AddRange(complaintGroups);
            chartPanel.Controls.Add(chart);
        }
        page.Controls.Add(chartPanel);

        var newComplaints = ModernUi.Section("Phản ánh mới cần xử lý", rightW, topPanelH);
        newComplaints.Location = new Point(chartPanel.Right + gap, y);
        AddSectionDots(newComplaints);
        var grid = CreateGrid(
            new[] { "STT", "Mã phản ánh", "Nội dung", "Căn hộ", "Người gửi", "Thời gian", "Ưu tiên", "Trạng thái" },
            RowsOrEmpty(complaints
                .Where(c => ViStatus(c.Status) == "Mới")
                .OrderByDescending(c => c.CreatedAt)
                .Take(5), 8, (c, i) => new object[]
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
        grid.Size = new Size(newComplaints.Width - 24, 184);
        ConfigureFixedWidthGrid(grid, 50, 110, 240, 90, 140, 140, 90, 110);
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
            new[] { "Ngày", "Hạng mục", "Khu vực", "Nội dung", "Trạng thái", "Người phụ trách" },
            RowsOrEmpty(upcomingSchedules.Take(5), 6, (s, _) => new object[]
            {
                DateText(s.ScheduledDate),
                s.Category,
                s.Location,
                Display(s.Note, s.AssetName),
                ViStatus(s.Status),
                Display(s.AssignedTo)
            }, "Không có lịch bảo trì"));
        scheduleGrid.Location = new Point(12, 42);
        scheduleGrid.Size = new Size(schedule.Width - 24, 194);
        ConfigureFixedWidthGrid(scheduleGrid, 110, 130, 150, 260, 110, 140);
        schedule.Controls.Add(scheduleGrid);
        page.Controls.Add(schedule);

        var notice = ModernUi.Section("Thông báo nhanh", rightW, bottomPanelH);
        notice.Location = new Point(schedule.Right + gap, y);
        var quickNotifications = BuildQuickNotifications()
            .GroupBy(item => $"{item.PageKey}|{item.Title}|{item.Detail}|{item.Time:yyyyMMddHHmm}")
            .Select(group => group.First())
            .OrderByDescending(item => item.Time)
            .Take(5)
            .ToList();
        if (quickNotifications.Count == 0)
        {
            var emptyNotice = ModernUi.Label("Không có thông báo mới.", 10f, FontStyle.Regular, ModernUi.Muted);
            emptyNotice.Location = new Point(18, 96);
            emptyNotice.Size = new Size(notice.Width - 36, 32);
            emptyNotice.TextAlign = ContentAlignment.MiddleCenter;
            notice.Controls.Add(emptyNotice);
        }
        for (int i = 0; i < quickNotifications.Count; i++)
        {
            AddNoticeRow(notice, i, quickNotifications[i]);
        }
        page.Controls.Add(notice);

        List<(string Title, string Detail, DateTime Time, string Tag, string PageKey, string Icon, Color Accent)> BuildQuickNotifications()
        {
            var items = new List<(string Title, string Detail, DateTime Time, string Tag, string PageKey, string Icon, Color Accent)>();

            void AddItem(string title, string detail, DateTime time, string tag, string pageKey, string icon, Color accent)
            {
                title = Display(title, "Thông báo");
                detail = Display(detail, "");
                if (time <= DateTime.MinValue)
                {
                    time = DateTime.Now;
                }

                items.Add((title, detail, time, Display(tag, "Mới"), pageKey, icon, accent));
            }

            foreach (var notification in notifications.Take(12))
            {
                string title = Display(notification.Title, Display(notification.Subject, "Thông báo"));
                string detail = Display(notification.Message, Display(notification.Body, Display(notification.Description, "")));
                string pageKey = NotificationPageKey(notification, title, detail);
                DateTime time = notification.CreatedAt > DateTime.MinValue
                    ? notification.CreatedAt
                    : notification.SentDate ?? DateTime.Now;
                string tag = notification.IsRead ? Display(notification.Priority, "Đã gửi") : "Mới";
                AddItem(title, detail, time, ViStatus(tag), pageKey, NotificationIcon(pageKey), notification.IsRead ? ModernUi.Blue : ModernUi.Orange);
            }

            foreach (var complaint in complaints
                .Where(c => ViStatus(c.Status) == "Mới")
                .OrderByDescending(c => c.CreatedAt)
                .Take(5))
            {
                AddItem(
                    "Phản ánh mới cần xử lý",
                    $"{Display(complaint.Title)} - căn hộ {Display(complaint.ApartmentCode)}",
                    complaint.CreatedAt,
                    "Mới",
                    "complaints",
                    "!",
                    ModernUi.Orange);
            }

            foreach (var complaint in complaints
                .Where(c => c.UpdatedAt > c.CreatedAt.AddMinutes(1) && ViStatus(c.Status) != "Mới")
                .OrderByDescending(c => c.UpdatedAt)
                .Take(3))
            {
                AddItem(
                    "Phản ánh vừa cập nhật",
                    $"{Display(complaint.Title)} - {ViStatus(complaint.Status)}",
                    complaint.UpdatedAt,
                    ViStatus(complaint.Status),
                    "complaints",
                    "■",
                    ModernUi.Blue);
            }

            foreach (var payment in pendingPayments.Take(5))
            {
                AddItem(
                    "Thanh toán chờ xác nhận",
                    $"{Display(payment.ResidentName, "Cư dân")} - {Money(payment.Amount)} VNĐ",
                    payment.CreatedAt,
                    "Chờ xử lý",
                    "invoices",
                    "₫",
                    ModernUi.Green);
            }

            foreach (var visitor in visitors
                .Where(v => ViStatus(v.Status) == "Chờ duyệt")
                .OrderByDescending(v => v.CreatedAt)
                .Take(5))
            {
                AddItem(
                    "Khách ra vào chờ duyệt",
                    $"{Display(visitor.VisitorName)} - căn hộ {Display(visitor.ApartmentCode)}",
                    visitor.CreatedAt,
                    "Chờ xử lý",
                    "visitors",
                    "♙",
                    Color.FromArgb(6, 182, 212));
            }

            foreach (var vehicle in vehicles
                .Where(v => ViStatus(v.Status) == "Chờ duyệt")
                .OrderByDescending(v => v.CreatedAt)
                .Take(5))
            {
                AddItem(
                    "Phương tiện chờ duyệt",
                    $"{Display(vehicle.LicensePlate)} - {Display(vehicle.ResidentName)}",
                    vehicle.CreatedAt,
                    "Chờ xử lý",
                    "vehicles",
                    "▣",
                    ModernUi.Orange);
            }

            foreach (var invoice in unpaidInvoiceItems
                .Where(i => i.DueDate.HasValue && i.DueDate.Value.Date < today)
                .OrderByDescending(i => i.DueDate)
                .Take(3))
            {
                AddItem(
                    "Hóa đơn quá hạn",
                    $"{Display(invoice.ApartmentCode)} còn {Money(Math.Max(0m, invoice.RemainingAmount))} VNĐ",
                    invoice.DueDate ?? invoice.CreatedAt,
                    "Quan trọng",
                    "invoices",
                    "!",
                    ModernUi.Red);
            }

            foreach (var maintenance in upcomingSchedules.Take(3))
            {
                AddItem(
                    "Nhắc lịch bảo trì",
                    $"{Display(maintenance.AssetName)} - {DateText(maintenance.ScheduledDate)}",
                    maintenance.ScheduledDate,
                    "Quan trọng",
                    "assets",
                    "◇",
                    Color.FromArgb(6, 182, 212));
            }

            return items;
        }

        static bool IsCurrentResident(ResidentDTO resident)
        {
            if (resident.EndDate.HasValue && resident.EndDate.Value.Date < DateTime.Today)
            {
                return false;
            }

            return IsActiveStatus(resident.Status) || ResidentLivingStatus(resident) == "Đang cư trú";
        }

        static decimal InvoiceRemaining(InvoiceDTO invoice)
        {
            return invoice.RemainingAmount > 0m
                ? invoice.RemainingAmount
                : Math.Max(0m, invoice.TotalAmount - invoice.PaidAmount);
        }

        static string ShortChartLabel(string label)
        {
            label = Display(label, "Khác").Trim();
            return label.Length <= 12 ? label : label[..9] + "...";
        }

        static string NotificationPageKey(NotificationDTO notification, string title, string detail)
        {
            string text = $"{notification.NotificationType} {notification.Type} {title} {detail}".ToLowerInvariant();
            if (text.Contains("complaint") || text.Contains("phản ánh") || text.Contains("phan anh"))
            {
                return "complaints";
            }

            if (text.Contains("payment") || text.Contains("invoice") || text.Contains("thanh toán") || text.Contains("thanh toan") || text.Contains("hóa đơn") || text.Contains("hoa don"))
            {
                return "invoices";
            }

            if (text.Contains("visitor") || text.Contains("khách") || text.Contains("khach"))
            {
                return "visitors";
            }

            if (text.Contains("vehicle") || text.Contains("phương tiện") || text.Contains("phuong tien"))
            {
                return "vehicles";
            }

            if (text.Contains("maintenance") || text.Contains("bảo trì") || text.Contains("bao tri"))
            {
                return "assets";
            }

            return "";
        }

        static string NotificationIcon(string pageKey)
        {
            return pageKey switch
            {
                "complaints" => "!",
                "invoices" => "₫",
                "visitors" => "♙",
                "vehicles" => "▣",
                "assets" => "◇",
                _ => "▰"
            };
        }

        static void AddSectionDots(Control parent)
        {
            var dots = ModernUi.OutlineButton("...", 28, 24);
            dots.Location = new Point(parent.Width - 40, 11);
            parent.Controls.Add(dots);
        }

        static void ConfigureFixedWidthGrid(DataGridView grid, params int[] widths)
        {
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.ScrollBars = ScrollBars.Both;
            grid.RowHeadersVisible = false;
            grid.AllowUserToResizeColumns = true;
            grid.AllowUserToResizeRows = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 36;
            grid.RowTemplate.Height = 34;
            ApplyColumnWidths();
            grid.DataBindingComplete += (_, _) => ApplyColumnWidths();

            void ApplyColumnWidths()
            {
                for (int i = 0; i < grid.Columns.Count && i < widths.Length; i++)
                {
                    grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    grid.Columns[i].MinimumWidth = Math.Min(widths[i], 50);
                    grid.Columns[i].Width = widths[i];
                }

                grid.ClearSelection();
            }
        }

        void AddNoticeRow(Control parent, int index, (string Title, string Detail, DateTime Time, string Tag, string PageKey, string Icon, Color Accent) item)
        {
            int y = 43 + index * 39;
            var row = new Panel
            {
                BackColor = Color.White,
                Location = new Point(12, y),
                Size = new Size(parent.Width - 24, 36),
                Cursor = Cursors.Hand
            };
            parent.Controls.Add(row);

            var icon = new CircleLabel
            {
                Text = item.Icon,
                CircleColor = Color.FromArgb(239, 246, 255),
                ForeColor = item.Accent,
                Font = ModernUi.Font(9f, FontStyle.Bold),
                Location = new Point(6, 6),
                Size = new Size(24, 24),
                TextAlign = ContentAlignment.MiddleCenter
            };
            row.Controls.Add(icon);

            int rightW = 118;
            var title = ModernUi.Label(item.Title, 8.6f, FontStyle.Bold, ModernUi.Navy);
            title.Location = new Point(42, 1);
            title.Size = new Size(row.Width - 48 - rightW, 17);
            title.AutoEllipsis = true;
            row.Controls.Add(title);

            var detail = ModernUi.Label(item.Detail, 8f, FontStyle.Regular, ModernUi.Text);
            detail.Location = new Point(42, 18);
            detail.Size = new Size(row.Width - 48 - rightW, 16);
            detail.AutoEllipsis = true;
            row.Controls.Add(detail);

            var time = ModernUi.Label(DateTimeText(item.Time), 7.8f, FontStyle.Regular, ModernUi.Muted);
            time.Location = new Point(row.Width - rightW, 0);
            time.Size = new Size(rightW - 4, 16);
            time.TextAlign = ContentAlignment.MiddleRight;
            time.AutoEllipsis = true;
            row.Controls.Add(time);

            var badge = ModernUi.Badge(item.Tag, item.Accent);
            int badgeW = Math.Min(92, Math.Max(58, TextRenderer.MeasureText(item.Tag, badge.Font).Width + 20));
            badge.Location = new Point(row.Width - badgeW - 4, 18);
            badge.Size = new Size(badgeW, 18);
            row.Controls.Add(badge);

            BindTileClick(row, (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(item.PageKey) && CanAccessPage(item.PageKey))
                {
                    Navigate(item.PageKey);
                    return;
                }

                MessageBox.Show(this,
                    $"{item.Title}\n\n{item.Detail}\n{DateTimeText(item.Time)}",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            });

            if (index < 4)
            {
                var line = new Panel
                {
                    BackColor = Color.FromArgb(232, 238, 246),
                    Location = new Point(12, y + 37),
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

        const int margin = 18;
        const int gap = 14;
        const int topY = 76;

        int scrollbar = SystemInformation.VerticalScrollBarWidth;
        int pageW = Math.Max(980, page.ClientSize.Width - margin * 2 - scrollbar);

        page.HorizontalScroll.Enabled = false;
        page.HorizontalScroll.Visible = false;

        ResidentDTO? resident = _session?.UserID > 0
            ? ResidentDAL.GetResidentByUserID(_session.UserID)
            : null;

        resident ??= ResidentDAL.GetAllResidents()
            .FirstOrDefault(r => string.Equals(r.Username, CurrentUsername(), StringComparison.OrdinalIgnoreCase));

        var apartment = resident == null ? null : ApartmentDAL.GetApartmentByID(resident.ApartmentID);

        var residentInvoices = resident == null
            ? new List<InvoiceDTO>()
            : InvoiceDAL.GetInvoicesByResident(resident.ResidentID)
                .OrderByDescending(i => i.Year)
                .ThenByDescending(i => i.Month)
                .ThenByDescending(i => i.CreatedAt)
                .ToList();

        var latestInvoice = residentInvoices.FirstOrDefault();

        var residentNotifications = _session?.UserID > 0
            ? NotificationDAL.GetUserNotifications(_session.UserID)
                .OrderByDescending(n => n.CreatedAt)
                .ToList()
            : new List<NotificationDTO>();

        var residentComplaints = resident == null
            ? new List<dynamic>()
            : ComplaintDAL.GetComplaintsByResident(resident.ResidentID);

        int unreadCount = residentNotifications.Count(n => !n.IsRead);
        int openComplaintCount = residentComplaints.Count(c => ViStatus(c.Status) is not ("Đã xử lý" or "Đã đóng"));

        decimal latestRemaining = latestInvoice == null
            ? 622800m
            : Math.Max(0m, latestInvoice.TotalAmount - latestInvoice.PaidAmount);

        string residentName = Display(resident?.FullName, CurrentDisplayName());
        string residentCode = resident == null ? "CD000123" : $"CD{resident.ResidentID:000000}";
        string apartmentCode = Display(resident?.ApartmentCode, "A-0101");
        string phone = Display(resident?.Phone, "0901 234 567");
        string email = Display(resident?.Email, "minhanh@gmail.com");

        string buildingText = apartment == null
            ? "Tòa A"
            : Display(apartment.BuildingName, "Tòa A");

        string floorText = apartment == null
            ? "Tầng 01"
            : $"Tầng {apartment.FloorNumber?.ToString("00") ?? "01"}";

        string areaText = apartment == null
            ? "48.5 m²"
            : $"{apartment.Area.ToString("N1", CultureInfo.InvariantCulture)} m²";

        int peopleCount = resident == null
            ? 3
            : Math.Max(1, ResidentDAL.GetAllResidents().Count(r => r.ApartmentID == resident.ApartmentID));

        int topCardH = 220;

        int usableTopW = pageW - gap * 3;
        int leftTopW = (int)(usableTopW * 0.28);
        int midTopW = (int)(usableTopW * 0.22);
        int invoiceTopW = (int)(usableTopW * 0.27);
        int qrTopW = usableTopW - leftTopW - midTopW - invoiceTopW;

        leftTopW = Math.Max(250, leftTopW);
        midTopW = Math.Max(210, midTopW);
        invoiceTopW = Math.Max(250, invoiceTopW);
        qrTopW = pageW - leftTopW - midTopW - invoiceTopW - gap * 3;

        if (qrTopW < 220)
        {
            int missing = 220 - qrTopW;
            leftTopW = Math.Max(235, leftTopW - missing / 3);
            midTopW = Math.Max(200, midTopW - missing / 3);
            invoiceTopW = Math.Max(235, invoiceTopW - missing / 3);
            qrTopW = pageW - leftTopW - midTopW - invoiceTopW - gap * 3;
        }

        var profileCard = CreateDemoCard("Thông tin cá nhân", leftTopW, topCardH);
        profileCard.Location = new Point(margin, topY);

        var profileIcon = new CircleLabel
        {
            Text = "●",
            CircleColor = Color.FromArgb(239, 246, 255),
            ForeColor = ModernUi.Blue,
            Font = ModernUi.Font(18f, FontStyle.Bold),
            Location = new Point(20, 58),
            Size = new Size(58, 58),
            TextAlign = ContentAlignment.MiddleCenter
        };
        profileCard.Controls.Add(profileIcon);

        int profileTextX = 92;
        int profileTextW = Math.Max(120, profileCard.Width - profileTextX - 18);

        AddCardText(profileCard, residentName, profileTextX, 46, profileTextW, 26, 11.4f, FontStyle.Bold, ModernUi.Navy);
        AddCardText(profileCard, $"Mã cư dân: {residentCode}", profileTextX, 74, profileTextW, 19, 8.1f, FontStyle.Regular, ModernUi.Text);
        AddCardText(profileCard, $"☎  {phone}", profileTextX, 96, profileTextW, 19, 8.1f, FontStyle.Regular, ModernUi.Text);
        AddCardText(profileCard, $"✉  {email}", profileTextX, 118, profileTextW, 19, 8.1f, FontStyle.Regular, ModernUi.Text);

        var livingBadge = ModernUi.Badge("Đang cư trú", ModernUi.Green);
        livingBadge.Location = new Point(profileTextX, 140);
        livingBadge.Size = new Size(96, 24);
        profileCard.Controls.Add(livingBadge);

        var profileBtn = ModernUi.OutlineButton("Xem hồ sơ", profileCard.Width - 40, 32);
        profileBtn.Location = new Point(20, profileCard.Height - 42);
        profileBtn.Click += (_, _) => Navigate("profile");
        profileCard.Controls.Add(profileBtn);
        page.Controls.Add(profileCard);

        var apartmentCard = CreateDemoCard("Căn hộ đang ở", midTopW, topCardH);
        apartmentCard.Location = new Point(profileCard.Right + gap, topY);

        var aptIcon = new CircleLabel
        {
            Text = "▥",
            CircleColor = ModernUi.Green,
            ForeColor = Color.White,
            Font = ModernUi.Font(24f, FontStyle.Bold),
            Location = new Point(26, 66),
            Size = new Size(64, 64),
            TextAlign = ContentAlignment.MiddleCenter
        };
        apartmentCard.Controls.Add(aptIcon);

        AddCardText(apartmentCard, apartmentCode, 112, 52, apartmentCard.Width - 130, 34, 16f, FontStyle.Bold, ModernUi.Navy);
        AddCardText(apartmentCard, $"{buildingText}   •   {floorText}", 112, 92, apartmentCard.Width - 130, 20, 8.8f, FontStyle.Regular, ModernUi.Text);
        AddCardText(apartmentCard, $"Diện tích: {areaText}", 112, 116, apartmentCard.Width - 130, 20, 8.8f, FontStyle.Regular, ModernUi.Text);
        AddCardText(apartmentCard, $"{peopleCount} người đang ở", 112, 140, apartmentCard.Width - 130, 20, 8.8f, FontStyle.Regular, ModernUi.Text);

        var aptBtn = ModernUi.OutlineButton("Xem chi tiết căn hộ  →", apartmentCard.Width - 40, 32);
        aptBtn.Location = new Point(20, apartmentCard.Height - 48);
        aptBtn.Click += (_, _) => Navigate("apartment-info");
        apartmentCard.Controls.Add(aptBtn);
        page.Controls.Add(apartmentCard);

        var invoiceCard = CreateDemoCard("Hóa đơn mới nhất", invoiceTopW, topCardH, Color.FromArgb(249, 115, 22));
        invoiceCard.Location = new Point(apartmentCard.Right + gap, topY);

        var invoiceIcon = new CircleLabel
        {
            Text = "▤",
            CircleColor = Color.FromArgb(249, 115, 22),
            ForeColor = Color.White,
            Font = ModernUi.Font(24f, FontStyle.Bold),
            Location = new Point(22, 64),
            Size = new Size(62, 62),
            TextAlign = ContentAlignment.MiddleCenter
        };
        invoiceCard.Controls.Add(invoiceIcon);

        string invoicePeriod = latestInvoice == null ? "Tháng 05/2026" : $"Tháng {latestInvoice.Month:00}/{latestInvoice.Year}";
        decimal invoiceTotal = latestInvoice == null ? 1384000m : latestInvoice.TotalAmount;
        DateTime dueDate = latestInvoice?.DueDate ?? new DateTime(2026, 5, 31);
        int dueDays = (dueDate.Date - DateTime.Today).Days;

        AddCardText(invoiceCard, invoicePeriod, 104, 48, invoiceCard.Width - 120, 28, 12.4f, FontStyle.Bold, ModernUi.Navy);
        AddCardText(invoiceCard, $"{Money(invoiceTotal)} VNĐ", 104, 78, invoiceCard.Width - 120, 28, 14f, FontStyle.Bold, ModernUi.Navy);

        var unpaidBadge = ModernUi.Badge(latestRemaining > 0 ? "Chưa thanh toán" : "Đã thanh toán", latestRemaining > 0 ? ModernUi.Red : ModernUi.Green);
        unpaidBadge.Location = new Point(104, 110);
        unpaidBadge.Size = new Size(116, 24);
        invoiceCard.Controls.Add(unpaidBadge);

        string dueText = dueDays > 0
            ? $"Hạn thanh toán: {DateText(dueDate)} (còn {dueDays} ngày)"
            : dueDays == 0
                ? $"Hạn thanh toán: {DateText(dueDate)} (hôm nay)"
                : $"Đã quá hạn: {Math.Abs(dueDays)} ngày";

        AddCardText(invoiceCard, dueText, 22, 144, invoiceCard.Width - 44, 22, 8.7f, FontStyle.Regular, dueDays < 0 ? ModernUi.Red : ModernUi.Text);

        int invoiceBtnGap = 10;
        int invoiceBtnW = Math.Max(92, (invoiceCard.Width - 44 - invoiceBtnGap) / 2);

        var btnViewInvoice = ModernUi.OutlineButton("Xem hóa đơn", invoiceBtnW, 32);
        btnViewInvoice.Location = new Point(22, invoiceCard.Height - 48);
        btnViewInvoice.Click += (_, _) => Navigate("my-invoices");
        invoiceCard.Controls.Add(btnViewInvoice);

        var btnPayNow = ModernUi.Button("Thanh toán", Color.FromArgb(249, 115, 22), invoiceBtnW, 32);
        btnPayNow.Location = new Point(btnViewInvoice.Right + invoiceBtnGap, invoiceCard.Height - 48);
        btnPayNow.Click += (_, _) => Navigate("payment");
        invoiceCard.Controls.Add(btnPayNow);
        page.Controls.Add(invoiceCard);

        var quickPay = CreateDemoCard("Thanh toán nhanh", qrTopW, topCardH);
        quickPay.Location = new Point(invoiceCard.Right + gap, topY);

        int qrSize = quickPay.Width < 260 ? 86 : 96;
        int qrLeft = quickPay.Width - qrSize - 18;
        int qrTextW = Math.Max(110, qrLeft - 34);

        AddCardText(quickPay, "Số tiền cần thanh toán", 20, 50, qrTextW, 20, 8.2f, FontStyle.Regular, ModernUi.Text);
        AddCardText(quickPay, $"{Money(latestRemaining)} VNĐ", 20, 76, qrTextW, 32, quickPay.Width < 260 ? 12.6f : 14.2f, FontStyle.Bold, ModernUi.Red);
        AddCardText(quickPay, "Quét QR để thanh toán", 20, 120, qrTextW, 20, 8.2f, FontStyle.Regular, ModernUi.Text);

        var qrBox = new PictureBox
        {
            Location = new Point(qrLeft, 50),
            Size = new Size(qrSize, qrSize),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        string[] qrImageCandidates =
        {
        Path.Combine(AppContext.BaseDirectory, "Assets", "qr_payment_test.jpg"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "qr_payment_test.jpg"))
    };

        string? qrImagePath = qrImageCandidates.FirstOrDefault(File.Exists);
        if (!string.IsNullOrWhiteSpace(qrImagePath))
        {
            using var loadedQr = Image.FromFile(qrImagePath);
            qrBox.Image = new Bitmap(loadedQr);
        }
        quickPay.Controls.Add(qrBox);

        string transferContent = $"{apartmentCode}-{invoicePeriod.Replace("Tháng ", "").Replace("/", "")}-{residentName.Replace(" ", "")}";

        int payBtnGap = 10;
        int payBtnW = Math.Max(88, (quickPay.Width - 40 - payBtnGap) / 2);

        var copyTransfer = ModernUi.OutlineButton("Sao chép CK", payBtnW, 32);
        copyTransfer.Location = new Point(20, quickPay.Height - 48);
        copyTransfer.Click += (_, _) =>
        {
            Clipboard.SetText(transferContent);
            MessageBox.Show(this, $"Đã sao chép nội dung chuyển khoản:\n{transferContent}", "Thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        quickPay.Controls.Add(copyTransfer);

        var paymentHistory = ModernUi.OutlineButton("Lịch sử", payBtnW, 32);
        paymentHistory.Location = new Point(copyTransfer.Right + payBtnGap, quickPay.Height - 48);
        paymentHistory.Click += (_, _) => Navigate("payment");
        quickPay.Controls.Add(paymentHistory);
        page.Controls.Add(quickPay);

        int row2Y = topY + topCardH + gap;
        int row2H = 300;

        int usableRow2W = pageW - gap * 2;
        int taskW = (int)(usableRow2W * 0.43);
        int noticeW = (int)(usableRow2W * 0.32);
        int complaintTopW = usableRow2W - taskW - noticeW;

        taskW = Math.Max(390, taskW);
        noticeW = Math.Max(300, noticeW);
        complaintTopW = pageW - taskW - noticeW - gap * 2;

        var tasks = CreateDemoCard("Việc cần xử lý", taskW, row2H);
        tasks.Location = new Point(margin, row2Y);

        var taskBadge = ModernUi.Badge("3", ModernUi.Red);
        taskBadge.Location = new Point(126, 14);
        taskBadge.Size = new Size(26, 22);
        tasks.Controls.Add(taskBadge);

        int taskY = 50;
        AddTaskItem(tasks, "₫", Color.FromArgb(249, 115, 22), $"Cần thanh toán {Money(latestRemaining)} VNĐ",
            $"Hóa đơn {invoicePeriod.Replace("Tháng ", "tháng ")} · Hạn thanh toán: {DateText(dueDate)}", "Quan trọng", ModernUi.Red, "payment", ref taskY);

        AddTaskItem(tasks, "●", Color.FromArgb(6, 182, 212), $"{Math.Max(1, openComplaintCount)} phản ánh đang xử lý",
            "Phản ánh về đèn hành lang không sáng", "Đang xử lý", ModernUi.Blue, "send-complaint", ref taskY);

        AddTaskItem(tasks, "!", ModernUi.Blue, "Thông báo bảo trì thang máy tòa A",
            "Dự kiến bảo trì ngày 28/05/2026 từ 09:00 - 11:00", "Mới", ModernUi.Teal, "notifications", ref taskY);

        var allTasks = ModernUi.OutlineButton("Xem tất cả việc cần xử lý", tasks.Width - 32, 32);
        allTasks.Location = new Point(16, tasks.Height - 46);
        allTasks.Click += (_, _) => Navigate("notifications");
        tasks.Controls.Add(allTasks);
        page.Controls.Add(tasks);

        var notices = CreateDemoCard("Thông báo mới", noticeW, row2H);
        notices.Location = new Point(tasks.Right + gap, row2Y);

        var noticeBadge = ModernUi.Badge(Math.Max(1, unreadCount).ToString("N0"), ModernUi.Red);
        noticeBadge.Location = new Point(122, 14);
        noticeBadge.Size = new Size(26, 22);
        notices.Controls.Add(noticeBadge);

        var notificationItems = residentNotifications.Take(3).ToList();
        var noticeRows = new List<(string Title, string Detail, string Time, Color Dot)>();

        foreach (var n in notificationItems)
        {
            string title = Display(n.Title, "Thông báo");
            string detail = string.IsNullOrWhiteSpace(n.Message) ? "Không có nội dung chi tiết." : n.Message.Trim();
            if (detail.Length > 56) detail = detail[..56] + "...";
            noticeRows.Add((title, detail, DateTimeText(n.CreatedAt), n.IsRead ? ModernUi.Blue : Color.FromArgb(249, 115, 22)));
        }

        while (noticeRows.Count < 3)
        {
            if (noticeRows.Count == 0)
                noticeRows.Add(("Bảo trì thang máy tòa A", "Dự kiến 28/05/2026 09:00 - 11:00", "Hôm nay, 09:30", ModernUi.Blue));
            else if (noticeRows.Count == 1)
                noticeRows.Add(("Thông báo về phí gửi xe tháng 05/2026", "Vui lòng thanh toán trước 31/05/2026", "Hôm qua, 16:45", Color.FromArgb(249, 115, 22)));
            else
                noticeRows.Add(("Cập nhật tiện ích: Sân thể thao", "Sân cầu lông sẽ mở cửa từ 06:00 - 22:00", "20/05/2026, 14:20", ModernUi.Blue));
        }

        int noticeY = 52;
        foreach (var notice in noticeRows.Take(3))
        {
            AddNoticeLine(notices, notice.Title, notice.Detail, notice.Time, notice.Dot, noticeY);
            noticeY += 70;
        }

        var allNotices = ModernUi.OutlineButton("Xem tất cả thông báo", notices.Width - 32, 32);
        allNotices.Location = new Point(16, notices.Height - 46);
        allNotices.Click += (_, _) => Navigate("notifications");
        notices.Controls.Add(allNotices);
        page.Controls.Add(notices);

        var latestComplaintBox = CreateDemoCard("Phản ánh gần nhất", complaintTopW, row2H);
        latestComplaintBox.Location = new Point(notices.Right + gap, row2Y);

        object? latestComplaint = residentComplaints
            .Cast<object>()
            .OrderByDescending(c => ReadDynamicDate(c, "CreatedAt", DateTime.MinValue))
            .FirstOrDefault();

        DateTime complaintCreated = latestComplaint == null
            ? new DateTime(2026, 5, 16, 8, 45, 0)
            : ReadDynamicDate(latestComplaint, "CreatedAt", DateTime.Today);

        string complaintTitle = latestComplaint == null
            ? "Đèn hành lang không sáng"
            : Display(ReadDynamicString(latestComplaint, "Title"), "Phản ánh của tôi");

        string complaintCode = latestComplaint == null
            ? "PA260515-001"
            : $"PA{complaintCreated:yyMMdd}-{ReadDynamicInt(latestComplaint, "ComplaintID", 1):000}";

        string complaintStatus = latestComplaint == null
            ? "Đang xử lý"
            : ViStatus(ReadDynamicString(latestComplaint, "Status"));

        var complaintInner = ModernUi.CardPanel(10);
        complaintInner.Location = new Point(16, 58);
        complaintInner.Size = new Size(latestComplaintBox.Width - 32, 164);
        latestComplaintBox.Controls.Add(complaintInner);

        var complaintIcon = new CircleLabel
        {
            Text = "▰",
            CircleColor = Color.FromArgb(249, 115, 22),
            ForeColor = Color.White,
            Font = ModernUi.Font(16f, FontStyle.Bold),
            Location = new Point(16, 20),
            Size = new Size(48, 48),
            TextAlign = ContentAlignment.MiddleCenter
        };
        complaintInner.Controls.Add(complaintIcon);

        AddCardText(complaintInner, complaintTitle, 78, 20, complaintInner.Width - 96, 24, 9.4f, FontStyle.Bold, ModernUi.Navy);
        AddCardText(complaintInner, $"Mã: {complaintCode}", 78, 46, complaintInner.Width - 96, 22, 8.7f, FontStyle.Bold, ModernUi.Text);

        var complaintStatusBadge = ModernUi.Badge(complaintStatus, complaintStatus is "Đã xử lý" or "Đã đóng" ? ModernUi.Green : ModernUi.Blue);
        complaintStatusBadge.Location = new Point(78, 78);
        complaintStatusBadge.Size = new Size(90, 24);
        complaintInner.Controls.Add(complaintStatusBadge);

        var line = new Panel
        {
            BackColor = Color.FromArgb(226, 232, 240),
            Location = new Point(16, 112),
            Size = new Size(complaintInner.Width - 32, 1)
        };
        complaintInner.Controls.Add(line);

        AddCardText(complaintInner, $"▣  {DateTimeText(complaintCreated)}", 18, 124, complaintInner.Width - 36, 20, 8.2f, FontStyle.Regular, ModernUi.Text);
        AddCardText(complaintInner, "♙  Ban quản lý tòa A", 18, 144, complaintInner.Width - 36, 20, 8.2f, FontStyle.Regular, ModernUi.Text);

        var complaintDetail = ModernUi.OutlineButton("Xem chi tiết phản ánh", latestComplaintBox.Width - 32, 32);
        complaintDetail.Location = new Point(16, latestComplaintBox.Height - 46);
        complaintDetail.Click += (_, _) => Navigate("send-complaint");
        latestComplaintBox.Controls.Add(complaintDetail);
        page.Controls.Add(latestComplaintBox);

        int row3Y = row2Y + row2H + gap;
        int usableRow3W = pageW - gap;

        int invoiceTableW = (int)(usableRow3W * 0.68);
        int complaintListW = usableRow3W - invoiceTableW;

        // Không ép min quá lớn, vì ép min là nguyên nhân làm panel bên phải bị che.
        invoiceTableW = Math.Max(600, invoiceTableW);
        complaintListW = pageW - invoiceTableW - gap;

        var invoicePanel = CreateDemoCard("Hóa đơn gần đây", invoiceTableW, 288);
        invoicePanel.Location = new Point(margin, row3Y);

        var invoiceRows = residentInvoices
            .Take(5)
            .Select(invoice => new object[]
            {
            $"{invoice.Month:00}/{invoice.Year}",
            DateText(invoice.CreatedAt),
            DateText(invoice.DueDate),
            Money(invoice.TotalAmount),
            Money(invoice.PaidAmount),
            Money(Math.Max(0m, invoice.TotalAmount - invoice.PaidAmount)),
            ViStatus(invoice.PaymentStatus),
            "Xem"
            })
            .ToList();

        var demoAmounts = new[] { 1384000m, 1681000m, 1846000m, 1295000m, 1450000m };
        var demoPaid = new[] { 761200m, 1681000m, 1846000m, 1295000m, 1450000m };
        var demoStatus = new[] { "Chưa thanh toán", "Đã thanh toán", "Đã thanh toán", "Đã thanh toán", "Đã thanh toán" };

        while (invoiceRows.Count < 5)
        {
            int index = invoiceRows.Count;
            DateTime period = new DateTime(2026, 5, 1).AddMonths(-index);
            decimal total = demoAmounts[index];
            decimal paid = demoPaid[index];
            invoiceRows.Add(new object[]
            {
            $"{period.Month:00}/{period.Year}",
            DateText(new DateTime(period.Year, period.Month, 20)),
            DateText(new DateTime(period.Year, period.Month, Math.Min(31, DateTime.DaysInMonth(period.Year, period.Month)))),
            Money(total),
            Money(paid),
            Money(Math.Max(0m, total - paid)),
            demoStatus[index],
            "Xem"
            });
        }

        var invoiceGrid = CreateGrid(
            new[] { "Kỳ", "Ngày phát hành", "Hạn thanh toán", "Số tiền", "Đã thanh toán", "Còn lại", "Trạng thái", "Hành động" },
            invoiceRows.ToArray());

        invoiceGrid.Location = new Point(14, 48);
        invoiceGrid.Size = new Size(invoicePanel.Width - 28, 178);
        invoiceGrid.RowTemplate.Height = 30;
        invoiceGrid.ColumnHeadersHeight = 34;

        // Cho phép kéo ngang trong bảng hóa đơn
        invoiceGrid.ScrollBars = ScrollBars.Both;
        invoiceGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        invoiceGrid.RowHeadersVisible = false;
        invoiceGrid.AllowUserToResizeColumns = false;
        invoiceGrid.AllowUserToResizeRows = false;

        invoiceGrid.DefaultCellStyle.Font = ModernUi.Font(7.6f);
        invoiceGrid.ColumnHeadersDefaultCellStyle.Font = ModernUi.Font(7.5f, FontStyle.Bold);
        invoiceGrid.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
        invoiceGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(2, 0, 2, 0);

        void FixInvoiceGridColumns()
        {
            if (invoiceGrid.Columns.Count < 8)
            {
                return;
            }

            invoiceGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            invoiceGrid.Columns[0].Width = 80;   // Kỳ
            invoiceGrid.Columns[1].Width = 120;  // Ngày phát hành
            invoiceGrid.Columns[2].Width = 120;  // Hạn thanh toán
            invoiceGrid.Columns[3].Width = 110;  // Số tiền
            invoiceGrid.Columns[4].Width = 115;  // Đã thanh toán
            invoiceGrid.Columns[5].Width = 100;  // Còn lại
            invoiceGrid.Columns[6].Width = 130;  // Trạng thái
            invoiceGrid.Columns[7].Width = 90;   // Hành động

            invoiceGrid.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            invoiceGrid.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            invoiceGrid.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            invoiceGrid.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            invoiceGrid.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            invoiceGrid.Columns[5].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            invoiceGrid.Columns[6].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            invoiceGrid.Columns[7].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        FixInvoiceGridColumns();
        invoiceGrid.DataBindingComplete += (_, _) => FixInvoiceGridColumns();

        invoiceGrid.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                Navigate("my-invoices");
            }
        };
        invoicePanel.Controls.Add(invoiceGrid);

var allInvoices = ModernUi.OutlineButton("Xem tất cả hóa đơn", invoicePanel.Width - 28, 32);
allInvoices.Location = new Point(14, invoicePanel.Height - 42);
        allInvoices.Click += (_, _) => Navigate("my-invoices");
        invoicePanel.Controls.Add(allInvoices);
        page.Controls.Add(invoicePanel);

        var complaintsList = CreateDemoCard("Phản ánh của tôi", complaintListW, 288);
        complaintsList.Location = new Point(invoicePanel.Right + gap, row3Y);

        var complaintRows = new List<(string Title, string Code, string Status, string Date, Color Accent)>();

        foreach (var c in residentComplaints.Cast<object>().OrderByDescending(c => ReadDynamicDate(c, "CreatedAt", DateTime.MinValue)).Take(3))
        {
            DateTime created = ReadDynamicDate(c, "CreatedAt", DateTime.Today);
            string title = Display(ReadDynamicString(c, "Title"), "Phản ánh");
            string code = $"PA{created:yyMMdd}-{ReadDynamicInt(c, "ComplaintID", 1):000}";
            string status = ViStatus(ReadDynamicString(c, "Status"));
            Color color = status is "Đã xử lý" or "Đã đóng" ? ModernUi.Green : status == "Mới" ? ModernUi.Orange : ModernUi.Blue;
            complaintRows.Add((title, code, status, DateText(created), color));
        }

        while (complaintRows.Count < 3)
        {
            if (complaintRows.Count == 0)
                complaintRows.Add(("Đèn hành lang không sáng", "PA260515-001", "Đang xử lý", "16/05/2026", ModernUi.Blue));
            else if (complaintRows.Count == 1)
                complaintRows.Add(("Nước chảy yếu tại căn hộ", "PA260502-002", "Đã xử lý", "02/05/2026", ModernUi.Green));
            else
                complaintRows.Add(("Thang máy hay dừng đột ngột", "PA260420-003", "Đã đóng", "20/04/2026", Color.FromArgb(148, 163, 184)));
        }

        int complaintY = 54;
        foreach (var item in complaintRows.Take(3))
        {
            AddComplaintRow(complaintsList, item.Title, item.Code, item.Status, item.Date, item.Accent, complaintY);
            complaintY += 58;
        }

        var newComplaint = ModernUi.OutlineButton("Gửi phản ánh mới", complaintsList.Width - 28, 32);
        newComplaint.Location = new Point(14, complaintsList.Height - 46);
        newComplaint.Click += (_, _) => Navigate("send-complaint");
        complaintsList.Controls.Add(newComplaint);
        page.Controls.Add(complaintsList);

        page.AutoScrollMinSize = new Size(0, row3Y + 322);
        page.HorizontalScroll.Enabled = false;
        page.HorizontalScroll.Visible = false;

        static RoundedPanel CreateDemoCard(string title, int width, int height, Color? titleColor = null)
        {
            var card = ModernUi.CardPanel(12);
            card.Size = new Size(width, height);

            var titleLabel = ModernUi.Label(title.ToUpperInvariant(), 9.2f, FontStyle.Bold, titleColor ?? ModernUi.Blue);
            titleLabel.Location = new Point(18, 14);
            titleLabel.Size = new Size(width - 36, 24);
            titleLabel.AutoEllipsis = true;
            card.Controls.Add(titleLabel);

            return card;
        }

        static void AddCardText(Control parent, string text, int x, int y, int width, int height, float size, FontStyle style, Color color)
        {
            var label = ModernUi.Label(text, size, style, color);
            label.Location = new Point(x, y);
            label.Size = new Size(width, height);
            label.AutoEllipsis = true;
            parent.Controls.Add(label);
        }

        void AddTaskItem(Control parent, string iconText, Color accent, string title, string detail, string badgeText, Color badgeColor, string target, ref int y)
        {
            var item = ModernUi.CardPanel(10);
            item.Location = new Point(16, y);
            item.Size = new Size(parent.Width - 32, 64);
            item.Cursor = Cursors.Hand;

            var icon = new CircleLabel
            {
                Text = iconText,
                CircleColor = accent,
                ForeColor = Color.White,
                Font = ModernUi.Font(13f, FontStyle.Bold),
                Location = new Point(12, 12),
                Size = new Size(40, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            item.Controls.Add(icon);

            var badge = ModernUi.Badge(badgeText, badgeColor);
            int badgeW = Math.Max(92, TextRenderer.MeasureText(badgeText, badge.Font).Width + 22);
            badge.Size = new Size(badgeW, 24);
            badge.Location = new Point(item.Width - badgeW - 14, 20);
            item.Controls.Add(badge);

            int textW = Math.Max(120, item.Width - 62 - badgeW - 34);

            AddCardText(item, title, 62, 10, textW, 22, 8.7f, FontStyle.Bold, ModernUi.Navy);
            AddCardText(item, detail, 62, 34, textW, 20, 8.1f, FontStyle.Regular, ModernUi.Text);

            BindTileClick(item, (_, _) => Navigate(target));
            parent.Controls.Add(item);
            y += 74;
        }

        static void AddNoticeLine(Control parent, string title, string detail, string time, Color dotColor, int y)
        {
            var dot = new CircleLabel
            {
                Text = "",
                CircleColor = dotColor,
                Location = new Point(18, y + 8),
                Size = new Size(8, 8)
            };
            parent.Controls.Add(dot);

            var titleLabel = ModernUi.Label(title, 8.8f, FontStyle.Bold, ModernUi.Navy);
            titleLabel.Location = new Point(42, y);
            titleLabel.Size = new Size(parent.Width - 160, 22);
            titleLabel.AutoEllipsis = true;
            parent.Controls.Add(titleLabel);

            var timeLabel = ModernUi.Label(time, 8.1f, FontStyle.Regular, ModernUi.Muted);
            timeLabel.Location = new Point(parent.Width - 122, y);
            timeLabel.Size = new Size(104, 22);
            timeLabel.TextAlign = ContentAlignment.MiddleRight;
            timeLabel.AutoEllipsis = true;
            parent.Controls.Add(timeLabel);

            var detailLabel = ModernUi.Label(detail, 8.2f, FontStyle.Regular, ModernUi.Text);
            detailLabel.Location = new Point(42, y + 24);
            detailLabel.Size = new Size(parent.Width - 60, 22);
            detailLabel.AutoEllipsis = true;
            parent.Controls.Add(detailLabel);

            var line = new Panel
            {
                BackColor = Color.FromArgb(226, 232, 240),
                Location = new Point(18, y + 56),
                Size = new Size(parent.Width - 36, 1)
            };
            parent.Controls.Add(line);
        }

        static void AddComplaintRow(Control parent, string title, string code, string status, string date, Color accent, int y)
        {
            var row = ModernUi.CardPanel(8);
            row.Location = new Point(14, y);
            row.Size = new Size(parent.Width - 28, 50);

            var icon = new CircleLabel
            {
                Text = "▣",
                CircleColor = Color.FromArgb(239, 246, 255),
                ForeColor = accent,
                Font = ModernUi.Font(12f, FontStyle.Bold),
                Location = new Point(12, 11),
                Size = new Size(28, 28),
                TextAlign = ContentAlignment.MiddleCenter
            };
            row.Controls.Add(icon);

            var titleLabel = ModernUi.Label(title, 8.6f, FontStyle.Bold, ModernUi.Navy);
            titleLabel.Location = new Point(52, 8);
            titleLabel.Size = new Size(row.Width - 210, 20);
            titleLabel.AutoEllipsis = true;
            row.Controls.Add(titleLabel);

            var codeLabel = ModernUi.Label(code, 8.1f, FontStyle.Regular, ModernUi.Text);
            codeLabel.Location = new Point(52, 28);
            codeLabel.Size = new Size(row.Width - 210, 18);
            codeLabel.AutoEllipsis = true;
            row.Controls.Add(codeLabel);

            var badge = ModernUi.Badge(status, accent);
            badge.Location = new Point(row.Width - 150, 13);
            badge.Size = new Size(82, 24);
            row.Controls.Add(badge);

            var dateLabel = ModernUi.Label(date, 8f, FontStyle.Regular, ModernUi.Muted);
            dateLabel.Location = new Point(row.Width - 66, 14);
            dateLabel.Size = new Size(54, 22);
            dateLabel.TextAlign = ContentAlignment.MiddleRight;
            row.Controls.Add(dateLabel);

            parent.Controls.Add(row);
        }

        static string ReadDynamicString(object? obj, string propertyName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return string.Empty;
            }

            var property = obj.GetType().GetProperty(propertyName);
            if (property == null)
            {
                return string.Empty;
            }

            return property.GetValue(obj, null)?.ToString() ?? string.Empty;
        }

        static int ReadDynamicInt(object? obj, string propertyName, int fallback = 0)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return fallback;
            }

            var property = obj.GetType().GetProperty(propertyName);
            if (property == null)
            {
                return fallback;
            }

            var value = property.GetValue(obj, null);
            if (value == null)
            {
                return fallback;
            }

            return int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : fallback;
        }

        static DateTime ReadDynamicDate(object? obj, string propertyName, DateTime fallback)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return fallback;
            }

            var property = obj.GetType().GetProperty(propertyName);
            if (property == null)
            {
                return fallback;
            }

            var value = property.GetValue(obj, null);
            if (value == null)
            {
                return fallback;
            }

            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            return DateTime.TryParse(value.ToString(), CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime parsed)
                ? parsed
                : fallback;
        }
    }

    private RoundedPanel CreateDebtCard(int unpaidCount, decimal debtAmount, int width, bool hasDebt, int height = 126)
    {
        var card = ModernUi.CardPanel();
        card.Size = new Size(width, height);

        Color accentColor = hasDebt ? ModernUi.Red : Color.FromArgb(249, 115, 22);
        var titleLabel = ModernUi.Label("NỢ CHƯA THU", 8.5f, FontStyle.Bold, accentColor);
        titleLabel.Location = new Point(16, 12);
        titleLabel.Size = new Size(width - 32, 22);
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        titleLabel.AutoEllipsis = true;
        card.Controls.Add(titleLabel);

        int iconSize = width < 190 ? 44 : 50;
        int iconLeft = 18;
        int iconTop = 48;
        var circle = new CircleLabel
        {
            Text = "!",
            CircleColor = accentColor,
            ForeColor = Color.White,
            Font = ModernUi.Font(iconSize >= 50 ? 18f : 16f, FontStyle.Bold),
            Size = new Size(iconSize, iconSize),
            Location = new Point(iconLeft, iconTop),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(circle);

        int textLeft = iconLeft + iconSize + 14;
        int textWidth = Math.Max(84, width - textLeft - 16);
        string debtText = $"{Money(debtAmount)} VNĐ";
        float valueFontSize = debtText.Length > 15 ? 11.5f : debtText.Length > 12 ? 12.5f : 15f;
        var valueLabel = ModernUi.Label(debtText, valueFontSize, FontStyle.Bold, accentColor);
        valueLabel.Location = new Point(textLeft, 44);
        valueLabel.Size = new Size(textWidth, 30);
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.AutoEllipsis = true;
        card.Controls.Add(valueLabel);

        var unitLabel = ModernUi.Label("Công nợ", 8.6f, FontStyle.Regular, ModernUi.Muted);
        unitLabel.Location = new Point(textLeft, 72);
        unitLabel.Size = new Size(textWidth, 18);
        unitLabel.TextAlign = ContentAlignment.MiddleLeft;
        card.Controls.Add(unitLabel);

        var detailLabel = ModernUi.Label($"{unpaidCount:N0} hóa đơn chưa thanh toán", 8.4f, FontStyle.Regular, ModernUi.Text);
        detailLabel.Location = new Point(textLeft, height - 32);
        detailLabel.Size = new Size(textWidth, 20);
        detailLabel.TextAlign = ContentAlignment.MiddleLeft;
        detailLabel.AutoEllipsis = true;
        card.Controls.Add(detailLabel);

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
