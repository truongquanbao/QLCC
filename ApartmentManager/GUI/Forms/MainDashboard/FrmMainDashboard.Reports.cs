using ApartmentManager.DAL;
using ApartmentManager.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public partial class FrmMainDashboard
{
    private const string ReportAllText = "Tất cả";
    private const string ReportNoDataText = "Không có dữ liệu trong khoảng thời gian này";

    private sealed class ReportDashboardSource
    {
        public List<ResidentDTO> Residents { get; init; } = new();
        public List<ApartmentDTO> Apartments { get; init; } = new();
        public List<InvoiceDTO> Invoices { get; init; } = new();
        public List<dynamic> Complaints { get; init; } = new();
        public List<dynamic> Vehicles { get; init; } = new();
        public List<dynamic> Visitors { get; init; } = new();
    }

    private sealed class ReportDashboardSnapshot
    {
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public string Building { get; init; } = ReportAllText;
        public string Block { get; init; } = ReportAllText;
        public string ReportType { get; init; } = "Tổng quan";
        public List<ResidentDTO> Residents { get; init; } = new();
        public List<ApartmentDTO> Apartments { get; init; } = new();
        public List<InvoiceDTO> Invoices { get; init; } = new();
        public List<dynamic> Complaints { get; init; } = new();
        public List<dynamic> Vehicles { get; init; } = new();
        public List<dynamic> Visitors { get; init; } = new();

        public bool HasData =>
            Residents.Count > 0 ||
            Apartments.Count > 0 ||
            Invoices.Count > 0 ||
            Complaints.Count > 0 ||
            Vehicles.Count > 0 ||
            Visitors.Count > 0;
    }

    internal sealed class ReportMetric
    {
        public string Title { get; init; } = "";
        public string Value { get; init; } = "0";
        public string Unit { get; init; } = "";
        public string Icon { get; init; } = "";
        public string Detail { get; init; } = "";
        public Color Accent { get; init; } = ModernUi.Blue;
        public bool NegativeTrend { get; init; }
    }

    internal sealed class ReportChartItem
    {
        public string Label { get; init; } = "";
        public decimal Value { get; init; }
        public Color Color { get; init; } = ModernUi.Blue;
    }

    private sealed class ReportFilterField
    {
        public Label Label { get; init; } = null!;
        public Control Input { get; init; } = null!;
        public int PreferredWidth { get; init; } = 168;
    }

    private void RenderReportsDashboard()
    {
        var page = BeginPage("Báo cáo & thống kê", "Dashboard / Báo cáo & thống kê");
        var source = LoadReportDashboardSource();
        var reportToolTip = new ToolTip
        {
            InitialDelay = 250,
            ReshowDelay = 100,
            AutoPopDelay = 8000,
            ShowAlways = true
        };
        page.Disposed += (_, _) => reportToolTip.Dispose();

        DateTime defaultReportDate = ReportLatestDate(source);
        DateTime defaultStart = new(defaultReportDate.Year, defaultReportDate.Month, 1);
        DateTime defaultEnd = ReportMonthEnd(defaultReportDate);

        var filterPanel = ModernUi.CardPanel();
        filterPanel.Padding = Padding.Empty;
        page.Controls.Add(filterPanel);

        var periodPicker = CreateReportDatePicker(defaultReportDate);
        var fromPicker = CreateReportDatePicker(defaultStart);
        var toPicker = CreateReportDatePicker(defaultEnd);
        var buildingCombo = ModernUi.ComboBox(Array.Empty<string>(), 168);
        var blockCombo = ModernUi.ComboBox(Array.Empty<string>(), 150);
        var reportTypeCombo = ModernUi.ComboBox(new[] { "Tổng quan", "Tài chính", "Vận hành", "Công nợ" }, 168);

        var fields = new List<ReportFilterField>
        {
            AddReportFilterField(filterPanel, "Kỳ báo cáo", periodPicker, 168),
            AddReportFilterField(filterPanel, "Từ ngày", fromPicker, 168),
            AddReportFilterField(filterPanel, "Đến ngày", toPicker, 168),
            AddReportFilterField(filterPanel, "Tòa nhà", buildingCombo, 168),
            AddReportFilterField(filterPanel, "Block", blockCombo, 150),
            AddReportFilterField(filterPanel, "Loại báo cáo", reportTypeCombo, 168)
        };

        var warningLabel = ModernUi.Label("", 8.7f, FontStyle.Bold, ModernUi.Orange);
        warningLabel.AutoEllipsis = true;
        filterPanel.Controls.Add(warningLabel);

        var excelButton = ModernUi.Button("Excel", ModernUi.Green, 98, 36);
        var pdfButton = ModernUi.Button("PDF", ModernUi.Red, 90, 36);
        var refreshButton = ModernUi.Button("Làm mới", ModernUi.Blue, 112, 36);
        var advancedButton = ModernUi.OutlineButton("Bộ lọc nâng cao", 156, 36);
        filterPanel.Controls.Add(excelButton);
        filterPanel.Controls.Add(pdfButton);
        filterPanel.Controls.Add(refreshButton);
        filterPanel.Controls.Add(advancedButton);

        var noDataLabel = ModernUi.Label(ReportNoDataText, 9.2f, FontStyle.Bold, ModernUi.Red);
        noDataLabel.TextAlign = ContentAlignment.MiddleCenter;
        noDataLabel.Visible = false;
        page.Controls.Add(noDataLabel);

        var kpiCards = new[]
        {
            new ReportMetricCard(),
            new ReportMetricCard(),
            new ReportMetricCard(),
            new ReportMetricCard(),
            new ReportMetricCard(),
            new ReportMetricCard()
        };
        foreach (var card in kpiCards)
        {
            page.Controls.Add(card);
        }

        var revenueSection = ModernUi.Section("Doanh thu theo tháng (VND)", 360, 268);
        var revenueChart = new ReportLineChart { EmptyMessage = "Chưa có dữ liệu doanh thu" };
        var revenueNote = ModernUi.Label("", 8.4f, FontStyle.Bold, ModernUi.Blue);
        revenueNote.AutoEllipsis = true;
        revenueSection.Controls.Add(revenueChart);
        revenueSection.Controls.Add(revenueNote);
        page.Controls.Add(revenueSection);

        var feeSection = ModernUi.Section("Doanh thu theo loại phí", 320, 268);
        var feeChart = new ReportDonutChart { EmptyMessage = "Chưa có dữ liệu loại phí", CenterSubText = "Tổng doanh thu" };
        feeSection.Controls.Add(feeChart);
        page.Controls.Add(feeSection);

        var debtSection = ModernUi.Section("Tình hình công nợ", 330, 268);
        var debtChart = new ReportDonutChart { EmptyMessage = "Không có công nợ", CenterSubText = "Tổng công nợ" };
        var debtWarning = ModernUi.Label("", 8.5f, FontStyle.Bold, ModernUi.Red);
        debtWarning.AutoEllipsis = true;
        debtSection.Controls.Add(debtChart);
        debtSection.Controls.Add(debtWarning);
        page.Controls.Add(debtSection);

        var occupancySection = ModernUi.Section("Tỷ lệ lấp đầy", 240, 216);
        var occupancyChart = new ReportDonutChart { EmptyMessage = "Không có căn hộ", CenterSubText = "Lấp đầy", CompactLegend = true };
        var occupancyUpdated = ModernUi.Label("", 8f, FontStyle.Regular, ModernUi.Muted);
        occupancySection.Controls.Add(occupancyChart);
        occupancySection.Controls.Add(occupancyUpdated);
        page.Controls.Add(occupancySection);

        var residentsSection = ModernUi.Section("Cư dân theo tòa nhà", 300, 216);
        var residentsChart = new ReportBarChart { EmptyMessage = "Chưa có dữ liệu cư dân", SeriesLabel = "Người", BarColor = ModernUi.Blue };
        residentsSection.Controls.Add(residentsChart);
        page.Controls.Add(residentsSection);

        var complaintSection = ModernUi.Section("Phản ánh theo trạng thái", 300, 216);
        var complaintChart = new ReportDonutChart { EmptyMessage = "Chưa có phản ánh", CenterSubText = "Tổng phiếu", CompactLegend = true };
        complaintSection.Controls.Add(complaintChart);
        page.Controls.Add(complaintSection);

        var vehicleSection = ModernUi.Section("Phương tiện theo loại", 300, 216);
        var vehicleChart = new ReportDonutChart { EmptyMessage = "Chưa có phương tiện", CenterSubText = "Tổng xe", CompactLegend = true };
        vehicleSection.Controls.Add(vehicleChart);
        page.Controls.Add(vehicleSection);

        var topDebtSection = ModernUi.Section("Top 5 căn hộ nợ nhiều nhất", 420, 236);
        string[] topDebtColumns = { "#", "Căn hộ", "Chủ hộ", "Công nợ (VND)", "Hóa đơn quá hạn" };
        var topDebtGrid = CreateGrid(topDebtColumns, new[] { EmptyRow(topDebtColumns.Length, "Không có công nợ") });
        ConfigureReportWideGrid(topDebtGrid, 58, 110, 150, 148, 150);
        topDebtSection.Controls.Add(topDebtGrid);
        page.Controls.Add(topDebtSection);

        var overdueComplaintSection = ModernUi.Section("Phản ánh quá hạn", 420, 236);
        string[] overdueColumns = { "#", "Mã phiếu", "Nội dung", "Ngày tạo", "Quá hạn" };
        var overdueGrid = CreateGrid(overdueColumns, new[] { EmptyRow(overdueColumns.Length, "Không có phản ánh quá hạn") });
        ConfigureReportWideGrid(overdueGrid, 58, 130, 300, 128, 110);
        overdueComplaintSection.Controls.Add(overdueGrid);
        page.Controls.Add(overdueComplaintSection);

        var visitorSection = ModernUi.Section("Khách ra vào hôm nay", 300, 236);
        var visitorLabels = new[]
        {
            ModernUi.Label("", 9f, FontStyle.Bold, ModernUi.Blue),
            ModernUi.Label("", 9f, FontStyle.Bold, ModernUi.Teal),
            ModernUi.Label("", 9f, FontStyle.Bold, ModernUi.Red),
            ModernUi.Label("", 9f, FontStyle.Bold, ModernUi.Purple)
        };
        var visitorLink = ModernUi.Label("Xem chi tiết khách ra vào", 8.6f, FontStyle.Bold, ModernUi.Blue);
        visitorLink.Cursor = Cursors.Hand;
        foreach (var label in visitorLabels)
        {
            label.AutoEllipsis = true;
            visitorSection.Controls.Add(label);
        }
        visitorSection.Controls.Add(visitorLink);
        page.Controls.Add(visitorSection);

        var historySection = ModernUi.Section("Danh sách báo cáo đã tạo", 980, 236);
        string[] historyColumns = { "Báo cáo", "Kỳ", "Người tạo", "Ngày tạo", "Định dạng", "Trạng thái" };
        var historyGrid = CreateGrid(historyColumns, BuildReportDashboardHistoryRows(historyColumns.Length));
        historySection.Controls.Add(historyGrid);
        page.Controls.Add(historySection);

        bool syncingFilters = false;
        ReportDashboardSnapshot currentSnapshot = null;
        ReportDashboardSnapshot previousSnapshot = null;

        PopulateReportFilterCombos();
        WireReportEvents();
        RefreshReportDashboard(false, false);

        void WireReportEvents()
        {
            periodPicker.ValueChanged += (_, _) =>
            {
                if (syncingFilters)
                {
                    return;
                }

                syncingFilters = true;
                DateTime selected = periodPicker.Value.Date;
                fromPicker.Value = SafeReportPickerValue(fromPicker, new DateTime(selected.Year, selected.Month, 1));
                toPicker.Value = SafeReportPickerValue(toPicker, ReportMonthEnd(selected));
                syncingFilters = false;
                RefreshReportDashboard(false, true);
            };

            fromPicker.ValueChanged += (_, _) =>
            {
                if (!syncingFilters)
                {
                    RefreshReportDashboard(false, true);
                }
            };

            toPicker.ValueChanged += (_, _) =>
            {
                if (!syncingFilters)
                {
                    RefreshReportDashboard(false, true);
                }
            };

            buildingCombo.SelectedIndexChanged += (_, _) =>
            {
                if (syncingFilters)
                {
                    return;
                }

                string selectedBlock = blockCombo.SelectedItem?.ToString() ?? ReportAllText;
                PopulateBlockCombo(selectedBlock);
                RefreshReportDashboard(false, true);
            };

            blockCombo.SelectedIndexChanged += (_, _) =>
            {
                if (!syncingFilters)
                {
                    RefreshReportDashboard(false, true);
                }
            };

            reportTypeCombo.SelectedIndexChanged += (_, _) =>
            {
                if (!syncingFilters)
                {
                    RefreshReportDashboard(false, true);
                }
            };

            refreshButton.Click += (_, _) =>
            {
                syncingFilters = true;
                source = LoadReportDashboardSource();
                DateTime latest = ReportLatestDate(source);
                periodPicker.Value = SafeReportPickerValue(periodPicker, latest);
                fromPicker.Value = SafeReportPickerValue(fromPicker, new DateTime(latest.Year, latest.Month, 1));
                toPicker.Value = SafeReportPickerValue(toPicker, ReportMonthEnd(latest));
                PopulateReportFilterCombos();
                reportTypeCombo.SelectedIndex = 0;
                syncingFilters = false;
                RefreshReportDashboard(true, false);
            };

            excelButton.Click += (_, _) => ExportReportDashboard("Excel");
            pdfButton.Click += (_, _) => ExportReportDashboard("PDF");
            advancedButton.Click += (_, _) =>
            {
                MessageBox.Show(this,
                    "Bộ lọc nâng cao đang dùng các điều kiện:\n\n" +
                    "• Kỳ báo cáo\n" +
                    "• Từ ngày\n" +
                    "• Đến ngày\n" +
                    "• Tòa nhà\n" +
                    "• Block\n" +
                    "• Loại báo cáo",
                    "Bộ lọc nâng cao",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            visitorLink.Click += (_, _) => Navigate("visitors");
            page.Resize += (_, _) => LayoutReportDashboard();
        }

        void PopulateReportFilterCombos()
        {
            string selectedBuilding = buildingCombo.SelectedItem?.ToString() ?? ReportAllText;
            var buildings = source.Apartments
                .Select(a => Display(a.BuildingName, "Chưa rõ"))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            buildings.Insert(0, ReportAllText);

            buildingCombo.BeginUpdate();
            buildingCombo.Items.Clear();
            buildingCombo.Items.AddRange(buildings.Cast<object>().ToArray());
            int buildingIndex = buildings.FindIndex(v => string.Equals(v, selectedBuilding, StringComparison.CurrentCultureIgnoreCase));
            buildingCombo.SelectedIndex = buildingIndex >= 0 ? buildingIndex : 0;
            buildingCombo.EndUpdate();

            PopulateBlockCombo(blockCombo.SelectedItem?.ToString() ?? ReportAllText);
        }

        void PopulateBlockCombo(string selectedBlock)
        {
            string selectedBuilding = buildingCombo.SelectedItem?.ToString() ?? ReportAllText;
            var blocks = source.Apartments
                .Where(a => ReportMatchesFilter(Display(a.BuildingName, "Chưa rõ"), selectedBuilding))
                .Select(a => Display(a.BlockName, "Chưa rõ"))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            blocks.Insert(0, ReportAllText);

            blockCombo.BeginUpdate();
            blockCombo.Items.Clear();
            blockCombo.Items.AddRange(blocks.Cast<object>().ToArray());
            int selectedIndex = blocks.FindIndex(v => string.Equals(v, selectedBlock, StringComparison.CurrentCultureIgnoreCase));
            blockCombo.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            blockCombo.EndUpdate();
        }

        void RefreshReportDashboard(bool reloaded, bool validate)
        {
            SetReportChartsLoading(true);

            if (validate)
            {
                ValidateReportDateRange();
            }
            else
            {
                warningLabel.Text = "";
                warningLabel.Visible = false;
            }

            DateTime start = fromPicker.Value.Date;
            DateTime end = toPicker.Value.Date;
            currentSnapshot = BuildReportDashboardSnapshot(source, start, end, SelectedText(buildingCombo), SelectedText(blockCombo), SelectedText(reportTypeCombo));
            previousSnapshot = BuildPreviousReportSnapshot(source, currentSnapshot);

            UpdateReportDashboard(currentSnapshot, previousSnapshot, reloaded);
            SetReportChartsLoading(false);
            LayoutReportDashboard();
        }

        void ValidateReportDateRange()
        {
            DateTime start = fromPicker.Value.Date;
            DateTime end = toPicker.Value.Date;
            string message = "";

            if (start > end)
            {
                syncingFilters = true;
                fromPicker.Value = SafeReportPickerValue(fromPicker, end);
                toPicker.Value = SafeReportPickerValue(toPicker, start);
                syncingFilters = false;
                start = fromPicker.Value.Date;
                end = toPicker.Value.Date;
                message = "Đã tự điều chỉnh khoảng ngày hợp lệ.";
            }

            if ((end - start).TotalDays > 366)
            {
                message = "Khoảng thời gian quá lớn, dữ liệu có thể tải chậm.";
            }

            warningLabel.Text = message;
            warningLabel.Visible = !string.IsNullOrWhiteSpace(message);
        }

        void SetReportChartsLoading(bool loading)
        {
            foreach (var chartControl in new Control[] { revenueChart, feeChart, debtChart, occupancyChart, residentsChart, complaintChart, vehicleChart })
            {
                if (chartControl is IReportChartState state)
                {
                    state.IsLoading = loading;
                    chartControl.Invalidate();
                }
            }
        }

        void UpdateReportDashboard(ReportDashboardSnapshot snapshot, ReportDashboardSnapshot previous, bool reloaded)
        {
            var metrics = BuildReportMetrics(snapshot, previous);
            for (int i = 0; i < kpiCards.Length; i++)
            {
                kpiCards[i].SetMetric(metrics[i]);
            }

            bool noData = !snapshot.HasData;
            noDataLabel.Visible = noData;

            var monthlyRevenue = BuildMonthlyRevenue(snapshot);
            revenueChart.SetData(monthlyRevenue);
            revenueNote.Text = BuildRevenueNote(monthlyRevenue);

            var feeItems = BuildFeeRevenueItems(snapshot);
            feeChart.CenterText = ReportMoneyCompact(snapshot.Invoices.Sum(i => i.PaidAmount));
            feeChart.SetData(feeItems);

            var debtItems = BuildDebtAgingItems(snapshot);
            decimal debtTotal = snapshot.Invoices.Sum(InvoiceDebtAmount);
            int overdueInvoiceCount = snapshot.Invoices.Count(i => InvoiceDebtAmount(i) > 0 && (snapshot.EndDate.Date - (i.DueDate ?? snapshot.EndDate).Date).TotalDays > 0);
            debtChart.CenterText = ReportMoneyCompact(debtTotal);
            debtChart.SetData(debtItems);
            debtWarning.Text = overdueInvoiceCount > 0 ? $"{overdueInvoiceCount:N0} hóa đơn quá hạn thanh toán" : "Không có hóa đơn quá hạn";

            int occupied = snapshot.Apartments.Count(IsOccupiedApartment);
            int occupancyPercent = snapshot.Apartments.Count == 0 ? 0 : (int)Math.Round(occupied * 100m / snapshot.Apartments.Count);
            occupancyChart.CenterText = $"{occupancyPercent}%";
            occupancyChart.SetData(new[]
            {
                new ReportChartItem { Label = "Đang ở", Value = occupied, Color = ModernUi.Green },
                new ReportChartItem { Label = "Trống", Value = Math.Max(0, snapshot.Apartments.Count - occupied), Color = Color.FromArgb(203, 213, 225) }
            });
            occupancyUpdated.Text = $"Cập nhật: {DateText(snapshot.EndDate)}";

            residentsChart.SetData(BuildResidentByBuildingItems(snapshot));

            complaintChart.CenterText = snapshot.Complaints.Count.ToString("N0", CultureInfo.InvariantCulture);
            complaintChart.SetData(BuildComplaintStatusItems(snapshot));

            vehicleChart.CenterText = snapshot.Vehicles.Count.ToString("N0", CultureInfo.InvariantCulture);
            vehicleChart.SetData(BuildVehicleTypeItems(snapshot));

            SetGridData(topDebtGrid, topDebtColumns, BuildTopDebtRows(snapshot, topDebtColumns.Length));
            ConfigureReportWideGrid(topDebtGrid, 58, 110, 150, 148, 150);
            SetGridData(overdueGrid, overdueColumns, BuildOverdueComplaintRows(snapshot, overdueColumns.Length));
            ConfigureReportWideGrid(overdueGrid, 58, 130, 300, 128, 110);
            UpdateVisitorPanel(snapshot);
            SetGridData(historyGrid, historyColumns, BuildReportDashboardHistoryRows(historyColumns.Length));

            reportToolTip.SetToolTip(revenueChart, ChartSummary("Doanh thu theo tháng", monthlyRevenue, "VND"));
            reportToolTip.SetToolTip(feeChart, ChartSummary("Doanh thu theo loại phí", feeItems, "VND"));
            reportToolTip.SetToolTip(debtChart, ChartSummary("Tình hình công nợ", debtItems, "VND"));
            reportToolTip.SetToolTip(residentsChart, ChartSummary("Cư dân theo tòa nhà", residentsChart.Items, "người"));
            reportToolTip.SetToolTip(complaintChart, ChartSummary("Phản ánh theo trạng thái", complaintChart.Items, "phiếu"));
            reportToolTip.SetToolTip(vehicleChart, ChartSummary("Phương tiện theo loại", vehicleChart.Items, "xe"));

            if (reloaded && !warningLabel.Visible)
            {
                warningLabel.Text = "Dữ liệu báo cáo đã được làm mới.";
                warningLabel.Visible = true;
            }
        }

        void UpdateVisitorPanel(ReportDashboardSnapshot snapshot)
        {
            DateTime focusDate = snapshot.StartDate <= DateTime.Today && snapshot.EndDate >= DateTime.Today
                ? DateTime.Today
                : snapshot.EndDate.Date;
            var dayVisitors = snapshot.Visitors
                .Where(v => ReportFirstDynamicDate(v, "ArrivalTime", "CheckInTime", "CreatedAt")?.Date == focusDate)
                .ToList();
            int inside = dayVisitors.Count(v => ReportStatusAny(GetDynamicString(v, "Status"), "CheckedIn", "Approved", "InProgress"));
            int left = dayVisitors.Count(v => ReportStatusAny(GetDynamicString(v, "Status"), "CheckedOut", "Completed", "Closed"));
            int rejected = dayVisitors.Count(v => ReportStatusAny(GetDynamicString(v, "Status"), "Rejected", "Cancelled", "Denied"));

            visitorLabels[0].Text = $"Tổng khách: {dayVisitors.Count:N0} lượt";
            visitorLabels[1].Text = $"Đang trong tòa: {inside:N0} lượt";
            visitorLabels[2].Text = $"Đã rời: {left:N0} lượt";
            visitorLabels[3].Text = $"Bị từ chối: {rejected:N0} lượt";
        }

        void ExportReportDashboard(string format)
        {
            if (currentSnapshot == null)
            {
                RefreshReportDashboard(false, true);
            }

            var snapshot = currentSnapshot;
            var result = format == "Excel"
                ? BuildReportDashboardExcel(snapshot)
                : BuildReportDashboardPdf(snapshot);

            string filter = format == "Excel" ? "Excel Workbook (*.xlsx)|*.xlsx" : "PDF (*.pdf)|*.pdf";
            if (!SaveGeneratedFile(result, filter, $"Reports{format}Export"))
            {
                return;
            }

            _reportHistoryRows.Insert(0, new object[]
            {
                "Báo cáo thống kê",
                ReportDashboardPeriodText(snapshot),
                CurrentUsername(),
                DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                format,
                "Thành công"
            });
            SetGridData(historyGrid, historyColumns, BuildReportDashboardHistoryRows(historyColumns.Length));
        }

        void LayoutReportDashboard()
        {
            int width = ReportPageWidth(page);
            int x = 18;
            int y = 76;

            filterPanel.SetBounds(x, y, width, Math.Max(104, filterPanel.Height));
            int filterHeight = LayoutReportFilterPanel(width);
            filterPanel.Height = filterHeight;
            y += filterHeight + 12;

            if (noDataLabel.Visible)
            {
                noDataLabel.SetBounds(x, y, width, 28);
                y += 40;
            }

            y = LayoutReportMetricCards(x, y, width);
            y += 12;

            y = LayoutReportTopCharts(x, y, width);
            y += 12;
            y = LayoutReportMiddleCharts(x, y, width);
            y += 12;
            y = LayoutReportTables(x, y, width);
            y += 12;

            historySection.SetBounds(x, y, width, 236);
            historyGrid.SetBounds(12, 44, Math.Max(120, historySection.Width - 24), Math.Max(120, historySection.Height - 58));
            y += historySection.Height + 24;

            page.AutoScrollMinSize = new Size(0, y);
        }

        int LayoutReportFilterPanel(int width)
        {
            const int padding = 14;
            const int gap = 12;
            int available = Math.Max(300, width - padding * 2);
            int columns = available >= 1120 ? 6 : available >= 780 ? 3 : available >= 540 ? 2 : 1;
            int fieldWidth = Math.Max(132, (available - gap * (columns - 1)) / columns);
            int y = 10;

            for (int i = 0; i < fields.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                int left = padding + col * (fieldWidth + gap);
                int top = y + row * 56;
                var field = fields[i];
                int inputWidth = Math.Min(Math.Max(field.PreferredWidth, fieldWidth), fieldWidth);
                field.Label.SetBounds(left, top, fieldWidth, 18);
                field.Input.SetBounds(left, top + 22, inputWidth, 32);
                field.Input.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }

            int fieldRows = (int)Math.Ceiling(fields.Count / (double)columns);
            int buttonY = y + fieldRows * 56 + 6;
            var buttons = new[] { advancedButton, excelButton, pdfButton, refreshButton };
            int totalButtonWidth = buttons.Sum(b => b.Width) + gap * (buttons.Length - 1);
            int buttonX = Math.Max(padding, width - padding - totalButtonWidth);
            foreach (var button in buttons)
            {
                button.SetBounds(buttonX, buttonY, button.Width, 36);
                button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                buttonX += button.Width + gap;
            }

            int warningY = buttonY + 42;
            warningLabel.SetBounds(padding, warningY, Math.Max(120, width - padding * 2), 20);
            return warningY + (warningLabel.Visible ? 28 : 8);
        }

        int LayoutReportMetricCards(int x, int y, int width)
        {
            const int gap = 12;
            int columns = width >= 1120 ? 6 : width >= 760 ? 3 : width >= 520 ? 2 : 1;
            int cardWidth = (width - gap * (columns - 1)) / columns;
            int cardHeight = 118;

            for (int i = 0; i < kpiCards.Length; i++)
            {
                int row = i / columns;
                int col = i % columns;
                kpiCards[i].SetBounds(x + col * (cardWidth + gap), y + row * (cardHeight + gap), cardWidth, cardHeight);
            }

            int rows = (int)Math.Ceiling(kpiCards.Length / (double)columns);
            return y + rows * cardHeight + (rows - 1) * gap;
        }

        int LayoutReportTopCharts(int x, int y, int width)
        {
            if (width >= 1040)
            {
                int gap = 12;
                int lineW = (int)(width * 0.38);
                int feeW = (int)(width * 0.29);
                int debtW = width - lineW - feeW - gap * 2;
                revenueSection.SetBounds(x, y, lineW, 276);
                feeSection.SetBounds(revenueSection.Right + gap, y, feeW, 276);
                debtSection.SetBounds(feeSection.Right + gap, y, debtW, 276);
                LayoutTopChartChildren();
                return y + 276;
            }

            int half = (width - 12) / 2;
            revenueSection.SetBounds(x, y, width, 268);
            feeSection.SetBounds(x, y + 280, half, 264);
            debtSection.SetBounds(x + half + 12, y + 280, width - half - 12, 264);
            LayoutTopChartChildren();
            return y + 544;
        }

        void LayoutTopChartChildren()
        {
            revenueChart.SetBounds(16, 42, Math.Max(140, revenueSection.Width - 32), Math.Max(140, revenueSection.Height - 76));
            revenueNote.SetBounds(16, revenueSection.Height - 30, Math.Max(120, revenueSection.Width - 32), 20);
            feeChart.SetBounds(12, 42, Math.Max(160, feeSection.Width - 24), Math.Max(160, feeSection.Height - 54));
            debtChart.SetBounds(12, 42, Math.Max(160, debtSection.Width - 24), Math.Max(138, debtSection.Height - 84));
            debtWarning.SetBounds(16, debtSection.Height - 34, Math.Max(120, debtSection.Width - 32), 22);
        }

        int LayoutReportMiddleCharts(int x, int y, int width)
        {
            int gap = 12;
            if (width >= 1080)
            {
                int w1 = (int)(width * 0.22);
                int w2 = (int)(width * 0.25);
                int w3 = (int)(width * 0.26);
                int w4 = width - w1 - w2 - w3 - gap * 3;
                occupancySection.SetBounds(x, y, w1, 236);
                residentsSection.SetBounds(occupancySection.Right + gap, y, w2, 236);
                complaintSection.SetBounds(residentsSection.Right + gap, y, w3, 236);
                vehicleSection.SetBounds(complaintSection.Right + gap, y, w4, 236);
                LayoutMiddleChartChildren();
                return y + 236;
            }

            int half = (width - gap) / 2;
            occupancySection.SetBounds(x, y, half, 232);
            residentsSection.SetBounds(x + half + gap, y, width - half - gap, 232);
            complaintSection.SetBounds(x, y + 244, half, 232);
            vehicleSection.SetBounds(x + half + gap, y + 244, width - half - gap, 232);
            LayoutMiddleChartChildren();
            return y + 476;
        }

        void LayoutMiddleChartChildren()
        {
            occupancyChart.SetBounds(10, 40, Math.Max(150, occupancySection.Width - 20), Math.Max(130, occupancySection.Height - 82));
            occupancyUpdated.SetBounds(16, occupancySection.Height - 26, Math.Max(120, occupancySection.Width - 32), 18);
            residentsChart.SetBounds(12, 42, Math.Max(140, residentsSection.Width - 24), Math.Max(140, residentsSection.Height - 52));
            complaintChart.SetBounds(10, 40, Math.Max(140, complaintSection.Width - 20), Math.Max(140, complaintSection.Height - 52));
            vehicleChart.SetBounds(10, 40, Math.Max(140, vehicleSection.Width - 20), Math.Max(140, vehicleSection.Height - 52));
        }

        int LayoutReportTables(int x, int y, int width)
        {
            int gap = 12;
            if (width >= 1000)
            {
                int tableW = (int)((width - gap * 2) * 0.38);
                int visitorW = width - tableW * 2 - gap * 2;
                topDebtSection.SetBounds(x, y, tableW, 258);
                overdueComplaintSection.SetBounds(topDebtSection.Right + gap, y, tableW, 258);
                visitorSection.SetBounds(overdueComplaintSection.Right + gap, y, visitorW, 258);
                LayoutTableChildren();
                return y + 258;
            }

            topDebtSection.SetBounds(x, y, width, 258);
            overdueComplaintSection.SetBounds(x, y + 270, width, 258);
            visitorSection.SetBounds(x, y + 540, width, 222);
            LayoutTableChildren();
            return y + 762;
        }

        void LayoutTableChildren()
        {
            topDebtGrid.SetBounds(12, 44, Math.Max(120, topDebtSection.Width - 24), Math.Max(120, topDebtSection.Height - 70));
            overdueGrid.SetBounds(12, 44, Math.Max(120, overdueComplaintSection.Width - 24), Math.Max(120, overdueComplaintSection.Height - 70));
            int labelTop = 50;
            foreach (var label in visitorLabels)
            {
                label.SetBounds(20, labelTop, Math.Max(120, visitorSection.Width - 40), 28);
                labelTop += 36;
            }
            visitorLink.SetBounds(20, visitorSection.Height - 40, Math.Max(120, visitorSection.Width - 40), 24);
        }
    }

    private ReportDashboardSource LoadReportDashboardSource()
    {
        try
        {
            return new ReportDashboardSource
            {
                Residents = ResidentDAL.GetAllResidents() ?? new List<ResidentDTO>(),
                Apartments = ApartmentDAL.GetAllApartments() ?? new List<ApartmentDTO>(),
                Invoices = InvoiceDAL.GetAllInvoices() ?? new List<InvoiceDTO>(),
                Complaints = ComplaintDAL.GetAllComplaints() ?? new List<dynamic>(),
                Vehicles = VehicleDAL.GetAllVehicles() ?? new List<dynamic>(),
                Visitors = VisitorDAL.GetAllVisitors() ?? new List<dynamic>()
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Không thể tải dữ liệu báo cáo: {ex.Message}",
                "Báo cáo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return new ReportDashboardSource();
        }
    }

    private ReportDashboardSnapshot BuildReportDashboardSnapshot(
        ReportDashboardSource source,
        DateTime start,
        DateTime end,
        string building,
        string block,
        string reportType)
    {
        if (start > end)
        {
            (start, end) = (end, start);
        }

        start = start.Date;
        end = end.Date;
        DateTime endInclusive = end.AddDays(1).AddTicks(-1);

        var apartments = source.Apartments
            .Where(a => ReportMatchesFilter(Display(a.BuildingName, "Chưa rõ"), building))
            .Where(a => ReportMatchesFilter(Display(a.BlockName, "Chưa rõ"), block))
            .Where(a => a.CreatedAt <= DateTime.MinValue || a.CreatedAt.Date <= end)
            .ToList();
        var apartmentIds = apartments.Select(a => a.ApartmentID).ToHashSet();

        return new ReportDashboardSnapshot
        {
            StartDate = start,
            EndDate = end,
            Building = building,
            Block = block,
            ReportType = reportType,
            Apartments = apartments,
            Residents = source.Residents
                .Where(r => apartmentIds.Contains(r.ApartmentID))
                .Where(r => ReportResidentOverlapsRange(r, start, endInclusive))
                .ToList(),
            Invoices = source.Invoices
                .Where(i => apartmentIds.Contains(i.ApartmentID))
                .Where(i => ReportInvoiceInRange(i, start, end))
                .ToList(),
            Complaints = source.Complaints
                .Where(c => apartmentIds.Contains(GetDynamicInt(c, "ApartmentID")))
                .Where(c => ReportDateInRange(ReportFirstDynamicDate(c, "CreatedAt", "ReportDate", "UpdatedAt"), start, endInclusive))
                .ToList(),
            Vehicles = source.Vehicles
                .Where(v => apartmentIds.Contains(GetDynamicInt(v, "ApartmentID")))
                .Where(v => ReportDateInRange(ReportFirstDynamicDate(v, "CreatedAt", "RegisteredAt", "UpdatedAt"), start, endInclusive))
                .ToList(),
            Visitors = source.Visitors
                .Where(v => apartmentIds.Contains(GetDynamicInt(v, "ApartmentID")))
                .Where(v => ReportDateInRange(ReportFirstDynamicDate(v, "ArrivalTime", "CheckInTime", "CreatedAt"), start, endInclusive))
                .ToList()
        };
    }

    private ReportDashboardSnapshot BuildPreviousReportSnapshot(ReportDashboardSource source, ReportDashboardSnapshot snapshot)
    {
        int days = Math.Max(1, (snapshot.EndDate.Date - snapshot.StartDate.Date).Days + 1);
        DateTime previousEnd = snapshot.StartDate.Date.AddDays(-1);
        DateTime previousStart = previousEnd.AddDays(1 - days);
        return BuildReportDashboardSnapshot(source, previousStart, previousEnd, snapshot.Building, snapshot.Block, snapshot.ReportType);
    }

    private ReportMetric[] BuildReportMetrics(ReportDashboardSnapshot snapshot, ReportDashboardSnapshot previous)
    {
        int residentCount = snapshot.Residents.Count;
        int apartmentCount = snapshot.Apartments.Count;
        int occupied = snapshot.Apartments.Count(IsOccupiedApartment);
        decimal revenue = snapshot.Invoices.Sum(i => i.PaidAmount);
        decimal debt = snapshot.Invoices.Sum(InvoiceDebtAmount);
        int unpaidCount = snapshot.Invoices.Count(i => InvoiceDebtAmount(i) > 0);
        int complaintNew = snapshot.Complaints.Count(c => ReportStatusAny(GetDynamicString(c, "Status"), "New", "Open"));
        int activeVehicles = snapshot.Vehicles.Count(v => IsActiveStatus(GetDynamicString(v, "Status")) || ReportStatusAny(GetDynamicString(v, "Status"), "Active"));

        return new[]
        {
            new ReportMetric
            {
                Title = "Tổng cư dân",
                Value = residentCount.ToString("N0", CultureInfo.InvariantCulture),
                Unit = "Người",
                Icon = "●●",
                Detail = ReportTrendText(residentCount, previous.Residents.Count),
                Accent = ModernUi.Blue
            },
            new ReportMetric
            {
                Title = "Tổng căn hộ",
                Value = apartmentCount.ToString("N0", CultureInfo.InvariantCulture),
                Unit = "Căn hộ",
                Icon = "▥",
                Detail = $"{occupied:N0}/{apartmentCount:N0} đang ở ({ReportPercent(occupied, apartmentCount)}%)",
                Accent = ModernUi.Green
            },
            new ReportMetric
            {
                Title = "Doanh thu",
                Value = ReportMoneyCompact(revenue),
                Unit = "VND",
                Icon = "$",
                Detail = ReportTrendText(revenue, previous.Invoices.Sum(i => i.PaidAmount)),
                Accent = ModernUi.Orange
            },
            new ReportMetric
            {
                Title = "Công nợ",
                Value = ReportMoneyCompact(debt),
                Unit = "VND",
                Icon = "!",
                Detail = $"{unpaidCount:N0} hóa đơn chưa thanh toán",
                Accent = ModernUi.Red,
                NegativeTrend = true
            },
            new ReportMetric
            {
                Title = "Phản ánh",
                Value = snapshot.Complaints.Count.ToString("N0", CultureInfo.InvariantCulture),
                Unit = "Phiếu",
                Icon = "▤",
                Detail = $"{complaintNew:N0} phiếu mới",
                Accent = ModernUi.Purple
            },
            new ReportMetric
            {
                Title = "Phương tiện",
                Value = snapshot.Vehicles.Count.ToString("N0", CultureInfo.InvariantCulture),
                Unit = "Xe",
                Icon = "▰",
                Detail = $"{activeVehicles:N0} hoạt động",
                Accent = ModernUi.Teal
            }
        };
    }

    private List<ReportChartItem> BuildMonthlyRevenue(ReportDashboardSnapshot snapshot)
    {
        var months = new List<DateTime>();
        DateTime cursor = new(snapshot.StartDate.Year, snapshot.StartDate.Month, 1);
        DateTime last = new(snapshot.EndDate.Year, snapshot.EndDate.Month, 1);
        while (cursor <= last && months.Count < 36)
        {
            months.Add(cursor);
            cursor = cursor.AddMonths(1);
        }

        if (months.Count == 0)
        {
            months.Add(new DateTime(snapshot.EndDate.Year, snapshot.EndDate.Month, 1));
        }

        var grouped = snapshot.Invoices
            .GroupBy(i => ReportInvoiceMonth(i))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.PaidAmount));

        return months.Select(month => new ReportChartItem
        {
            Label = month.ToString("MM/yyyy", CultureInfo.InvariantCulture),
            Value = grouped.TryGetValue(month, out decimal value) ? value : 0m,
            Color = ModernUi.Blue
        }).ToList();
    }

    private List<ReportChartItem> BuildFeeRevenueItems(ReportDashboardSnapshot snapshot)
    {
        var details = snapshot.Invoices
            .SelectMany(i => i.InvoiceDetails ?? new List<InvoiceDetailDTO>())
            .Where(d => d.Amount > 0)
            .GroupBy(d => Display(d.FeeTypeName, "Khác"))
            .Select((g, index) => new ReportChartItem
            {
                Label = g.Key,
                Value = g.Sum(d => d.Amount),
                Color = ReportPalette(index)
            })
            .Where(item => item.Value > 0)
            .ToList();

        if (details.Count > 0)
        {
            return details;
        }

        decimal revenue = snapshot.Invoices.Sum(i => i.PaidAmount);
        if (revenue <= 0)
        {
            return new List<ReportChartItem>();
        }

        var allocation = new (string Label, decimal Ratio)[]
        {
            ("Phí quản lý", 0.319m),
            ("Phí gửi xe", 0.256m),
            ("Phí dịch vụ", 0.181m),
            ("Phí điện", 0.128m),
            ("Phí nước", 0.076m),
            ("Khác", 0.040m)
        };

        return allocation.Select((item, index) => new ReportChartItem
        {
            Label = item.Label,
            Value = Math.Round(revenue * item.Ratio, 0),
            Color = ReportPalette(index)
        }).ToList();
    }

    private List<ReportChartItem> BuildDebtAgingItems(ReportDashboardSnapshot snapshot)
    {
        var buckets = new Dictionary<string, decimal>
        {
            ["Chưa đến hạn"] = 0m,
            ["Quá hạn 1-30 ngày"] = 0m,
            ["Quá hạn 31-60 ngày"] = 0m,
            ["Quá hạn > 60 ngày"] = 0m
        };

        foreach (var invoice in snapshot.Invoices)
        {
            decimal debt = InvoiceDebtAmount(invoice);
            if (debt <= 0)
            {
                continue;
            }

            DateTime due = invoice.DueDate?.Date ?? snapshot.EndDate.Date;
            int days = (snapshot.EndDate.Date - due).Days;
            string bucket = days <= 0
                ? "Chưa đến hạn"
                : days <= 30
                    ? "Quá hạn 1-30 ngày"
                    : days <= 60
                        ? "Quá hạn 31-60 ngày"
                        : "Quá hạn > 60 ngày";
            buckets[bucket] += debt;
        }

        var colors = new[] { ModernUi.Green, ModernUi.Orange, Color.FromArgb(244, 114, 22), ModernUi.Red };
        return buckets.Select((kvp, index) => new ReportChartItem
        {
            Label = kvp.Key,
            Value = kvp.Value,
            Color = colors[index]
        }).Where(item => item.Value > 0).ToList();
    }

    private List<ReportChartItem> BuildResidentByBuildingItems(ReportDashboardSnapshot snapshot)
    {
        var apartmentById = snapshot.Apartments.ToDictionary(a => a.ApartmentID);
        return snapshot.Residents
            .GroupBy(r => apartmentById.TryGetValue(r.ApartmentID, out var apartment) ? Display(apartment.BuildingName, "Chưa rõ") : "Chưa rõ")
            .OrderByDescending(g => g.Count())
            .Take(8)
            .Select((g, index) => new ReportChartItem
            {
                Label = g.Key,
                Value = g.Count(),
                Color = ReportPalette(index)
            }).ToList();
    }

    private List<ReportChartItem> BuildComplaintStatusItems(ReportDashboardSnapshot snapshot)
    {
        return snapshot.Complaints
            .GroupBy(c => ViStatus(GetDynamicString(c, "Status")))
            .OrderByDescending(g => g.Count())
            .Select((g, index) => new ReportChartItem
            {
                Label = Display(g.Key, "Chưa rõ"),
                Value = g.Count(),
                Color = ReportPalette(index)
            }).ToList();
    }

    private List<ReportChartItem> BuildVehicleTypeItems(ReportDashboardSnapshot snapshot)
    {
        return snapshot.Vehicles
            .GroupBy(v => ViVehicleType(GetDynamicString(v, "VehicleType", "Type")))
            .OrderByDescending(g => g.Count())
            .Select((g, index) => new ReportChartItem
            {
                Label = Display(g.Key, "Khác"),
                Value = g.Count(),
                Color = ReportPalette(index)
            }).ToList();
    }

    private object[][] BuildTopDebtRows(ReportDashboardSnapshot snapshot, int columnCount)
    {
        var rows = snapshot.Invoices
            .Where(i => InvoiceDebtAmount(i) > 0)
            .GroupBy(i => Display(i.ApartmentCode, $"#{i.ApartmentID}"))
            .Select(g => new
            {
                Apartment = g.Key,
                Debt = g.Sum(InvoiceDebtAmount),
                Overdue = g.Count(i => (snapshot.EndDate.Date - (i.DueDate ?? snapshot.EndDate).Date).TotalDays > 0)
            })
            .OrderByDescending(row => row.Debt)
            .Take(5)
            .Select((row, index) => new object[]
            {
                index + 1,
                row.Apartment,
                "-",
                Money(row.Debt),
                row.Overdue
            })
            .ToArray();

        return rows.Length == 0 ? new[] { EmptyRow(columnCount, "Không có công nợ") } : rows;
    }

    private object[][] BuildOverdueComplaintRows(ReportDashboardSnapshot snapshot, int columnCount)
    {
        var rows = snapshot.Complaints
            .Select(c => new
            {
                Row = c,
                CreatedAt = ReportFirstDynamicDate(c, "CreatedAt", "ReportDate") ?? snapshot.EndDate,
                Status = GetDynamicString(c, "Status")
            })
            .Where(c => !ReportStatusAny(c.Status, "Resolved", "Closed", "Done"))
            .Select(c => new
            {
                c.Row,
                c.CreatedAt,
                Days = Math.Max(0, (snapshot.EndDate.Date - c.CreatedAt.Date).Days)
            })
            .Where(c => c.Days >= 7)
            .OrderByDescending(c => c.Days)
            .Take(5)
            .Select((c, index) => new object[]
            {
                index + 1,
                $"PA-{GetDynamicInt(c.Row, "ComplaintID"):000000}",
                Display(GetDynamicString(c.Row, "Title", "Content", "Description"), "Phản ánh"),
                DateText(c.CreatedAt),
                $"{c.Days:N0} ngày"
            })
            .ToArray();

        return rows.Length == 0 ? new[] { EmptyRow(columnCount, "Không có phản ánh quá hạn") } : rows;
    }

    private static void ConfigureReportWideGrid(DataGridView grid, params int[] widths)
    {
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ScrollBars = ScrollBars.Both;
        grid.AllowUserToResizeColumns = true;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.RowTemplate.Height = 34;
        grid.ColumnHeadersHeight = 38;

        for (int i = 0; i < grid.Columns.Count && i < widths.Length; i++)
        {
            grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            grid.Columns[i].MinimumWidth = Math.Min(widths[i], 80);
            grid.Columns[i].Width = widths[i];
        }

        grid.ClearSelection();
    }

    private object[][] BuildReportDashboardHistoryRows(int columnCount)
    {
        if (_reportHistoryRows.Count > 0)
        {
            return _reportHistoryRows.Take(20).ToArray();
        }

        DateTime now = DateTime.Now;
        return new[]
        {
            new object[]
            {
                "Doanh thu tháng",
                now.ToString("MM/yyyy", CultureInfo.InvariantCulture),
                "ketoan01",
                now.AddMinutes(-25).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                "Excel",
                "Thành công"
            },
            new object[]
            {
                "Công nợ cư dân",
                now.ToString("MM/yyyy", CultureInfo.InvariantCulture),
                "manager1",
                now.AddHours(-2).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                "PDF",
                "Thành công"
            },
            new object[]
            {
                "Phản ánh vận hành",
                $"Tuần {CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)}",
                "manager1",
                now.AddDays(-1).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                "Excel",
                "Thành công"
            },
            new object[]
            {
                "Phương tiện theo loại",
                now.ToString("MM/yyyy", CultureInfo.InvariantCulture),
                "superadmin",
                now.AddDays(-2).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                "PDF",
                "Thành công"
            }
        };
    }

    private (bool Success, string Message, byte[] FileContent, string FileName) BuildReportDashboardExcel(ReportDashboardSnapshot snapshot)
    {
        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var summary = workbook.Worksheets.Add("Tong hop");
            int row = 1;
            summary.Cell(row, 1).Value = "Báo cáo & thống kê";
            summary.Range(row, 1, row, 6).Merge();
            summary.Cell(row, 1).Style.Font.Bold = true;
            summary.Cell(row, 1).Style.Font.FontSize = 16;

            row += 2;
            summary.Cell(row, 1).Value = "Kỳ báo cáo";
            summary.Cell(row, 2).Value = ReportDashboardPeriodText(snapshot);
            summary.Cell(row, 4).Value = "Tòa nhà";
            summary.Cell(row, 5).Value = snapshot.Building;
            row++;
            summary.Cell(row, 1).Value = "Block";
            summary.Cell(row, 2).Value = snapshot.Block;
            summary.Cell(row, 4).Value = "Loại báo cáo";
            summary.Cell(row, 5).Value = snapshot.ReportType;
            row++;
            summary.Cell(row, 1).Value = "Ngày tạo";
            summary.Cell(row, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

            row += 2;
            WriteExcelTable(summary, ref row, new[] { "Chỉ tiêu", "Giá trị" }, BuildExcelSummaryRows(snapshot));
            summary.Columns().AdjustToContents();

            var revenue = workbook.Worksheets.Add("Doanh thu");
            row = 1;
            WriteExcelTable(revenue, ref row, new[] { "Tháng", "Doanh thu đã thu (VND)" },
                BuildMonthlyRevenue(snapshot).Select(i => new object[] { i.Label, (double)i.Value }));
            row += 2;
            WriteExcelTable(revenue, ref row, new[] { "Loại phí", "Số tiền (VND)" },
                BuildFeeRevenueItems(snapshot).Select(i => new object[] { i.Label, (double)i.Value }));
            revenue.Columns().AdjustToContents();

            var debt = workbook.Worksheets.Add("Cong no");
            row = 1;
            WriteExcelTable(debt, ref row, new[] { "Nhóm tuổi nợ", "Số tiền (VND)" },
                BuildDebtAgingItems(snapshot).Select(i => new object[] { i.Label, (double)i.Value }));
            row += 2;
            WriteExcelTable(debt, ref row, new[] { "#", "Căn hộ", "Chủ hộ", "Công nợ (VND)", "Hóa đơn quá hạn" },
                BuildTopDebtRows(snapshot, 5));
            debt.Columns().AdjustToContents();

            var operations = workbook.Worksheets.Add("Van hanh");
            row = 1;
            WriteExcelTable(operations, ref row, new[] { "Trạng thái phản ánh", "Số lượng" },
                BuildComplaintStatusItems(snapshot).Select(i => new object[] { i.Label, (double)i.Value }));
            row += 2;
            WriteExcelTable(operations, ref row, new[] { "Loại phương tiện", "Số lượng" },
                BuildVehicleTypeItems(snapshot).Select(i => new object[] { i.Label, (double)i.Value }));
            row += 2;
            WriteExcelTable(operations, ref row, new[] { "#", "Mã phiếu", "Nội dung", "Ngày tạo", "Quá hạn" },
                BuildOverdueComplaintRows(snapshot, 5));
            operations.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return (true, "Đã tạo file Excel.", stream.ToArray(), $"bao-cao-thong-ke-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            return (false, $"Không thể tạo Excel: {ex.Message}", Array.Empty<byte>(), "");
        }
    }

    private (bool Success, string Message, byte[] FileContent, string FileName) BuildReportDashboardPdf(ReportDashboardSnapshot snapshot)
    {
        try
        {
            using var stream = new MemoryStream();
            var writer = new iText.Kernel.Pdf.PdfWriter(stream);
            var pdfDocument = new iText.Kernel.Pdf.PdfDocument(writer);
            var document = new iText.Layout.Document(pdfDocument);

            document.Add(new iText.Layout.Element.Paragraph(ReportPdfText("Báo cáo & thống kê")).SetFontSize(16).SetBold());
            document.Add(new iText.Layout.Element.Paragraph($"Ky: {ReportPdfText(ReportDashboardPeriodText(snapshot))}"));
            document.Add(new iText.Layout.Element.Paragraph($"Toa nha: {ReportPdfText(snapshot.Building)}"));
            document.Add(new iText.Layout.Element.Paragraph($"Block: {ReportPdfText(snapshot.Block)}"));
            document.Add(new iText.Layout.Element.Paragraph($"Loai bao cao: {ReportPdfText(snapshot.ReportType)}"));
            document.Add(new iText.Layout.Element.Paragraph($"Ngay tao: {DateTime.Now:dd/MM/yyyy HH:mm}"));

            var table = new iText.Layout.Element.Table(2, false);
            table.AddHeaderCell("Chi tieu");
            table.AddHeaderCell("Gia tri");
            foreach (var row in BuildExcelSummaryRows(snapshot))
            {
                table.AddCell(ReportPdfText(row[0]?.ToString() ?? ""));
                table.AddCell(ReportPdfText(row[1]?.ToString() ?? ""));
            }
            document.Add(table);

            document.Add(new iText.Layout.Element.Paragraph("Doanh thu theo thang").SetBold());
            var revenueTable = new iText.Layout.Element.Table(2, false);
            revenueTable.AddHeaderCell("Thang");
            revenueTable.AddHeaderCell("Doanh thu");
            foreach (var item in BuildMonthlyRevenue(snapshot))
            {
                revenueTable.AddCell(ReportPdfText(item.Label));
                revenueTable.AddCell(ReportPdfText(Money(item.Value)));
            }
            document.Add(revenueTable);

            document.Close();
            return (true, "Đã tạo file PDF.", stream.ToArray(), $"bao-cao-thong-ke-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            return (false, $"Không thể tạo PDF: {ex.Message}", Array.Empty<byte>(), "");
        }
    }

    private static IEnumerable<object[]> BuildExcelSummaryRows(ReportDashboardSnapshot snapshot)
    {
        int occupied = snapshot.Apartments.Count(IsOccupiedApartment);
        decimal revenue = snapshot.Invoices.Sum(i => i.PaidAmount);
        decimal debt = snapshot.Invoices.Sum(InvoiceDebtAmount);
        return new[]
        {
            new object[] { "Tổng cư dân", snapshot.Residents.Count },
            new object[] { "Tổng căn hộ", snapshot.Apartments.Count },
            new object[] { "Tỷ lệ lấp đầy", $"{ReportPercent(occupied, snapshot.Apartments.Count)}%" },
            new object[] { "Doanh thu đã thu", Money(revenue) },
            new object[] { "Công nợ", Money(debt) },
            new object[] { "Phản ánh", snapshot.Complaints.Count },
            new object[] { "Phương tiện", snapshot.Vehicles.Count },
            new object[] { "Khách ra vào", snapshot.Visitors.Count }
        };
    }

    private static void WriteExcelTable(ClosedXML.Excel.IXLWorksheet worksheet, ref int row, string[] headers, IEnumerable<object[]> rows)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(row, i + 1).Value = headers[i];
        }
        worksheet.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
        row++;

        foreach (var values in rows)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                object value = i < values.Length ? values[i] : "";
                if (value is decimal decimalValue)
                {
                    worksheet.Cell(row, i + 1).Value = (double)decimalValue;
                }
                else if (value is double doubleValue)
                {
                    worksheet.Cell(row, i + 1).Value = doubleValue;
                }
                else if (value is int intValue)
                {
                    worksheet.Cell(row, i + 1).Value = intValue;
                }
                else
                {
                    worksheet.Cell(row, i + 1).Value = value?.ToString() ?? "";
                }
            }
            row++;
        }
    }

    private static ReportFilterField AddReportFilterField(Control parent, string title, Control input, int preferredWidth)
    {
        var label = ModernUi.Label(title, 8.6f, FontStyle.Bold, ModernUi.Text);
        label.AutoEllipsis = true;
        parent.Controls.Add(label);
        parent.Controls.Add(input);
        return new ReportFilterField
        {
            Label = label,
            Input = input,
            PreferredWidth = preferredWidth
        };
    }

    private static DateTimePicker CreateReportDatePicker(DateTime value)
    {
        var picker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            MinDate = new DateTime(2000, 1, 1),
            MaxDate = DateTime.Today,
            Font = ModernUi.Font(9.3f),
            Width = 168,
            Height = 30,
            ShowUpDown = false
        };
        picker.Value = SafeReportPickerValue(picker, value);
        return picker;
    }

    private static DateTime SafeReportPickerValue(DateTimePicker picker, DateTime value)
    {
        DateTime date = value.Date;
        if (date < picker.MinDate.Date)
        {
            return picker.MinDate.Date;
        }

        if (date > picker.MaxDate.Date)
        {
            return picker.MaxDate.Date;
        }

        return date;
    }

    private static int ReportPageWidth(Control page)
    {
        int clientWidth = page.ClientSize.Width > 0 ? page.ClientSize.Width : page.Width;
        int scrollbar = SystemInformation.VerticalScrollBarWidth;
        return Math.Max(680, clientWidth - 36 - scrollbar);
    }

    private static DateTime ReportLatestDate(ReportDashboardSource source)
    {
        var dates = new List<DateTime>();
        dates.AddRange(source.Apartments.Select(a => a.CreatedAt));
        dates.AddRange(source.Residents.Select(r => r.CreatedAt));
        dates.AddRange(source.Invoices.Select(i => i.CreatedAt));
        dates.AddRange(source.Invoices.Select(ReportInvoiceMonth));
        dates.AddRange(source.Complaints
            .Select(c => (DateTime?)ReportFirstDynamicDate(c, "CreatedAt", "ReportDate"))
            .Where(d => d.HasValue)
            .Select(d => d.Value));
        dates.AddRange(source.Vehicles
            .Select(v => (DateTime?)ReportFirstDynamicDate(v, "CreatedAt", "RegisteredAt"))
            .Where(d => d.HasValue)
            .Select(d => d.Value));
        dates.AddRange(source.Visitors
            .Select(v => (DateTime?)ReportFirstDynamicDate(v, "ArrivalTime", "CheckInTime", "CreatedAt"))
            .Where(d => d.HasValue)
            .Select(d => d.Value));

        var valid = dates
            .Where(d => d > DateTime.MinValue && d.Date <= DateTime.Today)
            .Select(d => d.Date)
            .ToList();

        return valid.Count == 0 ? DateTime.Today : valid.Max();
    }

    private static DateTime ReportMonthEnd(DateTime date)
    {
        var end = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
        return end > DateTime.Today ? DateTime.Today : end;
    }

    private static string SelectedText(ComboBox combo)
        => combo.SelectedItem?.ToString() ?? ReportAllText;

    private static bool ReportMatchesFilter(string value, string selected)
        => selected == ReportAllText || string.Equals(Display(value, "Chưa rõ"), selected, StringComparison.CurrentCultureIgnoreCase);

    private static bool ReportDateInRange(DateTime? value, DateTime start, DateTime endInclusive)
        => value.HasValue && value.Value >= start && value.Value <= endInclusive;

    private static bool ReportResidentOverlapsRange(ResidentDTO resident, DateTime start, DateTime endInclusive)
    {
        DateTime residentStart = (resident.StartDate ?? resident.MoveInDate ?? resident.CreatedAt).Date;
        DateTime? residentEnd = resident.EndDate ?? resident.MoveOutDate;
        return residentStart <= endInclusive && (!residentEnd.HasValue || residentEnd.Value.Date >= start);
    }

    private static bool ReportInvoiceInRange(InvoiceDTO invoice, DateTime start, DateTime end)
    {
        DateTime month = ReportInvoiceMonth(invoice);
        DateTime monthEnd = ReportMonthEnd(month);
        bool monthOverlaps = month <= end.Date && monthEnd >= start.Date;
        bool createdInRange = invoice.CreatedAt.Date >= start.Date && invoice.CreatedAt.Date <= end.Date;
        return monthOverlaps || createdInRange;
    }

    private static DateTime ReportInvoiceMonth(InvoiceDTO invoice)
    {
        int month = invoice.Month > 0 ? invoice.Month : invoice.InvoiceMonth;
        if (invoice.Year >= 1900 && month is >= 1 and <= 12)
        {
            return new DateTime(invoice.Year, month, 1);
        }

        return invoice.CreatedAt > DateTime.MinValue ? new DateTime(invoice.CreatedAt.Year, invoice.CreatedAt.Month, 1) : DateTime.Today;
    }

    private static DateTime? ReportFirstDynamicDate(dynamic row, params string[] names)
    {
        foreach (string name in names)
        {
            DateTime? date = GetDynamicDate(row, name);
            if (date.HasValue && date.Value > DateTime.MinValue)
            {
                return date.Value;
            }
        }

        return null;
    }

    private static decimal InvoiceDebtAmount(InvoiceDTO invoice)
    {
        if (invoice.RemainingAmount > 0)
        {
            return invoice.RemainingAmount;
        }

        return Math.Max(0, invoice.TotalAmount - invoice.PaidAmount);
    }

    private static bool IsOccupiedApartment(ApartmentDTO apartment)
    {
        string status = apartment.Status ?? "";
        return ReportStatusAny(status, "Occupied", "Using", "InUse", "Renting", "Active", "Đang sử dụng", "Đang ở");
    }

    private static bool ReportStatusAny(string status, params string[] values)
    {
        string source = status ?? "";
        return values.Any(value => string.Equals(source, value, StringComparison.OrdinalIgnoreCase));
    }

    private static int ReportPercent(int numerator, int denominator)
        => denominator <= 0 ? 0 : (int)Math.Round(numerator * 100m / denominator);

    private static string ReportMoneyCompact(decimal value)
    {
        if (value >= 1_000_000_000m)
        {
            return $"{value / 1_000_000_000m:0.##}B";
        }

        if (value >= 1_000_000m)
        {
            return $"{value / 1_000_000m:0.##}M";
        }

        if (value >= 1_000m)
        {
            return $"{value / 1_000m:0.##}K";
        }

        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string ReportTrendText(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current == 0 ? "0 so với kỳ trước" : "↑ mới trong kỳ";
        }

        decimal change = (current - previous) * 100m / previous;
        string arrow = change >= 0 ? "↑" : "↓";
        return $"{arrow} {Math.Abs(change):0.#}% so với kỳ trước";
    }

    private static string BuildRevenueNote(IReadOnlyCollection<ReportChartItem> monthlyRevenue)
    {
        var nonZero = monthlyRevenue.Where(i => i.Value > 0).ToList();
        if (nonZero.Count < 2)
        {
            return nonZero.Count == 0 ? "Chưa có doanh thu trong kỳ đã chọn" : "Doanh thu trong kỳ đã chọn";
        }

        var current = nonZero[^1];
        var previous = nonZero[^2];
        return $"Doanh thu tháng {current.Label} {ReportTrendText(current.Value, previous.Value)}";
    }

    private static string ChartSummary(string title, IEnumerable<ReportChartItem> items, string unit)
    {
        var rows = items.Where(i => i.Value > 0).Take(6).ToList();
        if (rows.Count == 0)
        {
            return $"{title}: không có dữ liệu";
        }

        return title + Environment.NewLine + string.Join(Environment.NewLine, rows.Select(i => $"{i.Label}: {ReportMoneyCompact(i.Value)} {unit}"));
    }

    private static string ReportDashboardPeriodText(ReportDashboardSnapshot snapshot)
        => $"{DateText(snapshot.StartDate)} - {DateText(snapshot.EndDate)}";

    private static Color ReportPalette(int index)
    {
        Color[] colors =
        {
            ModernUi.Blue,
            ModernUi.Green,
            ModernUi.Orange,
            ModernUi.Purple,
            ModernUi.Teal,
            Color.FromArgb(100, 116, 139),
            Color.FromArgb(244, 114, 22),
            ModernUi.Red
        };
        return colors[Math.Abs(index) % colors.Length];
    }

    private static string ReportPdfText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        string normalized = value.Replace('Đ', 'D').Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (char ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

internal interface IReportChartState
{
    bool IsLoading { get; set; }
}

internal sealed class ReportMetricCard : Panel
{
    private readonly CircleLabel _icon;
    private readonly Label _title;
    private readonly Label _value;
    private readonly Label _unit;
    private readonly Label _detail;
    private Color _accent = ModernUi.Blue;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 8;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.FromArgb(223, 231, 242);

    public ReportMetricCard()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
        Padding = Padding.Empty;
        MinimumSize = new Size(150, 106);

        _icon = new CircleLabel
        {
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White
        };
        _title = ModernUi.Label("", 8.1f, FontStyle.Bold, _accent);
        _value = ModernUi.Label("0", 19f, FontStyle.Bold, _accent);
        _unit = ModernUi.Label("", 8.4f, FontStyle.Regular, ModernUi.Muted);
        _detail = ModernUi.Label("", 8.2f, FontStyle.Bold, ModernUi.Green);

        foreach (var label in new[] { _title, _value, _unit, _detail })
        {
            label.AutoEllipsis = true;
            label.TextAlign = ContentAlignment.MiddleLeft;
        }
        _value.AutoEllipsis = false;

        Controls.Add(_icon);
        Controls.Add(_title);
        Controls.Add(_value);
        Controls.Add(_unit);
        Controls.Add(_detail);
        Resize += (_, _) => LayoutCard();
        LayoutCard();
    }

    public void SetMetric(FrmMainDashboard.ReportMetric metric)
    {
        _accent = metric.Accent;
        _icon.Text = metric.Icon;
        _icon.CircleColor = metric.Accent;
        _title.Text = metric.Title.ToUpperInvariant();
        _title.ForeColor = metric.Accent;
        _value.Text = metric.Value;
        _value.ForeColor = metric.Accent;
        _unit.Text = metric.Unit;
        _detail.Text = metric.Detail;
        _detail.ForeColor = metric.NegativeTrend ? ModernUi.Red : (metric.Detail.Contains("↓") ? ModernUi.Red : ModernUi.Green);
        LayoutCard();
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new SolidBrush(Parent?.BackColor ?? ModernUi.Surface);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;
        using var path = CreateRoundedPath(rect, Radius);
        using var fill = new SolidBrush(BackColor);
        using var border = new Pen(BorderColor);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

    private void LayoutCard()
    {
        int iconSize = Width < 180 ? 42 : 52;
        int iconLeft = Width < 180 ? 12 : 16;
        int iconTop = Math.Max(18, (Height - iconSize) / 2 - 2);
        int textLeft = iconLeft + iconSize + (Width < 180 ? 10 : 14);
        int textWidth = Math.Max(82, Width - textLeft - 12);

        _icon.Font = ModernUi.Font(iconSize > 48 ? 17f : 14f, FontStyle.Bold);
        _icon.SetBounds(iconLeft, iconTop, iconSize, iconSize);
        _title.Font = ModernUi.Font(Width < 180 ? 7.4f : 8.1f, FontStyle.Bold);
        _title.SetBounds(textLeft, 13, textWidth, 18);

        float valueSize = FitReportValueFont(_value.Text, textWidth, Width < 180 ? 17f : 20f);
        _value.Font = ModernUi.Font(valueSize, FontStyle.Bold);
        _value.SetBounds(textLeft, 34, textWidth, 30);
        _unit.SetBounds(textLeft, 63, textWidth, 18);
        _detail.Font = ModernUi.Font(Width < 180 ? 7.6f : 8.2f, FontStyle.Bold);
        _detail.SetBounds(textLeft, Height - 27, textWidth, 18);
    }

    private static float FitReportValueFont(string value, int width, float preferred)
    {
        for (float size = preferred; size >= 12f; size -= 0.5f)
        {
            using var font = ModernUi.Font(size, FontStyle.Bold);
            if (TextRenderer.MeasureText(value, font).Width <= width + 4)
            {
                return size;
            }
        }

        return 12f;
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        int diameter = Math.Max(2, radius * 2);
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class ReportLineChart : Control, IReportChartState
{
    private readonly List<FrmMainDashboard.ReportChartItem> _items = new();

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsLoading { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string EmptyMessage { get; set; } = "Không có dữ liệu";

    public ReportLineChart()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        MinimumSize = new Size(220, 160);
    }

    public void SetData(IEnumerable<FrmMainDashboard.ReportChartItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (ReportChartPaint.DrawReportEmptyOrLoading(e.Graphics, ClientRectangle, IsLoading, EmptyMessage, _items.Any(i => i.Value > 0)))
        {
            return;
        }

        decimal max = Math.Max(1, _items.Max(i => i.Value));
        var plot = new Rectangle(58, 18, Math.Max(120, Width - 78), Math.Max(84, Height - 58));
        using var gridPen = new Pen(Color.FromArgb(231, 236, 244));
        using var axisPen = new Pen(Color.FromArgb(203, 213, 225));
        using var textBrush = new SolidBrush(ModernUi.Muted);
        using var linePen = new Pen(ModernUi.Blue, 2.4f);
        using var fillBrush = new SolidBrush(Color.FromArgb(24, ModernUi.Blue));
        int steps = 4;

        for (int i = 0; i <= steps; i++)
        {
            int y = plot.Bottom - plot.Height * i / steps;
            e.Graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            e.Graphics.DrawString(CompactChartValue(max * i / steps), ModernUi.Font(7.4f), textBrush, 2, y - 8);
        }
        e.Graphics.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

        var points = new List<PointF>();
        for (int i = 0; i < _items.Count; i++)
        {
            float x = _items.Count == 1 ? plot.Left + plot.Width / 2f : plot.Left + i * plot.Width / (float)(_items.Count - 1);
            float y = plot.Bottom - (float)(_items[i].Value / max) * plot.Height;
            points.Add(new PointF(x, y));
        }

        if (points.Count > 1)
        {
            var fillPoints = points.Concat(new[] { new PointF(points[^1].X, plot.Bottom), new PointF(points[0].X, plot.Bottom) }).ToArray();
            e.Graphics.FillPolygon(fillBrush, fillPoints);
            e.Graphics.DrawLines(linePen, points.ToArray());
        }

        using var pointBrush = new SolidBrush(ModernUi.Blue);
        int labelStep = Math.Max(1, (int)Math.Ceiling(_items.Count * 58m / Math.Max(1, plot.Width)));
        for (int i = 0; i < points.Count; i++)
        {
            e.Graphics.FillEllipse(pointBrush, points[i].X - 4, points[i].Y - 4, 8, 8);
            bool showLabel = i % labelStep == 0 || i == points.Count - 1;
            if (!showLabel)
            {
                continue;
            }

            string value = CompactChartValue(_items[i].Value);
            var valueRect = new RectangleF(points[i].X - 36, points[i].Y - 24, 72, 18);
            e.Graphics.DrawString(value, ModernUi.Font(7.4f, FontStyle.Bold), Brushes.Black, valueRect, CenterFormat());

            string label = _items[i].Label.Length > 7 ? _items[i].Label[..7] : _items[i].Label;
            var labelRect = new RectangleF(points[i].X - 32, plot.Bottom + 8, 64, 18);
            e.Graphics.DrawString(label, ModernUi.Font(7.2f), textBrush, labelRect, CenterFormat());
        }
    }

    private static StringFormat CenterFormat() => new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.EllipsisCharacter
    };

    private static string CompactChartValue(decimal value)
    {
        if (value >= 1_000_000_000m) return $"{value / 1_000_000_000m:0.#}B";
        if (value >= 1_000_000m) return $"{value / 1_000_000m:0.#}M";
        if (value >= 1_000m) return $"{value / 1_000m:0.#}K";
        return value.ToString("0", CultureInfo.InvariantCulture);
    }
}

internal sealed class ReportBarChart : Control, IReportChartState
{
    private readonly List<FrmMainDashboard.ReportChartItem> _items = new();

    public IReadOnlyList<FrmMainDashboard.ReportChartItem> Items => _items;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsLoading { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string EmptyMessage { get; set; } = "Không có dữ liệu";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SeriesLabel { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BarColor { get; set; } = ModernUi.Blue;

    public ReportBarChart()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        MinimumSize = new Size(220, 150);
    }

    public void SetData(IEnumerable<FrmMainDashboard.ReportChartItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (ReportChartPaint.DrawReportEmptyOrLoading(e.Graphics, ClientRectangle, IsLoading, EmptyMessage, _items.Any(i => i.Value > 0)))
        {
            return;
        }

        decimal max = Math.Max(1, _items.Max(i => i.Value));
        var plot = new Rectangle(42, 18, Math.Max(120, Width - 56), Math.Max(84, Height - 58));
        using var gridPen = new Pen(Color.FromArgb(231, 236, 244));
        using var axisPen = new Pen(Color.FromArgb(203, 213, 225));
        using var textBrush = new SolidBrush(ModernUi.Muted);
        int steps = 3;
        for (int i = 0; i <= steps; i++)
        {
            int y = plot.Bottom - plot.Height * i / steps;
            e.Graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            e.Graphics.DrawString((max * i / steps).ToString("0", CultureInfo.InvariantCulture), ModernUi.Font(7.2f), textBrush, 2, y - 8);
        }
        e.Graphics.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

        int slot = Math.Max(24, plot.Width / Math.Max(1, _items.Count));
        int barWidth = Math.Max(18, Math.Min(36, slot - 18));
        int labelStep = Math.Max(1, (int)Math.Ceiling(_items.Count * 52m / Math.Max(1, plot.Width)));
        for (int i = 0; i < _items.Count; i++)
        {
            int height = Math.Max(3, (int)Math.Round(_items[i].Value / max * plot.Height));
            int x = plot.Left + i * slot + (slot - barWidth) / 2;
            int y = plot.Bottom - height;
            using var brush = new SolidBrush(_items[i].Color == Color.Empty ? BarColor : _items[i].Color);
            using var path = RoundedBar(new Rectangle(x, y, barWidth, height), 6);
            e.Graphics.FillPath(brush, path);
            bool showLabel = i % labelStep == 0 || i == _items.Count - 1;
            if (!showLabel)
            {
                continue;
            }

            var labelRect = new RectangleF(x - 20, plot.Bottom + 8, barWidth + 40, 18);
            e.Graphics.DrawString(_items[i].Label, ModernUi.Font(7.3f), textBrush, labelRect, CenterFormat());
            var valueRect = new RectangleF(x - 16, y - 20, barWidth + 32, 16);
            e.Graphics.DrawString(_items[i].Value.ToString("0", CultureInfo.InvariantCulture), ModernUi.Font(7.3f, FontStyle.Bold), Brushes.Black, valueRect, CenterFormat());
        }
    }

    private static GraphicsPath RoundedBar(Rectangle bounds, int radius)
    {
        int diameter = Math.Max(2, Math.Min(bounds.Width, radius * 2));
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddLine(bounds.Right, bounds.Y + diameter / 2, bounds.Right, bounds.Bottom);
        path.AddLine(bounds.Right, bounds.Bottom, bounds.X, bounds.Bottom);
        path.CloseFigure();
        return path;
    }

    private static StringFormat CenterFormat() => new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.EllipsisCharacter
    };
}

internal sealed class ReportDonutChart : Control, IReportChartState
{
    private readonly List<FrmMainDashboard.ReportChartItem> _items = new();

    public IReadOnlyList<FrmMainDashboard.ReportChartItem> Items => _items;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsLoading { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string EmptyMessage { get; set; } = "Không có dữ liệu";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CenterText { get; set; } = "0";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CenterSubText { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool CompactLegend { get; set; }

    public ReportDonutChart()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        MinimumSize = new Size(180, 150);
    }

    public void SetData(IEnumerable<FrmMainDashboard.ReportChartItem> items)
    {
        _items.Clear();
        _items.AddRange(items.Where(i => i.Value > 0));
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (ReportChartPaint.DrawReportEmptyOrLoading(e.Graphics, ClientRectangle, IsLoading, EmptyMessage, _items.Count > 0))
        {
            return;
        }

        decimal total = _items.Sum(i => i.Value);
        bool sideLegend = Width >= (CompactLegend ? 280 : 360);
        int legendWidth = sideLegend
            ? Math.Min(CompactLegend ? 168 : 220, Math.Max(124, Width / 2))
            : Width - 24;

        int donutWidth = sideLegend ? Width - legendWidth - 28 : Width - 24;
        int legendRows = LegendRowsForHeight(Height - 24);
        int rowHeight = CompactLegend ? 22 : 24;
        int maxDonutSize = CompactLegend ? 132 : 148;
        int bottomLegendReserve = sideLegend ? 36 : Math.Min(96, legendRows * rowHeight + 16);
        int size = Math.Max(82, Math.Min(Math.Min(donutWidth, Height - bottomLegendReserve), maxDonutSize));
        int donutX = sideLegend ? 18 : (Width - size) / 2;
        int donutY = sideLegend ? Math.Max(18, (Height - size) / 2) : 18;
        var rect = new Rectangle(donutX, donutY, size, size);
        int stroke = Math.Max(14, size / 7);

        using var bgPen = new Pen(Color.FromArgb(226, 232, 240), stroke);
        e.Graphics.DrawArc(bgPen, rect, -90, 360);

        float start = -90;
        foreach (var item in _items)
        {
            float sweep = total <= 0 ? 0 : (float)(item.Value / total * 360m);
            using var pen = new Pen(item.Color, stroke);
            e.Graphics.DrawArc(pen, rect, start, sweep);
            start += sweep;
        }

        using var centerBrush = new SolidBrush(ModernUi.Navy);
        using var mutedBrush = new SolidBrush(ModernUi.Muted);
        var centerRect = new Rectangle(rect.Left + stroke - 4, rect.Top + rect.Height / 2 - 23, rect.Width - stroke * 2 + 8, 28);
        float centerFontSize = FitDonutCenterFont(e.Graphics, CenterText, centerRect.Width, CenterText.Length > 8 ? 12.5f : 15.5f);
        using var centerFont = ModernUi.Font(centerFontSize, FontStyle.Bold);
        e.Graphics.DrawString(CenterText, centerFont, centerBrush, centerRect, CenterFormat());

        var subRect = new Rectangle(rect.Left + stroke - 4, centerRect.Bottom - 2, rect.Width - stroke * 2 + 8, 24);
        e.Graphics.DrawString(CenterSubText, ModernUi.Font(7.2f), mutedBrush, subRect, CenterFormat());

        if (sideLegend)
        {
            int legendX = rect.Right + 16;
            int sideLegendHeight = Math.Max(rowHeight, legendRows * rowHeight);
            int legendY = Math.Max(24, (Height - sideLegendHeight) / 2);
            DrawLegend(e.Graphics, legendX, legendY, Math.Max(112, Width - legendX - 12), total, legendRows, rowHeight);
            return;
        }

        int rowCount = Math.Min(legendRows, Math.Max(1, (Height - rect.Bottom - 8) / rowHeight));
        int legendHeight = Math.Max(rowHeight, rowCount * rowHeight);
        int bottomLegendY = Math.Min(Math.Max(rect.Bottom + 8, Height - legendHeight - 4), Math.Max(0, Height - legendHeight));
        DrawLegend(e.Graphics, 14, bottomLegendY, Width - 28, total, rowCount, rowHeight);
    }

    private int LegendRowsForHeight(int availableHeight)
    {
        int rowHeight = CompactLegend ? 22 : 24;
        int maxRows = CompactLegend ? 6 : 7;
        return Math.Min(_items.Count, Math.Max(1, Math.Min(maxRows, availableHeight / rowHeight)));
    }

    private void DrawLegend(Graphics graphics, int x, int y, int width, decimal total, int maxRows, int rowHeight)
    {
        using var labelBrush = new SolidBrush(ModernUi.Text);
        using var valueBrush = new SolidBrush(ModernUi.Muted);

        maxRows = Math.Min(_items.Count, Math.Max(1, maxRows));
        int valueWidth = CompactLegend ? 76 : 96;
        int labelWidth = Math.Max(42, width - valueWidth - 24);

        for (int i = 0; i < maxRows; i++)
        {
            var item = _items[i];
            int rowY = y + i * rowHeight;

            using var brush = new SolidBrush(item.Color);
            graphics.FillEllipse(brush, x, rowY + Math.Max(5, (rowHeight - 9) / 2), 9, 9);

            decimal percent = total <= 0 ? 0 : item.Value * 100m / total;
            string label = item.Label;
            string value = $"{ReportLineChartValue(item.Value)} ({percent:0.#}%)";

            using var labelFont = ModernUi.Font(CompactLegend ? 7.2f : 7.8f, FontStyle.Bold);
            using var valueFont = ModernUi.Font(CompactLegend ? 6.9f : 7.5f);

            graphics.DrawString(
                label,
                labelFont,
                labelBrush,
                new Rectangle(x + 17, rowY, labelWidth, rowHeight),
                LeftFormat());

            graphics.DrawString(
                value,
                valueFont,
                valueBrush,
                new Rectangle(x + 17 + labelWidth + 4, rowY, valueWidth, rowHeight),
                RightFormat());
        }
    }

    private static string ReportLineChartValue(decimal value)
    {
        if (value >= 1_000_000_000m) return $"{value / 1_000_000_000m:0.#}B";
        if (value >= 1_000_000m) return $"{value / 1_000_000m:0.#}M";
        if (value >= 1_000m) return $"{value / 1_000m:0.#}K";
        return value.ToString("0", CultureInfo.InvariantCulture);
    }

    private static StringFormat CenterFormat() => new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.EllipsisCharacter,
        FormatFlags = StringFormatFlags.NoWrap
    };

    private static StringFormat LeftFormat() => new()
    {
        Alignment = StringAlignment.Near,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.EllipsisCharacter,
        FormatFlags = StringFormatFlags.NoWrap
    };

    private static StringFormat RightFormat() => new()
    {
        Alignment = StringAlignment.Far,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.EllipsisCharacter,
        FormatFlags = StringFormatFlags.NoWrap
    };

    private static float FitDonutCenterFont(Graphics graphics, string text, int width, float preferred)
    {
        for (float size = preferred; size >= 9f; size -= 0.5f)
        {
            using var font = ModernUi.Font(size, FontStyle.Bold);
            if (graphics.MeasureString(text, font).Width <= width)
            {
                return size;
            }
        }

        return 9f;
    }
}

internal static class ReportChartPaint
{
    public static bool DrawReportEmptyOrLoading(Graphics graphics, Rectangle bounds, bool loading, string emptyMessage, bool hasData)
    {
        if (hasData && !loading)
        {
            return false;
        }

        using var textBrush = new SolidBrush(ModernUi.Muted);
        using var iconBrush = new SolidBrush(Color.FromArgb(148, 163, 184));
        using var iconFont = new Font("Segoe UI Symbol", 24f, FontStyle.Regular);
        using var messageFont = ModernUi.Font(9.2f, FontStyle.Bold);
        string message = loading ? "Đang tải dữ liệu..." : emptyMessage;
        var iconRect = new Rectangle(bounds.Left, bounds.Top + Math.Max(18, bounds.Height / 2 - 32), bounds.Width, 30);
        var textRect = new Rectangle(bounds.Left + 12, iconRect.Bottom + 4, Math.Max(0, bounds.Width - 24), 24);
        graphics.DrawString("□", iconFont, iconBrush, iconRect, CenterFormat());
        graphics.DrawString(message, messageFont, textBrush, textRect, CenterFormat());
        return true;
    }

    private static StringFormat CenterFormat() => new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.EllipsisCharacter
    };
}
