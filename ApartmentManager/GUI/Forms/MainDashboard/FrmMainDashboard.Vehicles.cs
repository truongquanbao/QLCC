using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
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
    private void RenderVehicles()
    {
        if (IsResident)
        {
            RenderResidentVehicles();
            return;
        }

        RenderStaffVehicles();
    }

    private void RenderStaffVehicles()
    {
        var page = BeginPage("PHƯƠNG TIỆN", "");
        int w = PageWorkWidth();
        int y = 86;

        var vehicles = LoadVehicleRows(false);
        var residents = ResidentDAL.GetAllResidents();
        var filteredVehicles = new List<VehicleViewModel>(vehicles);
        VehicleViewModel selectedVehicle = vehicles.FirstOrDefault();
        bool suppressGridSelection = false;

        Button headerAddButton = null;
        Button headerCardButton = null;
        Button headerExportButton = null;

        var header = page.Controls.OfType<Panel>().FirstOrDefault(p => p.Dock == DockStyle.Top);
        if (header != null)
        {
            foreach (var quickSearch in header.Controls.OfType<RoundedPanel>()
                         .Where(p => p.Controls.OfType<TextBox>().Any(t => t.PlaceholderText == "Tìm kiếm nhanh..."))
                         .ToList())
            {
                header.Controls.Remove(quickSearch);
                quickSearch.Dispose();
            }

            headerAddButton = ModernUi.Button("+  Thêm phương tiện", ModernUi.Blue, 170, 38);
            headerCardButton = ModernUi.Button("Cấp thẻ xe", ModernUi.Orange, 140, 38);
            headerExportButton = ModernUi.Button("Xuất báo cáo", ModernUi.Green, 140, 38);
            header.Controls.Add(headerAddButton);
            header.Controls.Add(headerCardButton);
            header.Controls.Add(headerExportButton);

            void LayoutVehicleHeaderButtons()
            {
                int right = Math.Max(480, header.ClientSize.Width - 204);
                headerExportButton.SetBounds(right - headerExportButton.Width, 18, headerExportButton.Width, 38);
                headerCardButton.SetBounds(headerExportButton.Left - 12 - headerCardButton.Width, 18, headerCardButton.Width, 38);
                headerAddButton.SetBounds(headerCardButton.Left - 12 - headerAddButton.Width, 18, headerAddButton.Width, 38);
            }

            header.Resize += (_, _) => LayoutVehicleHeaderButtons();
            LayoutVehicleHeaderButtons();
        }

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 86);
        var typeFilter = AddVehicleFilter(filters, "Loại phương tiện", new[] { "Tất cả", "Ô tô", "Xe máy", "Xe đạp", "Khác" }, 16, 168);
        var buildingFilter = AddVehicleFilter(filters, "Tòa nhà", BuildFilterOptions(vehicles.Select(v => v.Building)), 204, 168);
        var areaFilter = AddVehicleFilter(filters, "Khu vực", BuildFilterOptions(vehicles.Select(v => v.Area)), 392, 178);
        var statusFilter = AddVehicleFilter(filters, "Trạng thái", new[] { "Tất cả", "Đang hoạt động", "Chờ duyệt", "Tạm khóa", "Hết hạn", "Đã hủy" }, 590, 178);
        var search = ModernUi.SearchBox("Tìm kiếm biển số, chủ xe, số thẻ...", Math.Max(260, w - 980), 34);
        search.Location = new Point(Math.Max(786, w - 462), 38);
        filters.Controls.Add(search);
        var searchInput = search.Controls.OfType<TextBox>().First();
        var refreshButton = ModernUi.OutlineButton("Làm mới", 108, 34);
        refreshButton.Location = new Point(w - 124, 38);
        filters.Controls.Add(refreshButton);
        page.Controls.Add(filters);

        y += 100;
        int leftW = (int)(w * 0.62);
        int rightW = w - leftW - 12;
        const int mainHeight = 486;

        var list = ModernUi.Section("Danh sách phương tiện (0)", leftW, mainHeight);
        list.Location = new Point(18, y);
        var listTitle = list.Controls.OfType<Label>().FirstOrDefault();
        var grid = ModernUi.Grid();
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 378);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ScrollBars = ScrollBars.Both;
        grid.RowTemplate.Height = 34;
        grid.ColumnHeadersHeight = 38;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.CellFormatting += (_, e) => ApplyGridCellStyle(e);
        list.Controls.Add(grid);
        var paging = ModernUi.Label("", 9f, FontStyle.Regular, ModernUi.Text);
        paging.Location = new Point(18, 438);
        paging.Size = new Size(list.Width - 36, 26);
        list.Controls.Add(paging);
        page.Controls.Add(list);

        var detail = ModernUi.Section("Chi tiết phương tiện", rightW, mainHeight);
        detail.Location = new Point(list.Right + 12, y);
        var detailBody = new Panel
        {
            Location = new Point(12, 42),
            Size = new Size(detail.Width - 24, detail.Height - 54),
            BackColor = Color.White
        };
        detail.Controls.Add(detailBody);
        page.Controls.Add(detail);

        y += mainHeight + 14;
        int statsW = (int)(w * 0.40);
        int chartW = (w - statsW - 24) / 2;
        var stats = ModernUi.Section("Thống kê phương tiện", statsW, 218);
        stats.Location = new Point(18, y);
        page.Controls.Add(stats);
        var buildingChart = ModernUi.Section("Phương tiện theo tòa nhà", chartW, 218);
        buildingChart.Location = new Point(stats.Right + 12, y);
        page.Controls.Add(buildingChart);
        var areaChart = ModernUi.Section("Phương tiện theo khu vực", w - statsW - chartW - 24, 218);
        areaChart.Location = new Point(buildingChart.Right + 12, y);
        page.Controls.Add(areaChart);

        ComboBox AddVehicleFilter(Control parent, string label, string[] options, int x, int width)
        {
            var lbl = ModernUi.Label(label, 8.4f, FontStyle.Bold, ModernUi.Text);
            lbl.Location = new Point(x, 14);
            lbl.Size = new Size(width, 18);
            parent.Controls.Add(lbl);
            var combo = ModernUi.ComboBox(options, width);
            combo.Location = new Point(x, 38);
            combo.Height = 30;
            parent.Controls.Add(combo);
            return combo;
        }

        void ReloadData(int selectedId = 0)
        {
            int targetId = selectedId > 0 ? selectedId : selectedVehicle?.VehicleID ?? 0;
            vehicles = LoadVehicleRows(false);
            residents = ResidentDAL.GetAllResidents();
            selectedVehicle = targetId > 0
                ? vehicles.FirstOrDefault(v => v.VehicleID == targetId)
                : vehicles.FirstOrDefault();
            ApplyFilters();
        }

        void ApplyFilters()
        {
            string type = typeFilter.Text;
            string building = buildingFilter.Text;
            string area = areaFilter.Text;
            string status = statusFilter.Text;
            string keyword = searchInput.Text.Trim();

            filteredVehicles = vehicles
                .Where(v => type == "Tất cả" || v.VehicleType == type || (type == "Khác" && v.VehicleType != "Ô tô" && v.VehicleType != "Xe máy" && v.VehicleType != "Xe đạp"))
                .Where(v => building == "Tất cả" || v.Building == building)
                .Where(v => area == "Tất cả" || v.Area == area)
                .Where(v => status == "Tất cả" || v.Status == status)
                .Where(v => keyword.Length == 0 ||
                    v.PlateNumber.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    v.OwnerName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    v.CardNumber.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    v.Apartment.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            if (selectedVehicle == null || (selectedVehicle.VehicleID > 0 && !filteredVehicles.Any(v => v.VehicleID == selectedVehicle.VehicleID)))
            {
                selectedVehicle = filteredVehicles.FirstOrDefault();
            }

            RefreshGrid();
            RenderDetail();
        }

        void RefreshGrid()
        {
            var table = new DataTable();
            table.Columns.Add("Chọn", typeof(bool));
            table.Columns.Add("STT", typeof(int));
            table.Columns.Add("Biển số");
            table.Columns.Add("Loại xe");
            table.Columns.Add("Chủ sở hữu");
            table.Columns.Add("Số thẻ");
            table.Columns.Add("Căn hộ");
            table.Columns.Add("Khu vực");
            table.Columns.Add("Trạng thái");
            table.Columns.Add("Hạn thẻ");

            for (int i = 0; i < filteredVehicles.Count; i++)
            {
                var vehicle = filteredVehicles[i];
                table.Rows.Add(
                    selectedVehicle != null && vehicle.VehicleID == selectedVehicle.VehicleID,
                    i + 1,
                    vehicle.PlateNumber,
                    vehicle.VehicleType,
                    vehicle.OwnerName,
                    vehicle.CardNumber,
                    vehicle.Apartment,
                    vehicle.Area,
                    vehicle.Status,
                    DateText(vehicle.ExpiredAt));
            }

            suppressGridSelection = true;
            grid.DataSource = table;
            if (grid.Columns.Count > 0)
            {
                int[] widths = { 46, 46, 104, 82, 132, 92, 82, 118, 112, 104 };
                for (int i = 0; i < Math.Min(widths.Length, grid.Columns.Count); i++)
                {
                    grid.Columns[i].Width = widths[i];
                    grid.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                    grid.Columns[i].DefaultCellStyle.Alignment = i is 2 or 4 or 7 ? DataGridViewContentAlignment.MiddleLeft : DataGridViewContentAlignment.MiddleCenter;
                }
            }

            int selectedIndex = selectedVehicle == null ? -1 : filteredVehicles.FindIndex(v => v.VehicleID == selectedVehicle.VehicleID);
            if (selectedIndex >= 0 && selectedIndex < grid.Rows.Count)
            {
                grid.ClearSelection();
                grid.Rows[selectedIndex].Selected = true;
                grid.CurrentCell = grid.Rows[selectedIndex].Cells[Math.Min(1, grid.Columns.Count - 1)];
            }
            suppressGridSelection = false;

            listTitle.Text = $"DANH SÁCH PHƯƠNG TIỆN ({filteredVehicles.Count})";
            paging.Text = filteredVehicles.Count == 0
                ? "Không có phương tiện phù hợp"
                : $"Hiển thị 1 - {filteredVehicles.Count:N0} / {vehicles.Count:N0} bản ghi";
            RefreshBottom();
        }

        void RenderDetail()
        {
            detailBody.Controls.Clear();
            if (selectedVehicle == null)
            {
                var empty = ModernUi.Label("Chọn một phương tiện để xem chi tiết, hoặc bấm Thêm phương tiện.", 10f, FontStyle.Regular, ModernUi.Muted);
                empty.SetBounds(12, 20, detailBody.Width - 24, 28);
                detailBody.Controls.Add(empty);
                return;
            }

            bool isNew = selectedVehicle.VehicleID <= 0;
            var photo = new PictureBox
            {
                Location = new Point(4, 2),
                Size = new Size(Math.Max(180, detailBody.Width / 2 - 14), 150),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = BuildVehicleImage(selectedVehicle.VehicleType)
            };
            detailBody.Controls.Add(photo);

            int infoX = photo.Right + 18;
            int infoW = Math.Max(180, detailBody.Width - infoX - 4);
            var plate = AddVehicleInput(detailBody, "Biển số", selectedVehicle.PlateNumber, infoX, 4, infoW);
            var type = AddVehicleValueCombo(detailBody, "Loại xe", VehicleTypeOptions(), VehicleTypeDbValue(selectedVehicle.VehicleType), infoX, 44, infoW);
            var brand = AddVehicleInput(detailBody, "Hãng xe", selectedVehicle.Brand, infoX, 84, infoW);
            var color = AddVehicleInput(detailBody, "Màu sắc", selectedVehicle.Color, infoX, 124, infoW);

            int rowY = 166;
            var resident = AddResidentCombo(detailBody, "Chủ sở hữu", residents, selectedVehicle.ResidentID, 4, rowY, detailBody.Width - 8);
            resident.Enabled = isNew;
            rowY += 42;
            var model = AddVehicleInput(detailBody, "Dòng xe", selectedVehicle.Model, 4, rowY, (detailBody.Width - 20) / 2);
            var yearMade = AddVehicleInput(detailBody, "Năm sản xuất", selectedVehicle.YearMade <= 0 ? DateTime.Today.Year.ToString(CultureInfo.InvariantCulture) : selectedVehicle.YearMade.ToString(CultureInfo.InvariantCulture), model.Right + 12, rowY, detailBody.Width - model.Right - 16);
            rowY += 42;
            var cardNumber = AddVehicleInput(detailBody, "Số thẻ", selectedVehicle.CardNumber, 4, rowY, (detailBody.Width - 20) / 2);
            var area = AddVehicleValueCombo(detailBody, "Khu vực gửi", VehicleParkingAreaOptions(), selectedVehicle.Area, cardNumber.Right + 12, rowY, detailBody.Width - cardNumber.Right - 16);
            rowY += 42;
            var expired = AddVehicleInput(detailBody, "Ngày hết hạn", DateText(selectedVehicle.ExpiredAt), 4, rowY, (detailBody.Width - 20) / 2);
            var status = AddVehicleValueCombo(detailBody, "Trạng thái", VehicleStatusOptions(), VehicleStatusDbValue(selectedVehicle.Status), expired.Right + 12, rowY, detailBody.Width - expired.Right - 16);
            rowY += 42;
            var note = AddVehicleInput(detailBody, "Ghi chú", selectedVehicle.Note, 4, rowY, detailBody.Width - 8);

            int buttonY = detailBody.Height - 38;
            int buttonW = Math.Max(82, (detailBody.Width - 28) / 4);
            var update = ModernUi.Button(isNew ? "Lưu mới" : "Cập nhật", ModernUi.Blue, buttonW, 32);
            var renew = ModernUi.Button("Gia hạn thẻ", ModernUi.Orange, buttonW, 32);
            var lockDetail = ModernUi.Button("Tạm khóa", ModernUi.Red, buttonW, 32);
            var delete = ModernUi.OutlineButton("Xóa", buttonW, 32);
            update.Location = new Point(4, buttonY);
            renew.Location = new Point(update.Right + 8, buttonY);
            lockDetail.Location = new Point(renew.Right + 8, buttonY);
            delete.Location = new Point(lockDetail.Right + 8, buttonY);
            delete.ForeColor = ModernUi.Text;
            detailBody.Controls.Add(update);
            detailBody.Controls.Add(renew);
            detailBody.Controls.Add(lockDetail);
            detailBody.Controls.Add(delete);

            renew.Enabled = !isNew;
            lockDetail.Enabled = !isNew;
            delete.Enabled = !isNew;

            update.Click += (_, _) =>
            {
                var savedId = SaveVehicleFromInputs(
                    selectedVehicle.VehicleID,
                    resident.GetSelectedValueInt(),
                    plate.Text,
                    ComboBoxHelper.GetSelectedValueString(type),
                    brand.Text,
                    model.Text,
                    yearMade.Text,
                    color.Text,
                    note.Text,
                    ComboBoxHelper.GetSelectedValueString(area),
                    cardNumber.Text,
                    expired.Text,
                    ComboBoxHelper.GetSelectedValueString(status));

                if (savedId > 0)
                {
                    ReloadData(savedId);
                }
            };

            renew.Click += (_, _) =>
            {
                selectedVehicle.ExpiredAt = DateTime.Today.AddYears(1);
                selectedVehicle.Status = "Đang hoạt động";
                PersistExistingVehicle(selectedVehicle, "Active", true);
                ReloadData(selectedVehicle.VehicleID);
            };

            lockDetail.Click += (_, _) =>
            {
                selectedVehicle.Status = "Tạm khóa";
                PersistExistingVehicle(selectedVehicle, "Inactive", true);
                ReloadData(selectedVehicle.VehicleID);
            };

            delete.Click += (_, _) => DeleteSelectedVehicle();
        }

        int SaveVehicleFromInputs(
            int vehicleId,
            int residentId,
            string plate,
            string vehicleType,
            string brand,
            string model,
            string yearMade,
            string color,
            string note,
            string area,
            string cardNumber,
            string expired,
            string status)
        {
            if (residentId <= 0)
            {
                MessageBox.Show("Vui lòng chọn cư dân.", "Phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }

            if (!int.TryParse(yearMade.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedYear))
            {
                MessageBox.Show("Năm sản xuất không hợp lệ.", "Phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }

            DateTime? parsedExpired = ParseVehicleDate(expired);
            if (!parsedExpired.HasValue)
            {
                MessageBox.Show("Ngày hết hạn không hợp lệ. Vui lòng nhập dạng dd/MM/yyyy.", "Phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }

            if (vehicleId <= 0)
            {
                var create = VehicleBLL.CreateVehicle(
                    residentId,
                    plate.Trim(),
                    string.IsNullOrWhiteSpace(vehicleType) ? "Other" : vehicleType,
                    brand.Trim(),
                    model.Trim(),
                    parsedYear,
                    color.Trim(),
                    note.Trim(),
                    area,
                    cardNumber.Trim(),
                    parsedExpired);

                if (!create.Success)
                {
                    MessageBox.Show(create.Message, "Phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 0;
                }

                VehicleBLL.UpdateVehicleStatus(create.VehicleID, string.IsNullOrWhiteSpace(status) ? "Active" : status);
                MessageBox.Show("Đã thêm phương tiện.", "Phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return create.VehicleID;
            }

            var update = VehicleBLL.UpdateVehicle(
                vehicleId,
                plate.Trim(),
                string.IsNullOrWhiteSpace(vehicleType) ? "Other" : vehicleType,
                brand.Trim(),
                model.Trim(),
                parsedYear,
                color.Trim(),
                note.Trim(),
                area,
                cardNumber.Trim(),
                parsedExpired);

            if (!update.Success)
            {
                MessageBox.Show(update.Message, "Phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }

            VehicleBLL.UpdateVehicleStatus(vehicleId, string.IsNullOrWhiteSpace(status) ? "Active" : status);
            MessageBox.Show("Đã cập nhật phương tiện.", "Phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return vehicleId;
        }

        void PersistExistingVehicle(VehicleViewModel vehicle, string status, bool showMessage)
        {
            var result = VehicleBLL.UpdateVehicle(
                vehicle.VehicleID,
                vehicle.PlateNumber,
                VehicleTypeDbValue(vehicle.VehicleType),
                vehicle.Brand,
                Display(vehicle.Model, vehicle.Brand),
                vehicle.YearMade <= 0 ? DateTime.Today.Year : vehicle.YearMade,
                vehicle.Color,
                vehicle.Note,
                vehicle.Area,
                vehicle.CardNumber,
                vehicle.ExpiredAt);

            if (result.Success)
            {
                VehicleBLL.UpdateVehicleStatus(vehicle.VehicleID, status);
                if (showMessage)
                {
                    MessageBox.Show("Đã cập nhật trạng thái thẻ xe.", "Phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else if (showMessage)
            {
                MessageBox.Show(result.Message, "Phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void AddVehicle()
        {
            var resident = residents.FirstOrDefault();
            selectedVehicle = BuildVehicleDraft(resident);
            RenderDetail();
        }

        void IssueCard()
        {
            if (selectedVehicle == null || selectedVehicle.VehicleID <= 0)
            {
                MessageBox.Show("Vui lòng chọn phương tiện đã lưu.", "Cấp thẻ xe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedVehicle.CardNumber))
            {
                selectedVehicle.CardNumber = $"TH{selectedVehicle.VehicleID:000000}";
            }

            selectedVehicle.ExpiredAt = DateTime.Today.AddYears(1);
            selectedVehicle.Status = "Đang hoạt động";
            PersistExistingVehicle(selectedVehicle, "Active", true);
            ReloadData(selectedVehicle.VehicleID);
        }

        void DeleteSelectedVehicle()
        {
            if (selectedVehicle == null || selectedVehicle.VehicleID <= 0)
            {
                MessageBox.Show("Vui lòng chọn phương tiện.", "Xóa phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Xóa phương tiện {selectedVehicle.PlateNumber}?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            var result = VehicleBLL.DeleteVehicle(selectedVehicle.VehicleID);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Xóa phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            selectedVehicle = null;
            ReloadData();
        }

        void RefreshBottom()
        {
            foreach (var panel in new[] { stats, buildingChart, areaChart })
            {
                foreach (Control child in panel.Controls.Cast<Control>().Skip(1).ToList())
                {
                    panel.Controls.Remove(child);
                    child.Dispose();
                }
            }

            int miniW = Math.Max(92, (stats.Width - 52) / 3);
            int miniH = 72;
            AddVehicleMiniStat(stats, "Tổng phương tiện", vehicles.Count, ModernUi.Blue, 16, 46, miniW, miniH);
            AddVehicleMiniStat(stats, "Ô tô", vehicles.Count(v => v.VehicleType == "Ô tô"), ModernUi.Blue, 28 + miniW, 46, miniW, miniH);
            AddVehicleMiniStat(stats, "Xe máy", vehicles.Count(v => v.VehicleType == "Xe máy"), ModernUi.Green, 40 + miniW * 2, 46, miniW, miniH);
            AddVehicleMiniStat(stats, "Chờ duyệt", vehicles.Count(v => v.Status == "Chờ duyệt"), ModernUi.Orange, 16, 130, miniW, miniH);
            AddVehicleMiniStat(stats, "Hết hạn", vehicles.Count(v => v.Status == "Hết hạn"), ModernUi.Red, 28 + miniW, 130, miniW, miniH);

            var byBuilding = new BarChartPanel
            {
                Location = new Point(12, 42),
                Size = new Size(buildingChart.Width - 24, 160),
                BarColor = ModernUi.Blue,
                ShowValueLabels = true,
                GridSteps = 4
            };
            byBuilding.Bars.AddRange(vehicles.GroupBy(v => Display(v.Building, "Chưa rõ")).OrderBy(g => g.Key).Select(g => (g.Key.Replace("Tòa ", ""), g.Count())));
            byBuilding.AxisMax = Math.Max(1, byBuilding.Bars.Count == 0 ? 1 : byBuilding.Bars.Max(b => b.Value) + 1);
            buildingChart.Controls.Add(byBuilding);

            var byArea = new BarChartPanel
            {
                Location = new Point(12, 42),
                Size = new Size(areaChart.Width - 24, 160),
                BarColor = ModernUi.Orange,
                ShowValueLabels = true,
                GridSteps = 4
            };
            byArea.Bars.AddRange(vehicles.GroupBy(v => Display(v.Area, "Chưa rõ")).OrderBy(g => g.Key).Select(g => (g.Key.Replace("Khu ", ""), g.Count())));
            byArea.AxisMax = Math.Max(1, byArea.Bars.Count == 0 ? 1 : byArea.Bars.Max(b => b.Value) + 1);
            areaChart.Controls.Add(byArea);
        }

        typeFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        buildingFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        areaFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        statusFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        searchInput.TextChanged += (_, _) => ApplyFilters();
        refreshButton.Click += (_, _) =>
        {
            typeFilter.SelectedIndex = 0;
            buildingFilter.SelectedIndex = 0;
            areaFilter.SelectedIndex = 0;
            statusFilter.SelectedIndex = 0;
            searchInput.Clear();
            ReloadData();
        };
        grid.SelectionChanged += (_, _) =>
        {
            if (suppressGridSelection)
            {
                return;
            }

            if (grid.CurrentRow?.Index >= 0 && grid.CurrentRow.Index < filteredVehicles.Count)
            {
                selectedVehicle = filteredVehicles[grid.CurrentRow.Index];
                RefreshGrid();
                RenderDetail();
            }
        };

        if (headerAddButton != null)
        {
            headerAddButton.Click += (_, _) => AddVehicle();
        }

        if (headerCardButton != null)
        {
            headerCardButton.Click += (_, _) => IssueCard();
        }

        if (headerExportButton != null)
        {
            headerExportButton.Click += (_, _) => ExportVehicleCsv(vehicles);
        }

        ApplyFilters();
        if (ConsumeQuickAction("vehicles", "add"))
        {
            AddVehicle();
        }
        page.AutoScroll = true;
        page.AutoScrollMinSize = new Size(0, y + 250);
    }

    private void RenderResidentVehicles()
    {
        var resident = GetCurrentResident();
        var page = BeginPage("Xe của tôi", resident == null ? "" : $"Căn hộ {Display(resident.ApartmentCode)}");
        int w = PageWorkWidth();
        int y = 86;

        if (resident == null)
        {
            var empty = ModernUi.Section("Không tìm thấy hồ sơ cư dân", w, 180);
            empty.Location = new Point(18, y);
            empty.Controls.Add(ModernUi.Label("Tài khoản này chưa được liên kết với hồ sơ cư dân nên chưa thể đăng ký phương tiện.", 10f, FontStyle.Regular, ModernUi.Text));
            empty.Controls[1].SetBounds(18, 58, empty.Width - 36, 48);
            page.Controls.Add(empty);
            return;
        }

        var vehicles = LoadVehicleRows(true);
        VehicleViewModel selectedVehicle = vehicles.FirstOrDefault();
        bool suppressGridSelection = false;

        int formW = Math.Max(420, (int)(w * 0.42));
        int listW = w - formW - 12;
        var form = ModernUi.Section("Đăng ký phương tiện", formW, 372);
        form.Location = new Point(18, y);
        page.Controls.Add(form);

        var type = AddVehicleValueCombo(form, "Loại xe", VehicleTypeOptions(), "Motorcycle", 18, 52, (form.Width - 54) / 2);
        var plate = AddVehicleInput(form, "Biển số", "", type.Right + 18, 52, form.Width - type.Right - 36);
        var brand = AddVehicleInput(form, "Hãng xe", "", 18, 104, (form.Width - 54) / 2);
        var model = AddVehicleInput(form, "Dòng xe", "", brand.Right + 18, 104, form.Width - brand.Right - 36);
        var yearMade = AddVehicleInput(form, "Năm sản xuất", DateTime.Today.Year.ToString(CultureInfo.InvariantCulture), 18, 156, (form.Width - 54) / 2);
        var color = AddVehicleInput(form, "Màu sắc", "", yearMade.Right + 18, 156, form.Width - yearMade.Right - 36);
        var area = AddVehicleValueCombo(form, "Khu vực gửi", VehicleParkingAreaOptions(), "Khu ngoài trời", 18, 208, (form.Width - 54) / 2);
        var note = AddVehicleInput(form, "Ghi chú", "", area.Right + 18, 208, form.Width - area.Right - 36);
        var submit = ModernUi.Button("Gửi đăng ký", ModernUi.Blue, 138, 34);
        submit.Location = new Point(18, 304);
        form.Controls.Add(submit);

        var list = ModernUi.Section($"Phương tiện đã đăng ký ({vehicles.Count})", listW, 372);
        list.Location = new Point(form.Right + 12, y);
        var listTitle = list.Controls.OfType<Label>().FirstOrDefault();
        var grid = ModernUi.Grid();
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 284);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ScrollBars = ScrollBars.Both;
        grid.RowTemplate.Height = 34;
        grid.CellFormatting += (_, e) => ApplyGridCellStyle(e);
        list.Controls.Add(grid);
        page.Controls.Add(list);

        y += 390;
        var detail = ModernUi.Section("Chi tiết thẻ xe", w, 236);
        detail.Location = new Point(18, y);
        var detailBody = new Panel
        {
            Location = new Point(12, 42),
            Size = new Size(detail.Width - 24, detail.Height - 54),
            BackColor = Color.White
        };
        detail.Controls.Add(detailBody);
        page.Controls.Add(detail);

        void ReloadResidentData(int selectedId = 0)
        {
            vehicles = LoadVehicleRows(true);
            selectedVehicle = selectedId > 0
                ? vehicles.FirstOrDefault(v => v.VehicleID == selectedId)
                : vehicles.FirstOrDefault();
            RefreshResidentGrid();
            RenderResidentDetail();
        }

        void RefreshResidentGrid()
        {
            var table = new DataTable();
            table.Columns.Add("STT", typeof(int));
            table.Columns.Add("Biển số");
            table.Columns.Add("Loại xe");
            table.Columns.Add("Hãng xe");
            table.Columns.Add("Khu vực");
            table.Columns.Add("Hạn thẻ");
            table.Columns.Add("Trạng thái");

            for (int i = 0; i < vehicles.Count; i++)
            {
                var vehicle = vehicles[i];
                table.Rows.Add(i + 1, vehicle.PlateNumber, vehicle.VehicleType, Display(vehicle.Brand), Display(vehicle.Area), DateText(vehicle.ExpiredAt), vehicle.Status);
            }

            suppressGridSelection = true;
            grid.DataSource = table;
            if (grid.Columns.Count > 0)
            {
                int[] widths = { 50, 120, 95, 120, 128, 110, 120 };
                for (int i = 0; i < Math.Min(widths.Length, grid.Columns.Count); i++)
                {
                    grid.Columns[i].Width = widths[i];
                    grid.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }

            int selectedIndex = selectedVehicle == null ? -1 : vehicles.FindIndex(v => v.VehicleID == selectedVehicle.VehicleID);
            if (selectedIndex >= 0 && selectedIndex < grid.Rows.Count)
            {
                grid.ClearSelection();
                grid.Rows[selectedIndex].Selected = true;
                grid.CurrentCell = grid.Rows[selectedIndex].Cells[Math.Min(1, grid.Columns.Count - 1)];
            }

            suppressGridSelection = false;
            listTitle.Text = $"PHƯƠNG TIỆN ĐÃ ĐĂNG KÝ ({vehicles.Count})";
        }

        void RenderResidentDetail()
        {
            detailBody.Controls.Clear();
            if (selectedVehicle == null)
            {
                var empty = ModernUi.Label("Bạn chưa có phương tiện nào được đăng ký.", 10f, FontStyle.Regular, ModernUi.Muted);
                empty.SetBounds(18, 18, detailBody.Width - 36, 26);
                detailBody.Controls.Add(empty);
                return;
            }

            var image = new PictureBox
            {
                Location = new Point(8, 8),
                Size = new Size(220, 138),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = BuildVehicleImage(selectedVehicle.VehicleType)
            };
            detailBody.Controls.Add(image);

            int x = image.Right + 28;
            int colW = Math.Max(160, (detailBody.Width - x - 36) / 3);
            AddVehicleInfo(detailBody, "Biển số", selectedVehicle.PlateNumber, x, 14, colW);
            AddVehicleInfo(detailBody, "Loại xe", selectedVehicle.VehicleType, x + colW, 14, colW);
            AddVehicleInfo(detailBody, "Trạng thái", selectedVehicle.Status, x + colW * 2, 14, colW);
            AddVehicleInfo(detailBody, "Số thẻ", selectedVehicle.CardNumber, x, 74, colW);
            AddVehicleInfo(detailBody, "Khu vực gửi", selectedVehicle.Area, x + colW, 74, colW);
            AddVehicleInfo(detailBody, "Hạn thẻ", DateText(selectedVehicle.ExpiredAt), x + colW * 2, 74, colW);

            var badge = ModernUi.Badge(selectedVehicle.Status, VehicleStatusColor(selectedVehicle.Status));
            badge.SetBounds(x, 134, Math.Min(220, detailBody.Width - x - 18), 28);
            detailBody.Controls.Add(badge);
        }

        submit.Click += (_, _) =>
        {
            if (!int.TryParse(yearMade.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedYear))
            {
                MessageBox.Show("Năm sản xuất không hợp lệ.", "Đăng ký phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = VehicleBLL.CreateVehicle(
                resident.ResidentID,
                plate.Text.Trim(),
                ComboBoxHelper.GetSelectedValueString(type),
                brand.Text.Trim(),
                model.Text.Trim(),
                parsedYear,
                color.Text.Trim(),
                note.Text.Trim(),
                ComboBoxHelper.GetSelectedValueString(area),
                string.Empty,
                DateTime.Today.AddYears(1));

            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Đăng ký phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            VehicleBLL.UpdateVehicleStatus(result.VehicleID, "Pending");
            MessageBox.Show("Đã gửi đăng ký phương tiện. Ban quản lý sẽ kiểm tra và cấp thẻ.", "Đăng ký phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Information);
            plate.Clear();
            brand.Clear();
            model.Clear();
            color.Clear();
            note.Clear();
            ReloadResidentData(result.VehicleID);
        };

        grid.SelectionChanged += (_, _) =>
        {
            if (suppressGridSelection)
            {
                return;
            }

            if (grid.CurrentRow?.Index >= 0 && grid.CurrentRow.Index < vehicles.Count)
            {
                selectedVehicle = vehicles[grid.CurrentRow.Index];
                RefreshResidentGrid();
                RenderResidentDetail();
            }
        };

        RefreshResidentGrid();
        RenderResidentDetail();
        page.AutoScroll = true;
        page.AutoScrollMinSize = new Size(0, y + 280);
    }

    private List<VehicleViewModel> LoadVehicleRows(bool currentResidentOnly)
    {
        ResidentDTO resident = null;
        if (currentResidentOnly)
        {
            resident = GetCurrentResident();
            if (resident == null)
            {
                return new List<VehicleViewModel>();
            }
        }

        var rows = currentResidentOnly
            ? VehicleDAL.GetVehiclesByResident(resident.ResidentID)
            : VehicleDAL.GetAllVehicles();

        return rows
            .Select(MapVehicleRow)
            .OrderBy(v => v.Building)
            .ThenBy(v => v.Apartment)
            .ThenBy(v => v.PlateNumber)
            .ToList();
    }

    private VehicleViewModel MapVehicleRow(dynamic row)
    {
        int vehicleId = VehicleDynamicInt(row, "VehicleID");
        DateTime registeredAt = VehicleDynamicDate(row, "RegisteredAt", "CreatedAt") ?? DateTime.Today;
        DateTime expiredAt = VehicleDynamicDate(row, "ExpiredAt") ?? registeredAt.AddYears(1);
        string apartment = VehicleDynamicString(row, "ApartmentCode");
        string building = VehicleDynamicString(row, "BuildingName");
        if (string.IsNullOrWhiteSpace(building) && apartment.Length > 0)
        {
            building = $"Tòa {apartment[0]}";
        }

        return new VehicleViewModel
        {
            VehicleID = vehicleId,
            ResidentID = VehicleDynamicInt(row, "ResidentID"),
            VehicleCode = $"XE{vehicleId:000}",
            PlateNumber = VehicleDynamicString(row, "LicensePlate", "PlateNumber"),
            VehicleType = VehicleTypeText(VehicleDynamicString(row, "VehicleType")),
            Brand = VehicleDynamicString(row, "Brand"),
            Model = VehicleDynamicString(row, "Model"),
            YearMade = VehicleDynamicInt(row, "YearMade", DateTime.Today.Year),
            Color = VehicleDynamicString(row, "Color"),
            CardNumber = VehicleDynamicString(row, new[] { "CardNumber" }, $"TH{vehicleId:000000}"),
            OwnerName = VehicleDynamicString(row, "ResidentName", "OwnerName", "FullName"),
            Phone = VehicleDynamicString(row, "Phone"),
            Building = string.IsNullOrWhiteSpace(building) ? "Chưa rõ" : building,
            Apartment = string.IsNullOrWhiteSpace(apartment) ? "-" : apartment,
            Area = VehicleDynamicString(row, new[] { "ParkingArea", "Area" }, "Khu ngoài trời"),
            Status = VehicleStatusText(VehicleDynamicString(row, "Status"), expiredAt),
            Note = VehicleDynamicString(row, new[] { "Note" }, string.Empty),
            RegisteredAt = registeredAt,
            ExpiredAt = expiredAt,
            UpdatedAt = VehicleDynamicDate(row, "UpdatedAt")
        };
    }

    private VehicleViewModel BuildVehicleDraft(ResidentDTO resident)
    {
        return new VehicleViewModel
        {
            VehicleID = 0,
            ResidentID = resident?.ResidentID ?? 0,
            VehicleCode = "Mới",
            PlateNumber = string.Empty,
            VehicleType = "Xe máy",
            Brand = string.Empty,
            Model = string.Empty,
            YearMade = DateTime.Today.Year,
            Color = string.Empty,
            CardNumber = string.Empty,
            OwnerName = Display(resident?.FullName, ""),
            Phone = Display(resident?.Phone, ""),
            Building = !string.IsNullOrWhiteSpace(resident?.ApartmentCode) ? $"Tòa {resident.ApartmentCode[0]}" : "",
            Apartment = Display(resident?.ApartmentCode, ""),
            Area = "Khu ngoài trời",
            Status = "Đang hoạt động",
            RegisteredAt = DateTime.Today,
            ExpiredAt = DateTime.Today.AddYears(1)
        };
    }

    private static TextBox AddVehicleInput(Control parent, string label, string value, int x, int y, int width)
    {
        var lbl = ModernUi.Label(label + ":", 8.6f, FontStyle.Regular, ModernUi.Muted);
        lbl.SetBounds(x, y, width, 18);
        parent.Controls.Add(lbl);
        var input = ModernUi.TextBox("", width);
        input.Text = value ?? string.Empty;
        input.SetBounds(x, y + 18, width, 28);
        parent.Controls.Add(input);
        return input;
    }

    private static ComboBox AddVehicleValueCombo(Control parent, string label, IEnumerable<(string Text, string Value)> options, string selectedValue, int x, int y, int width)
    {
        var lbl = ModernUi.Label(label + ":", 8.6f, FontStyle.Regular, ModernUi.Muted);
        lbl.SetBounds(x, y, width, 18);
        parent.Controls.Add(lbl);

        var combo = ModernUi.ComboBox(Array.Empty<string>(), width);
        combo.SetBounds(x, y + 18, width, 28);
        foreach (var option in options)
        {
            combo.AddOption(option.Text, option.Value);
        }

        ComboBoxHelper.SelectValue(combo, selectedValue);
        if (combo.SelectedIndex < 0 && combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }

        parent.Controls.Add(combo);
        return combo;
    }

    private static ComboBox AddResidentCombo(Control parent, string label, IEnumerable<ResidentDTO> residents, int selectedResidentId, int x, int y, int width)
    {
        var lbl = ModernUi.Label(label + ":", 8.6f, FontStyle.Regular, ModernUi.Muted);
        lbl.SetBounds(x, y, width, 18);
        parent.Controls.Add(lbl);

        var combo = ModernUi.ComboBox(Array.Empty<string>(), width);
        combo.SetBounds(x, y + 18, width, 28);
        foreach (var resident in residents)
        {
            string text = $"{Display(resident.ApartmentCode)} - {Display(resident.FullName)}";
            combo.AddOption(text, resident.ResidentID);
        }

        ComboBoxHelper.SelectValue(combo, selectedResidentId);
        if (combo.SelectedIndex < 0 && combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }

        parent.Controls.Add(combo);
        return combo;
    }

    private static void AddVehicleInfo(Control parent, string label, string value, int x, int y, int width)
    {
        var caption = ModernUi.Label(label, 8.2f, FontStyle.Bold, ModernUi.Muted);
        caption.SetBounds(x, y, width, 16);
        parent.Controls.Add(caption);

        var text = ModernUi.Label(Display(value), 9.5f, FontStyle.Bold, ModernUi.Text);
        text.SetBounds(x, y + 18, width, 24);
        text.AutoEllipsis = true;
        parent.Controls.Add(text);
    }

    private static void AddVehicleMiniStat(Control parent, string title, int value, Color color, int x, int y, int width, int height)
    {
        var card = ModernUi.CardPanel(6);
        card.SetBounds(x, y, width, height);
        var valueLabel = ModernUi.Label(value.ToString("N0"), 15f, FontStyle.Bold, color);
        valueLabel.SetBounds(10, 8, width - 20, 24);
        card.Controls.Add(valueLabel);
        var titleLabel = ModernUi.Label(title, 8.2f, FontStyle.Bold, ModernUi.Text);
        titleLabel.SetBounds(10, 36, width - 20, 26);
        titleLabel.AutoEllipsis = true;
        card.Controls.Add(titleLabel);
        parent.Controls.Add(card);
    }

    private static IEnumerable<(string Text, string Value)> VehicleTypeOptions()
    {
        yield return ("Ô tô", "Car");
        yield return ("Xe máy", "Motorcycle");
        yield return ("Xe đạp", "Bicycle");
        yield return ("Xe đạp điện", "ElectricBike");
        yield return ("Xe tải", "Truck");
        yield return ("Khác", "Other");
    }

    private static IEnumerable<(string Text, string Value)> VehicleStatusOptions()
    {
        yield return ("Đang hoạt động", "Active");
        yield return ("Chờ duyệt", "Pending");
        yield return ("Tạm khóa", "Inactive");
        yield return ("Hết hạn", "Expired");
        yield return ("Đã hủy", "Sold");
    }

    private static IEnumerable<(string Text, string Value)> VehicleParkingAreaOptions()
    {
        yield return ("Hầm B1", "Hầm B1");
        yield return ("Hầm B2", "Hầm B2");
        yield return ("Khu ngoài trời", "Khu ngoài trời");
        yield return ("Khu xe đạp", "Khu xe đạp");
    }

    private static string[] BuildFilterOptions(IEnumerable<string> values)
    {
        return new[] { "Tất cả" }
            .Concat(values.Where(v => !string.IsNullOrWhiteSpace(v) && v != "-").Distinct().OrderBy(v => v))
            .ToArray();
    }

    private static string VehicleTypeText(string value)
    {
        return (value ?? string.Empty).Trim() switch
        {
            "Car" => "Ô tô",
            "Motorcycle" or "Motorbike" or "Scooter" => "Xe máy",
            "Bicycle" => "Xe đạp",
            "ElectricBike" => "Xe đạp điện",
            "Truck" => "Xe tải",
            "" => "Khác",
            "-" => "Khác",
            var other => ViVehicleType(other)
        };
    }

    private static string VehicleTypeDbValue(string value)
    {
        return (value ?? string.Empty).Trim() switch
        {
            "Ô tô" => "Car",
            "Xe máy" => "Motorcycle",
            "Xe đạp" => "Bicycle",
            "Xe đạp điện" => "ElectricBike",
            "Xe tải" => "Truck",
            "" => "Other",
            "-" => "Other",
            var other => other
        };
    }

    private static string VehicleStatusText(string value, DateTime expiredAt)
    {
        string status = (value ?? string.Empty).Trim() switch
        {
            "Active" => "Đang hoạt động",
            "Inactive" => "Tạm khóa",
            "Pending" => "Chờ duyệt",
            "Expired" => "Hết hạn",
            "Sold" => "Đã hủy",
            "" => "Đang hoạt động",
            "-" => "Đang hoạt động",
            var other => ViStatus(other)
        };

        return status == "Đang hoạt động" && expiredAt.Date < DateTime.Today
            ? "Hết hạn"
            : status;
    }

    private static string VehicleStatusDbValue(string value)
    {
        return (value ?? string.Empty).Trim() switch
        {
            "Đang hoạt động" => "Active",
            "Tạm khóa" => "Inactive",
            "Chờ duyệt" => "Pending",
            "Hết hạn" => "Expired",
            "Đã hủy" => "Sold",
            "" => "Active",
            "-" => "Active",
            var other => other
        };
    }

    private static Color VehicleStatusColor(string status)
    {
        return status switch
        {
            "Đang hoạt động" => ModernUi.Green,
            "Chờ duyệt" => ModernUi.Orange,
            "Tạm khóa" => ModernUi.Orange,
            "Hết hạn" => ModernUi.Red,
            "Đã hủy" => ModernUi.Muted,
            _ => ModernUi.Muted
        };
    }

    private static DateTime? ParseVehicleDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim() == "-")
        {
            return null;
        }

        string[] formats = { "dd/MM/yyyy", "yyyy-MM-dd", "MM/dd/yyyy" };
        if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exact))
        {
            return exact;
        }

        return DateTime.TryParse(value, out DateTime parsed) ? parsed : null;
    }

    private static string VehicleDynamicString(dynamic obj, params string[] names)
        => VehicleDynamicString(obj, names, string.Empty);

    private static string VehicleDynamicString(dynamic obj, string[] names, string fallback)
    {
        if (obj == null) return fallback;
        object source = obj;
        var type = source.GetType();
        foreach (var name in names)
        {
            var prop = type.GetProperty(name);
            if (prop == null) continue;
            var value = prop.GetValue(source);
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                return value.ToString();
            }
        }

        return fallback;
    }

    private static int VehicleDynamicInt(dynamic obj, string name, int fallback = 0)
    {
        if (obj == null) return fallback;
        object source = obj;
        var prop = source.GetType().GetProperty(name);
        if (prop == null) return fallback;
        var value = prop.GetValue(source);
        if (value == null) return fallback;
        string text = value.ToString();
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : fallback;
    }

    private static DateTime? VehicleDynamicDate(dynamic obj, params string[] names)
    {
        if (obj == null) return null;
        object source = obj;
        var type = source.GetType();
        foreach (var name in names)
        {
            var prop = type.GetProperty(name);
            if (prop == null) continue;
            var value = prop.GetValue(source);
            if (value is DateTime date)
            {
                return date;
            }

            if (value != null && DateTime.TryParse(value.ToString(), out DateTime parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static Bitmap BuildVehicleImage(string type)
    {
        var image = new Bitmap(360, 220);
        using var g = Graphics.FromImage(image);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(238, 242, 247));
        using var floorBrush = new SolidBrush(Color.FromArgb(220, 226, 235));
        g.FillRectangle(floorBrush, 0, 150, 360, 70);
        using var accent = new SolidBrush(type == "Ô tô" ? ModernUi.Blue : ModernUi.Green);
        using var dark = new SolidBrush(Color.FromArgb(31, 41, 55));
        using var white = new SolidBrush(Color.White);
        using var pen = new Pen(Color.FromArgb(105, 116, 134), 4);

        if (type == "Ô tô")
        {
            FillRoundVehicle(g, accent, new Rectangle(54, 92, 248, 56), 18);
            FillRoundVehicle(g, accent, new Rectangle(96, 52, 130, 58), 18);
            g.FillRectangle(white, 114, 66, 44, 30);
            g.FillRectangle(white, 166, 66, 44, 30);
            g.FillEllipse(dark, 82, 132, 42, 42);
            g.FillEllipse(dark, 236, 132, 42, 42);
            g.FillEllipse(white, 94, 144, 18, 18);
            g.FillEllipse(white, 248, 144, 18, 18);
        }
        else
        {
            g.DrawEllipse(pen, 78, 128, 54, 54);
            g.DrawEllipse(pen, 228, 128, 54, 54);
            g.DrawLine(pen, 106, 150, 164, 92);
            g.DrawLine(pen, 164, 92, 238, 150);
            g.DrawLine(pen, 128, 150, 238, 150);
            g.DrawLine(pen, 206, 92, 246, 80);
            FillRoundVehicle(g, accent, new Rectangle(146, 72, 72, 28), 12);
            FillRoundVehicle(g, accent, new Rectangle(170, 100, 40, 34), 10);
        }

        using var font = ModernUi.Font(16f, FontStyle.Bold);
        string label = type == "Ô tô" ? "CAR" : "VEHICLE";
        g.DrawString(label, font, dark, new Rectangle(0, 14, 360, 30), new StringFormat { Alignment = StringAlignment.Center });
        return image;
    }

    private static void FillRoundVehicle(Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = new GraphicsPath();
        int diameter = Math.Max(2, radius * 2);
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }

    private static void ExportVehicleCsv(IEnumerable<VehicleViewModel> vehicles)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Xuất báo cáo phương tiện",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"phuong-tien-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var lines = new List<string>
        {
            "Ma xe,Bien so,Loai xe,Chu so huu,Can ho,So the,Khu vuc,Trang thai,Ngay dang ky,Ngay het han"
        };

        lines.AddRange(vehicles.Select(v => string.Join(",",
            Csv(v.VehicleCode),
            Csv(v.PlateNumber),
            Csv(v.VehicleType),
            Csv(v.OwnerName),
            Csv(v.Apartment),
            Csv(v.CardNumber),
            Csv(v.Area),
            Csv(v.Status),
            Csv(DateText(v.RegisteredAt)),
            Csv(DateText(v.ExpiredAt)))));

        File.WriteAllLines(dialog.FileName, lines);
        MessageBox.Show("Đã xuất báo cáo phương tiện.", "Phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Information);

        static string Csv(string value)
        {
            value ??= string.Empty;
            return value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }
    }
}
