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
    private readonly List<object[]> _reportHistoryRows = new();

    private sealed class ReportSnapshot
    {
        public List<ResidentDTO> Residents { get; init; } = new();
        public List<ApartmentDTO> Apartments { get; init; } = new();
        public List<InvoiceDTO> Invoices { get; init; } = new();
        public List<dynamic> Complaints { get; init; } = new();
        public List<dynamic> Vehicles { get; init; } = new();
        public List<dynamic> Visitors { get; init; } = new();
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public string Building { get; init; } = "Tất cả";
    }

    private void RenderReports()
    {
        var page = BeginPage("Báo cáo & thống kê", "Dashboard / Báo cáo");
        int w = PageWorkWidth();
        int y = 72;
        List<ResidentDTO> residents = new();
        List<ApartmentDTO> apartments = new();
        List<InvoiceDTO> invoices = new();
        List<dynamic> complaints = new();
        List<dynamic> vehicles = new();
        List<dynamic> visitors = new();
        LoadSourceData();

        DateTime defaultReportDate = LatestReportDate();
        DateTime defaultStart = new(defaultReportDate.Year, defaultReportDate.Month, 1);
        DateTime defaultEnd = MonthEnd(defaultReportDate);

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 96);
        var periodPicker = AddReportMonthPicker(filters, "Kỳ báo cáo", defaultReportDate, 14);
        var fromPicker = AddReportDatePicker(filters, "Từ ngày", defaultStart, 238);
        var toPicker = AddReportDatePicker(filters, "Đến ngày", defaultEnd, 462);
        var buildingFilter = AddReportBuildingFilter(filters, apartments, 686);
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
        var statHost = new Panel
        {
            Location = new Point(0, y),
            Size = new Size(w + 36, 124),
            BackColor = ModernUi.Surface
        };
        page.Controls.Add(statHost);

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
        revenue.Controls.Add(chart);
        page.Controls.Add(revenue);

        var operations = ModernUi.Section("Vận hành tòa nhà", w - chartW - 12, 264);
        operations.Location = new Point(revenue.Right + 12, y);
        int donutW = Math.Min(270, Math.Max(220, operations.Width - 280));
        var occupancy = new DonutChartPanel
        {
            Percent = 0,
            CenterText = "0%",
            SubText = "Lấp đầy",
            AccentColor = ModernUi.Green,
            Location = new Point(16, 48),
            Size = new Size(donutW, 168)
        };
        operations.Controls.Add(occupancy);
        var reportText = ModernUi.Label("",
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
        complaintChart.Controls.Add(complaintBars);
        page.Controls.Add(complaintChart);

        var saved = ModernUi.Section("Danh sách báo cáo đã tạo", w - complaintW - 12, 234);
        saved.Location = new Point(complaintChart.Right + 12, y);
        string[] reportColumns = { "Báo cáo", "Kỳ", "Người tạo", "Ngày tạo", "Định dạng", "Trạng thái" };
        var savedGrid = CreateGrid(
            reportColumns,
            BuildReportHistoryRows());
        savedGrid.Location = new Point(12, 44);
        savedGrid.Size = new Size(saved.Width - 24, 150);
        saved.Controls.Add(savedGrid);
        page.Controls.Add(saved);

        bool syncingDates = false;

        periodPicker.ValueChanged += (_, _) =>
        {
            if (syncingDates)
            {
                return;
            }

            syncingDates = true;
            DateTime selectedMonth = periodPicker.Value.Date;
            fromPicker.Value = SafePickerValue(fromPicker, new DateTime(selectedMonth.Year, selectedMonth.Month, 1));
            toPicker.Value = SafePickerValue(toPicker, MonthEnd(selectedMonth));
            syncingDates = false;
            RefreshReportData();
        };

        fromPicker.ValueChanged += (_, _) =>
        {
            if (syncingDates)
            {
                return;
            }

            if (fromPicker.Value.Date > toPicker.Value.Date)
            {
                syncingDates = true;
                toPicker.Value = SafePickerValue(toPicker, fromPicker.Value.Date);
                syncingDates = false;
            }

            RefreshReportData();
        };

        toPicker.ValueChanged += (_, _) =>
        {
            if (syncingDates)
            {
                return;
            }

            if (toPicker.Value.Date < fromPicker.Value.Date)
            {
                syncingDates = true;
                fromPicker.Value = SafePickerValue(fromPicker, toPicker.Value.Date);
                syncingDates = false;
            }

            RefreshReportData();
        };

        buildingFilter.SelectedIndexChanged += (_, _) => RefreshReportData();
        refresh.Click += (_, _) =>
        {
            LoadSourceData();
            RefreshBuildingFilterItems();
            RefreshReportData();
        };
        excel.Click += (_, _) => ExportCurrentReport("Excel");
        pdf.Click += (_, _) => ExportCurrentReport("PDF");

        RefreshReportData();

        void LoadSourceData()
        {
            residents = ResidentDAL.GetAllResidents();
            apartments = ApartmentDAL.GetAllApartments();
            invoices = InvoiceDAL.GetAllInvoices();
            complaints = ComplaintDAL.GetAllComplaints();
            vehicles = VehicleDAL.GetAllVehicles();
            visitors = VisitorDAL.GetAllVisitors();
        }

        DateTime LatestReportDate()
        {
            var dates = new List<DateTime>();
            dates.AddRange(invoices.Select(i => i.CreatedAt));
            dates.AddRange(apartments.Select(a => a.CreatedAt));
            dates.AddRange(residents.Select(r => r.CreatedAt));
            dates.AddRange(complaints.Select(c => FirstDynamicDate(c, "CreatedAt")).Where(d => d.HasValue).Select(d => d!.Value));
            dates.AddRange(vehicles.Select(v => FirstDynamicDate(v, "CreatedAt", "RegisteredAt")).Where(d => d.HasValue).Select(d => d!.Value));
            dates.AddRange(visitors.Select(v => FirstDynamicDate(v, "ArrivalTime", "CheckInTime", "CreatedAt")).Where(d => d.HasValue).Select(d => d!.Value));

            var validDates = dates
                .Where(d => d > DateTime.MinValue && d.Date <= DateTime.Today)
                .Select(d => d.Date)
                .ToList();

            return validDates.Count == 0 ? DateTime.Today : validDates.Max();
        }

        DateTime MonthEnd(DateTime date)
        {
            var end = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
            return end > DateTime.Today ? DateTime.Today : end;
        }

        DateTime SafePickerValue(DateTimePicker picker, DateTime value)
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

        void RefreshReportData()
        {
            var snapshot = BuildSnapshot();
            RenderStatCards(snapshot);
            RenderCharts(snapshot);
            SetGridData(savedGrid, reportColumns, BuildReportHistoryRows());
        }

        void RefreshBuildingFilterItems()
        {
            string selected = buildingFilter.SelectedItem?.ToString() ?? "Tất cả";
            var buildings = apartments
                .Select(a => Display(a.BuildingName, "Chưa rõ"))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            buildings.Insert(0, "Tất cả");

            buildingFilter.BeginUpdate();
            buildingFilter.Items.Clear();
            foreach (string building in buildings)
            {
                buildingFilter.Items.Add(building);
            }

            int selectedIndex = buildings.FindIndex(name => string.Equals(name, selected, StringComparison.CurrentCultureIgnoreCase));
            buildingFilter.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            buildingFilter.EndUpdate();
        }

        ReportSnapshot BuildSnapshot()
        {
            DateTime start = fromPicker.Value.Date;
            DateTime end = toPicker.Value.Date;
            if (start > end)
            {
                (start, end) = (end, start);
            }

            DateTime endInclusive = end.AddDays(1).AddTicks(-1);
            string selectedBuilding = buildingFilter.SelectedItem?.ToString() ?? "Tất cả";

            var filteredApartments = apartments
                .Where(a => BuildingMatches(a.BuildingName, selectedBuilding))
                .Where(a => a.CreatedAt <= DateTime.MinValue || a.CreatedAt.Date <= end)
                .ToList();

            var apartmentIds = filteredApartments.Select(a => a.ApartmentID).ToHashSet();

            return new ReportSnapshot
            {
                StartDate = start,
                EndDate = end,
                Building = selectedBuilding,
                Apartments = filteredApartments,
                Residents = residents
                    .Where(r => apartmentIds.Contains(r.ApartmentID))
                    .Where(r => ResidentOverlapsRange(r, start, endInclusive))
                    .ToList(),
                Invoices = invoices
                    .Where(i => apartmentIds.Contains(i.ApartmentID))
                    .Where(i => InRange(i.CreatedAt, start, endInclusive))
                    .ToList(),
                Complaints = complaints
                    .Where(c => apartmentIds.Contains(GetDynamicInt(c, "ApartmentID")))
                    .Where(c => InRange(FirstDynamicDate(c, "CreatedAt", "ReportDate"), start, endInclusive))
                    .ToList(),
                Vehicles = vehicles
                    .Where(v => apartmentIds.Contains(GetDynamicInt(v, "ApartmentID")))
                    .Where(v => InRange(FirstDynamicDate(v, "CreatedAt", "RegisteredAt"), start, endInclusive))
                    .ToList(),
                Visitors = visitors
                    .Where(v => apartmentIds.Contains(GetDynamicInt(v, "ApartmentID")))
                    .Where(v => InRange(FirstDynamicDate(v, "ArrivalTime", "CheckInTime", "CreatedAt"), start, endInclusive))
                    .ToList()
            };
        }

        bool BuildingMatches(string buildingName, string selectedBuilding)
            => selectedBuilding == "Tất cả" ||
               string.Equals(Display(buildingName, "Chưa rõ"), selectedBuilding, StringComparison.CurrentCultureIgnoreCase);

        bool InRange(DateTime? value, DateTime start, DateTime endInclusive)
            => value.HasValue && value.Value >= start && value.Value <= endInclusive;

        bool ResidentOverlapsRange(ResidentDTO resident, DateTime start, DateTime endInclusive)
        {
            DateTime residentStart = (resident.StartDate ?? resident.MoveInDate ?? resident.CreatedAt).Date;
            DateTime? residentEnd = resident.EndDate ?? resident.MoveOutDate;
            return residentStart <= endInclusive && (!residentEnd.HasValue || residentEnd.Value.Date >= start);
        }

        DateTime? FirstDynamicDate(dynamic row, params string[] names)
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

        void RenderStatCards(ReportSnapshot snapshot)
        {
            statHost.Controls.Clear();

            int cardW = Math.Max(150, (w - 36 - 12 * 5) / 6);
            int occupied = snapshot.Apartments.Count(a => ViStatus(a.Status) == "Đang sử dụng");
            int occupancyRate = snapshot.Apartments.Count == 0 ? 0 : (int)Math.Round(occupied * 100m / snapshot.Apartments.Count);
            decimal revenueTotal = snapshot.Invoices.Sum(i => i.PaidAmount);
            decimal debtTotal = snapshot.Invoices.Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount));
            int unpaidCount = snapshot.Invoices.Count(i => ViStatus(i.PaymentStatus) != "Đã thanh toán");

            AddRow(statHost, 0, 12,
                ModernUi.StatCard("Cư dân", snapshot.Residents.Count.ToString("N0"), "Người", ModernUi.Blue, "●●", $"{snapshot.Residents.Count(r => IsActiveStatus(r.Status))} hoạt động", cardW, 124),
                ModernUi.StatCard("Lấp đầy", $"{occupancyRate}%", "Căn hộ", ModernUi.Green, "◔", $"{occupied:N0}/{snapshot.Apartments.Count:N0}", cardW, 124),
                ModernUi.StatCard("Doanh thu", MoneyShort(revenueTotal), "VNĐ", ModernUi.Orange, "$", "Đã thu", cardW, 124),
                ModernUi.StatCard("Công nợ", MoneyShort(debtTotal), "VNĐ", ModernUi.Red, "!", $"{unpaidCount:N0} hóa đơn", cardW, 124),
                ModernUi.StatCard("Phản ánh", snapshot.Complaints.Count.ToString("N0"), "Phiếu", ModernUi.Purple, "▤", $"{snapshot.Complaints.Count(c => ViStatus(c.Status) == "Mới")} mới", cardW, 124),
                ModernUi.StatCard("Phương tiện", snapshot.Vehicles.Count.ToString("N0"), "Xe", ModernUi.Teal, "▣", $"{snapshot.Vehicles.Count(v => IsActiveStatus(v.Status))} hoạt động", cardW, 124));
        }

        void RenderCharts(ReportSnapshot snapshot)
        {
            int occupied = snapshot.Apartments.Count(a => ViStatus(a.Status) == "Đang sử dụng");
            int occupancyRate = snapshot.Apartments.Count == 0 ? 0 : (int)Math.Round(occupied * 100m / snapshot.Apartments.Count);

            var monthly = snapshot.Invoices
                .GroupBy(i => new DateTime(i.Year, i.Month, 1))
                .OrderBy(g => g.Key)
                .TakeLast(12)
                .Select(g => (Label: $"T{g.Key.Month}", Value: ChartValue(g.Sum(i => i.PaidAmount))))
                .ToList();

            chart.Bars.Clear();
            chart.AxisMax = Math.Max(1, monthly.Count == 0 ? 1 : (int)(monthly.Max(m => m.Value) * 1.2m));
            chart.Bars.AddRange(monthly);
            chart.Invalidate();

            occupancy.Percent = occupancyRate;
            occupancy.CenterText = $"{occupancyRate}%";
            occupancy.PrimaryLabel = "Đã thanh toán";
            occupancy.PrimaryValue = $"{snapshot.Invoices.Count(i => ViStatus(i.PaymentStatus) == "Đã thanh toán"):N0}";
            occupancy.SecondaryLabel = "Chưa thanh toán";
            occupancy.SecondaryValue = $"{snapshot.Invoices.Count(i => ViStatus(i.PaymentStatus) != "Đã thanh toán"):N0}";
            occupancy.Invalidate();

            reportText.Text =
                $"Phản ánh đã xử lý: {snapshot.Complaints.Count(c => ViStatus(c.Status) == "Đã xử lý"):N0} / {snapshot.Complaints.Count:N0}\r\n" +
                $"Khách ra vào trong kỳ: {snapshot.Visitors.Count:N0} lượt\r\n" +
                $"Căn hộ bảo trì: {snapshot.Apartments.Count(a => ViStatus(a.Status) == "Bảo trì"):N0}\r\n" +
                $"Phương tiện hoạt động: {snapshot.Vehicles.Count(v => IsActiveStatus(v.Status)):N0} / {snapshot.Vehicles.Count:N0}";

            var complaintGroups = snapshot.Complaints
                .GroupBy(c => (string)Display(c.Category, "Khác"))
                .OrderByDescending(g => g.Count())
                .Take(6)
                .Select(g => (Label: g.Key.Length > 10 ? g.Key[..10] : g.Key, Value: g.Count()))
                .ToList();

            complaintBars.Bars.Clear();
            complaintBars.AxisMax = Math.Max(1, complaintGroups.Count == 0 ? 1 : (int)(complaintGroups.Max(g => g.Value) * 1.2m));
            complaintBars.Bars.AddRange(complaintGroups);
            complaintBars.Invalidate();
        }

        object[][] BuildReportHistoryRows()
        {
            return _reportHistoryRows.Count == 0
                ? new[] { EmptyRow(reportColumns.Length, "Chưa có báo cáo được tạo trong phiên này") }
                : _reportHistoryRows.Take(20).ToArray();
        }

        string ReportPeriodText(ReportSnapshot snapshot)
            => $"{DateText(snapshot.StartDate)} - {DateText(snapshot.EndDate)}";

        void ExportCurrentReport(string format)
        {
            var snapshot = BuildSnapshot();
            var result = format == "Excel"
                ? BuildExcelReport(snapshot)
                : BuildPdfReport(snapshot);
            string filter = format == "Excel" ? "Excel Workbook (*.xlsx)|*.xlsx" : "PDF (*.pdf)|*.pdf";

            if (!SaveGeneratedFile(result, filter, $"Reports{format}Export"))
            {
                return;
            }

            _reportHistoryRows.Insert(0, new object[]
            {
                "Báo cáo thống kê",
                ReportPeriodText(snapshot),
                CurrentUsername(),
                DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                format,
                "Thành công"
            });
            SetGridData(savedGrid, reportColumns, BuildReportHistoryRows());
        }

        (bool Success, string Message, byte[] FileContent, string FileName) BuildExcelReport(ReportSnapshot snapshot)
        {
            try
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Tong hop");

                worksheet.Cell(1, 1).Value = "Báo cáo thống kê";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Range(1, 1, 1, 4).Merge();

                worksheet.Cell(2, 1).Value = "Kỳ";
                worksheet.Cell(2, 2).Value = ReportPeriodText(snapshot);
                worksheet.Cell(3, 1).Value = "Tòa nhà";
                worksheet.Cell(3, 2).Value = snapshot.Building;
                worksheet.Cell(4, 1).Value = "Ngày tạo";
                worksheet.Cell(4, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

                int row = 6;
                worksheet.Cell(row, 1).Value = "Chỉ tiêu";
                worksheet.Cell(row, 2).Value = "Giá trị";
                worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;

                var summary = new (string Label, string Value)[]
                {
                    ("Cư dân", snapshot.Residents.Count.ToString("N0", CultureInfo.InvariantCulture)),
                    ("Căn hộ", snapshot.Apartments.Count.ToString("N0", CultureInfo.InvariantCulture)),
                    ("Doanh thu đã thu", Money(snapshot.Invoices.Sum(i => i.PaidAmount))),
                    ("Công nợ", Money(snapshot.Invoices.Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount)))),
                    ("Phản ánh", snapshot.Complaints.Count.ToString("N0", CultureInfo.InvariantCulture)),
                    ("Phương tiện", snapshot.Vehicles.Count.ToString("N0", CultureInfo.InvariantCulture)),
                    ("Khách ra vào", snapshot.Visitors.Count.ToString("N0", CultureInfo.InvariantCulture))
                };

                foreach (var item in summary)
                {
                    row++;
                    worksheet.Cell(row, 1).Value = item.Label;
                    worksheet.Cell(row, 2).Value = item.Value;
                }

                row += 3;
                worksheet.Cell(row, 1).Value = "Doanh thu theo tháng";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                row++;
                worksheet.Cell(row, 1).Value = "Tháng";
                worksheet.Cell(row, 2).Value = "Đã thu";
                worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;

                foreach (var month in snapshot.Invoices.GroupBy(i => new DateTime(i.Year, i.Month, 1)).OrderBy(g => g.Key))
                {
                    row++;
                    worksheet.Cell(row, 1).Value = month.Key.ToString("MM/yyyy", CultureInfo.InvariantCulture);
                    worksheet.Cell(row, 2).Value = (double)month.Sum(i => i.PaidAmount);
                }

                row += 3;
                worksheet.Cell(row, 1).Value = "Phản ánh theo loại";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                row++;
                worksheet.Cell(row, 1).Value = "Loại";
                worksheet.Cell(row, 2).Value = "Số lượng";
                worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;

                foreach (var group in snapshot.Complaints.GroupBy(c => (string)Display(c.Category, "Khác")).OrderByDescending(g => g.Count()))
                {
                    row++;
                    worksheet.Cell(row, 1).Value = group.Key;
                    worksheet.Cell(row, 2).Value = group.Count();
                }

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return (true, "Đã tạo file Excel.", stream.ToArray(), $"bao-cao-thong-ke-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                return (false, $"Không thể tạo Excel: {ex.Message}", null, null);
            }
        }

        (bool Success, string Message, byte[] FileContent, string FileName) BuildPdfReport(ReportSnapshot snapshot)
        {
            try
            {
                using var stream = new MemoryStream();
                var writer = new iText.Kernel.Pdf.PdfWriter(stream);
                var pdfDocument = new iText.Kernel.Pdf.PdfDocument(writer);
                var document = new iText.Layout.Document(pdfDocument);

                document.Add(new iText.Layout.Element.Paragraph("Bao cao thong ke").SetFontSize(16).SetBold());
                document.Add(new iText.Layout.Element.Paragraph($"Ky: {ReportPeriodText(snapshot)}"));
                document.Add(new iText.Layout.Element.Paragraph($"Toa nha: {PdfText(snapshot.Building)}"));
                document.Add(new iText.Layout.Element.Paragraph($"Ngay tao: {DateTime.Now:dd/MM/yyyy HH:mm}"));

                var table = new iText.Layout.Element.Table(2, false);
                table.AddHeaderCell("Chi tieu");
                table.AddHeaderCell("Gia tri");
                table.AddCell("Cu dan");
                table.AddCell(snapshot.Residents.Count.ToString("N0", CultureInfo.InvariantCulture));
                table.AddCell("Can ho");
                table.AddCell(snapshot.Apartments.Count.ToString("N0", CultureInfo.InvariantCulture));
                table.AddCell("Doanh thu da thu");
                table.AddCell(Money(snapshot.Invoices.Sum(i => i.PaidAmount)) + " VND");
                table.AddCell("Cong no");
                table.AddCell(Money(snapshot.Invoices.Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount))) + " VND");
                table.AddCell("Phan anh");
                table.AddCell(snapshot.Complaints.Count.ToString("N0", CultureInfo.InvariantCulture));
                table.AddCell("Phuong tien");
                table.AddCell(snapshot.Vehicles.Count.ToString("N0", CultureInfo.InvariantCulture));
                table.AddCell("Khach ra vao");
                table.AddCell(snapshot.Visitors.Count.ToString("N0", CultureInfo.InvariantCulture));
                document.Add(table);

                document.Close();
                return (true, "Đã tạo file PDF.", stream.ToArray(), $"bao-cao-thong-ke-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                return (false, $"Không thể tạo PDF: {ex.Message}", null, null);
            }
        }

        string PdfText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            string normalized = value
                .Replace('Đ', 'D')
                .Replace('đ', 'd')
                .Normalize(System.Text.NormalizationForm.FormD);
            var chars = new List<char>();
            foreach (char ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    chars.Add(ch);
                }
            }

            return new string(chars.ToArray()).Normalize(System.Text.NormalizationForm.FormC);
        }
    }

    private static DateTimePicker AddReportDatePicker(Control parent, string label, DateTime selected, int x)
    {
        var lbl = ModernUi.Label(label, 8.7f, FontStyle.Bold, ModernUi.Text);
        lbl.Location = new Point(x, 8);
        lbl.Size = new Size(170, 18);
        parent.Controls.Add(lbl);

        var picker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            MinDate = new DateTime(2000, 1, 1),
            MaxDate = DateTime.Today,
            Width = 210,
            Height = 30,
            Font = ModernUi.Font(9.5f),
            Location = new Point(x, 30)
        };
        picker.Value = selected.Date < picker.MinDate ? picker.MinDate : selected.Date > picker.MaxDate ? picker.MaxDate : selected.Date;
        parent.Controls.Add(picker);
        return picker;
    }

    private static DateTimePicker AddReportMonthPicker(Control parent, string label, DateTime selected, int x)
    {
        var picker = AddReportDatePicker(parent, label, selected, x);
        picker.CustomFormat = "'Tháng' MM/yyyy";
        return picker;
    }

    private static ComboBox AddReportBuildingFilter(Control parent, IEnumerable<ApartmentDTO> apartments, int x)
    {
        var lbl = ModernUi.Label("Tòa nhà", 8.7f, FontStyle.Bold, ModernUi.Text);
        lbl.Location = new Point(x, 8);
        lbl.Size = new Size(170, 18);
        parent.Controls.Add(lbl);

        var buildings = apartments
            .Select(a => Display(a.BuildingName, "Chưa rõ"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        buildings.Insert(0, "Tất cả");

        var combo = ModernUi.ComboBox(buildings, 210);
        combo.Location = new Point(x, 30);
        parent.Controls.Add(combo);
        return combo;
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
