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
    private void RenderResidents()
    {
        var page = BeginPage("Quản lý cư dân", "Dashboard / Cư dân");
        int w = PageWorkWidth();
        int y = 72;
        var allResidents = ResidentDAL.GetAllResidents();
        var apartments = ApartmentDAL.GetAllApartments();
        var apartmentById = apartments.ToDictionary(a => a.ApartmentID);
        List<ResidentDTO> displayResidents = new();

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 126);
        page.Controls.Add(filters);

        int filterGap = 16;
        int filterWidth = Math.Max(150, (filters.Width - 32 - filterGap * 3) / 4);
        var buildingFilter = AddResidentFilterCombo(filters, "Tòa nhà", 16, 10, filterWidth);
        var apartmentFilter = AddResidentFilterCombo(filters, "Căn hộ", buildingFilter.Right + filterGap, 10, filterWidth);
        var statusFilter = AddResidentFilterCombo(filters, "Tình trạng cư trú", apartmentFilter.Right + filterGap, 10, filterWidth);
        var roleFilter = AddResidentFilterCombo(filters, "Vai trò trong căn hộ", statusFilter.Right + filterGap, 10, filterWidth);
        int searchButtonWidth = 116;
        int resetButtonWidth = 104;
        int searchButtonGap = 12;
        int searchWidth = Math.Max(280, filters.Width - 32 - searchButtonWidth - resetButtonWidth - searchButtonGap * 2);
        var search = ModernUi.TextBox("Nhập tên, CCCD, SĐT hoặc căn hộ...", searchWidth);
        search.Location = new Point(16, 62);
        filters.Controls.Add(search);

        var searchButton = ModernUi.Button("Tìm kiếm", ModernUi.Blue, searchButtonWidth, 34);
        searchButton.Location = new Point(search.Right + searchButtonGap, 60);
        filters.Controls.Add(searchButton);

        var resetButton = ModernUi.OutlineButton("Đặt lại", resetButtonWidth, 34);
        resetButton.Location = new Point(searchButton.Right + searchButtonGap, 60);
        filters.Controls.Add(resetButton);

        var filterSummary = ModernUi.Label("", 8.7f, FontStyle.Regular, ModernUi.Muted);
        filterSummary.Location = new Point(16, 96);
        filterSummary.Size = new Size(filters.Width - 32, 18);
        filters.Controls.Add(filterSummary);

        y += 142;
        int leftW = Math.Min(Math.Max(740, (int)Math.Round(w * 0.65)), w - 320);
        int rightW = w - leftW - 12;

        var list = ModernUi.Section("Danh sách cư dân theo căn hộ", leftW, 552);
        list.Location = new Point(18, y);
        var gridColumns = new[] { "Mã cư dân", "Họ tên", "Tòa", "Block", "Tầng", "Căn hộ", "Vai trò", "Tình trạng", "Ngày vào ở" };
        var grid = CreateGrid(gridColumns, new[] { EmptyRow(gridColumns.Length, "Không có cư dân") });
        gridColumns[8] = "SĐT";
        if (grid.Columns.Count > 8)
        {
            grid.Columns[8].HeaderText = "SĐT";
        }
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 404);
        grid.ColumnHeadersHeight = 40;
        grid.RowTemplate.Height = 36;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        list.Controls.Add(grid);

        var listSummary = ModernUi.Label("", 8.8f, FontStyle.Bold, ModernUi.Blue);
        listSummary.Location = new Point(18, 488);
        listSummary.Size = new Size(list.Width - 36, 22);
        list.Controls.Add(listSummary);

        var listHint = ModernUi.Label("Chọn cư dân để xem căn hộ liên kết và các cư dân cùng căn.", 8.7f, FontStyle.Regular, ModernUi.Muted);
        listHint.Location = new Point(18, 512);
        listHint.Size = new Size(list.Width - 36, 22);
        list.Controls.Add(listHint);
        var residentPager = AddPaginationControls(list, 18, 456, list.Width - 270, 454, list.Width - 80, 454, list.Width - 300);
        residentPager.SummaryLabel.Font = ModernUi.Font(9.1f, FontStyle.Bold);
        residentPager.SummaryLabel.ForeColor = ModernUi.Navy;
        residentPager.PageButton.Font = ModernUi.Font(9.2f, FontStyle.Bold);
        residentPager.FirstButton.Font = ModernUi.Font(9.5f, FontStyle.Bold);
        residentPager.PreviousButton.Font = ModernUi.Font(9.5f, FontStyle.Bold);
        residentPager.NextButton.Font = ModernUi.Font(9.5f, FontStyle.Bold);
        residentPager.LastButton.Font = ModernUi.Font(9.5f, FontStyle.Bold);
        residentPager.PageSizeCombo.Font = ModernUi.Font(9f, FontStyle.Bold);
        page.Controls.Add(list);

        var details = ModernUi.Section("Thông tin cư dân và căn hộ", rightW, 552);
        details.Location = new Point(list.Right + 12, y);
        var detailBody = new Panel
        {
            Location = new Point(0, 34),
            Size = new Size(details.Width, details.Height - 34),
            BackColor = Color.Transparent
        };
        details.Controls.Add(detailBody);
        page.Controls.Add(details);
        page.Controls.Add(new Panel
        {
            Location = new Point(0, details.Bottom + 20),
            Size = new Size(1, 20),
            BackColor = Color.Transparent
        });

        bool suppressFilterEvents = false;
        var residentPagination = new PaginationState();

        void SetFilterItems(ComboBox combo, IEnumerable<string> items, string preferred)
        {
            string[] options = items
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.CurrentCultureIgnoreCase)
                .Prepend("Tất cả")
                .ToArray();

            combo.BeginUpdate();
            combo.Items.Clear();
            combo.Items.AddRange(options.Cast<object>().ToArray());
            int selectedIndex = Array.FindIndex(options, option => string.Equals(option, preferred, StringComparison.OrdinalIgnoreCase));
            combo.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            combo.EndUpdate();
        }

        void RefreshApartmentFilter()
        {
            suppressFilterEvents = true;
            try
            {
                string selectedBuilding = buildingFilter.SelectedItem?.ToString() ?? "Tất cả";
                string currentApartment = apartmentFilter.SelectedItem?.ToString() ?? "Tất cả";
                IEnumerable<string> apartmentsForFilter = apartments
                    .Where(a => selectedBuilding == "Tất cả" || string.Equals(BuildingShort(a.BuildingName), selectedBuilding, StringComparison.OrdinalIgnoreCase))
                    .Select(a => Display(a.ApartmentCode));
                SetFilterItems(apartmentFilter, apartmentsForFilter, currentApartment);
            }
            finally
            {
                suppressFilterEvents = false;
            }
        }

        void BindResidentGrid(IReadOnlyList<ResidentDTO> residents)
        {
            SetGridData(
                grid,
                gridColumns,
                RowsOrEmpty(residents, gridColumns.Length, (resident, _) =>
                {
                    apartmentById.TryGetValue(resident.ApartmentID, out var apartment);
                    return new object[]
                    {
                        $"CD{resident.ResidentID:0000}",
                        Display(resident.FullName),
                        BuildingShort(apartment?.BuildingName),
                        BlockShort(apartment?.BlockName),
                        apartment?.FloorNumber?.ToString("00") ?? "-",
                        Display(resident.ApartmentCode),
                        Display(resident.RelationshipWithOwner),
                        ResidentLivingStatus(resident),
                        Display(resident.Phone)
                    };
                }, "Không có cư dân phù hợp"));

            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.ScrollBars = ScrollBars.Both;
            grid.ColumnHeadersHeight = 40;
            grid.RowTemplate.Height = 36;
            grid.DefaultCellStyle.Font = ModernUi.Font(8.2f);
            grid.ColumnHeadersDefaultCellStyle.Font = ModernUi.Font(8.4f, FontStyle.Bold);

            int[] widths = { 90, 180, 80, 70, 70, 100, 110, 110, 120 };
            for (int i = 0; i < grid.Columns.Count && i < widths.Length; i++)
            {
                grid.Columns[i].Width = widths[i];
                grid.Columns[i].MinimumWidth = widths[i];
            }

            if (grid.Columns.Count >= 9)
            {
                grid.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                grid.Columns[6].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            grid.ClearSelection();
        }

        void SelectResident(ResidentDTO? resident)
        {
            ApartmentDTO? apartment = null;
            if (resident != null)
            {
                apartmentById.TryGetValue(resident.ApartmentID, out apartment);
            }

            var roommates = resident == null
                ? new List<ResidentDTO>()
                : allResidents
                    .Where(r => r.ApartmentID == resident.ApartmentID)
                    .OrderBy(r => Display(r.RelationshipWithOwner))
                    .ThenBy(r => Display(r.FullName))
                    .ToList();

            RenderResidentLinkedDetails(detailBody, resident, apartment, roommates);

            grid.ClearSelection();
            if (resident == null)
            {
                return;
            }

            for (int i = 0; i < grid.Rows.Count; i++)
            {
                if (residentPagination.StartIndex + i < displayResidents.Count &&
                    displayResidents[residentPagination.StartIndex + i].ResidentID == resident.ResidentID)
                {
                    grid.Rows[i].Selected = true;
                    grid.CurrentCell = grid.Rows[i].Cells[0];
                    break;
                }
            }
        }

        void ApplyFilters(ResidentDTO? preferredResident = null)
        {
            string selectedBuilding = buildingFilter.SelectedItem?.ToString() ?? "Tất cả";
            string selectedApartment = apartmentFilter.SelectedItem?.ToString() ?? "Tất cả";
            string selectedStatus = statusFilter.SelectedItem?.ToString() ?? "Tất cả";
            string selectedRole = roleFilter.SelectedItem?.ToString() ?? "Tất cả";
            string searchText = (search.Text ?? string.Empty).Trim();

            displayResidents = allResidents
                .Where(resident =>
                {
                    apartmentById.TryGetValue(resident.ApartmentID, out var apartment);
                    string residentCode = $"CD{resident.ResidentID:0000}";
                    string buildingName = BuildingShort(apartment?.BuildingName);
                    string buildingFullName = Display(apartment?.BuildingName, "");
                    string blockName = BlockShort(apartment?.BlockName);
                    string blockFullName = Display(apartment?.BlockName, "");
                    string floorNumber = apartment?.FloorNumber?.ToString("00", CultureInfo.InvariantCulture) ?? string.Empty;
                    string floorLabel = apartment?.FloorNumber.HasValue == true
                        ? $"Tầng {apartment.FloorNumber.Value:00}"
                        : string.Empty;
                    string apartmentCode = Display(resident.ApartmentCode);
                    string residentStatus = ResidentLivingStatus(resident);
                    string relationship = Display(resident.RelationshipWithOwner);

                    if (selectedBuilding != "Tất cả" && !string.Equals(buildingName, selectedBuilding, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (selectedApartment != "Tất cả" && !string.Equals(apartmentCode, selectedApartment, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (selectedStatus != "Tất cả" && !string.Equals(residentStatus, selectedStatus, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (selectedRole != "Tất cả" && !string.Equals(relationship, selectedRole, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (searchText.Length == 0)
                    {
                        return true;
                    }

                    string haystack = string.Join(" ", new[]
                    {
                        residentCode,
                        Display(resident.FullName, ""),
                        Display(resident.CCCD, ""),
                        Display(resident.Phone, ""),
                        Display(resident.Email, ""),
                        apartmentCode,
                        buildingName,
                        buildingFullName,
                        blockName,
                        blockFullName,
                        floorNumber,
                        floorLabel
                    });

                    return haystack.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0;
                })
                .OrderBy(resident => Display(resident.ApartmentCode, ""))
                .ThenBy(resident => Display(resident.RelationshipWithOwner, ""))
                .ThenBy(resident => Display(resident.FullName, ""))
                .ToList();

            IReadOnlyList<ResidentDTO> pageResidents = Paginate(displayResidents, residentPagination);
            BindResidentGrid(pageResidents);
            UpdatePaginationControls(residentPagination, residentPager, "cư dân");

            int apartmentCount = displayResidents
                .Select(resident => resident.ApartmentID)
                .Distinct()
                .Count();
            listSummary.Text = $"Lọc được {displayResidents.Count:N0} / {allResidents.Count:N0} cư dân - {apartmentCount:N0} căn hộ có cư dân.";
            filterSummary.Text = selectedBuilding == "Tất cả"
                ? "Bộ lọc áp dụng trên danh sách cư dân liên kết với căn hộ hiện có trong hệ thống."
                : $"Đang áp dụng bộ lọc theo {selectedBuilding} và các căn hộ liên quan.";

            ResidentDTO? residentToSelect = preferredResident != null
                ? pageResidents.FirstOrDefault(r => r.ResidentID == preferredResident.ResidentID)
                : pageResidents.FirstOrDefault();
            SelectResident(residentToSelect);
        }

        void ResetResidentFilters()
        {
            suppressFilterEvents = true;
            try
            {
                buildingFilter.SelectedIndex = 0;
                statusFilter.SelectedIndex = 0;
                roleFilter.SelectedIndex = 0;
                search.Clear();
                RefreshApartmentFilter();
                apartmentFilter.SelectedIndex = apartmentFilter.Items.Count > 0 ? 0 : -1;
            }
            finally
            {
                suppressFilterEvents = false;
            }

            residentPagination.MoveToFirstPage();
            ApplyFilters();
        }

        var buildingOptions = apartments.Select(a => BuildingShort(a.BuildingName));
        var statusOptions = allResidents.Select(ResidentLivingStatus);
        var roleOptions = allResidents.Select(r => Display(r.RelationshipWithOwner));
        SetFilterItems(buildingFilter, buildingOptions, "Tất cả");
        SetFilterItems(statusFilter, statusOptions, "Tất cả");
        SetFilterItems(roleFilter, roleOptions, "Tất cả");
        RefreshApartmentFilter();

        buildingFilter.SelectedIndexChanged += (_, _) =>
        {
            if (suppressFilterEvents)
            {
                return;
            }

            RefreshApartmentFilter();
            residentPagination.MoveToFirstPage();
            ApplyFilters();
        };
        apartmentFilter.SelectedIndexChanged += (_, _) =>
        {
            if (!suppressFilterEvents)
            {
                residentPagination.MoveToFirstPage();
                ApplyFilters();
            }
        };
        statusFilter.SelectedIndexChanged += (_, _) =>
        {
            if (!suppressFilterEvents)
            {
                residentPagination.MoveToFirstPage();
                ApplyFilters();
            }
        };
        roleFilter.SelectedIndexChanged += (_, _) =>
        {
            if (!suppressFilterEvents)
            {
                residentPagination.MoveToFirstPage();
                ApplyFilters();
            }
        };
        search.TextChanged += (_, _) =>
        {
            residentPagination.MoveToFirstPage();
            ApplyFilters();
        };
        searchButton.Click += (_, _) =>
        {
            residentPagination.MoveToFirstPage();
            ApplyFilters();
        };
        resetButton.Click += (_, _) => ResetResidentFilters();
        search.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            residentPagination.MoveToFirstPage();
            ApplyFilters();
        };
        grid.CellClick += (_, e) =>
        {
            int absoluteIndex = residentPagination.StartIndex + e.RowIndex;
            if (e.RowIndex >= 0 && absoluteIndex >= 0 && absoluteIndex < displayResidents.Count)
            {
                SelectResident(displayResidents[absoluteIndex]);
            }
        };

        residentPager.FirstButton.Click += (_, _) =>
        {
            residentPagination.MoveToFirstPage();
            ApplyFilters();
        };
        residentPager.PreviousButton.Click += (_, _) =>
        {
            residentPagination.MoveToPreviousPage();
            ApplyFilters();
        };
        residentPager.NextButton.Click += (_, _) =>
        {
            residentPagination.MoveToNextPage();
            ApplyFilters();
        };
        residentPager.LastButton.Click += (_, _) =>
        {
            residentPagination.MoveToLastPage();
            ApplyFilters();
        };
        residentPager.PageSizeCombo.SelectedIndexChanged += (_, _) =>
        {
            residentPagination.SetPageSize(ParsePageSize(residentPager.PageSizeCombo.SelectedItem, residentPagination.PageSize));
            ApplyFilters();
        };

        ApplyFilters(allResidents.FirstOrDefault());
    }

    private void RenderInvoices()
    {
#if false
        var page = BeginPage("Quản lý hóa đơn & phí dịch vụ", "Dashboard / Hóa đơn - phí dịch vụ");

#endif
        var page = BeginPage("Quản lý hóa đơn & phí dịch vụ", "Dashboard / Hóa đơn & phí dịch vụ");
        int w = PageWorkWidth();
        int y = 72;
        int gap = 12;

        var invoices = InvoiceDAL.GetAllInvoices();
        var apartments = ApartmentDAL.GetAllApartments();
        var residents = ResidentDAL.GetAllResidents();

        var residentsByApartment = residents
            .Where(r => r.ApartmentID > 0)
            .GroupBy(r => r.ApartmentID)
            .ToDictionary(g => g.Key, g => g.First());

        // Nếu database chưa có hóa đơn, tạo dữ liệu demo chỉ để hiển thị UI.
        // Không ghi vào database.
        if (invoices.Count == 0)
        {
            var sampleApartments = apartments.Take(8).ToList();

            if (sampleApartments.Count == 0)
            {
                sampleApartments = new List<ApartmentDTO>
            {
                new ApartmentDTO { ApartmentID = 1, ApartmentCode = "A-0605", BuildingName = "Tòa A", BlockName = "A", FloorNumber = 6, Area = 92, ApartmentType = "3 PN - 2 WC", Status = "Occupied" },
                new ApartmentDTO { ApartmentID = 2, ApartmentCode = "A-0503", BuildingName = "Tòa A", BlockName = "A", FloorNumber = 5, Area = 68, ApartmentType = "2 PN - 1 WC", Status = "Occupied" },
                new ApartmentDTO { ApartmentID = 3, ApartmentCode = "A-0404", BuildingName = "Tòa A", BlockName = "A", FloorNumber = 4, Area = 75, ApartmentType = "2 PN - 1 WC", Status = "Occupied" }
            };
            }

            int id = 501;
            foreach (var apartment in sampleApartments)
            {
                decimal amount = 950000 + (id % 5) * 100000;
                decimal paid = id % 4 == 0 ? 0 : amount;

                invoices.Add(new InvoiceDTO
                {
                    InvoiceID = id,
                    ApartmentID = apartment.ApartmentID,
                    ApartmentCode = apartment.ApartmentCode,
                    Month = id % 2 == 0 ? 5 : 4,
                    Year = 2024,
                    DueDate = new DateTime(2024, id % 2 == 0 ? 5 : 4, 15),
                    CreatedAt = new DateTime(2024, id % 2 == 0 ? 5 : 4, 1),
                    UpdatedAt = paid > 0 ? new DateTime(2024, id % 2 == 0 ? 5 : 4, 10) : null,
                    TotalAmount = amount,
                    PaidAmount = paid,
                    RemainingAmount = amount - paid,
                    PaymentStatus = paid <= 0 ? (id % 3 == 0 ? "Overdue" : "Unpaid") : "Paid",
                    Status = paid <= 0 ? (id % 3 == 0 ? "Overdue" : "Unpaid") : "Paid",
                    Note = "Dữ liệu demo hiển thị giao diện"
                });

                id++;
            }
        }

        var monthItems = Enumerable.Range(1, 12)
            .Select(m => $"{m:00}")
            .Prepend("Tất cả")
            .ToArray();

        var yearItems = invoices
            .Select(i => i.Year)
            .Where(year => year > 0)
            .Distinct()
            .OrderByDescending(year => year)
            .Select(year => year.ToString(CultureInfo.InvariantCulture))
            .Prepend("Tất cả")
            .ToArray();

        if (yearItems.Length == 1)
        {
            yearItems = new[] { "Tất cả", DateTime.Now.Year.ToString(CultureInfo.InvariantCulture), "2024" };
        }

        var apartmentItems = apartments
            .Select(a => Display(a.ApartmentCode, ""))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct()
            .OrderBy(code => code)
            .Prepend("Tất cả")
            .ToArray();

        if (apartmentItems.Length == 1)
        {
            apartmentItems = invoices
                .Select(i => Display(i.ApartmentCode, ""))
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct()
                .OrderBy(code => code)
                .Prepend("Tất cả")
                .ToArray();
        }

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 118);

        var monthLabel = ModernUi.Label("Tháng", 8.7f, FontStyle.Bold, ModernUi.Text);
        monthLabel.Location = new Point(14, 12);
        monthLabel.Size = new Size(150, 18);
        filters.Controls.Add(monthLabel);

        var monthCombo = ModernUi.ComboBox(monthItems, 150);
        monthCombo.Location = new Point(14, 34);
        monthCombo.SelectedItem = monthItems.Contains("05") ? "05" : "Tất cả";
        filters.Controls.Add(monthCombo);

        var yearLabel = ModernUi.Label("Năm", 8.7f, FontStyle.Bold, ModernUi.Text);
        yearLabel.Location = new Point(180, 12);
        yearLabel.Size = new Size(150, 18);
        filters.Controls.Add(yearLabel);

        var yearCombo = ModernUi.ComboBox(yearItems, 150);
        yearCombo.Location = new Point(180, 34);
        yearCombo.SelectedItem = yearItems.Contains("2024") ? "2024" : yearItems.FirstOrDefault();
        filters.Controls.Add(yearCombo);

        var apartmentLabel = ModernUi.Label("Căn hộ", 8.7f, FontStyle.Bold, ModernUi.Text);
        apartmentLabel.Location = new Point(346, 12);
        apartmentLabel.Size = new Size(170, 18);
        filters.Controls.Add(apartmentLabel);

        var apartmentCombo = ModernUi.ComboBox(apartmentItems, 210);
        apartmentCombo.Location = new Point(346, 34);
        apartmentCombo.SelectedItem = "Tất cả";
        filters.Controls.Add(apartmentCombo);

        var statusFilterCombo = AddInvoiceStatusFilter(filters, "Trạng thái thanh toán", "Tất cả", 574, 12);

        var search = ModernUi.TextBox("Tìm mã hóa đơn, căn hộ, chủ hộ, SĐT...", 360);
        search.Location = new Point(14, 76);
        filters.Controls.Add(search);

        var searchButton = ModernUi.Button("⌕  Tìm kiếm", ModernUi.Blue, 120, 32);
        searchButton.Location = new Point(390, 76);
        filters.Controls.Add(searchButton);

        var resetButton = ModernUi.OutlineButton("⟳  Đặt lại", 110, 32);
        resetButton.Location = new Point(522, 76);
        filters.Controls.Add(resetButton);

        page.Controls.Add(filters);

        y += 134;

        int leftW = Math.Max(680, (int)(w * 0.64));
        int rightW = w - leftW - gap;

        var list = ModernUi.Section("Danh sách hóa đơn", leftW, 430);
        list.Location = new Point(18, y);

        string[] invoiceColumns =
        {
        "Mã hóa đơn",
        "Tháng/Năm",
        "Căn hộ",
        "Chủ hộ",
        "Tổng tiền",
        "Hạn thanh toán",
        "Trạng thái",
        "Ngày thanh toán"
    };

        var grid = CreateGrid(invoiceColumns, new[] { EmptyRow(invoiceColumns.Length, "Không có hóa đơn") });
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 316);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ScrollBars = ScrollBars.Vertical;
        list.Controls.Add(grid);

        var invoicePager = AddPaginationControls(
            list,
            16,
            382,
            Math.Max(260, list.Width - 420),
            378,
            list.Width - 86,
            378,
            Math.Max(200, list.Width - 470));

        page.Controls.Add(list);

        var detail = ModernUi.Section("Chi tiết hóa đơn", rightW, 430);
        detail.Location = new Point(list.Right + gap, y);
        page.Controls.Add(detail);

        var invoicePagination = new PaginationState();
        List<InvoiceDTO> displayInvoices = new();
        InvoiceDTO? selectedInvoice = null;

        void ApplyInvoiceGridStyle()
        {
            if (grid.Columns.Count == 0)
            {
                return;
            }

            grid.Columns[0].Width = 105;
            grid.Columns[1].Width = 80;
            grid.Columns[2].Width = 80;
            grid.Columns[3].Width = 145;
            grid.Columns[4].Width = 110;
            grid.Columns[5].Width = 105;
            grid.Columns[6].Width = 120;
            grid.Columns[7].Width = 105;
        }

        ResidentDTO? ResidentForInvoice(InvoiceDTO? invoice)
        {
            if (invoice == null)
            {
                return null;
            }

            residentsByApartment.TryGetValue(invoice.ApartmentID, out var resident);
            return resident;
        }

        void SelectInvoice(InvoiceDTO? invoice)
        {
            selectedInvoice = invoice;
            detail.Controls.Clear();
            AddInvoiceDetail(detail, selectedInvoice, ResidentForInvoice(selectedInvoice));

            grid.ClearSelection();

            if (selectedInvoice == null)
            {
                return;
            }

            for (int i = 0; i < grid.Rows.Count; i++)
            {
                int absoluteIndex = invoicePagination.StartIndex + i;
                if (absoluteIndex >= 0 &&
                    absoluteIndex < displayInvoices.Count &&
                    displayInvoices[absoluteIndex].InvoiceID == selectedInvoice.InvoiceID)
                {
                    grid.Rows[i].Selected = true;
                    grid.CurrentCell = grid.Rows[i].Cells[0];
                    break;
                }
            }
        }

        DonutChartPanel invoiceStatsDonut = null;
        Label invoiceStatsSummary = null;

        void ApplyInvoiceFilters(int? preferredInvoiceId = null)
        {
            string keyword = (search.Text ?? string.Empty).Trim();
            string monthText = monthCombo.Text.Trim();
            string yearText = yearCombo.Text.Trim();
            string apartmentText = apartmentCombo.Text.Trim();
            string statusText = statusFilterCombo.Text.Trim();

            displayInvoices = invoices
                .Where(invoice =>
                {
                    var resident = ResidentForInvoice(invoice);

                    if (monthText != "Tất cả" &&
                        int.TryParse(monthText, out int month) &&
                        invoice.Month != month)
                    {
                        return false;
                    }

                    if (yearText != "Tất cả" &&
                        int.TryParse(yearText, out int year) &&
                        invoice.Year != year)
                    {
                        return false;
                    }

                    if (apartmentText != "Tất cả" &&
                        !string.Equals(Display(invoice.ApartmentCode, ""), apartmentText, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (statusText != "Tất cả" &&
                        !string.Equals(ViStatus(invoice.PaymentStatus), statusText, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (keyword.Length == 0)
                    {
                        return true;
                    }

                    string haystack = string.Join(" ",
                        InvoiceCode(invoice),
                        Display(invoice.ApartmentCode, ""),
                        Display(resident?.FullName, ""),
                        Display(resident?.Phone, ""),
                        Display(resident?.Email, ""),
                        $"{invoice.Month:00}/{invoice.Year}",
                        ViStatus(invoice.PaymentStatus),
                        Display(invoice.Note, ""));

                    return haystack.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0;
                })
                .OrderByDescending(invoice => invoice.Year)
                .ThenByDescending(invoice => invoice.Month)
                .ThenBy(invoice => Display(invoice.ApartmentCode, ""))
                .ToList();

            if (preferredInvoiceId.HasValue)
            {
                int preferredIndex = displayInvoices.FindIndex(invoice => invoice.InvoiceID == preferredInvoiceId.Value);
                if (preferredIndex >= 0)
                {
                    invoicePagination.CurrentPage = FindPageForIndex(preferredIndex, invoicePagination.PageSize);
                }
            }

            var pageInvoices = Paginate(displayInvoices, invoicePagination);

            SetGridData(
                grid,
                invoiceColumns,
                RowsOrEmpty(pageInvoices, invoiceColumns.Length, (invoice, _) =>
                {
                    var resident = ResidentForInvoice(invoice);
                    return new object[]
                    {
                    InvoiceCode(invoice),
                    $"{invoice.Month:00}/{invoice.Year}",
                    Display(invoice.ApartmentCode),
                    Display(resident?.FullName),
                    Money(invoice.TotalAmount),
                    DateText(invoice.DueDate),
                    ViStatus(invoice.PaymentStatus),
                    invoice.PaidAmount > 0 ? DateText(invoice.UpdatedAt) : "-"
                    };
                }, "Không có hóa đơn phù hợp"));

            ApplyInvoiceGridStyle();
            UpdatePaginationControls(invoicePagination, invoicePager, "hóa đơn");

            InvoiceDTO? invoiceToSelect = preferredInvoiceId.HasValue
                ? pageInvoices.FirstOrDefault(invoice => invoice.InvoiceID == preferredInvoiceId.Value)
                : pageInvoices.FirstOrDefault();

            SelectInvoice(invoiceToSelect);
            UpdateInvoiceStats();
        }

        invoicePager.FirstButton.Click += (_, _) =>
        {
            invoicePagination.MoveToFirstPage();
            ApplyInvoiceFilters();
        };

        invoicePager.PreviousButton.Click += (_, _) =>
        {
            invoicePagination.MoveToPreviousPage();
            ApplyInvoiceFilters();
        };

        invoicePager.NextButton.Click += (_, _) =>
        {
            invoicePagination.MoveToNextPage();
            ApplyInvoiceFilters();
        };

        invoicePager.LastButton.Click += (_, _) =>
        {
            invoicePagination.MoveToLastPage();
            ApplyInvoiceFilters();
        };

        invoicePager.PageSizeCombo.SelectedIndexChanged += (_, _) =>
        {
            invoicePagination.SetPageSize(ParsePageSize(invoicePager.PageSizeCombo.SelectedItem, invoicePagination.PageSize));
            ApplyInvoiceFilters();
        };

        monthCombo.SelectedIndexChanged += (_, _) =>
        {
            invoicePagination.MoveToFirstPage();
            ApplyInvoiceFilters();
        };

        yearCombo.SelectedIndexChanged += (_, _) =>
        {
            invoicePagination.MoveToFirstPage();
            ApplyInvoiceFilters();
        };

        apartmentCombo.SelectedIndexChanged += (_, _) =>
        {
            invoicePagination.MoveToFirstPage();
            ApplyInvoiceFilters();
        };

        statusFilterCombo.SelectedIndexChanged += (_, _) =>
        {
            invoicePagination.MoveToFirstPage();
            ApplyInvoiceFilters();
        };

        search.TextChanged += (_, _) =>
        {
            invoicePagination.MoveToFirstPage();
            ApplyInvoiceFilters();
        };

        searchButton.Click += (_, _) =>
        {
            invoicePagination.MoveToFirstPage();
            ApplyInvoiceFilters();
        };

        resetButton.Click += (_, _) =>
        {
            monthCombo.SelectedItem = "Tất cả";
            yearCombo.SelectedItem = "Tất cả";
            apartmentCombo.SelectedItem = "Tất cả";
            statusFilterCombo.SelectedItem = "Tất cả";
            search.Text = "";
            invoicePagination.MoveToFirstPage();
            ApplyInvoiceFilters();
        };

        grid.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            int absoluteIndex = invoicePagination.StartIndex + e.RowIndex;
            if (absoluteIndex >= 0 && absoluteIndex < displayInvoices.Count)
            {
                SelectInvoice(displayInvoices[absoluteIndex]);
            }
        };

        y += 446;

        var stats = ModernUi.Section("Tình hình thanh toán", (int)(w * 0.50), 230);
        stats.Location = new Point(18, y);

        invoiceStatsDonut = new DonutChartPanel
        {
            Percent = 0,
            CenterText = "0",
            SubText = "Tổng HĐ",
            AccentColor = ModernUi.Green,
            PrimaryLabel = "",
            PrimaryValue = "",
            SecondaryLabel = "",
            SecondaryValue = "",
            Location = new Point(20, 38),
            Size = new Size(210, 160)
        };
        stats.Controls.Add(invoiceStatsDonut);

        invoiceStatsSummary = ModernUi.Label("", 9.2f, FontStyle.Regular, ModernUi.Text);
        invoiceStatsSummary.Location = new Point(250, 34);
        invoiceStatsSummary.Size = new Size(stats.Width - 270, 170);
        stats.Controls.Add(invoiceStatsSummary);

        page.Controls.Add(stats);

        var actions = ModernUi.Section("Thao tác nhanh", w - stats.Width - gap, 230);
        actions.Location = new Point(stats.Right + gap, y);
        AddInvoiceQuickActions(actions);

        page.Controls.Add(actions);

        void UpdateInvoiceStats()
        {
            var statInvoices = displayInvoices ?? new List<InvoiceDTO>();

            int totalCount = statInvoices.Count;
            int paidCount = statInvoices.Count(i => ViStatus(i.PaymentStatus) == "Đã thanh toán");
            int overdueCount = statInvoices.Count(i => ViStatus(i.PaymentStatus) == "Quá hạn");
            int pendingCount = totalCount - paidCount - overdueCount;

            decimal totalAmount = statInvoices.Sum(i => i.TotalAmount);
            decimal paidAmount = statInvoices.Sum(i => i.PaidAmount);
            decimal debtAmount = statInvoices.Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount));

            int paidPercent = totalCount == 0 ? 0 : (int)Math.Round(paidCount * 100m / totalCount);

            if (invoiceStatsDonut == null || invoiceStatsSummary == null)
            {
                return;
            }

            invoiceStatsDonut.Percent = paidPercent;
            invoiceStatsDonut.CenterText = totalCount.ToString("N0");
            invoiceStatsDonut.SubText = "Tổng HĐ";
            invoiceStatsDonut.PrimaryLabel = "";
            invoiceStatsDonut.PrimaryValue = "";
            invoiceStatsDonut.SecondaryLabel = "";
            invoiceStatsDonut.SecondaryValue = "";
            invoiceStatsDonut.Invalidate();

            invoiceStatsSummary.Text =
                 $"Tổng hóa đơn: {totalCount:N0}\r\n" +
               $"Đã thanh toán: {paidCount:N0}\r\n" +
               $"Chờ thanh toán: {pendingCount:N0}\r\n" +
               $"Quá hạn: {overdueCount:N0}\r\n" +
               $"Tổng phát sinh: {Money(totalAmount)} VNĐ\r\n" +
               $"Đã thu: {Money(paidAmount)} VNĐ\r\n" +
               $"Còn phải thu: {Money(debtAmount)} VNĐ";
        }

        page.AutoScrollMinSize = new Size(0, y + 320);

        ApplyInvoiceFilters();
    }

}
