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

    private sealed class SystemLogView
    {
        public int LogID { get; set; }
        public int UserID { get; set; }
        public bool HasUserID { get; set; }
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
        public string Action { get; set; } = "";
        public string Module { get; set; } = "";
        public string Description { get; set; } = "";
        public string IPAddress { get; set; } = "";
        public string Level { get; set; } = "";
        public string Device { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    private void RenderSystemLogs()
    {
        const string allText = "Tất cả";
        const int pageSizeDefault = 20;
        string[] logColumns = { "ID", "Thời gian", "Người dùng", "Vai trò", "Module", "Hành động", "IP", "Mức độ", "Nội dung", "Thiết bị" };
        string[] issueColumns = { "Thời gian", "Mức độ", "Người dùng", "Hành động", "Module", "Nội dung", "IP" };

        var page = BeginPage("Log hệ thống", "Dashboard / Log hệ thống");
        var allLogs = LoadSystemLogRows();
        var filteredLogs = new List<SystemLogView>();
        var pageRows = new List<SystemLogView>();
        var currentPage = 1;
        var pageSize = pageSizeDefault;
        SystemLogView selectedLog = null;
        bool syncingFilters = false;
        bool autoRefreshEnabled = true;
        DateTime latestLogDate;
        DateTime earliestLogDate;
        DateTime lastRefreshAt = DateTime.Now;
        UpdateLogDateBounds();

        var autoRefreshTimer = new Timer { Interval = 60_000 };
        page.Disposed += (_, _) =>
        {
            autoRefreshTimer.Stop();
            autoRefreshTimer.Dispose();
        };

        var filterPanel = ModernUi.CardPanel();
        filterPanel.Padding = Padding.Empty;
        page.Controls.Add(filterPanel);

        var fromPicker = CreateLogDatePicker(earliestLogDate);
        var toPicker = CreateLogDatePicker(latestLogDate);
        var userCombo = ModernUi.ComboBox(Array.Empty<string>(), 170);
        var actionCombo = ModernUi.ComboBox(Array.Empty<string>(), 170);
        var levelCombo = ModernUi.ComboBox(Array.Empty<string>(), 150);
        var moduleCombo = ModernUi.ComboBox(Array.Empty<string>(), 160);
        var ipCombo = ModernUi.ComboBox(Array.Empty<string>(), 160);
        var searchLabel = ModernUi.Label("Tìm kiếm", 8.6f, FontStyle.Bold, ModernUi.Text);
        var searchBox = ModernUi.TextBox("Tìm theo module, nội dung, IP, người dùng...", 320);
        filterPanel.Controls.Add(searchLabel);
        filterPanel.Controls.Add(searchBox);

        var filterFields = new List<(Label Label, Control Input, int PreferredWidth)>
        {
            AddLogFilterField(filterPanel, "Từ ngày", fromPicker, 150),
            AddLogFilterField(filterPanel, "Đến ngày", toPicker, 150),
            AddLogFilterField(filterPanel, "Người dùng", userCombo, 170),
            AddLogFilterField(filterPanel, "Hành động", actionCombo, 170),
            AddLogFilterField(filterPanel, "Mức độ", levelCombo, 150),
            AddLogFilterField(filterPanel, "Module", moduleCombo, 160),
            AddLogFilterField(filterPanel, "IP", ipCombo, 160)
        };

        var refreshButton = ModernUi.Button("Làm mới", ModernUi.Blue, 108, 34);
        var advancedButton = ModernUi.OutlineButton("Bộ lọc nâng cao", 154, 34);
        var excelButton = ModernUi.Button("Xuất Excel", ModernUi.Green, 112, 34);
        var pdfButton = ModernUi.Button("Xuất PDF", ModernUi.Red, 104, 34);
        var clearFilterButton = ModernUi.OutlineButton("Xóa bộ lọc", 116, 34);
        var detailButton = ModernUi.Button("Xem chi tiết", ModernUi.Blue, 118, 34);
        filterPanel.Controls.Add(refreshButton);
        filterPanel.Controls.Add(advancedButton);
        filterPanel.Controls.Add(excelButton);
        filterPanel.Controls.Add(pdfButton);
        filterPanel.Controls.Add(clearFilterButton);
        filterPanel.Controls.Add(detailButton);

        var emptyState = ModernUi.CardPanel(12);
        emptyState.Padding = Padding.Empty;
        emptyState.Visible = false;
        var emptyIcon = ModernUi.Label("□", 22f, FontStyle.Bold, Color.FromArgb(148, 163, 184));
        emptyIcon.TextAlign = ContentAlignment.MiddleCenter;
        var emptyText = ModernUi.Label("Không có dữ liệu log phù hợp", 10f, FontStyle.Bold, ModernUi.Muted);
        emptyText.TextAlign = ContentAlignment.MiddleCenter;
        emptyState.Controls.Add(emptyIcon);
        emptyState.Controls.Add(emptyText);
        page.Controls.Add(emptyState);

        var listSection = ModernUi.Section("Danh sách log", 720, 420);
        var grid = CreateSystemLogGrid(logColumns);
        listSection.Controls.Add(grid);
        var pagerLabel = ModernUi.Label("", 8.8f, FontStyle.Regular, ModernUi.Muted);
        var firstButton = ModernUi.OutlineButton("«", 38, 30);
        var previousButton = ModernUi.OutlineButton("‹", 38, 30);
        var nextButton = ModernUi.OutlineButton("›", 38, 30);
        var lastButton = ModernUi.OutlineButton("»", 38, 30);
        foreach (var pagerButton in new[] { firstButton, previousButton, nextButton, lastButton })
        {
            pagerButton.Font = new Font("Segoe UI Symbol", 10.5f, FontStyle.Bold);
            pagerButton.Padding = Padding.Empty;
            pagerButton.UseVisualStyleBackColor = false;
        }
        var pageSizeCombo = ModernUi.ComboBox(new[] { "10", "20", "50", "100" }, 78);
        pageSizeCombo.SelectedItem = pageSizeDefault.ToString(CultureInfo.InvariantCulture);
        listSection.Controls.Add(pagerLabel);
        listSection.Controls.Add(firstButton);
        listSection.Controls.Add(previousButton);
        listSection.Controls.Add(nextButton);
        listSection.Controls.Add(lastButton);
        listSection.Controls.Add(pageSizeCombo);
        page.Controls.Add(listSection);

        var detailSection = ModernUi.Section("Chi tiết log", 360, 420);
        var detailRows = new List<(string Key, Label Caption, Control Value)>();
        AddDetailRow("time", "Thời gian");
        AddDetailRow("user", "Người dùng");
        AddDetailRow("role", "Vai trò");
        AddDetailRow("action", "Hành động");
        AddDetailRow("module", "Module");
        AddDetailRow("ip", "IP");
        var levelCaption = ModernUi.Label("Mức độ", 8.8f, FontStyle.Regular, ModernUi.Text);
        var levelBadge = CreateLogBadge("-");
        detailSection.Controls.Add(levelCaption);
        detailSection.Controls.Add(levelBadge);
        detailRows.Add(("level", levelCaption, levelBadge));
        AddDetailRow("device", "Thiết bị / Trình duyệt");
        var contentLabel = ModernUi.Label("Nội dung", 8.8f, FontStyle.Regular, ModernUi.Text);
        var contentBox = CreateReadonlyLogBox();
        var noteLabel = ModernUi.Label("Ghi chú", 8.8f, FontStyle.Regular, ModernUi.Text);
        var noteBox = CreateReadonlyLogBox();
        noteBox.Height = 52;
        foreach (var label in detailRows.Select(r => r.Caption).Concat(new[] { contentLabel, noteLabel }))
        {
            label.AutoEllipsis = true;
        }
        detailSection.Controls.Add(contentLabel);
        detailSection.Controls.Add(contentBox);
        detailSection.Controls.Add(noteLabel);
        detailSection.Controls.Add(noteBox);
        page.Controls.Add(detailSection);

        var statsSection = ModernUi.Section("Thống kê log", 320, 178);
        var totalMetric = CreateLogMetricCard(statsSection, "Tổng log", "≡", ModernUi.Blue);
        var successMetric = CreateLogMetricCard(statsSection, "Thành công", "✓", ModernUi.Green);
        var warningMetric = CreateLogMetricCard(statsSection, "Cảnh báo", "!", ModernUi.Orange);
        var errorMetric = CreateLogMetricCard(statsSection, "Lỗi", "×", ModernUi.Red);
        page.Controls.Add(statsSection);

        var severitySection = ModernUi.Section("Log theo mức độ", 320, 178);
        var severityChart = new ReportDonutChart
        {
            CompactLegend = true,
            CenterSubText = "Tổng log",
            EmptyMessage = "Không có dữ liệu log phù hợp"
        };
        severitySection.Controls.Add(severityChart);
        page.Controls.Add(severitySection);

        var moduleSection = ModernUi.Section("Log theo module", 320, 178);
        var moduleChart = new ReportDonutChart
        {
            CompactLegend = true,
            CenterSubText = "Tổng log",
            EmptyMessage = "Không có dữ liệu log phù hợp"
        };
        moduleSection.Controls.Add(moduleChart);
        page.Controls.Add(moduleSection);

        var topUsersSection = ModernUi.Section("Top người dùng hoạt động", 300, 178);
        var topUsersBody = new Panel { BackColor = Color.White };
        topUsersSection.Controls.Add(topUsersBody);
        page.Controls.Add(topUsersSection);

        var recentIssueSection = ModernUi.Section("Cảnh báo & lỗi gần đây", 720, 230);
        var issueGrid = CreateSystemLogGrid(issueColumns, includeHiddenId: false);
        recentIssueSection.Controls.Add(issueGrid);
        page.Controls.Add(recentIssueSection);

        var timeSection = ModernUi.Section("Hoạt động theo thời gian", 430, 230);
        var timeChart = new ReportLineChart
        {
            EmptyMessage = "Không có dữ liệu log phù hợp"
        };
        timeSection.Controls.Add(timeChart);
        page.Controls.Add(timeSection);

        var autoSection = ModernUi.Section("Auto refresh trạng thái", 260, 230);
        var autoStateLabel = ModernUi.Label("", 9.4f, FontStyle.Bold, ModernUi.Green);
        var autoLastLabel = ModernUi.Label("", 8.8f, FontStyle.Regular, ModernUi.Text);
        var autoIntervalLabel = ModernUi.Label("Cập nhật mỗi: 60 giây", 8.8f, FontStyle.Regular, ModernUi.Muted);
        var autoPauseButton = ModernUi.OutlineButton("Tạm dừng", 132, 36);
        foreach (var label in new[] { autoStateLabel, autoLastLabel, autoIntervalLabel })
        {
            label.AutoEllipsis = true;
        }
        autoSection.Controls.Add(autoStateLabel);
        autoSection.Controls.Add(autoLastLabel);
        autoSection.Controls.Add(autoIntervalLabel);
        autoSection.Controls.Add(autoPauseButton);
        page.Controls.Add(autoSection);

        PopulateLogFilterCombos();
        WireLogEvents();
        autoRefreshTimer.Start();
        RefreshSystemLogView(resetPage: true);

        void AddDetailRow(string key, string title)
        {
            var caption = ModernUi.Label(title, 8.8f, FontStyle.Regular, ModernUi.Text);
            var value = CreateReadonlyLogTextBox();
            detailSection.Controls.Add(caption);
            detailSection.Controls.Add(value);
            detailRows.Add((key, caption, value));
        }

        void WireLogEvents()
        {
            fromPicker.ValueChanged += (_, _) =>
            {
                if (!syncingFilters)
                {
                    RefreshSystemLogView(resetPage: true);
                }
            };
            toPicker.ValueChanged += (_, _) =>
            {
                if (!syncingFilters)
                {
                    RefreshSystemLogView(resetPage: true);
                }
            };
            userCombo.SelectedIndexChanged += (_, _) => RefreshSystemLogView(resetPage: true);
            actionCombo.SelectedIndexChanged += (_, _) => RefreshSystemLogView(resetPage: true);
            levelCombo.SelectedIndexChanged += (_, _) => RefreshSystemLogView(resetPage: true);
            moduleCombo.SelectedIndexChanged += (_, _) => RefreshSystemLogView(resetPage: true);
            ipCombo.SelectedIndexChanged += (_, _) => RefreshSystemLogView(resetPage: true);
            searchBox.TextChanged += (_, _) => RefreshSystemLogView(resetPage: true);

            refreshButton.Click += (_, _) =>
            {
                ReloadLogSource(showMessage: true);
            };
            advancedButton.Click += (_, _) => ShowAdvancedLogFilterDialog();
            excelButton.Click += (_, _) => ExportSystemLogs("Excel");
            pdfButton.Click += (_, _) => ExportSystemLogs("PDF");
            clearFilterButton.Click += (_, _) => ClearLogFilters();
            detailButton.Click += (_, _) => ShowSelectedLogDetail();

            firstButton.Click += (_, _) =>
            {
                currentPage = 1;
                RefreshSystemLogView(resetPage: false);
            };
            previousButton.Click += (_, _) =>
            {
                currentPage = Math.Max(1, currentPage - 1);
                RefreshSystemLogView(resetPage: false);
            };
            nextButton.Click += (_, _) =>
            {
                currentPage = Math.Min(TotalPages(), currentPage + 1);
                RefreshSystemLogView(resetPage: false);
            };
            lastButton.Click += (_, _) =>
            {
                currentPage = TotalPages();
                RefreshSystemLogView(resetPage: false);
            };
            pageSizeCombo.SelectedIndexChanged += (_, _) =>
            {
                pageSize = ParsePageSize(pageSizeCombo.SelectedItem, pageSizeDefault);
                RefreshSystemLogView(resetPage: true);
            };
            grid.SelectionChanged += (_, _) =>
            {
                var selected = SelectedGridLog();
                if (selected != null)
                {
                    selectedLog = selected;
                    UpdateLogDetail(selectedLog);
                }
            };
            grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    ShowSelectedLogDetail();
                }
            };
            autoPauseButton.Click += (_, _) =>
            {
                autoRefreshEnabled = !autoRefreshEnabled;
                UpdateAutoRefreshStatus();
            };
            autoRefreshTimer.Tick += (_, _) =>
            {
                if (autoRefreshEnabled && !syncingFilters && !IsDisposed)
                {
                    ReloadLogSource(showMessage: false);
                }
            };
            page.Resize += (_, _) => LayoutSystemLogs();
        }

        void PopulateLogFilterCombos()
        {
            SetComboItems(userCombo, allLogs.Select(l => l.Username).Where(v => !string.IsNullOrWhiteSpace(v)));
            SetComboItems(actionCombo, allLogs.Select(l => l.Action).Where(v => !string.IsNullOrWhiteSpace(v)));
            SetComboItems(levelCombo, allLogs.Select(l => l.Level).Where(v => !string.IsNullOrWhiteSpace(v)));
            SetComboItems(moduleCombo, allLogs.Select(l => l.Module).Where(v => !string.IsNullOrWhiteSpace(v)));
            SetComboItems(ipCombo, allLogs.Select(l => l.IPAddress).Where(v => !string.IsNullOrWhiteSpace(v)));
        }

        void SetComboItems(ComboBox combo, IEnumerable<string> values)
        {
            string selected = SelectedComboText(combo);
            var items = values
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            items.Insert(0, allText);

            syncingFilters = true;
            combo.BeginUpdate();
            combo.Items.Clear();
            combo.Items.AddRange(items.Cast<object>().ToArray());
            SelectComboValue(combo, selected);
            combo.EndUpdate();
            syncingFilters = false;
        }

        void SelectComboValue(ComboBox combo, string value)
        {
            int selectedIndex = -1;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (string.Equals(combo.Items[i]?.ToString(), value, StringComparison.CurrentCultureIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }

            combo.SelectedIndex = selectedIndex >= 0 ? selectedIndex : combo.Items.Count > 0 ? 0 : -1;
        }

        void ReloadLogSource(bool showMessage)
        {
            allLogs = LoadSystemLogRows();
            UpdateLogDateBounds();
            PopulateLogFilterCombos();
            lastRefreshAt = DateTime.Now;
            RefreshSystemLogView(resetPage: false);
            UpdateAutoRefreshStatus();
            if (showMessage)
            {
                MessageBox.Show(this, "Dữ liệu log đã được làm mới.", "Log hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        void UpdateLogDateBounds()
        {
            latestLogDate = allLogs.Count == 0 ? DateTime.Today : allLogs.Max(l => l.Timestamp.Date);
            if (latestLogDate > DateTime.Today)
            {
                latestLogDate = DateTime.Today;
            }

            earliestLogDate = allLogs.Count == 0 ? DateTime.Today.AddDays(-7) : allLogs.Min(l => l.Timestamp.Date);
            if (earliestLogDate > latestLogDate)
            {
                earliestLogDate = latestLogDate.AddDays(-7);
            }
        }

        void ClearLogFilters()
        {
            syncingFilters = true;
            fromPicker.Value = SafeLogDatePickerValue(fromPicker, earliestLogDate);
            toPicker.Value = SafeLogDatePickerValue(toPicker, latestLogDate);
            foreach (var combo in new[] { userCombo, actionCombo, levelCombo, moduleCombo, ipCombo })
            {
                if (combo.Items.Count > 0)
                {
                    combo.SelectedIndex = 0;
                }
            }
            searchBox.Text = string.Empty;
            syncingFilters = false;
            RefreshSystemLogView(resetPage: true);
        }

        void RefreshSystemLogView(bool resetPage)
        {
            if (syncingFilters)
            {
                return;
            }

            if (!ValidateLogDateRange())
            {
                return;
            }

            if (resetPage)
            {
                currentPage = 1;
            }

            filteredLogs = ApplyLogFilters().ToList();
            currentPage = Math.Max(1, Math.Min(currentPage, TotalPages()));
            int start = (currentPage - 1) * pageSize;
            pageRows = filteredLogs.Skip(start).Take(pageSize).ToList();

            SetGridData(grid, logColumns, BuildLogRows(pageRows));
            ConfigureSystemLogGrid(grid, includeHiddenId: true);

            var issueRows = filteredLogs
                .Where(l => IsWarningOrError(l.Level))
                .Take(12)
                .ToList();
            SetGridData(issueGrid, issueColumns, BuildIssueRows(issueRows));
            ConfigureSystemLogGrid(issueGrid, includeHiddenId: false);

            selectedLog = pageRows.FirstOrDefault();
            UpdateLogDetail(selectedLog);
            UpdatePager();
            UpdateMetricsAndCharts();
            RenderTopUsers();
            UpdateAutoRefreshStatus();
            emptyState.Visible = filteredLogs.Count == 0;
            LayoutSystemLogs();
        }

        bool ValidateLogDateRange()
        {
            DateTime from = fromPicker.Value.Date;
            DateTime to = toPicker.Value.Date;
            if (from <= to)
            {
                return true;
            }

            MessageBox.Show(this,
                "Từ ngày không được lớn hơn Đến ngày. Hệ thống sẽ tự reset về khoảng ngày hợp lệ gần nhất.",
                "Bộ lọc thời gian",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            syncingFilters = true;
            fromPicker.Value = SafeLogDatePickerValue(fromPicker, to);
            syncingFilters = false;
            return true;
        }

        IEnumerable<SystemLogView> ApplyLogFilters()
        {
            DateTime from = fromPicker.Value.Date;
            DateTime toInclusive = toPicker.Value.Date.AddDays(1).AddTicks(-1);
            string selectedUser = SelectedComboText(userCombo);
            string selectedAction = SelectedComboText(actionCombo);
            string selectedLevel = SelectedComboText(levelCombo);
            string selectedModule = SelectedComboText(moduleCombo);
            string selectedIp = SelectedComboText(ipCombo);
            string keyword = searchBox.Text.Trim();

            return allLogs
                .Where(l => l.Timestamp >= from && l.Timestamp <= toInclusive)
                .Where(l => selectedUser == allText || string.Equals(l.Username, selectedUser, StringComparison.CurrentCultureIgnoreCase))
                .Where(l => selectedAction == allText || string.Equals(l.Action, selectedAction, StringComparison.CurrentCultureIgnoreCase))
                .Where(l => selectedLevel == allText || string.Equals(l.Level, selectedLevel, StringComparison.CurrentCultureIgnoreCase))
                .Where(l => selectedModule == allText || string.Equals(l.Module, selectedModule, StringComparison.CurrentCultureIgnoreCase))
                .Where(l => selectedIp == allText || string.Equals(l.IPAddress, selectedIp, StringComparison.CurrentCultureIgnoreCase))
                .Where(l => string.IsNullOrWhiteSpace(keyword) || LogContainsKeyword(l, keyword))
                .OrderByDescending(l => l.Timestamp);
        }

        bool LogContainsKeyword(SystemLogView log, string keyword)
        {
            return DateTimeText(log.Timestamp).Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Username.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Role.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Action.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Module.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Description.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.IPAddress.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Level.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Device.Contains(keyword, StringComparison.CurrentCultureIgnoreCase);
        }

        int TotalPages()
            => Math.Max(1, (int)Math.Ceiling(filteredLogs.Count / (double)Math.Max(1, pageSize)));

        void UpdatePager()
        {
            int total = filteredLogs.Count;
            int start = total == 0 ? 0 : (currentPage - 1) * pageSize + 1;
            int end = Math.Min(total, currentPage * pageSize);
            pagerLabel.Text = $"Hiển thị {start:N0} - {end:N0} / {total:N0} dòng log";
            StylePagerButton(firstButton, currentPage > 1);
            StylePagerButton(previousButton, currentPage > 1);
            StylePagerButton(nextButton, currentPage < TotalPages() && total > 0);
            StylePagerButton(lastButton, currentPage < TotalPages() && total > 0);
            detailButton.Enabled = selectedLog != null;
        }

        void StylePagerButton(Button button, bool active)
        {
            button.Enabled = true;
            button.Cursor = active ? Cursors.Hand : Cursors.No;
            button.ForeColor = active ? ModernUi.Blue : Color.FromArgb(148, 163, 184);
            button.BackColor = Color.White;
            button.FlatAppearance.BorderColor = active ? ModernUi.Border : Color.FromArgb(226, 232, 240);
        }

        SystemLogView SelectedGridLog()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.IsNewRow || grid.CurrentRow.Cells.Count == 0)
            {
                return null;
            }

            string rawId = Convert.ToString(grid.CurrentRow.Cells[0].Value) ?? "";
            return int.TryParse(rawId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
                ? filteredLogs.FirstOrDefault(l => l.LogID == id)
                : null;
        }

        void ShowSelectedLogDetail()
        {
            var selected = SelectedGridLog() ?? selectedLog;
            if (selected == null)
            {
                MessageBox.Show(this, "Không có log phù hợp để xem chi tiết.", "Chi tiết log", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            selectedLog = selected;
            UpdateLogDetail(selectedLog);
            MessageBox.Show(this,
                $"Thời gian: {DateTimeText(selected.Timestamp)}\n" +
                $"User: {selected.Username}\n" +
                $"Vai trò: {selected.Role}\n" +
                $"Module: {selected.Module}\n" +
                $"Hành động: {selected.Action}\n" +
                $"IP: {selected.IPAddress}\n" +
                $"Mức độ: {selected.Level}\n" +
                $"Thiết bị: {selected.Device}\n\n" +
                $"Nội dung:\n{selected.Description}",
                "Chi tiết log",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        void UpdateLogDetail(SystemLogView log)
        {
            SetDetailValue("time", log == null ? "" : DateTimeText(log.Timestamp));
            SetDetailValue("user", log == null ? "" : log.Username);
            SetDetailValue("role", log == null ? "" : log.Role);
            SetDetailValue("action", log == null ? "" : log.Action);
            SetDetailValue("module", log == null ? "" : log.Module);
            SetDetailValue("ip", log == null ? "" : log.IPAddress);
            SetDetailValue("device", log == null ? "" : log.Device);
            string level = log == null ? "-" : log.Level;
            levelBadge.Text = level;
            ApplyLogBadgeStyle(levelBadge, level);
            contentBox.Text = log == null ? "" : log.Description;
            noteBox.Text = log == null ? "" : $"LogID: {log.LogID:N0} | UserID: {(log.HasUserID ? log.UserID.ToString(CultureInfo.InvariantCulture) : "-")}";
        }

        void SetDetailValue(string key, string value)
        {
            var row = detailRows.FirstOrDefault(r => r.Key == key);
            if (row.Value is TextBox textBox)
            {
                textBox.Text = value;
            }
            else if (row.Value != null)
            {
                row.Value.Text = value;
            }
        }

        void ExportSystemLogs(string format)
        {
            var result = format == "Excel"
                ? BuildSystemLogsExcel(filteredLogs)
                : BuildSystemLogsPdf(filteredLogs);
            string filter = format == "Excel" ? "Excel Workbook (*.xlsx)|*.xlsx" : "PDF (*.pdf)|*.pdf";
            SaveGeneratedFile(result, filter, $"SystemLogs{format}Export");
        }

        void UpdateMetricsAndCharts()
        {
            int total = filteredLogs.Count;
            int success = filteredLogs.Count(l => string.Equals(l.Level, "Thành công", StringComparison.CurrentCultureIgnoreCase));
            int warning = filteredLogs.Count(l => string.Equals(l.Level, "Cảnh báo", StringComparison.CurrentCultureIgnoreCase));
            int error = filteredLogs.Count(l => string.Equals(l.Level, "Lỗi", StringComparison.CurrentCultureIgnoreCase));
            int info = Math.Max(0, total - success - warning - error);

            UpdateMetric(totalMetric, total, "Tổng log trong bộ lọc");
            UpdateMetric(successMetric, success, $"{Percent(success, total):0.#}% tổng log");
            UpdateMetric(warningMetric, warning, $"{Percent(warning, total):0.#}% cần theo dõi");
            UpdateMetric(errorMetric, error, $"{Percent(error, total):0.#}% lỗi");

            severityChart.CenterText = total.ToString("N0", CultureInfo.InvariantCulture);
            severityChart.SetData(new[]
            {
                new ReportChartItem { Label = "Thông tin", Value = info, Color = ModernUi.Blue },
                new ReportChartItem { Label = "Thành công", Value = success, Color = ModernUi.Green },
                new ReportChartItem { Label = "Cảnh báo", Value = warning, Color = ModernUi.Orange },
                new ReportChartItem { Label = "Lỗi", Value = error, Color = ModernUi.Red }
            });

            moduleChart.CenterText = total.ToString("N0", CultureInfo.InvariantCulture);
            moduleChart.SetData(filteredLogs
                .GroupBy(l => Display(l.Module, "Khác"))
                .OrderByDescending(g => g.Count())
                .Take(6)
                .Select((g, index) => new ReportChartItem
                {
                    Label = g.Key,
                    Value = g.Count(),
                    Color = ReportPalette(index)
                }));

            timeChart.SetData(BuildActivityTimeItems());
        }

        List<ReportChartItem> BuildActivityTimeItems()
        {
            DateTime from = fromPicker.Value.Date;
            DateTime to = toPicker.Value.Date;
            if (filteredLogs.Count == 0)
            {
                return new List<ReportChartItem>();
            }

            int days = Math.Max(0, (to - from).Days);
            if (days <= 1)
            {
                return Enumerable.Range(0, 24)
                    .Select(hour => new ReportChartItem
                    {
                        Label = $"{hour:00}h",
                        Value = filteredLogs.Count(l => l.Timestamp.Hour == hour),
                        Color = ModernUi.Blue
                    })
                    .ToList();
            }

            if (days <= 31)
            {
                return Enumerable.Range(0, days + 1)
                    .Select(offset => from.AddDays(offset))
                    .Select(day => new ReportChartItem
                    {
                        Label = day.ToString("dd/MM", CultureInfo.InvariantCulture),
                        Value = filteredLogs.Count(l => l.Timestamp.Date == day),
                        Color = ModernUi.Blue
                    })
                    .ToList();
            }

            return filteredLogs
                .GroupBy(l => new DateTime(l.Timestamp.Year, l.Timestamp.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new ReportChartItem
                {
                    Label = g.Key.ToString("MM/yyyy", CultureInfo.InvariantCulture),
                    Value = g.Count(),
                    Color = ModernUi.Blue
                })
                .ToList();
        }

        void RenderTopUsers()
        {
            topUsersBody.SuspendLayout();
            topUsersBody.Controls.Clear();
            var users = filteredLogs
                .GroupBy(l => Display(l.Username, "system"))
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToList();

            if (users.Count == 0)
            {
                var empty = ModernUi.Label("Không có dữ liệu người dùng", 8.8f, FontStyle.Bold, ModernUi.Muted);
                empty.TextAlign = ContentAlignment.MiddleCenter;
                empty.Dock = DockStyle.Fill;
                topUsersBody.Controls.Add(empty);
                topUsersBody.ResumeLayout();
                return;
            }

            int y = 4;
            for (int i = 0; i < users.Count; i++)
            {
                var rank = ModernUi.Label($"{i + 1}.", 8.7f, FontStyle.Bold, ModernUi.Muted);
                var name = ModernUi.Label(users[i].Key, 8.7f, FontStyle.Bold, ModernUi.Text);
                name.AutoEllipsis = true;
                var count = ModernUi.Label(users[i].Count().ToString("N0", CultureInfo.InvariantCulture), 8.7f, FontStyle.Bold, ModernUi.Blue);
                count.TextAlign = ContentAlignment.MiddleRight;
                rank.SetBounds(0, y, 28, 22);
                name.SetBounds(30, y, Math.Max(90, topUsersBody.Width - 92), 22);
                count.SetBounds(Math.Max(120, topUsersBody.Width - 56), y, 56, 22);
                topUsersBody.Controls.Add(rank);
                topUsersBody.Controls.Add(name);
                topUsersBody.Controls.Add(count);
                y += 25;
            }

            var link = ModernUi.Label("Xem chi tiết", 8.7f, FontStyle.Bold, ModernUi.Blue);
            link.Cursor = Cursors.Hand;
            link.SetBounds(0, Math.Max(y + 4, topUsersBody.Height - 28), 120, 22);
            link.Click += (_, _) => ShowSelectedLogDetail();
            topUsersBody.Controls.Add(link);
            topUsersBody.ResumeLayout();
        }

        void UpdateAutoRefreshStatus()
        {
            autoStateLabel.Text = $"Tự động làm mới: {(autoRefreshEnabled ? "Bật" : "Tắt")}";
            autoStateLabel.ForeColor = autoRefreshEnabled ? ModernUi.Green : ModernUi.Red;
            autoLastLabel.Text = $"Cập nhật lần cuối: {lastRefreshAt:dd/MM/yyyy HH:mm:ss}";
            autoPauseButton.Text = autoRefreshEnabled ? "Tạm dừng" : "Bật lại";
        }

        void ShowAdvancedLogFilterDialog()
        {
            using var dialog = new Form
            {
                Text = "Bộ lọc nâng cao - Log hệ thống",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(560, 398),
                Font = ModernUi.Font(9.5f),
                BackColor = ModernUi.Surface
            };

            var card = ModernUi.CardPanel(12);
            card.SetBounds(14, 14, dialog.ClientSize.Width - 28, dialog.ClientSize.Height - 28);
            card.Padding = Padding.Empty;
            dialog.Controls.Add(card);

            var title = ModernUi.Label("Bộ lọc nâng cao", 13f, FontStyle.Bold, ModernUi.Navy);
            title.SetBounds(18, 12, 300, 30);
            card.Controls.Add(title);

            var dialogFrom = CreateLogDatePicker(fromPicker.Value);
            var dialogTo = CreateLogDatePicker(toPicker.Value);
            var dialogUser = ModernUi.ComboBox(userCombo.Items.Cast<object>().Select(i => i.ToString() ?? "").Where(i => !string.IsNullOrWhiteSpace(i)), 220);
            var dialogAction = ModernUi.ComboBox(actionCombo.Items.Cast<object>().Select(i => i.ToString() ?? "").Where(i => !string.IsNullOrWhiteSpace(i)), 220);
            var dialogLevel = ModernUi.ComboBox(levelCombo.Items.Cast<object>().Select(i => i.ToString() ?? "").Where(i => !string.IsNullOrWhiteSpace(i)), 220);
            var dialogModule = ModernUi.ComboBox(moduleCombo.Items.Cast<object>().Select(i => i.ToString() ?? "").Where(i => !string.IsNullOrWhiteSpace(i)), 220);
            var dialogIp = ModernUi.ComboBox(ipCombo.Items.Cast<object>().Select(i => i.ToString() ?? "").Where(i => !string.IsNullOrWhiteSpace(i)), 220);

            AddDialogFilterField(card, "Khoảng thời gian - từ ngày", dialogFrom, 18, 56);
            AddDialogFilterField(card, "Khoảng thời gian - đến ngày", dialogTo, 284, 56);
            AddDialogFilterField(card, "Người dùng", dialogUser, 18, 120);
            AddDialogFilterField(card, "Loại log", dialogAction, 284, 120);
            AddDialogFilterField(card, "Trạng thái", dialogLevel, 18, 184);
            AddDialogFilterField(card, "Module", dialogModule, 284, 184);
            AddDialogFilterField(card, "IP", dialogIp, 18, 248);

            SelectComboValue(dialogUser, SelectedComboText(userCombo));
            SelectComboValue(dialogAction, SelectedComboText(actionCombo));
            SelectComboValue(dialogLevel, SelectedComboText(levelCombo));
            SelectComboValue(dialogModule, SelectedComboText(moduleCombo));
            SelectComboValue(dialogIp, SelectedComboText(ipCombo));

            var apply = ModernUi.Button("Áp dụng", ModernUi.Blue, 110, 34);
            var close = ModernUi.OutlineButton("Đóng", 96, 34);
            apply.SetBounds(card.Width - 224, card.Height - 52, 110, 34);
            close.SetBounds(card.Width - 106, card.Height - 52, 96, 34);
            card.Controls.Add(apply);
            card.Controls.Add(close);

            close.Click += (_, _) => dialog.Close();
            apply.Click += (_, _) =>
            {
                if (dialogFrom.Value.Date > dialogTo.Value.Date)
                {
                    MessageBox.Show(dialog, "Từ ngày không được lớn hơn Đến ngày.", "Bộ lọc nâng cao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dialogFrom.Value = SafeLogDatePickerValue(dialogFrom, dialogTo.Value);
                    return;
                }

                syncingFilters = true;
                fromPicker.Value = SafeLogDatePickerValue(fromPicker, dialogFrom.Value);
                toPicker.Value = SafeLogDatePickerValue(toPicker, dialogTo.Value);
                SelectComboValue(userCombo, SelectedComboText(dialogUser));
                SelectComboValue(actionCombo, SelectedComboText(dialogAction));
                SelectComboValue(levelCombo, SelectedComboText(dialogLevel));
                SelectComboValue(moduleCombo, SelectedComboText(dialogModule));
                SelectComboValue(ipCombo, SelectedComboText(dialogIp));
                syncingFilters = false;
                RefreshSystemLogView(resetPage: true);
                dialog.Close();
            };

            dialog.ShowDialog(this);
        }

        void LayoutSystemLogs()
        {
            int width = PageWorkWidth();
            int x = 18;
            int y = 68;
            int gap = 12;

            filterPanel.SetBounds(x, y, width, LayoutLogFilterPanel(width));
            y += filterPanel.Height + gap;

            if (emptyState.Visible)
            {
                emptyState.SetBounds(x, y, width, 72);
                emptyIcon.SetBounds(0, 8, emptyState.Width, 28);
                emptyText.SetBounds(12, 38, emptyState.Width - 24, 24);
                y += emptyState.Height + gap;
            }

            if (width >= 1180)
            {
                int mainHeight = width >= 1500 ? 500 : 472;
                int detailW = Math.Max(360, Math.Min(440, width / 4));
                int listW = width - detailW - gap;
                listSection.SetBounds(x, y, listW, mainHeight);
                detailSection.SetBounds(listSection.Right + gap, y, detailW, mainHeight);
                y += mainHeight + gap;
            }
            else
            {
                listSection.SetBounds(x, y, width, 486);
                detailSection.SetBounds(x, listSection.Bottom + gap, width, 474);
                y = detailSection.Bottom + gap;
            }

            LayoutLogListChildren();
            LayoutLogDetailChildren();

            if (width >= 1180)
            {
                const int analyticsHeight = 224;
                int topUsersW = Math.Max(280, width / 5);
                int statsW = Math.Max(360, width / 4);
                int chartW = (width - statsW - topUsersW - gap * 3) / 2;
                statsSection.SetBounds(x, y, statsW, analyticsHeight);
                severitySection.SetBounds(statsSection.Right + gap, y, chartW, analyticsHeight);
                moduleSection.SetBounds(severitySection.Right + gap, y, chartW, analyticsHeight);
                topUsersSection.SetBounds(moduleSection.Right + gap, y, topUsersW, analyticsHeight);
                y += analyticsHeight + gap;

                int rightW = Math.Max(380, width / 3);
                int leftW = width - rightW - gap;
                recentIssueSection.SetBounds(x, y, leftW, 260);
                timeSection.SetBounds(recentIssueSection.Right + gap, y, rightW, 260);
                autoSection.SetBounds(timeSection.Left, timeSection.Bottom + gap, rightW, 154);
                y = autoSection.Bottom + 24;
            }
            else
            {
                statsSection.SetBounds(x, y, width, 220);
                y += statsSection.Height + gap;
                int half = (width - gap) / 2;
                if (width >= 760)
                {
                    severitySection.SetBounds(x, y, half, 224);
                    moduleSection.SetBounds(severitySection.Right + gap, y, half, 224);
                    y += 224 + gap;
                }
                else
                {
                    severitySection.SetBounds(x, y, width, 224);
                    moduleSection.SetBounds(x, severitySection.Bottom + gap, width, 224);
                    y = moduleSection.Bottom + gap;
                }

                topUsersSection.SetBounds(x, y, width, 210);
                y += topUsersSection.Height + gap;
                recentIssueSection.SetBounds(x, y, width, 260);
                y += recentIssueSection.Height + gap;
                timeSection.SetBounds(x, y, width, 246);
                y += timeSection.Height + gap;
                autoSection.SetBounds(x, y, width, 154);
                y += autoSection.Height + 24;
            }

            LayoutStatsCards();
            LayoutChartSections();
            LayoutTopUsersSection();
            LayoutAutoSection();
            page.AutoScrollMinSize = new Size(0, y);
        }

        int LayoutLogFilterPanel(int width)
        {
            const int pad = 14;
            const int gap = 12;
            int available = Math.Max(320, width - pad * 2);
            int columns = available >= 1180 ? 7 : available >= 980 ? 4 : available >= 680 ? 3 : available >= 500 ? 2 : 1;
            int fieldWidth = Math.Max(136, (available - gap * (columns - 1)) / columns);

            for (int i = 0; i < filterFields.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                int left = pad + col * (fieldWidth + gap);
                int top = 12 + row * 56;
                filterFields[i].Label.SetBounds(left, top, fieldWidth, 18);
                filterFields[i].Input.SetBounds(left, top + 22, fieldWidth, 32);
            }

            int fieldRows = (int)Math.Ceiling(filterFields.Count / (double)columns);
            int searchTop = 12 + fieldRows * 56 + 10;
            var buttons = new[] { refreshButton, advancedButton, excelButton, pdfButton, clearFilterButton, detailButton };
            int totalButtonWidth = buttons.Sum(b => b.Width) + gap * (buttons.Length - 1);

            searchLabel.SetBounds(pad, searchTop, 140, 18);
            if (available >= 920)
            {
                int searchW = Math.Max(260, available - totalButtonWidth - gap);
                searchBox.SetBounds(pad, searchTop + 22, searchW, 32);
                int buttonX = width - pad - totalButtonWidth;
                foreach (var button in buttons)
                {
                    button.SetBounds(buttonX, searchTop + 21, button.Width, 36);
                    buttonX += button.Width + gap;
                }

                return searchTop + 70;
            }

            searchBox.SetBounds(pad, searchTop + 22, available, 32);
            int buttonY = searchTop + 66;
            int wrapButtonX = pad;
            foreach (var button in buttons)
            {
                if (wrapButtonX > pad && wrapButtonX + button.Width > width - pad)
                {
                    wrapButtonX = pad;
                    buttonY += 42;
                }

                button.SetBounds(wrapButtonX, buttonY, button.Width, 36);
                wrapButtonX += button.Width + gap;
            }

            return buttonY + 50;
        }

        void LayoutLogListChildren()
        {
            const int sidePad = 16;
            const int top = 44;
            const int pagerH = 34;
            const int pagerGap = 12;
            int pagerTop = listSection.Height - pagerH - 14;
            int gridHeight = Math.Max(210, pagerTop - top - pagerGap);
            grid.SetBounds(sidePad, top, Math.Max(180, listSection.Width - sidePad * 2), gridHeight);
            ConfigureSystemLogGrid(grid, includeHiddenId: true);

            pagerLabel.SetBounds(sidePad + 2, pagerTop + 2, Math.Max(170, listSection.Width - 500), 28);

            int right = listSection.Width - sidePad;
            pageSizeCombo.SetBounds(right - pageSizeCombo.Width, pagerTop + 1, pageSizeCombo.Width, 30);
            right = pageSizeCombo.Left - 12;
            lastButton.SetBounds(right - lastButton.Width, pagerTop, lastButton.Width, 30);
            nextButton.SetBounds(lastButton.Left - nextButton.Width - 8, pagerTop, nextButton.Width, 30);
            previousButton.SetBounds(nextButton.Left - previousButton.Width - 8, pagerTop, previousButton.Width, 30);
            firstButton.SetBounds(previousButton.Left - firstButton.Width - 8, pagerTop, firstButton.Width, 30);
        }

        void LayoutLogDetailChildren()
        {
            int pad = 16;
            int labelW = detailSection.Width < 420 ? 112 : 132;
            int inputX = pad + labelW + 10;
            int inputW = Math.Max(140, detailSection.Width - inputX - pad);
            int y = 44;
            foreach (var row in detailRows)
            {
                row.Caption.SetBounds(pad, y, labelW, 28);
                row.Value.SetBounds(inputX, y, inputW, 26);
                y += 34;
            }

            int noteH = 58;
            int noteTop = detailSection.Height - noteH - 16;
            int contentTop = y + 8;
            int contentH = Math.Max(68, noteTop - contentTop - 34);
            contentLabel.SetBounds(pad, contentTop, labelW, 24);
            contentBox.SetBounds(inputX, contentTop, inputW, contentH);
            noteLabel.SetBounds(pad, noteTop, labelW, 24);
            noteBox.SetBounds(inputX, noteTop, inputW, noteH);
        }

        void LayoutStatsCards()
        {
            var metrics = new[] { totalMetric.Card, successMetric.Card, warningMetric.Card, errorMetric.Card };
            int pad = 16;
            int gap = 10;
            int columns = statsSection.Width >= 520 ? 4 : 2;
            int cardW = Math.Max(120, (statsSection.Width - pad * 2 - gap * (columns - 1)) / columns);
            int cardH = columns == 4
                ? Math.Max(122, statsSection.Height - 64)
                : Math.Max(76, (statsSection.Height - 64 - gap) / 2);
            for (int i = 0; i < metrics.Length; i++)
            {
                int row = i / columns;
                int col = i % columns;
                metrics[i].SetBounds(pad + col * (cardW + gap), 48 + row * (cardH + gap), cardW, cardH);
            }
        }

        void LayoutChartSections()
        {
            severityChart.SetBounds(12, 42, Math.Max(180, severitySection.Width - 24), Math.Max(150, severitySection.Height - 58));
            moduleChart.SetBounds(12, 42, Math.Max(180, moduleSection.Width - 24), Math.Max(150, moduleSection.Height - 58));
            issueGrid.SetBounds(14, 44, Math.Max(160, recentIssueSection.Width - 28), Math.Max(150, recentIssueSection.Height - 62));
            ConfigureSystemLogGrid(issueGrid, includeHiddenId: false);
            timeChart.SetBounds(12, 42, Math.Max(180, timeSection.Width - 24), Math.Max(140, timeSection.Height - 56));
        }

        void LayoutTopUsersSection()
        {
            topUsersBody.SetBounds(16, 44, Math.Max(120, topUsersSection.Width - 32), Math.Max(90, topUsersSection.Height - 58));
            RenderTopUsers();
        }

        void LayoutAutoSection()
        {
            int pad = 16;
            autoStateLabel.SetBounds(pad, 44, autoSection.Width - pad * 2, 24);
            autoLastLabel.SetBounds(pad, 72, autoSection.Width - pad * 2, 22);
            autoIntervalLabel.SetBounds(pad, 98, autoSection.Width - pad * 2, 22);

            if (autoSection.Width >= 360)
            {
                autoPauseButton.SetBounds(autoSection.Width - pad - autoPauseButton.Width, autoSection.Height - 48, autoPauseButton.Width, 36);
            }
            else
            {
                autoPauseButton.SetBounds(pad, autoSection.Height - 48, Math.Min(autoPauseButton.Width, autoSection.Width - pad * 2), 36);
            }
        }

        LayoutSystemLogs();
    }

    private void RenderSystemLogsLegacy()
    {
        const string allText = "Tất cả";
        const int pageSizeDefault = 20;
        string[] logColumns = { "ID", "Thời gian", "Người dùng", "Vai trò", "Module", "Hành động", "IP", "Mức độ", "Nội dung", "Thiết bị" };
        string[] securityColumns = { "Thời gian", "Nguồn", "Sự kiện", "Mức độ", "Trạng thái" };

        var page = BeginPage("Log hệ thống", "Dashboard / Log hệ thống");
        var allLogs = LoadSystemLogRows();
        var filteredLogs = new List<SystemLogView>();
        var pageRows = new List<SystemLogView>();
        var currentPage = 1;
        var pageSize = pageSizeDefault;
        SystemLogView selectedLog = null;
        bool syncingFilters = false;

        DateTime latestLogDate = allLogs.Count == 0 ? DateTime.Today : allLogs.Max(l => l.Timestamp.Date);
        DateTime earliestLogDate = allLogs.Count == 0 ? DateTime.Today.AddDays(-30) : allLogs.Min(l => l.Timestamp.Date);
        if (latestLogDate > DateTime.Today)
        {
            latestLogDate = DateTime.Today;
        }

        var filters = ModernUi.CardPanel();
        filters.Padding = Padding.Empty;
        page.Controls.Add(filters);

        var fromPicker = CreateLogDatePicker(earliestLogDate);
        var toPicker = CreateLogDatePicker(latestLogDate);
        var userCombo = ModernUi.ComboBox(Array.Empty<string>(), 180);
        var actionCombo = ModernUi.ComboBox(Array.Empty<string>(), 180);
        var levelCombo = ModernUi.ComboBox(Array.Empty<string>(), 150);
        var moduleCombo = ModernUi.ComboBox(Array.Empty<string>(), 170);
        var ipCombo = ModernUi.ComboBox(Array.Empty<string>(), 170);
        var searchBox = ModernUi.TextBox("Tìm kiếm thời gian, user, module, nội dung, IP...", 260);

        var filterFields = new List<(Label Label, Control Input, int PreferredWidth)>
        {
            AddLogFilterField(filters, "Từ ngày", fromPicker, 150),
            AddLogFilterField(filters, "Đến ngày", toPicker, 150),
            AddLogFilterField(filters, "Người dùng", userCombo, 180),
            AddLogFilterField(filters, "Hành động", actionCombo, 180),
            AddLogFilterField(filters, "Mức độ", levelCombo, 150),
            AddLogFilterField(filters, "Module", moduleCombo, 170),
            AddLogFilterField(filters, "IP", ipCombo, 170),
            AddLogFilterField(filters, "Tìm kiếm", searchBox, 280)
        };

        var excelButton = ModernUi.Button("Xuất Excel", ModernUi.Green, 108, 36);
        var pdfButton = ModernUi.Button("Xuất PDF", ModernUi.Red, 96, 36);
        var refreshButton = ModernUi.Button("Làm mới", ModernUi.Blue, 102, 36);
        var advancedButton = ModernUi.OutlineButton("Bộ lọc nâng cao", 156, 36);
        var clearFilterButton = ModernUi.OutlineButton("Xóa bộ lọc", 116, 36);
        var detailButton = ModernUi.Button("Xem chi tiết", ModernUi.Blue, 116, 36);
        filters.Controls.Add(excelButton);
        filters.Controls.Add(pdfButton);
        filters.Controls.Add(refreshButton);
        filters.Controls.Add(advancedButton);
        filters.Controls.Add(clearFilterButton);
        filters.Controls.Add(detailButton);

        var emptyLabel = ModernUi.Label("Không có log phù hợp", 9.2f, FontStyle.Bold, ModernUi.Red);
        emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        emptyLabel.Visible = false;
        page.Controls.Add(emptyLabel);

        var list = ModernUi.Section("Nhật ký hoạt động", 720, 480);
        var grid = CreateSystemLogGrid(logColumns);
        list.Controls.Add(grid);

        var pagerLabel = ModernUi.Label("", 9f, FontStyle.Regular, ModernUi.Text);
        var firstButton = ModernUi.OutlineButton("«", 34, 30);
        var previousButton = ModernUi.OutlineButton("‹", 34, 30);
        var nextButton = ModernUi.OutlineButton("›", 34, 30);
        var lastButton = ModernUi.OutlineButton("»", 34, 30);
        var pageSizeCombo = ModernUi.ComboBox(new[] { "10", "20", "50", "100" }, 76);
        pageSizeCombo.SelectedItem = pageSizeDefault.ToString(CultureInfo.InvariantCulture);
        list.Controls.Add(pagerLabel);
        list.Controls.Add(firstButton);
        list.Controls.Add(previousButton);
        list.Controls.Add(nextButton);
        list.Controls.Add(lastButton);
        list.Controls.Add(pageSizeCombo);
        page.Controls.Add(list);

        var detail = ModernUi.Section("Chi tiết log", 360, 480);
        var detailFields = new Dictionary<string, TextBox>
        {
            ["time"] = AddLogDetailField(detail, "Thời gian", 48),
            ["user"] = AddLogDetailField(detail, "Người dùng", 86),
            ["role"] = AddLogDetailField(detail, "Vai trò", 124),
            ["module"] = AddLogDetailField(detail, "Module", 162),
            ["action"] = AddLogDetailField(detail, "Hành động", 200),
            ["ip"] = AddLogDetailField(detail, "IP", 238),
            ["level"] = AddLogDetailField(detail, "Mức độ", 276),
            ["device"] = AddLogDetailField(detail, "Thiết bị", 314)
        };
        var contentLabel = ModernUi.Label("Nội dung", 8.8f, FontStyle.Regular, ModernUi.Text);
        var contentBox = new TextBox
        {
            ReadOnly = true,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = ModernUi.Font(9f),
            BorderStyle = BorderStyle.FixedSingle
        };
        detail.Controls.Add(contentLabel);
        detail.Controls.Add(contentBox);
        page.Controls.Add(detail);

        var security = ModernUi.Section("Cảnh báo bảo mật gần đây", 720, 210);
        var securityGrid = CreateSystemLogGrid(securityColumns, includeHiddenId: false);
        security.Controls.Add(securityGrid);
        page.Controls.Add(security);

        PopulateLogFilterCombos();
        WireLogEvents();
        RefreshSystemLogView(resetPage: true);

        void WireLogEvents()
        {
            fromPicker.ValueChanged += (_, _) =>
            {
                if (!syncingFilters)
                {
                    RefreshSystemLogView(resetPage: true);
                }
            };
            toPicker.ValueChanged += (_, _) =>
            {
                if (!syncingFilters)
                {
                    RefreshSystemLogView(resetPage: true);
                }
            };
            userCombo.SelectedIndexChanged += (_, _) => RefreshSystemLogView(resetPage: true);
            actionCombo.SelectedIndexChanged += (_, _) => RefreshSystemLogView(resetPage: true);
            levelCombo.SelectedIndexChanged += (_, _) => RefreshSystemLogView(resetPage: true);
            moduleCombo.SelectedIndexChanged += (_, _) => RefreshSystemLogView(resetPage: true);
            ipCombo.SelectedIndexChanged += (_, _) => RefreshSystemLogView(resetPage: true);
            searchBox.TextChanged += (_, _) => RefreshSystemLogView(resetPage: true);

            excelButton.Click += (_, _) => ExportSystemLogs("Excel");
            pdfButton.Click += (_, _) => ExportSystemLogs("PDF");
            refreshButton.Click += (_, _) =>
            {
                allLogs = LoadSystemLogRows();
                PopulateLogFilterCombos();
                RefreshSystemLogView(resetPage: true);
                MessageBox.Show(this, "Dữ liệu log đã được làm mới.", "Log hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            advancedButton.Click += (_, _) =>
            {
                MessageBox.Show(this,
                    "Bộ lọc nâng cao đang dùng các điều kiện:\n\n" +
                    "• Khoảng thời gian\n" +
                    "• Người dùng\n" +
                    "• Hành động\n" +
                    "• Mức độ\n" +
                    "• Module\n" +
                    "• IP\n" +
                    "• Tìm kiếm",
                    "Bộ lọc nâng cao",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };
            clearFilterButton.Click += (_, _) => ClearLogFilters();
            detailButton.Click += (_, _) => ShowSelectedLogDetail();

            firstButton.Click += (_, _) =>
            {
                currentPage = 1;
                RefreshSystemLogView(resetPage: false);
            };
            previousButton.Click += (_, _) =>
            {
                currentPage = Math.Max(1, currentPage - 1);
                RefreshSystemLogView(resetPage: false);
            };
            nextButton.Click += (_, _) =>
            {
                currentPage = Math.Min(TotalPages(), currentPage + 1);
                RefreshSystemLogView(resetPage: false);
            };
            lastButton.Click += (_, _) =>
            {
                currentPage = TotalPages();
                RefreshSystemLogView(resetPage: false);
            };
            pageSizeCombo.SelectedIndexChanged += (_, _) =>
            {
                pageSize = ParsePageSize(pageSizeCombo.SelectedItem, pageSizeDefault);
                RefreshSystemLogView(resetPage: true);
            };
            grid.SelectionChanged += (_, _) =>
            {
                var selected = SelectedGridLog();
                if (selected != null)
                {
                    selectedLog = selected;
                    UpdateLogDetail(selectedLog);
                }
            };
            grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    ShowSelectedLogDetail();
                }
            };
            page.Resize += (_, _) => LayoutSystemLogs();
        }

        void PopulateLogFilterCombos()
        {
            SetComboItems(userCombo, allLogs.Select(l => l.Username).Where(v => !string.IsNullOrWhiteSpace(v)));
            SetComboItems(actionCombo, allLogs.Select(l => l.Action).Where(v => !string.IsNullOrWhiteSpace(v)));
            SetComboItems(levelCombo, allLogs.Select(l => l.Level).Where(v => !string.IsNullOrWhiteSpace(v)));
            SetComboItems(moduleCombo, allLogs.Select(l => l.Module).Where(v => !string.IsNullOrWhiteSpace(v)));
            SetComboItems(ipCombo, allLogs.Select(l => l.IPAddress).Where(v => !string.IsNullOrWhiteSpace(v)));
        }

        void SetComboItems(ComboBox combo, IEnumerable<string> values)
        {
            string selected = SelectedComboText(combo);
            var items = values
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            items.Insert(0, allText);

            syncingFilters = true;
            combo.BeginUpdate();
            combo.Items.Clear();
            combo.Items.AddRange(items.Cast<object>().ToArray());
            int selectedIndex = items.FindIndex(v => string.Equals(v, selected, StringComparison.CurrentCultureIgnoreCase));
            combo.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            combo.EndUpdate();
            syncingFilters = false;
        }

        void ClearLogFilters()
        {
            syncingFilters = true;
            fromPicker.Value = SafeLogDatePickerValue(fromPicker, earliestLogDate);
            toPicker.Value = SafeLogDatePickerValue(toPicker, latestLogDate);
            foreach (var combo in new[] { userCombo, actionCombo, levelCombo, moduleCombo, ipCombo })
            {
                if (combo.Items.Count > 0)
                {
                    combo.SelectedIndex = 0;
                }
            }
            searchBox.Text = string.Empty;
            syncingFilters = false;
            RefreshSystemLogView(resetPage: true);
        }

        void RefreshSystemLogView(bool resetPage)
        {
            if (syncingFilters)
            {
                return;
            }

            if (!ValidateLogDateRange())
            {
                return;
            }

            if (resetPage)
            {
                currentPage = 1;
            }

            filteredLogs = ApplyLogFilters().ToList();
            currentPage = Math.Max(1, Math.Min(currentPage, TotalPages()));
            int start = (currentPage - 1) * pageSize;
            pageRows = filteredLogs.Skip(start).Take(pageSize).ToList();

            SetGridData(grid, logColumns, BuildLogRows(pageRows));
            ConfigureSystemLogGrid(grid, includeHiddenId: true);

            var securityRows = filteredLogs
                .Where(l => !string.Equals(l.Level, "Thông tin", StringComparison.CurrentCultureIgnoreCase))
                .Take(20)
                .ToList();
            SetGridData(securityGrid, securityColumns, BuildSecurityRows(securityRows));
            ConfigureSystemLogGrid(securityGrid, includeHiddenId: false);

            emptyLabel.Visible = filteredLogs.Count == 0;
            selectedLog = pageRows.FirstOrDefault();
            UpdateLogDetail(selectedLog);
            UpdatePager();
            LayoutSystemLogs();
        }

        bool ValidateLogDateRange()
        {
            DateTime from = fromPicker.Value.Date;
            DateTime to = toPicker.Value.Date;
            if (from <= to)
            {
                return true;
            }

            MessageBox.Show(this,
                "Từ ngày không được lớn hơn Đến ngày. Hệ thống sẽ tự reset về khoảng ngày hợp lệ gần nhất.",
                "Bộ lọc thời gian",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            syncingFilters = true;
            fromPicker.Value = SafeLogDatePickerValue(fromPicker, to);
            syncingFilters = false;
            return true;
        }

        IEnumerable<SystemLogView> ApplyLogFilters()
        {
            DateTime from = fromPicker.Value.Date;
            DateTime toInclusive = toPicker.Value.Date.AddDays(1).AddTicks(-1);
            string selectedUser = SelectedComboText(userCombo);
            string selectedAction = SelectedComboText(actionCombo);
            string selectedLevel = SelectedComboText(levelCombo);
            string selectedModule = SelectedComboText(moduleCombo);
            string selectedIp = SelectedComboText(ipCombo);
            string keyword = searchBox.Text.Trim();

            return allLogs
                .Where(l => l.Timestamp >= from && l.Timestamp <= toInclusive)
                .Where(l => selectedUser == allText || string.Equals(l.Username, selectedUser, StringComparison.CurrentCultureIgnoreCase))
                .Where(l => selectedAction == allText || string.Equals(l.Action, selectedAction, StringComparison.CurrentCultureIgnoreCase))
                .Where(l => selectedLevel == allText || string.Equals(l.Level, selectedLevel, StringComparison.CurrentCultureIgnoreCase))
                .Where(l => selectedModule == allText || string.Equals(l.Module, selectedModule, StringComparison.CurrentCultureIgnoreCase))
                .Where(l => selectedIp == allText || string.Equals(l.IPAddress, selectedIp, StringComparison.CurrentCultureIgnoreCase))
                .Where(l => string.IsNullOrWhiteSpace(keyword) || LogContainsKeyword(l, keyword))
                .OrderByDescending(l => l.Timestamp);
        }

        bool LogContainsKeyword(SystemLogView log, string keyword)
        {
            return DateTimeText(log.Timestamp).Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Username.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Role.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Action.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Module.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Description.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.IPAddress.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Level.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                   log.Device.Contains(keyword, StringComparison.CurrentCultureIgnoreCase);
        }

        int TotalPages()
            => Math.Max(1, (int)Math.Ceiling(filteredLogs.Count / (double)Math.Max(1, pageSize)));

        void UpdatePager()
        {
            int total = filteredLogs.Count;
            int start = total == 0 ? 0 : (currentPage - 1) * pageSize + 1;
            int end = Math.Min(total, currentPage * pageSize);
            pagerLabel.Text = $"Hiển thị {start:N0} - {end:N0} / {total:N0} dòng log";
            firstButton.Enabled = currentPage > 1;
            previousButton.Enabled = currentPage > 1;
            nextButton.Enabled = currentPage < TotalPages() && total > 0;
            lastButton.Enabled = currentPage < TotalPages() && total > 0;
            detailButton.Enabled = selectedLog != null;
        }

        SystemLogView SelectedGridLog()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.IsNewRow || grid.CurrentRow.Cells.Count == 0)
            {
                return null;
            }

            string rawId = Convert.ToString(grid.CurrentRow.Cells[0].Value) ?? "";
            return int.TryParse(rawId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
                ? filteredLogs.FirstOrDefault(l => l.LogID == id)
                : null;
        }

        void ShowSelectedLogDetail()
        {
            var selected = SelectedGridLog() ?? selectedLog;
            if (selected == null)
            {
                MessageBox.Show(this, "Không có log phù hợp để xem chi tiết.", "Chi tiết log", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            selectedLog = selected;
            UpdateLogDetail(selectedLog);
            MessageBox.Show(this,
                $"Thời gian: {DateTimeText(selected.Timestamp)}\n" +
                $"User: {selected.Username}\n" +
                $"Vai trò: {selected.Role}\n" +
                $"Module: {selected.Module}\n" +
                $"Hành động: {selected.Action}\n" +
                $"IP: {selected.IPAddress}\n" +
                $"Mức độ: {selected.Level}\n" +
                $"Thiết bị: {selected.Device}\n\n" +
                $"Nội dung:\n{selected.Description}",
                "Chi tiết log",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        void UpdateLogDetail(SystemLogView log)
        {
            detailFields["time"].Text = log == null ? "" : DateTimeText(log.Timestamp);
            detailFields["user"].Text = log == null ? "" : log.Username;
            detailFields["role"].Text = log == null ? "" : log.Role;
            detailFields["module"].Text = log == null ? "" : log.Module;
            detailFields["action"].Text = log == null ? "" : log.Action;
            detailFields["ip"].Text = log == null ? "" : log.IPAddress;
            detailFields["level"].Text = log == null ? "" : log.Level;
            detailFields["device"].Text = log == null ? "" : log.Device;
            contentBox.Text = log == null ? "" : log.Description;
        }

        void ExportSystemLogs(string format)
        {
            var result = format == "Excel"
                ? BuildSystemLogsExcel(filteredLogs)
                : BuildSystemLogsPdf(filteredLogs);
            string filter = format == "Excel" ? "Excel Workbook (*.xlsx)|*.xlsx" : "PDF (*.pdf)|*.pdf";
            SaveGeneratedFile(result, filter, $"SystemLogs{format}Export");
        }

        void LayoutSystemLogs()
        {
            int width = PageWorkWidth();
            int x = 18;
            int y = 76;

            filters.SetBounds(x, y, width, LayoutLogFilterPanel(width));
            y += filters.Height + 12;

            if (emptyLabel.Visible)
            {
                emptyLabel.SetBounds(x, y, width, 28);
                y += 40;
            }

            int gap = 12;
            if (width >= 1120)
            {
                int leftW = Math.Max(680, (int)(width * 0.68));
                int rightW = width - leftW - gap;
                list.SetBounds(x, y, leftW, 492);
                detail.SetBounds(list.Right + gap, y, rightW, 492);
                y += 504;
            }
            else
            {
                list.SetBounds(x, y, width, 492);
                detail.SetBounds(x, list.Bottom + gap, width, 430);
                y = detail.Bottom + gap;
            }

            LayoutLogListChildren();
            LayoutLogDetailChildren();

            security.SetBounds(x, y, width, 220);
            securityGrid.SetBounds(12, 44, Math.Max(120, security.Width - 24), Math.Max(120, security.Height - 62));
            y += security.Height + 24;
            page.AutoScrollMinSize = new Size(0, y);
        }

        int LayoutLogFilterPanel(int width)
        {
            const int pad = 14;
            const int gap = 12;
            int available = Math.Max(320, width - pad * 2);
            int columns = available >= 1180 ? 4 : available >= 860 ? 3 : available >= 560 ? 2 : 1;
            int fieldWidth = Math.Max(138, (available - gap * (columns - 1)) / columns);

            for (int i = 0; i < filterFields.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                int left = pad + col * (fieldWidth + gap);
                int top = 12 + row * 56;
                filterFields[i].Label.SetBounds(left, top, fieldWidth, 18);
                filterFields[i].Input.SetBounds(left, top + 22, Math.Min(fieldWidth, Math.Max(120, filterFields[i].PreferredWidth)), 32);
            }

            int fieldRows = (int)Math.Ceiling(filterFields.Count / (double)columns);
            int buttonY = 12 + fieldRows * 56 + 8;
            var buttons = new[] { advancedButton, clearFilterButton, excelButton, pdfButton, refreshButton, detailButton };
            int totalWidth = buttons.Sum(b => b.Width) + gap * (buttons.Length - 1);
            int buttonX = Math.Max(pad, width - pad - totalWidth);
            foreach (var button in buttons)
            {
                button.SetBounds(buttonX, buttonY, button.Width, 36);
                buttonX += button.Width + gap;
            }

            return buttonY + 50;
        }

        void LayoutLogListChildren()
        {
            grid.SetBounds(12, 44, Math.Max(120, list.Width - 24), Math.Max(160, list.Height - 96));
            int pagerTop = list.Height - 42;
            pagerLabel.SetBounds(18, pagerTop, Math.Max(160, list.Width - 420), 30);

            int right = list.Width - 18;
            pageSizeCombo.SetBounds(right - pageSizeCombo.Width, pagerTop, pageSizeCombo.Width, 30);
            right = pageSizeCombo.Left - 10;
            lastButton.SetBounds(right - 34, pagerTop, 34, 30);
            nextButton.SetBounds(lastButton.Left - 40, pagerTop, 34, 30);
            previousButton.SetBounds(nextButton.Left - 40, pagerTop, 34, 30);
            firstButton.SetBounds(previousButton.Left - 40, pagerTop, 34, 30);
        }

        void LayoutLogDetailChildren()
        {
            int labelW = detail.Width < 420 ? 88 : 104;
            int inputX = 16 + labelW + 10;
            int inputW = Math.Max(120, detail.Width - inputX - 16);
            foreach (Control control in detail.Controls)
            {
                if (control.Tag is int top)
                {
                    if (control is Label label)
                    {
                        label.SetBounds(16, top, labelW, 28);
                    }
                    else
                    {
                        control.SetBounds(inputX, top, inputW, 28);
                    }
                }
            }

            int contentTop = detail.Height < 460 ? 350 : 356;
            contentLabel.SetBounds(16, contentTop, labelW, 24);
            contentBox.SetBounds(inputX, contentTop, inputW, Math.Max(58, detail.Height - contentTop - 22));
        }

        LayoutSystemLogs();
    }

    private List<SystemLogView> LoadSystemLogRows()
    {
        var users = UserDAL.GetAllUsers();
        var userById = users.ToDictionary(u => u.UserID);
        var userByName = users
            .Where(u => !string.IsNullOrWhiteSpace(u.Username))
            .GroupBy(u => u.Username)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.CurrentCultureIgnoreCase);

        var rows = new List<SystemLogView>();
        foreach (dynamic raw in AuditLogDAL.GetAuditLogs(limit: 5000))
        {
            object userIdValue = DynamicProperty(raw, "UserID");
            bool hasUserId = userIdValue is int;
            int userId = hasUserId ? (int)userIdValue : 0;
            string username = TextValue(DynamicProperty(raw, "Username"), "system");
            UserDTO user = null;
            if (hasUserId)
            {
                userById.TryGetValue(userId, out user);
            }
            if (user == null && !string.IsNullOrWhiteSpace(username))
            {
                userByName.TryGetValue(username, out user);
            }

            string action = TextValue(DynamicProperty(raw, "Action"), "-");
            string module = TextValue(DynamicProperty(raw, "EntityName"), "-");
            string ip = TextValue(DynamicProperty(raw, "IPAddress"), "-");
            DateTime timestamp = DynamicProperty(raw, "Timestamp") is DateTime value ? value : DateTime.MinValue;
            rows.Add(new SystemLogView
            {
                LogID = DynamicProperty(raw, "LogID") is int logId ? logId : 0,
                UserID = userId,
                HasUserID = hasUserId,
                Username = username,
                Role = Display(user?.RoleName, "-"),
                Action = action,
                Module = module,
                Description = TextValue(DynamicProperty(raw, "Description"), $"{action} {module}"),
                IPAddress = ip,
                Level = AuditLevel(action),
                Device = ip == Environment.MachineName ? Environment.MachineName : Display(ip, Environment.MachineName),
                Timestamp = timestamp
            });
        }

        return rows
            .Where(r => r.Timestamp > DateTime.MinValue)
            .OrderByDescending(r => r.Timestamp)
            .ToList();

        static object DynamicProperty(object source, string propertyName)
        {
            if (source == null)
            {
                return null;
            }

            var property = source.GetType().GetProperty(propertyName);
            return property == null ? null : property.GetValue(source);
        }

        static string TextValue(object value, string fallback)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }
    }

    private static (Label Label, Control Input, int PreferredWidth) AddLogFilterField(Control parent, string title, Control input, int preferredWidth)
    {
        var label = ModernUi.Label(title, 8.6f, FontStyle.Bold, ModernUi.Text);
        label.AutoEllipsis = true;
        parent.Controls.Add(label);
        parent.Controls.Add(input);
        return (label, input, preferredWidth);
    }

    private static void AddDialogFilterField(Control parent, string title, Control input, int x, int y)
    {
        var label = ModernUi.Label(title, 8.5f, FontStyle.Bold, ModernUi.Text);
        label.SetBounds(x, y, 230, 18);
        input.SetBounds(x, y + 22, 220, 32);
        parent.Controls.Add(label);
        parent.Controls.Add(input);
    }

    private static TextBox CreateReadonlyLogTextBox()
    {
        return new TextBox
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = ModernUi.Text,
            Font = ModernUi.Font(8.9f)
        };
    }

    private static TextBox CreateReadonlyLogBox()
    {
        return new TextBox
        {
            ReadOnly = true,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = ModernUi.Text,
            Font = ModernUi.Font(8.9f)
        };
    }

    private static Label CreateLogBadge(string text)
    {
        var badge = ModernUi.Badge(text, ModernUi.Blue);
        badge.Font = ModernUi.Font(8.4f, FontStyle.Bold);
        ApplyLogBadgeStyle(badge, text);
        return badge;
    }

    private static void ApplyLogBadgeStyle(Label badge, string level)
    {
        Color fore = LogLevelColor(level);
        Color back = level switch
        {
            "Lỗi" => Color.FromArgb(255, 235, 235),
            "Cảnh báo" => Color.FromArgb(255, 244, 224),
            "Thành công" => Color.FromArgb(232, 248, 235),
            _ => Color.FromArgb(232, 241, 255)
        };

        badge.ForeColor = fore;
        badge.BackColor = back;
        badge.TextAlign = ContentAlignment.MiddleCenter;
    }

    private static Color LogLevelColor(string level)
    {
        return level switch
        {
            "Lỗi" => ModernUi.Red,
            "Cảnh báo" => ModernUi.Orange,
            "Thành công" => ModernUi.Green,
            _ => ModernUi.Blue
        };
    }

    private static bool IsWarningOrError(string level)
        => string.Equals(level, "Cảnh báo", StringComparison.CurrentCultureIgnoreCase) ||
           string.Equals(level, "Lỗi", StringComparison.CurrentCultureIgnoreCase);

    private static decimal Percent(int numerator, int denominator)
        => denominator <= 0 ? 0m : numerator * 100m / denominator;

    private static (RoundedPanel Card, Label Value, Label Detail) CreateLogMetricCard(Control parent, string title, string iconText, Color accent)
    {
        var card = ModernUi.CardPanel(10);
        card.Padding = Padding.Empty;

        var icon = new CircleLabel
        {
            Text = iconText,
            CircleColor = Color.FromArgb(238, 245, 255),
            ForeColor = accent,
            Font = ModernUi.Font(13f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(icon);

        var titleLabel = ModernUi.Label(title, 8.2f, FontStyle.Bold, ModernUi.Muted);
        titleLabel.AutoEllipsis = true;
        card.Controls.Add(titleLabel);

        var valueLabel = ModernUi.Label("0", 17.5f, FontStyle.Bold, accent);
        valueLabel.AutoEllipsis = true;
        card.Controls.Add(valueLabel);

        var detailLabel = ModernUi.Label("", 7.8f, FontStyle.Regular, ModernUi.Muted);
        detailLabel.AutoEllipsis = true;
        card.Controls.Add(detailLabel);

        void LayoutMetricCard()
        {
            int iconSize = card.Width < 150 ? 38 : 44;
            icon.CircleColor = ControlPaint.Light(accent, 0.82f);
            icon.SetBounds(12, Math.Max(8, (card.Height - iconSize) / 2), iconSize, iconSize);
            int textLeft = icon.Right + 10;
            int textWidth = Math.Max(68, card.Width - textLeft - 10);
            titleLabel.SetBounds(textLeft, 8, textWidth, 18);
            valueLabel.Font = ModernUi.Font(card.Width < 145 ? 14.2f : 17.5f, FontStyle.Bold);
            valueLabel.SetBounds(textLeft, 28, textWidth, 30);
            detailLabel.SetBounds(textLeft, card.Height - 24, textWidth, 18);
        }

        card.Resize += (_, _) => LayoutMetricCard();
        parent.Controls.Add(card);
        LayoutMetricCard();
        return (card, valueLabel, detailLabel);
    }

    private static void UpdateMetric((RoundedPanel Card, Label Value, Label Detail) metric, int value, string detail)
    {
        metric.Value.Text = value.ToString("N0", CultureInfo.InvariantCulture);
        metric.Detail.Text = detail;
    }

    private static DateTimePicker CreateLogDatePicker(DateTime value)
    {
        var picker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            MinDate = new DateTime(2000, 1, 1),
            MaxDate = DateTime.Today,
            Font = ModernUi.Font(9.3f),
            Width = 150,
            Height = 32,
            ShowUpDown = false
        };
        picker.Value = SafeLogDatePickerValue(picker, value);
        return picker;
    }

    private static DateTime SafeLogDatePickerValue(DateTimePicker picker, DateTime value)
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

    private static string SelectedComboText(ComboBox combo)
        => combo.SelectedItem?.ToString() ?? "Tất cả";

    private static TextBox AddLogDetailField(Control parent, string labelText, int y)
    {
        var label = ModernUi.Label(labelText, 8.8f, FontStyle.Regular, ModernUi.Text);
        label.Tag = y;
        parent.Controls.Add(label);

        var input = new TextBox
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            Font = ModernUi.Font(9f),
            Tag = y
        };
        parent.Controls.Add(input);
        return input;
    }

    private static DataGridView CreateSystemLogGrid(string[] columns, bool includeHiddenId = true)
    {
        var grid = ModernUi.Grid();
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ScrollBars = ScrollBars.Both;
        grid.RowHeadersVisible = false;
        grid.ShowCellToolTips = true;
        grid.AllowUserToResizeColumns = true;
        grid.AllowUserToResizeRows = false;
        grid.RowTemplate.Height = 34;
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = ModernUi.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = ModernUi.Font(8.4f, FontStyle.Bold);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(190, 219, 255);
        grid.DefaultCellStyle.SelectionForeColor = ModernUi.Navy;
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                e.ToolTipText = Convert.ToString(grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) ?? string.Empty;
            }
        };
        grid.CellFormatting += (_, e) =>
        {
            ApplyGridCellStyle(e);
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Value is not string text)
            {
                return;
            }

            string header = grid.Columns[e.ColumnIndex].HeaderText;
            int maxLength = header switch
            {
                "Nội dung" or "Sự kiện" => 86,
                "Hành động" => 54,
                "Thiết bị" => 46,
                _ => 0
            };

            if (maxLength <= 0)
            {
                return;
            }

            string singleLine = text.Replace("\r", " ").Replace("\n", " ").Trim();
            if (singleLine.Length <= maxLength)
            {
                return;
            }

            e.Value = $"{singleLine[..Math.Max(0, maxLength - 3)]}...";
            e.FormattingApplied = true;
        };
        grid.CellMouseEnter += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < grid.Rows.Count && !grid.Rows[e.RowIndex].Selected)
            {
                grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(241, 246, 255);
            }
        };
        grid.CellMouseLeave += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < grid.Rows.Count && !grid.Rows[e.RowIndex].Selected)
            {
                grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = e.RowIndex % 2 == 0
                    ? Color.White
                    : Color.FromArgb(250, 252, 255);
            }
        };
        SetGridData(grid, columns, new[] { EmptyRow(columns.Length, includeHiddenId ? "Không có dữ liệu log phù hợp" : "Không có cảnh báo hoặc lỗi") });
        ConfigureSystemLogGrid(grid, includeHiddenId);
        return grid;
    }

    private static void ConfigureSystemLogGrid(DataGridView grid, bool includeHiddenId)
    {
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ScrollBars = ScrollBars.Both;
        grid.RowHeadersVisible = false;
        if (includeHiddenId && grid.Columns.Count > 0)
        {
            grid.Columns[0].Visible = false;
            grid.Columns[0].MinimumWidth = 5;
            grid.Columns[0].Width = 5;
        }

        int visibleMinimumWidth = 0;
        for (int i = 0; i < grid.Columns.Count; i++)
        {
            var column = grid.Columns[i];
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            string header = column.HeaderText;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            column.MinimumWidth = header switch
            {
                "ID" when includeHiddenId => 5,
                "Thời gian" => 128,
                "Người dùng" => 112,
                "Vai trò" => 104,
                "Module" => 106,
                "Hành động" => 138,
                "IP" or "Nguồn" => 116,
                "Mức độ" or "Trạng thái" => 98,
                "Thiết bị" => 142,
                "Nội dung" or "Sự kiện" => 268,
                _ => 86
            };
            column.Width = column.MinimumWidth;
            column.DefaultCellStyle.Alignment = header is "Nội dung" or "Sự kiện" or "Hành động" or "Module"
                ? DataGridViewContentAlignment.MiddleLeft
                : DataGridViewContentAlignment.MiddleCenter;
            column.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            if (column.Visible)
            {
                visibleMinimumWidth += column.Width;
            }
        }

        int extra = grid.ClientSize.Width - visibleMinimumWidth - SystemInformation.VerticalScrollBarWidth - 4;
        if (extra > 0)
        {
            DataGridViewColumn contentColumn = grid.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(c => c.Visible && (c.HeaderText == "Nội dung" || c.HeaderText == "Sự kiện"));
            DataGridViewColumn deviceColumn = grid.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(c => c.Visible && c.HeaderText == "Thiết bị");

            if (contentColumn != null)
            {
                int contentExtra = Math.Min(extra, Math.Max(0, extra * 2 / 3));
                contentColumn.Width += contentExtra;
                extra -= contentExtra;
            }

            if (deviceColumn != null && extra > 0)
            {
                deviceColumn.Width += extra;
                extra = 0;
            }

            if (extra > 0 && grid.Columns.Count > 0)
            {
                var lastVisible = grid.Columns.Cast<DataGridViewColumn>().LastOrDefault(c => c.Visible);
                if (lastVisible != null)
                {
                    lastVisible.Width += extra;
                }
            }
        }

        grid.ClearSelection();
    }

    private static object[][] BuildLogRows(IEnumerable<SystemLogView> logs)
    {
        var rows = logs.Select(log => new object[]
        {
            log.LogID,
            DateTimeText(log.Timestamp),
            log.Username,
            log.Role,
            log.Module,
            log.Action,
            log.IPAddress,
            log.Level,
            log.Description,
            log.Device
        }).ToArray();

        return rows.Length > 0 ? rows : new[] { EmptyRow(10, "Không có log phù hợp") };
    }

    private static object[][] BuildSecurityRows(IEnumerable<SystemLogView> logs)
    {
        var rows = logs.Select(log => new object[]
        {
            DateTimeText(log.Timestamp),
            log.IPAddress,
            log.Description,
            log.Level,
            "Đã ghi nhận"
        }).ToArray();

        return rows.Length > 0 ? rows : new[] { EmptyRow(5, "Không có cảnh báo bảo mật") };
    }

    private static object[][] BuildIssueRows(IEnumerable<SystemLogView> logs)
    {
        var rows = logs.Select(log => new object[]
        {
            DateTimeText(log.Timestamp),
            log.Level,
            log.Username,
            log.Action,
            log.Module,
            log.Description,
            log.IPAddress
        }).ToArray();

        return rows.Length > 0 ? rows : new[] { EmptyRow(7, "Không có cảnh báo hoặc lỗi") };
    }

    private (bool Success, string Message, byte[] FileContent, string FileName) BuildSystemLogsExcel(IReadOnlyList<SystemLogView> logs)
    {
        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Log he thong");
            worksheet.Cell(1, 1).Value = "Log hệ thống";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(1, 1, 1, 9).Merge();

            worksheet.Cell(2, 1).Value = "Ngày tạo";
            worksheet.Cell(2, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            worksheet.Cell(3, 1).Value = "Số dòng";
            worksheet.Cell(3, 2).Value = logs.Count;

            string[] headers = { "Thời gian", "Người dùng", "Vai trò", "Module", "Hành động", "IP", "Mức độ", "Nội dung", "Thiết bị" };
            int row = 5;
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(row, i + 1).Value = headers[i];
            }
            worksheet.Range(row, 1, row, headers.Length).Style.Font.Bold = true;

            foreach (var log in logs)
            {
                row++;
                worksheet.Cell(row, 1).Value = DateTimeText(log.Timestamp);
                worksheet.Cell(row, 2).Value = log.Username;
                worksheet.Cell(row, 3).Value = log.Role;
                worksheet.Cell(row, 4).Value = log.Module;
                worksheet.Cell(row, 5).Value = log.Action;
                worksheet.Cell(row, 6).Value = log.IPAddress;
                worksheet.Cell(row, 7).Value = log.Level;
                worksheet.Cell(row, 8).Value = log.Description;
                worksheet.Cell(row, 9).Value = log.Device;
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return (true, "Đã tạo file Excel.", stream.ToArray(), $"log-he-thong-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            return (false, $"Không thể tạo Excel log: {ex.Message}", Array.Empty<byte>(), "");
        }
    }

    private (bool Success, string Message, byte[] FileContent, string FileName) BuildSystemLogsPdf(IReadOnlyList<SystemLogView> logs)
    {
        try
        {
            using var stream = new MemoryStream();
            var writer = new iText.Kernel.Pdf.PdfWriter(stream);
            var pdfDocument = new iText.Kernel.Pdf.PdfDocument(writer);
            var document = new iText.Layout.Document(pdfDocument);

            document.Add(new iText.Layout.Element.Paragraph(LogPdfText("Log hệ thống")).SetFontSize(16).SetBold());
            document.Add(new iText.Layout.Element.Paragraph($"Ngay tao: {DateTime.Now:dd/MM/yyyy HH:mm}"));
            document.Add(new iText.Layout.Element.Paragraph($"So dong: {logs.Count:N0}"));

            var table = new iText.Layout.Element.Table(7, false);
            foreach (string header in new[] { "Thoi gian", "User", "Module", "Hanh dong", "IP", "Muc do", "Noi dung" })
            {
                table.AddHeaderCell(header);
            }

            foreach (var log in logs)
            {
                table.AddCell(DateTimeText(log.Timestamp));
                table.AddCell(LogPdfText(log.Username));
                table.AddCell(LogPdfText(log.Module));
                table.AddCell(LogPdfText(log.Action));
                table.AddCell(LogPdfText(log.IPAddress));
                table.AddCell(LogPdfText(log.Level));
                table.AddCell(LogPdfText(log.Description));
            }

            document.Add(table);
            document.Close();
            return (true, "Đã tạo file PDF.", stream.ToArray(), $"log-he-thong-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            return (false, $"Không thể tạo PDF log: {ex.Message}", Array.Empty<byte>(), "");
        }
    }

    private static string LogPdfText(string value)
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

    private void RenderSystemSettings()
    {
        var page = BeginPage("Cấu hình hệ thống", "Dashboard / Cấu hình hệ thống");
        var systemConfigs = GetSystemConfigs();

        string systemName = ConfigValue(systemConfigs, "AppName", ConfigurationHelper.GetAppSetting("AppName"));
        string systemAddress = ConfigValue(systemConfigs, "SystemAddress", "123 Đường ABC, Phường XYZ, Quận 1, TP. Hồ Chí Minh");
        string supportEmail = ConfigValue(systemConfigs, "SupportEmail", "support@chungcu.com.vn");
        string supportPhone = ConfigValue(systemConfigs, "SupportPhone", "(028) 1234 5678");
        string systemDescription = ConfigValue(
            systemConfigs,
            "SystemDescription",
            "Hệ thống quản lý toàn diện dành cho khu chung cư, hỗ trợ cư dân và ban quản lý trong công tác vận hành.");
        string lastBackupRaw = ConfigValue(systemConfigs, "LastBackupAt", "");
        DateTime? lastBackupAt = ParseDashboardDate(lastBackupRaw);
        string lastBackupText = lastBackupAt.HasValue ? DateTimeText(lastBackupAt.Value) : BackupDisplayText(lastBackupRaw);
        string selectedLogoPath = ConfigValue(systemConfigs, "SystemLogoPath", "");

        var featureSpecs = new[]
        {
            ("⚙", "Thông tin chung", "Cấu hình thông tin chung của hệ thống như tên, logo, địa chỉ...", ModernUi.Purple),
            ("👥", "Quản lý người dùng", "Cấu hình chính sách người dùng, mật khẩu, đăng nhập...", ModernUi.Blue),
            ("▣", "Phân quyền hệ thống", "Quản lý vai trò, quyền hạn và phân quyền chức năng...", ModernUi.Green),
            ("▥", "Cấu hình tòa nhà", "Cấu hình thông tin tòa nhà, block, tầng, căn hộ...", ModernUi.Orange),
            ("$", "Cấu hình phí", "Cấu hình các loại phí, đơn giá, chu kỳ thu, chính sách phí...", ModernUi.Red),
            ("●", "Cấu hình thông báo", "Cấu hình gửi email, SMS, thông báo ứng dụng...", Color.FromArgb(241, 166, 0)),
            ("☁", "Sao lưu & phục hồi", "Quản lý sao lưu dữ liệu và phục hồi hệ thống...", ModernUi.Teal),
            ("▤", "Nhật ký cấu hình", "Xem lịch sử thay đổi cấu hình hệ thống...", ModernUi.Purple)
        };
        var featureCards = featureSpecs
            .Select(spec => CreateSettingsFeatureCard(spec.Item1, spec.Item2, spec.Item3, spec.Item4))
            .ToList();
        foreach (var card in featureCards)
        {
            page.Controls.Add(card.Card);
        }

        var systemInfoCard = ModernUi.Section("Thông tin hệ thống", 620, 456);
        page.Controls.Add(systemInfoCard);
        var systemDivider = new Panel { BackColor = ModernUi.Border };
        systemInfoCard.Controls.Add(systemDivider);

        var logoTitle = ModernUi.Label("Logo hệ thống", 8.8f, FontStyle.Bold, ModernUi.Text);
        systemInfoCard.Controls.Add(logoTitle);
        var logoFrame = ModernUi.CardPanel(8);
        logoFrame.Padding = Padding.Empty;
        logoFrame.BackColor = Color.White;
        logoFrame.BorderColor = Color.FromArgb(207, 216, 228);
        systemInfoCard.Controls.Add(logoFrame);
        var logoMark = ModernUi.Label("▥", 42f, FontStyle.Bold, Color.FromArgb(224, 169, 52));
        logoMark.TextAlign = ContentAlignment.MiddleCenter;
        logoFrame.Controls.Add(logoMark);
        var logoName = ModernUi.Label("CHUNG CƯ\r\nSMART HOME", 15f, FontStyle.Bold, ModernUi.Blue);
        logoName.TextAlign = ContentAlignment.MiddleCenter;
        logoFrame.Controls.Add(logoName);
        var logoPreview = new PictureBox
        {
            BackColor = Color.White,
            SizeMode = PictureBoxSizeMode.Zoom,
            Visible = false
        };
        logoFrame.Controls.Add(logoPreview);
        var chooseLogo = ModernUi.OutlineButton("▣  Chọn ảnh", 116, 34);
        systemInfoCard.Controls.Add(chooseLogo);
        var logoNote = ModernUi.Label("Định dạng: PNG, JPG (Tối đa 2MB)", 8.4f, FontStyle.Regular, ModernUi.Muted);
        logoNote.AutoEllipsis = true;
        systemInfoCard.Controls.Add(logoNote);

        var systemFields = new List<(Label Label, RoundedPanel Host, TextBox Input, int Height)>();
        var systemNameInput = AddSettingsTextField(systemInfoCard, systemFields, "Tên hệ thống *", systemName);
        var systemAddressInput = AddSettingsTextField(systemInfoCard, systemFields, "Địa chỉ", systemAddress);
        var supportEmailInput = AddSettingsTextField(systemInfoCard, systemFields, "Email liên hệ", supportEmail);
        var supportPhoneInput = AddSettingsTextField(systemInfoCard, systemFields, "Số điện thoại", supportPhone);
        var systemDescriptionInput = AddSettingsTextField(systemInfoCard, systemFields, "Mô tả hệ thống", systemDescription, true);
        var saveSystemButton = ModernUi.Button("▣  Lưu thay đổi", ModernUi.Blue, 142, 36);
        systemInfoCard.Controls.Add(saveSystemButton);

        var generalCard = ModernUi.Section("Cấu hình chung", 620, 456);
        page.Controls.Add(generalCard);
        var generalDivider = new Panel { BackColor = ModernUi.Border };
        generalCard.Controls.Add(generalDivider);

        var generalFields = new List<(Label Label, Control Input, int Height)>();
        var languageCombo = AddSettingsComboField(generalCard, generalFields, "Ngôn ngữ hệ thống", new[] { ConfigValue(systemConfigs, "Language", "Tiếng Việt"), "Tiếng Việt", "English" });
        var timeZoneCombo = AddSettingsComboField(generalCard, generalFields, "Múi giờ", new[] { ConfigValue(systemConfigs, "TimeZone", "(UTC+07:00) Bangkok, Hanoi, Jakarta"), "(UTC+07:00) Bangkok, Hanoi, Jakarta", TimeZoneInfo.Local.Id });
        var dateFormatCombo = AddSettingsComboField(generalCard, generalFields, "Định dạng ngày", new[] { ConfigValue(systemConfigs, "DateTimeFormat", "dd/MM/yyyy (22/05/2025)"), "dd/MM/yyyy", "dd/MM/yyyy HH:mm" });
        var numberFormatCombo = AddSettingsComboField(generalCard, generalFields, "Định dạng số", new[] { ConfigValue(systemConfigs, "NumberFormat", "1.234.567,89"), "1.234.567,89", "1,234,567.89" });
        var maxRowsInput = AddSettingsSmallTextField(generalCard, generalFields, "Số dòng hiển thị tối đa trên bảng", ConfigValue(systemConfigs, "MaxTableRows", "100"));
        var autoLogoutInput = AddSettingsSmallTextField(generalCard, generalFields, "Tự động đăng xuất sau (phút)", ConfigValue(systemConfigs, "SessionTimeoutMinutes", ConfigurationHelper.GetAppSetting("SessionTimeoutMinutes")));

        var toggleRows = new List<(Label Label, CheckBox Toggle)>();
        var multiDeviceToggle = AddSettingsToggle(generalCard, toggleRows, "Cho phép đăng nhập đồng thời nhiều thiết bị", ConfigValue(systemConfigs, "AllowMultiDeviceLogin", "true"));
        var browserNotificationToggle = AddSettingsToggle(generalCard, toggleRows, "Hiển thị thông báo trên trình duyệt", ConfigValue(systemConfigs, "BrowserNotifications", "true"));
        var emailComplaintToggle = AddSettingsToggle(generalCard, toggleRows, "Gửi email khi có phản ánh mới", ConfigValue(systemConfigs, "EmailNewComplaint", "true"));
        var maintenanceModeToggle = AddSettingsToggle(generalCard, toggleRows, "Bật chế độ bảo trì hệ thống", ConfigValue(systemConfigs, "MaintenanceMode", "false"));

        var saveGeneralButton = ModernUi.Button("▣  Lưu thay đổi", ModernUi.Blue, 142, 36);
        generalCard.Controls.Add(saveGeneralButton);

        var backupCard = ModernUi.Section("Cấu hình sao lưu tự động", 620, 136);
        page.Controls.Add(backupCard);
        var backupDivider = new Panel { BackColor = ModernUi.Border };
        backupCard.Controls.Add(backupDivider);
        var backupFields = new List<(Label Label, Control Input, int Height)>();
        var backupFrequencyCombo = AddSettingsComboField(backupCard, backupFields, "Tần suất sao lưu", new[] { ConfigValue(systemConfigs, "AutoBackupFrequency", "Hằng ngày"), "Hằng ngày", "Hằng tuần", "Hằng tháng" });
        var backupTimeInput = AddSettingsSmallTextField(backupCard, backupFields, "Thời gian sao lưu", ConfigValue(systemConfigs, "AutoBackupTime", "02:00"));
        var backupRetentionInput = AddSettingsSmallTextField(backupCard, backupFields, "Lưu trữ tối đa (bản)", ConfigValue(systemConfigs, "BackupRetentionCount", "30"));
        var backupStatusCard = ModernUi.CardPanel(8);
        backupStatusCard.BackColor = Color.FromArgb(235, 250, 240);
        backupStatusCard.BorderColor = Color.FromArgb(151, 220, 172);
        backupCard.Controls.Add(backupStatusCard);
        var backupStatusIcon = new CircleLabel
        {
            Text = "✓",
            CircleColor = ModernUi.Green,
            ForeColor = Color.White,
            Font = ModernUi.Font(12f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        backupStatusCard.Controls.Add(backupStatusIcon);
        var backupStatusTitle = ModernUi.Label("Thành công", 9.2f, FontStyle.Bold, ModernUi.Green);
        backupStatusCard.Controls.Add(backupStatusTitle);
        var backupStatusDetail = ModernUi.Label(lastBackupAt.HasValue ? lastBackupText : "Chưa có bản sao lưu", 8.7f, FontStyle.Regular, ModernUi.Text);
        backupStatusDetail.AutoEllipsis = true;
        backupStatusCard.Controls.Add(backupStatusDetail);
        var configureBackupButton = ModernUi.OutlineButton("⚙  Cấu hình", 138, 36);
        backupCard.Controls.Add(configureBackupButton);

        LoadLogoPreview(selectedLogoPath, false);

        chooseLogo.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Chọn logo hệ thống",
                Filter = "Ảnh PNG/JPG|*.png;*.jpg;*.jpeg|Tất cả file|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var fileInfo = new FileInfo(dialog.FileName);
            if (fileInfo.Length > 2 * 1024 * 1024)
            {
                MessageBox.Show(this, "Ảnh logo tối đa 2MB.", "Cấu hình hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selectedLogoPath = dialog.FileName;
            LoadLogoPreview(selectedLogoPath, true);
        };

        saveSystemButton.Click += (_, _) =>
        {
            UpsertSystemConfig("AppName", systemNameInput.Text.Trim(), "Tên hệ thống");
            UpsertSystemConfig("SystemAddress", systemAddressInput.Text.Trim(), "Địa chỉ hệ thống");
            UpsertSystemConfig("SupportEmail", supportEmailInput.Text.Trim(), "Email liên hệ");
            UpsertSystemConfig("SupportPhone", supportPhoneInput.Text.Trim(), "Số điện thoại liên hệ");
            UpsertSystemConfig("SystemDescription", systemDescriptionInput.Text.Trim(), "Mô tả hệ thống");
            if (!string.IsNullOrWhiteSpace(selectedLogoPath))
            {
                UpsertSystemConfig("SystemLogoPath", selectedLogoPath, "Đường dẫn logo hệ thống");
            }

            MessageBox.Show(this, "Đã lưu thông tin hệ thống.", "Cấu hình hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        saveGeneralButton.Click += (_, _) =>
        {
            UpsertSystemConfig("Language", Display(languageCombo.SelectedItem?.ToString()), "Ngôn ngữ hệ thống");
            UpsertSystemConfig("TimeZone", Display(timeZoneCombo.SelectedItem?.ToString()), "Múi giờ hệ thống");
            UpsertSystemConfig("DateTimeFormat", Display(dateFormatCombo.SelectedItem?.ToString()), "Định dạng ngày");
            UpsertSystemConfig("NumberFormat", Display(numberFormatCombo.SelectedItem?.ToString()), "Định dạng số");
            UpsertSystemConfig("MaxTableRows", maxRowsInput.Text.Trim(), "Số dòng hiển thị tối đa trên bảng");
            UpsertSystemConfig("SessionTimeoutMinutes", autoLogoutInput.Text.Trim(), "Tự động đăng xuất sau");
            UpsertSystemConfig("AllowMultiDeviceLogin", multiDeviceToggle.Checked ? "true" : "false", "Cho phép đăng nhập đồng thời nhiều thiết bị");
            UpsertSystemConfig("BrowserNotifications", browserNotificationToggle.Checked ? "true" : "false", "Hiển thị thông báo trên trình duyệt");
            UpsertSystemConfig("EmailNewComplaint", emailComplaintToggle.Checked ? "true" : "false", "Gửi email khi có phản ánh mới");
            UpsertSystemConfig("MaintenanceMode", maintenanceModeToggle.Checked ? "true" : "false", "Bật chế độ bảo trì hệ thống");

            MessageBox.Show(this, "Đã lưu cấu hình chung.", "Cấu hình hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        configureBackupButton.Click += (_, _) =>
        {
            UpsertSystemConfig("AutoBackupFrequency", Display(backupFrequencyCombo.SelectedItem?.ToString()), "Tần suất sao lưu tự động");
            UpsertSystemConfig("AutoBackupTime", backupTimeInput.Text.Trim(), "Thời gian sao lưu tự động");
            UpsertSystemConfig("BackupRetentionCount", backupRetentionInput.Text.Trim(), "Số bản sao lưu lưu trữ tối đa");

            MessageBox.Show(this, "Đã lưu cấu hình sao lưu tự động.", "Cấu hình hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        void LoadLogoPreview(string path, bool showWarning)
        {
            if (logoPreview.Image != null)
            {
                logoPreview.Image.Dispose();
                logoPreview.Image = null;
            }

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                logoPreview.Visible = false;
                logoMark.Visible = true;
                logoName.Visible = true;
                return;
            }

            try
            {
                using var image = Image.FromFile(path);
                logoPreview.Image = new Bitmap(image);
                logoPreview.Visible = true;
                logoMark.Visible = false;
                logoName.Visible = false;
            }
            catch (Exception ex)
            {
                logoPreview.Visible = false;
                logoMark.Visible = true;
                logoName.Visible = true;
                if (showWarning)
                {
                    MessageBox.Show(this, $"Không thể tải ảnh logo:\n{ex.Message}", "Cấu hình hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        void LayoutSettingsPage()
        {
            int width = PageWorkWidth();
            int left = 20;
            int gap = 16;
            int y = 84;

            int featureColumns = 4;
            int featureHeight = 98;
            int featureWidth = Math.Max(220, (width - gap * (featureColumns - 1)) / featureColumns);
            for (int i = 0; i < featureCards.Count; i++)
            {
                int row = i / featureColumns;
                int column = i % featureColumns;
                featureCards[i].Card.SetBounds(left + column * (featureWidth + gap), y + row * (featureHeight + 14), featureWidth, featureHeight);
            }

            y += ((featureCards.Count + featureColumns - 1) / featureColumns) * (featureHeight + 14) - 14 + 16;

            bool twoColumns = width >= 980;
            int largeHeight = 456;
            int largeWidth = twoColumns ? (width - gap) / 2 : width;
            systemInfoCard.SetBounds(left, y, largeWidth, largeHeight);
            generalCard.SetBounds(twoColumns ? systemInfoCard.Right + gap : left, twoColumns ? y : y + largeHeight + gap, largeWidth, largeHeight);

            LayoutSystemInfoCard(systemInfoCard.Width, systemInfoCard.Height);
            LayoutGeneralCard(generalCard.Width, generalCard.Height);

            y = twoColumns ? y + largeHeight + gap : generalCard.Bottom + gap;
            int backupHeight = width >= 1180 ? 136 : 218;
            backupCard.SetBounds(left, y, width, backupHeight);
            LayoutBackupCard(backupCard.Width, backupCard.Height);

            page.AutoScrollMinSize = new Size(0, backupCard.Bottom + 34);
        }

        void LayoutSystemInfoCard(int cardWidth, int cardHeight)
        {
            systemDivider.SetBounds(18, 42, Math.Max(80, cardWidth - 36), 1);
            int pad = 18;
            int top = 54;
            int logoWidth = Math.Min(194, Math.Max(160, cardWidth / 3 - 18));
            logoTitle.SetBounds(pad, top, logoWidth, 20);
            logoFrame.SetBounds(pad, top + 28, logoWidth, 158);
            logoPreview.SetBounds(12, 12, Math.Max(48, logoFrame.Width - 24), Math.Max(48, logoFrame.Height - 24));
            logoMark.SetBounds(18, 18, logoWidth - 36, 50);
            logoName.SetBounds(12, 78, logoWidth - 24, 56);
            chooseLogo.SetBounds(pad, logoFrame.Bottom + 12, 118, 34);
            logoNote.SetBounds(pad, chooseLogo.Bottom + 8, Math.Max(180, logoWidth + 52), 22);

            int fieldX = pad + logoWidth + 26;
            int fieldWidth = Math.Max(230, cardWidth - fieldX - pad);
            int fieldY = top;
            foreach (var field in systemFields)
            {
                field.Label.SetBounds(fieldX, fieldY, fieldWidth, 20);
                field.Host.SetBounds(fieldX, fieldY + 24, fieldWidth, field.Height);
                LayoutSettingsTextHost(field.Host, field.Input, field.Height);
                fieldY += field.Height + 22;
            }

            saveSystemButton.SetBounds(cardWidth - pad - saveSystemButton.Width, cardHeight - 54, saveSystemButton.Width, 36);
        }

        void LayoutGeneralCard(int cardWidth, int cardHeight)
        {
            generalDivider.SetBounds(18, 42, Math.Max(80, cardWidth - 36), 1);
            int pad = 18;
            int labelWidth = Math.Min(250, Math.Max(176, cardWidth / 3));
            int inputX = pad + labelWidth + 18;
            int inputWidth = Math.Max(220, cardWidth - inputX - pad);
            int rowY = 56;
            foreach (var field in generalFields)
            {
                field.Label.SetBounds(pad, rowY + 6, labelWidth, 24);
                field.Input.SetBounds(inputX, rowY, inputWidth, field.Height);
                if (field.Input is RoundedPanel host && host.Controls.Count > 0)
                {
                    LayoutSettingsHostInput(host, host.Controls[0], field.Height);
                }
                rowY += 36;
            }

            rowY += 10;
            foreach (var toggle in toggleRows)
            {
                toggle.Label.SetBounds(pad, rowY + 3, Math.Max(200, cardWidth - pad * 2 - 96), 24);
                toggle.Toggle.SetBounds(cardWidth - pad - 66, rowY, 52, 26);
                rowY += 30;
            }

            saveGeneralButton.SetBounds(cardWidth - pad - saveGeneralButton.Width, cardHeight - 54, saveGeneralButton.Width, 36);
        }

        void LayoutBackupCard(int cardWidth, int cardHeight)
        {
            backupDivider.SetBounds(18, 42, Math.Max(80, cardWidth - 36), 1);
            int pad = 18;
            int top = 58;
            int fieldWidth = cardWidth >= 1180 ? Math.Max(210, (cardWidth - 520 - pad * 2 - 32) / 3) : Math.Max(220, (cardWidth - pad * 2 - 16) / 2);
            int x = pad;
            int y = top;
            for (int i = 0; i < backupFields.Count; i++)
            {
                var field = backupFields[i];
                field.Label.SetBounds(x, y, fieldWidth, 20);
                field.Input.SetBounds(x, y + 24, fieldWidth, field.Height);
                if (field.Input is RoundedPanel host && host.Controls.Count > 0)
                {
                    LayoutSettingsHostInput(host, host.Controls[0], field.Height);
                }

                x += fieldWidth + 16;
                if (cardWidth < 1180 && i == 1)
                {
                    x = pad;
                    y += 74;
                }
            }

            int statusWidth = cardWidth >= 1180 ? 272 : Math.Max(290, cardWidth - pad * 2 - 156);
            int statusX = cardWidth >= 1180 ? Math.Max(x + 10, cardWidth - pad - statusWidth - 156) : pad;
            int statusY = cardWidth >= 1180 ? top + 3 : y + 74;
            backupStatusCard.SetBounds(statusX, statusY, statusWidth, 64);
            backupStatusIcon.SetBounds(16, 18, 28, 28);
            backupStatusTitle.SetBounds(58, 12, statusWidth - 74, 22);
            backupStatusDetail.SetBounds(58, 34, statusWidth - 74, 20);
            configureBackupButton.SetBounds(cardWidth - pad - configureBackupButton.Width, statusY + 14, configureBackupButton.Width, 36);
        }

        page.Resize += (_, _) => LayoutSettingsPage();
        LayoutSettingsPage();

        static (RoundedPanel Card, CircleLabel Icon, Label Title, Label Description) CreateSettingsFeatureCard(string iconText, string title, string description, Color accent)
        {
            var card = ModernUi.CardPanel(12);
            card.Cursor = Cursors.Hand;
            card.Padding = Padding.Empty;

            var icon = new CircleLabel
            {
                Text = iconText,
                CircleColor = Color.FromArgb(235, 244, 255),
                ForeColor = accent,
                Font = ModernUi.Font(18f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(icon);

            var titleLabel = ModernUi.Label(title, 10f, FontStyle.Bold, ModernUi.Text);
            titleLabel.AutoEllipsis = true;
            titleLabel.Cursor = Cursors.Hand;
            card.Controls.Add(titleLabel);

            var descLabel = ModernUi.Label(description, 8.6f, FontStyle.Regular, ModernUi.Muted);
            descLabel.AutoEllipsis = true;
            descLabel.Cursor = Cursors.Hand;
            card.Controls.Add(descLabel);

            void LayoutCard()
            {
                int iconSize = card.Width < 250 ? 48 : 56;
                int iconTop = Math.Max(18, (card.Height - iconSize) / 2);
                int textLeft = 20 + iconSize + 18;
                icon.SetBounds(20, iconTop, iconSize, iconSize);
                titleLabel.SetBounds(textLeft, 21, Math.Max(110, card.Width - textLeft - 18), 24);
                descLabel.SetBounds(textLeft, 49, Math.Max(110, card.Width - textLeft - 18), 38);
            }

            Color normal = card.BackColor;
            card.MouseEnter += (_, _) => card.BackColor = Color.FromArgb(252, 254, 255);
            card.MouseLeave += (_, _) => card.BackColor = normal;
            foreach (Control child in card.Controls)
            {
                child.MouseEnter += (_, _) => card.BackColor = Color.FromArgb(252, 254, 255);
                child.MouseLeave += (_, _) => card.BackColor = normal;
            }

            card.Resize += (_, _) => LayoutCard();
            LayoutCard();
            return (card, icon, titleLabel, descLabel);
        }

        static TextBox AddSettingsTextField(Control parent, List<(Label Label, RoundedPanel Host, TextBox Input, int Height)> fields, string label, string value, bool multiline = false)
        {
            var labelControl = ModernUi.Label(label, 8.8f, FontStyle.Regular, ModernUi.Text);
            parent.Controls.Add(labelControl);

            var textBox = new TextBox
            {
                Text = value,
                BorderStyle = BorderStyle.None,
                Font = ModernUi.Font(9.2f),
                ForeColor = ModernUi.Text,
                BackColor = Color.White,
                Multiline = multiline,
                ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
                WordWrap = multiline
            };

            var host = CreateSettingsInputHost(textBox, multiline ? 66 : 34);
            parent.Controls.Add(host);
            fields.Add((labelControl, host, textBox, multiline ? 66 : 34));
            return textBox;
        }

        static TextBox AddSettingsSmallTextField(Control parent, List<(Label Label, Control Input, int Height)> fields, string label, string value)
        {
            var labelControl = ModernUi.Label(label, 8.8f, FontStyle.Regular, ModernUi.Text);
            parent.Controls.Add(labelControl);

            var textBox = new TextBox
            {
                Text = value,
                BorderStyle = BorderStyle.None,
                Font = ModernUi.Font(9.2f),
                ForeColor = ModernUi.Text,
                BackColor = Color.White
            };
            var host = CreateSettingsInputHost(textBox, 34);
            parent.Controls.Add(host);
            fields.Add((labelControl, host, 34));
            return textBox;
        }

        static ComboBox AddSettingsComboField(Control parent, List<(Label Label, Control Input, int Height)> fields, string label, IEnumerable<string> values)
        {
            var labelControl = ModernUi.Label(label, 8.8f, FontStyle.Regular, ModernUi.Text);
            parent.Controls.Add(labelControl);
            var uniqueValues = values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
            var combo = ModernUi.ComboBox(uniqueValues.Length == 0 ? new[] { "-" } : uniqueValues, 220);
            combo.Height = 34;
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = Color.White;
            var host = CreateSettingsInputHost(combo, 34);
            parent.Controls.Add(host);
            fields.Add((labelControl, host, 34));
            return combo;
        }

        static CheckBox AddSettingsToggle(Control parent, List<(Label Label, CheckBox Toggle)> toggles, string label, string value)
        {
            var labelControl = ModernUi.Label(label, 8.8f, FontStyle.Regular, ModernUi.Text);
            parent.Controls.Add(labelControl);

            bool enabled = value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Đã bật", StringComparison.OrdinalIgnoreCase);
            var toggle = new CheckBox
            {
                Appearance = Appearance.Button,
                AutoSize = false,
                Checked = enabled,
                FlatStyle = FlatStyle.Flat,
                Font = ModernUi.Font(11f, FontStyle.Bold),
                ForeColor = Color.White,
                Size = new Size(52, 26),
                Text = "●",
                Cursor = Cursors.Hand,
                TextAlign = enabled ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 5, 1),
                UseVisualStyleBackColor = false
            };
            toggle.FlatAppearance.BorderSize = 0;

            void ApplyToggleStyle()
            {
                toggle.BackColor = toggle.Checked ? Color.FromArgb(82, 196, 123) : Color.FromArgb(203, 213, 225);
                toggle.FlatAppearance.MouseOverBackColor = toggle.Checked ? Color.FromArgb(69, 179, 109) : Color.FromArgb(190, 201, 215);
                toggle.TextAlign = toggle.Checked ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            }

            toggle.CheckedChanged += (_, _) => ApplyToggleStyle();
            ApplyToggleStyle();
            parent.Controls.Add(toggle);
            toggles.Add((labelControl, toggle));
            return toggle;
        }

        static RoundedPanel CreateSettingsInputHost(Control input, int height)
        {
            var host = ModernUi.CardPanel(6);
            host.Padding = Padding.Empty;
            host.BackColor = Color.White;
            host.BorderColor = Color.FromArgb(207, 216, 228);
            host.Height = height;
            host.Controls.Add(input);
            host.Resize += (_, _) => LayoutSettingsHostInput(host, input, height);
            LayoutSettingsHostInput(host, input, height);
            return host;
        }

        static void LayoutSettingsHostInput(Control host, Control input, int height)
        {
            if (input is TextBox textBox)
            {
                LayoutSettingsTextHost(host, textBox, height);
                return;
            }

            if (input is ComboBox comboBox)
            {
                comboBox.SetBounds(8, Math.Max(3, (host.Height - 28) / 2), Math.Max(40, host.Width - 16), 28);
            }
        }

        static void LayoutSettingsTextHost(Control host, TextBox input, int height)
        {
            if (input.Multiline)
            {
                input.SetBounds(10, 8, Math.Max(40, host.Width - 20), Math.Max(24, host.Height - 16));
                return;
            }

            input.SetBounds(10, Math.Max(7, (host.Height - input.PreferredHeight) / 2), Math.Max(40, host.Width - 20), input.PreferredHeight);
        }
    }
}
