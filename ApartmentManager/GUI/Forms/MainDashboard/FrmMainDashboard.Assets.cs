using ApartmentManager.DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public partial class FrmMainDashboard
{
    private void RenderAssets()
    {
        var page = BeginPage("Tài sản", "Vận hành / Tài sản");
        int w = PageWorkWidth();
        int y = 86;

        var assets = LoadAssetRows();
        var schedules = AssetDAL.GetMaintenanceSchedules();
        var filteredAssets = new List<AssetViewModel>(assets);
        AssetViewModel selectedAsset = assets.FirstOrDefault();
        bool suppressGridSelection = false;

        var addButton = ModernUi.Button("+  Thêm tài sản", ModernUi.Blue, 138, 36);
        var scheduleButton = ModernUi.Button("Lập lịch", ModernUi.Orange, 112, 36);
        var repairButton = ModernUi.Button("Ghi sửa chữa", ModernUi.Green, 132, 36);
        var exportButton = ModernUi.Button("Xuất CSV", ModernUi.Teal, 104, 36);
        AddDashboardActionBar(page, y - 2, w, addButton, scheduleButton, repairButton, exportButton);
        y += 54;

        int statsW = (int)(w * 0.32);
        int dueW = (int)(w * 0.34);
        var stats = ModernUi.Section("Tổng quan tài sản", statsW, 236);
        stats.Location = new Point(18, y);
        page.Controls.Add(stats);
        var due = ModernUi.Section("Cảnh báo bảo trì", dueW, 236);
        due.Location = new Point(stats.Right + 12, y);
        page.Controls.Add(due);
        var plan = ModernUi.Section("Lịch bảo trì", w - statsW - dueW - 24, 236);
        plan.Location = new Point(due.Right + 12, y);
        page.Controls.Add(plan);

        y += 250;

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 132);
        var typeFilter = AddAssetFilter(filters, "Loại tài sản", BuildFilterOptions(assets.Select(a => a.AssetType)), 16, 160);
        var locationFilter = AddAssetFilter(filters, "Khu vực", BuildFilterOptions(assets.Select(a => a.Location)), 196, 160);
        var conditionFilter = AddAssetFilter(filters, "Tình trạng", new[] { "Tất cả", "Tốt", "Cần bảo trì", "Bảo trì", "Hỏng", "Thanh lý" }, 376, 160);
        var dueFilter = AddAssetFilter(filters, "Hạn bảo trì", new[] { "Tất cả", "Quá hạn", "7 ngày", "30 ngày", "Chưa có lịch" }, 556, 150);
        var search = ModernUi.SearchBox("Tìm mã, tên tài sản, vị trí...", 260, 34);
        filters.Controls.Add(search);
        var searchInput = search.Controls.OfType<TextBox>().First();
        var refreshButton = ModernUi.OutlineButton("Làm mới", 108, 34);
        filters.Controls.Add(refreshButton);
        int filterHeight = LayoutSearchWithRefresh(search, refreshButton, w, 84, 38, 724);
        filters.Height = filterHeight;
        page.Controls.Add(filters);

        y += filterHeight + 14;
        var frameWidths = DashboardListDetailWidths(w);
        int leftW = frameWidths.LeftWidth;
        int rightW = frameWidths.RightWidth;
        const int mainHeight = 506;

        var list = ModernUi.Section("Danh mục tài sản (0)", leftW, mainHeight);
        list.Location = new Point(18, y);
        var listTitle = list.Controls.OfType<Label>().FirstOrDefault();
        var grid = ModernUi.Grid();
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 386);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ScrollBars = ScrollBars.Both;
        grid.RowTemplate.Height = 34;
        grid.ColumnHeadersHeight = 38;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.CellFormatting += (_, e) => ApplyGridCellStyle(e);
        list.Controls.Add(grid);
        var paging = ModernUi.Label("", 9f, FontStyle.Regular, ModernUi.Text);
        paging.Location = new Point(18, mainHeight - 54);
        paging.Size = new Size(list.Width - 36, 26);
        list.Controls.Add(paging);
        page.Controls.Add(list);

        var detail = ModernUi.Section("Thông tin tài sản", rightW, mainHeight);
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

        ComboBox AddAssetFilter(Control parent, string label, string[] options, int x, int width)
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
            int targetId = selectedId > 0 ? selectedId : selectedAsset?.AssetID ?? 0;
            assets = LoadAssetRows();
            schedules = AssetDAL.GetMaintenanceSchedules();
            selectedAsset = targetId > 0
                ? assets.FirstOrDefault(a => a.AssetID == targetId)
                : assets.FirstOrDefault();
            RefreshFilterOptions();
            ApplyFilters();
        }

        void RefreshFilterOptions()
        {
            ResetAssetFilter(typeFilter, BuildFilterOptions(assets.Select(a => a.AssetType)));
            ResetAssetFilter(locationFilter, BuildFilterOptions(assets.Select(a => a.Location)));
        }

        void ApplyFilters()
        {
            string type = typeFilter.Text;
            string location = locationFilter.Text;
            string condition = conditionFilter.Text;
            string dueState = dueFilter.Text;
            string keyword = searchInput.Text.Trim();

            filteredAssets = assets
                .Where(a => type == "Tất cả" || a.AssetType == type)
                .Where(a => location == "Tất cả" || a.Location == location)
                .Where(a => condition == "Tất cả" || a.Condition == condition)
                .Where(a => AssetMatchesDueFilter(a, dueState))
                .Where(a => keyword.Length == 0 ||
                    a.AssetCode.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    a.AssetName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    a.AssetType.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    a.Location.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    a.Note.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(a => AssetDueRank(a))
                .ThenBy(a => a.AssetType)
                .ThenBy(a => a.Location)
                .ThenBy(a => a.AssetName)
                .ToList();

            if (selectedAsset == null || (selectedAsset.AssetID > 0 && !filteredAssets.Any(a => a.AssetID == selectedAsset.AssetID)))
            {
                selectedAsset = filteredAssets.FirstOrDefault();
            }

            RefreshGrid();
            RenderDetail();
        }

        void RefreshGrid()
        {
            var table = new DataTable();
            table.Columns.Add("Chọn", typeof(bool));
            table.Columns.Add("Mã tài sản");
            table.Columns.Add("Tên tài sản");
            table.Columns.Add("Loại");
            table.Columns.Add("Vị trí");
            table.Columns.Add("Tình trạng");
            table.Columns.Add("Bảo trì gần nhất");
            table.Columns.Add("Bảo trì tiếp theo");
            table.Columns.Add("Cảnh báo");
            table.Columns.Add("Chi phí");

            foreach (var asset in filteredAssets)
            {
                table.Rows.Add(
                    selectedAsset != null && asset.AssetID == selectedAsset.AssetID,
                    asset.AssetCode,
                    asset.AssetName,
                    asset.AssetType,
                    asset.Location,
                    asset.Condition,
                    DateText(asset.LastMaintenanceDate),
                    DateText(asset.NextMaintenanceDate),
                    AssetDueStatus(asset),
                    Money(asset.RepairCost));
            }

            suppressGridSelection = true;
            grid.DataSource = table;
            if (grid.Columns.Count > 0)
            {
                int[] widths = { 46, 116, 172, 112, 126, 112, 122, 122, 104, 104 };
                for (int i = 0; i < Math.Min(widths.Length, grid.Columns.Count); i++)
                {
                    grid.Columns[i].Width = widths[i];
                    grid.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                    grid.Columns[i].DefaultCellStyle.Alignment = i is 2 or 4
                        ? DataGridViewContentAlignment.MiddleLeft
                        : DataGridViewContentAlignment.MiddleCenter;
                }
            }

            int selectedIndex = selectedAsset == null ? -1 : filteredAssets.FindIndex(a => a.AssetID == selectedAsset.AssetID);
            if (selectedIndex >= 0 && selectedIndex < grid.Rows.Count)
            {
                grid.ClearSelection();
                grid.Rows[selectedIndex].Selected = true;
                grid.CurrentCell = grid.Rows[selectedIndex].Cells[Math.Min(1, grid.Columns.Count - 1)];
            }
            suppressGridSelection = false;

            if (listTitle != null)
            {
                listTitle.Text = $"DANH MỤC TÀI SẢN ({filteredAssets.Count})";
            }

            paging.Text = filteredAssets.Count == 0
                ? "Không có tài sản phù hợp bộ lọc"
                : $"Hiển thị 1 - {filteredAssets.Count:N0} / {assets.Count:N0} tài sản";
            RefreshBottom();
        }

        void RenderDetail()
        {
            detailBody.Controls.Clear();
            if (selectedAsset == null)
            {
                var empty = ModernUi.Label("Chọn một tài sản để xem chi tiết, hoặc bấm Thêm tài sản để tạo mới.", 10f, FontStyle.Regular, ModernUi.Muted);
                empty.SetBounds(12, 20, detailBody.Width - 24, 42);
                detailBody.Controls.Add(empty);
                return;
            }

            bool isNew = selectedAsset.AssetID <= 0;
            var title = ModernUi.Label(isNew ? "Tài sản mới" : selectedAsset.AssetCode, 13f, FontStyle.Bold, ModernUi.Navy);
            title.SetBounds(4, 2, Math.Max(130, detailBody.Width - 160), 30);
            detailBody.Controls.Add(title);

            var badge = ModernUi.Badge(AssetDueStatus(selectedAsset), AssetDueColor(selectedAsset));
            badge.SetBounds(detailBody.Width - 148, 4, 144, 26);
            detailBody.Controls.Add(badge);

            int rowY = 42;
            int halfW = (detailBody.Width - 20) / 2;
            var code = AddAssetInput(detailBody, "Mã tài sản", selectedAsset.AssetCode, 4, rowY, halfW);
            var name = AddAssetInput(detailBody, "Tên tài sản", selectedAsset.AssetName, code.Right + 12, rowY, detailBody.Width - code.Right - 16);
            rowY += 48;
            var type = AddAssetValueCombo(detailBody, "Loại", AssetTypeOptions(assets), selectedAsset.AssetType, 4, rowY, halfW);
            var location = AddAssetInput(detailBody, "Vị trí", selectedAsset.Location, type.Right + 12, rowY, detailBody.Width - type.Right - 16);
            rowY += 48;
            var purchaseDate = AddAssetInput(detailBody, "Ngày mua", DateText(selectedAsset.PurchaseDate), 4, rowY, halfW);
            var condition = AddAssetValueCombo(detailBody, "Tình trạng", AssetConditionOptions(), selectedAsset.Condition, purchaseDate.Right + 12, rowY, detailBody.Width - purchaseDate.Right - 16);
            rowY += 48;
            var lastMaintenance = AddAssetInput(detailBody, "Bảo trì gần nhất", DateText(selectedAsset.LastMaintenanceDate), 4, rowY, halfW);
            var nextMaintenance = AddAssetInput(detailBody, "Bảo trì tiếp theo", DateText(selectedAsset.NextMaintenanceDate), lastMaintenance.Right + 12, rowY, detailBody.Width - lastMaintenance.Right - 16);
            rowY += 48;
            var repairCost = AddAssetInput(detailBody, "Chi phí sửa chữa", selectedAsset.RepairCost <= 0 ? "0" : selectedAsset.RepairCost.ToString("N0", CultureInfo.InvariantCulture), 4, rowY, halfW);
            var note = AddAssetInput(detailBody, "Ghi chú", selectedAsset.Note, repairCost.Right + 12, rowY, detailBody.Width - repairCost.Right - 16);

            int buttonY = detailBody.Height - 78;
            int buttonW = Math.Max(88, (detailBody.Width - 24) / 3);
            var save = ModernUi.Button(isNew ? "Lưu mới" : "Cập nhật", ModernUi.Blue, buttonW, 32);
            var schedule = ModernUi.Button("Lập lịch", ModernUi.Orange, buttonW, 32);
            var repair = ModernUi.Button("Ghi sửa chữa", ModernUi.Green, buttonW, 32);
            save.Location = new Point(4, buttonY);
            schedule.Location = new Point(save.Right + 8, buttonY);
            repair.Location = new Point(schedule.Right + 8, buttonY);
            detailBody.Controls.Add(save);
            detailBody.Controls.Add(schedule);
            detailBody.Controls.Add(repair);

            var delete = ModernUi.OutlineButton("Xóa", buttonW, 32);
            delete.Location = new Point(4, buttonY + 40);
            delete.ForeColor = ModernUi.Text;
            detailBody.Controls.Add(delete);

            schedule.Enabled = !isNew;
            repair.Enabled = !isNew;
            delete.Enabled = !isNew;

            save.Click += (_, _) =>
            {
                int savedId = SaveAssetFromInputs(
                    selectedAsset.AssetID,
                    code.Text,
                    name.Text,
                    ComboBoxHelper.GetSelectedValueString(type),
                    location.Text,
                    purchaseDate.Text,
                    ComboBoxHelper.GetSelectedValueString(condition),
                    lastMaintenance.Text,
                    nextMaintenance.Text,
                    repairCost.Text,
                    note.Text);

                if (savedId > 0)
                {
                    ReloadData(savedId);
                }
            };

            schedule.Click += (_, _) => ScheduleSelectedAsset();
            repair.Click += (_, _) => RecordSelectedAssetRepair();
            delete.Click += (_, _) => DeleteSelectedAsset();
        }

        int SaveAssetFromInputs(
            int assetId,
            string assetCode,
            string assetName,
            string assetType,
            string location,
            string purchaseDate,
            string condition,
            string lastMaintenance,
            string nextMaintenance,
            string repairCost,
            string note)
        {
            if (string.IsNullOrWhiteSpace(assetCode) || string.IsNullOrWhiteSpace(assetName) ||
                string.IsNullOrWhiteSpace(assetType) || string.IsNullOrWhiteSpace(location))
            {
                MessageBox.Show("Vui lòng nhập đủ mã, tên, loại và vị trí tài sản.", "Tài sản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }

            DateTime? parsedPurchase = ParseAssetDate(purchaseDate);
            DateTime? parsedLast = ParseAssetDate(lastMaintenance);
            DateTime? parsedNext = ParseAssetDate(nextMaintenance);
            if (!TryParseMoney(repairCost, out decimal parsedCost))
            {
                MessageBox.Show("Chi phí sửa chữa không hợp lệ.", "Tài sản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }

            if (assetId <= 0)
            {
                int newId = AssetDAL.CreateAsset(
                    assetCode,
                    assetName,
                    assetType,
                    location,
                    parsedPurchase,
                    condition,
                    parsedLast,
                    parsedNext,
                    parsedCost,
                    note);

                if (newId <= 0)
                {
                    MessageBox.Show("Không thể thêm tài sản. Vui lòng kiểm tra mã tài sản có bị trùng hoặc bảng dữ liệu đã được tạo chưa.", "Tài sản", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 0;
                }

                MessageBox.Show("Đã thêm tài sản.", "Tài sản", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return newId;
            }

            bool updated = AssetDAL.UpdateAsset(
                assetId,
                assetCode,
                assetName,
                assetType,
                location,
                parsedPurchase,
                condition,
                parsedLast,
                parsedNext,
                parsedCost,
                note);

            if (!updated)
            {
                MessageBox.Show("Không thể cập nhật tài sản.", "Tài sản", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }

            MessageBox.Show("Đã cập nhật tài sản.", "Tài sản", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return assetId;
        }

        void AddAsset()
        {
            selectedAsset = BuildAssetDraft();
            RenderDetail();
        }

        void ScheduleSelectedAsset()
        {
            if (!EnsurePersistedAsset("Lập lịch bảo trì"))
            {
                return;
            }

            if (!ShowMaintenanceScheduleDialog(selectedAsset, out string category, out DateTime scheduledDate, out string status, out string assignedTo, out string note))
            {
                return;
            }

            int scheduleId = AssetDAL.CreateMaintenanceSchedule(selectedAsset.AssetID, category, scheduledDate, status, assignedTo, note);
            if (scheduleId <= 0)
            {
                MessageBox.Show("Không thể lập lịch bảo trì.", "Lập lịch bảo trì", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DateTime? next = selectedAsset.NextMaintenanceDate;
            if (!next.HasValue || scheduledDate.Date < next.Value.Date)
            {
                AssetDAL.UpdateAsset(
                    selectedAsset.AssetID,
                    selectedAsset.AssetCode,
                    selectedAsset.AssetName,
                    selectedAsset.AssetType,
                    selectedAsset.Location,
                    selectedAsset.PurchaseDate,
                    selectedAsset.Condition,
                    selectedAsset.LastMaintenanceDate,
                    scheduledDate,
                    selectedAsset.RepairCost,
                    selectedAsset.Note);
            }

            MessageBox.Show("Đã lập lịch bảo trì.", "Lập lịch bảo trì", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReloadData(selectedAsset.AssetID);
        }

        void RecordSelectedAssetRepair()
        {
            if (!EnsurePersistedAsset("Ghi sửa chữa"))
            {
                return;
            }

            if (!ShowAssetRepairDialog(selectedAsset, out string condition, out DateTime lastDate, out DateTime? nextDate, out decimal cost, out string note))
            {
                return;
            }

            string mergedNote = string.IsNullOrWhiteSpace(note) ? selectedAsset.Note : note;
            bool updated = AssetDAL.UpdateAsset(
                selectedAsset.AssetID,
                selectedAsset.AssetCode,
                selectedAsset.AssetName,
                selectedAsset.AssetType,
                selectedAsset.Location,
                selectedAsset.PurchaseDate,
                condition,
                lastDate,
                nextDate,
                cost,
                mergedNote);

            if (!updated)
            {
                MessageBox.Show("Không thể ghi nhận sửa chữa.", "Ghi sửa chữa", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AssetDAL.CreateMaintenanceSchedule(
                selectedAsset.AssetID,
                "Ghi sửa chữa",
                lastDate,
                "Hoàn thành",
                CurrentDisplayName(),
                note);

            MessageBox.Show("Đã ghi nhận sửa chữa tài sản.", "Ghi sửa chữa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReloadData(selectedAsset.AssetID);
        }

        void DeleteSelectedAsset()
        {
            if (!EnsurePersistedAsset("Xóa tài sản"))
            {
                return;
            }

            if (MessageBox.Show($"Xóa tài sản {selectedAsset.AssetCode}?\nCác lịch bảo trì liên quan cũng sẽ bị xóa.", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            if (!AssetDAL.DeleteAsset(selectedAsset.AssetID))
            {
                MessageBox.Show("Không thể xóa tài sản.", "Xóa tài sản", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            selectedAsset = null;
            ReloadData();
        }

        bool EnsurePersistedAsset(string caption)
        {
            if (selectedAsset == null || selectedAsset.AssetID <= 0)
            {
                MessageBox.Show("Vui lòng chọn tài sản đã lưu.", caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        void RefreshBottom()
        {
            foreach (var panel in new[] { stats, due, plan })
            {
                foreach (Control child in panel.Controls.Cast<Control>().Skip(1).ToList())
                {
                    panel.Controls.Remove(child);
                    child.Dispose();
                }
            }

            int miniW = Math.Max(92, (stats.Width - 52) / 3);
            AddAssetMiniStat(stats, "Tổng tài sản", assets.Count, ModernUi.Blue, 16, 46, miniW, 72);
            AddAssetMiniStat(stats, "Quá hạn", assets.Count(a => AssetDueStatus(a) == "Quá hạn"), ModernUi.Red, 28 + miniW, 46, miniW, 72);
            AddAssetMiniStat(stats, "7 ngày", assets.Count(a => a.NextMaintenanceDate.HasValue && a.NextMaintenanceDate.Value.Date > DateTime.Today && a.NextMaintenanceDate.Value.Date <= DateTime.Today.AddDays(7)), ModernUi.Orange, 40 + miniW * 2, 46, miniW, 72);
            AddAssetMiniStat(stats, "Cần xử lý", assets.Count(a => a.Condition == "Hỏng" || a.Condition == "Cần bảo trì" || a.Condition == "Bảo trì"), ModernUi.Teal, 16, 130, miniW, 72);
            AddAssetMiniStat(stats, "Chi phí", (int)Math.Min(int.MaxValue, assets.Sum(a => a.RepairCost) / 1000m), ModernUi.Green, 28 + miniW, 130, miniW, 72, "nghìn");

            var dueRows = assets
                .Where(a => a.NextMaintenanceDate.HasValue && a.NextMaintenanceDate.Value.Date <= DateTime.Today.AddDays(30))
                .OrderBy(a => a.NextMaintenanceDate)
                .Take(8)
                .ToList();

            var dueGrid = ModernUi.Grid();
            dueGrid.Location = new Point(12, 42);
            dueGrid.Size = new Size(due.Width - 24, 156);
            dueGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dueGrid.ScrollBars = ScrollBars.Vertical;
            dueGrid.CellFormatting += (_, e) => ApplyGridCellStyle(e);
            var dueTable = new DataTable();
            dueTable.Columns.Add("Tài sản");
            dueTable.Columns.Add("Vị trí");
            dueTable.Columns.Add("Hạn");
            dueTable.Columns.Add("Mức độ");
            foreach (var asset in dueRows)
            {
                dueTable.Rows.Add(asset.AssetName, asset.Location, DateText(asset.NextMaintenanceDate), AssetDueStatus(asset));
            }
            if (dueRows.Count == 0)
            {
                dueTable.Rows.Add("Không có tài sản đến hạn", "", "", "");
            }
            dueGrid.DataSource = dueTable;
            if (dueGrid.Columns.Count > 0)
            {
                int[] widths = { 132, 92, 86, 92 };
                for (int i = 0; i < Math.Min(widths.Length, dueGrid.Columns.Count); i++)
                {
                    dueGrid.Columns[i].Width = widths[i];
                    dueGrid.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }
            due.Controls.Add(dueGrid);

            var planGrid = ModernUi.Grid();
            planGrid.Location = new Point(12, 42);
            planGrid.Size = new Size(plan.Width - 24, 156);
            planGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            planGrid.ScrollBars = ScrollBars.Vertical;
            planGrid.CellFormatting += (_, e) => ApplyGridCellStyle(e);
            var planTable = new DataTable();
            planTable.Columns.Add("Ngày");
            planTable.Columns.Add("Hạng mục");
            planTable.Columns.Add("Tài sản");
            planTable.Columns.Add("Trạng thái");
            foreach (var item in schedules.OrderBy(s => s.ScheduledDate).Take(10))
            {
                planTable.Rows.Add(DateText(item.ScheduledDate), item.Category, item.AssetName, item.Status);
            }
            if (schedules.Count == 0)
            {
                planTable.Rows.Add("Không có lịch bảo trì", "", "", "");
            }
            planGrid.DataSource = planTable;
            if (planGrid.Columns.Count > 0)
            {
                int[] widths = { 78, 128, 118, 104 };
                for (int i = 0; i < Math.Min(widths.Length, planGrid.Columns.Count); i++)
                {
                    planGrid.Columns[i].Width = widths[i];
                    planGrid.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }
            plan.Controls.Add(planGrid);
        }

        typeFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        locationFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        conditionFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        dueFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        searchInput.TextChanged += (_, _) => ApplyFilters();
        refreshButton.Click += (_, _) =>
        {
            typeFilter.SelectedIndex = 0;
            locationFilter.SelectedIndex = 0;
            conditionFilter.SelectedIndex = 0;
            dueFilter.SelectedIndex = 0;
            searchInput.Clear();
            ReloadData();
        };
        grid.SelectionChanged += (_, _) =>
        {
            if (suppressGridSelection)
            {
                return;
            }

            if (grid.CurrentRow?.Index >= 0 && grid.CurrentRow.Index < filteredAssets.Count)
            {
                selectedAsset = filteredAssets[grid.CurrentRow.Index];
                RefreshGrid();
                RenderDetail();
            }
        };

        addButton.Click += (_, _) => AddAsset();
        scheduleButton.Click += (_, _) => ScheduleSelectedAsset();
        repairButton.Click += (_, _) => RecordSelectedAssetRepair();
        exportButton.Click += (_, _) => ExportAssetCsv(assets);

        ApplyFilters();
        if (ConsumeQuickAction("assets", "add"))
        {
            AddAsset();
        }
        page.AutoScroll = true;
        page.AutoScrollMinSize = new Size(0, y + 270);
    }

    private static List<AssetViewModel> LoadAssetRows()
    {
        return AssetDAL.GetAllAssets()
            .Select(a => new AssetViewModel
            {
                AssetID = a.AssetID,
                AssetCode = Display(a.AssetCode, ""),
                AssetName = Display(a.AssetName, ""),
                AssetType = Display(a.AssetType, "Khác"),
                Location = Display(a.Location, "Chưa rõ"),
                PurchaseDate = a.PurchaseDate,
                Condition = AssetConditionText(a.Condition),
                LastMaintenanceDate = a.LastMaintenanceDate,
                NextMaintenanceDate = a.NextMaintenanceDate,
                RepairCost = a.RepairCost,
                Note = Display(a.Note, "")
            })
            .ToList();
    }

    private static AssetViewModel BuildAssetDraft()
    {
        return new AssetViewModel
        {
            AssetID = 0,
            AssetCode = $"TS-{DateTime.Now:yyMMdd-HHmm}",
            AssetName = string.Empty,
            AssetType = "Khác",
            Location = string.Empty,
            PurchaseDate = DateTime.Today,
            Condition = "Tốt",
            LastMaintenanceDate = null,
            NextMaintenanceDate = DateTime.Today.AddMonths(1),
            RepairCost = 0,
            Note = string.Empty
        };
    }

    private static TextBox AddAssetInput(Control parent, string label, string value, int x, int y, int width)
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

    private static ComboBox AddAssetValueCombo(Control parent, string label, IEnumerable<(string Text, string Value)> options, string selectedValue, int x, int y, int width)
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

    private static void AddAssetMiniStat(Control parent, string title, int value, Color color, int x, int y, int width, int height, string suffix = "")
    {
        var card = ModernUi.CardPanel(6);
        card.SetBounds(x, y, width, height);

        string text = suffix.Length == 0 ? value.ToString("N0") : $"{value:N0} {suffix}";
        var valueLabel = ModernUi.Label(text, text.Length > 9 ? 11f : 15f, FontStyle.Bold, color);
        valueLabel.SetBounds(10, 8, width - 20, 24);
        card.Controls.Add(valueLabel);

        var titleLabel = ModernUi.Label(title, 8.2f, FontStyle.Bold, ModernUi.Text);
        titleLabel.SetBounds(10, 36, width - 20, 26);
        titleLabel.AutoEllipsis = true;
        card.Controls.Add(titleLabel);
        parent.Controls.Add(card);
    }

    private static IEnumerable<(string Text, string Value)> AssetTypeOptions(IEnumerable<AssetViewModel> assets)
    {
        var defaults = new[] { "Thang máy", "PCCC", "An ninh", "Cấp nước", "Điện", "Cảnh quan", "Khác" };
        foreach (string item in defaults.Concat(assets.Select(a => a.AssetType)).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().OrderBy(v => v))
        {
            yield return (item, item);
        }
    }

    private static IEnumerable<(string Text, string Value)> AssetConditionOptions()
    {
        yield return ("Tốt", "Tốt");
        yield return ("Cần bảo trì", "Cần bảo trì");
        yield return ("Bảo trì", "Bảo trì");
        yield return ("Hỏng", "Hỏng");
        yield return ("Thanh lý", "Thanh lý");
    }

    private static string AssetConditionText(string value)
    {
        return (value ?? string.Empty).Trim() switch
        {
            "Good" or "Active" or "Tốt" => "Tốt",
            "NeedMaintenance" or "Cần bảo trì" => "Cần bảo trì",
            "Maintenance" or "Bảo trì" => "Bảo trì",
            "Broken" or "Hỏng" => "Hỏng",
            "Disposed" or "Sold" or "Thanh lý" => "Thanh lý",
            "" => "Tốt",
            "-" => "Tốt",
            var other => other
        };
    }

    private static bool AssetMatchesDueFilter(AssetViewModel asset, string filter)
    {
        DateTime today = DateTime.Today;
        return filter switch
        {
            "Quá hạn" => asset.NextMaintenanceDate.HasValue && asset.NextMaintenanceDate.Value.Date <= today,
            "7 ngày" => asset.NextMaintenanceDate.HasValue && asset.NextMaintenanceDate.Value.Date > today && asset.NextMaintenanceDate.Value.Date <= today.AddDays(7),
            "30 ngày" => asset.NextMaintenanceDate.HasValue && asset.NextMaintenanceDate.Value.Date > today && asset.NextMaintenanceDate.Value.Date <= today.AddDays(30),
            "Chưa có lịch" => !asset.NextMaintenanceDate.HasValue,
            _ => true
        };
    }

    private static string AssetDueStatus(AssetViewModel asset)
    {
        if (asset.Condition == "Hỏng")
        {
            return "Cần sửa";
        }

        if (!asset.NextMaintenanceDate.HasValue)
        {
            return "Chưa có lịch";
        }

        DateTime date = asset.NextMaintenanceDate.Value.Date;
        if (date <= DateTime.Today)
        {
            return "Quá hạn";
        }

        if (date <= DateTime.Today.AddDays(7))
        {
            return "7 ngày";
        }

        if (date <= DateTime.Today.AddDays(30))
        {
            return "30 ngày";
        }

        return "Ổn định";
    }

    private static int AssetDueRank(AssetViewModel asset)
    {
        return AssetDueStatus(asset) switch
        {
            "Cần sửa" => 0,
            "Quá hạn" => 1,
            "7 ngày" => 2,
            "30 ngày" => 3,
            "Chưa có lịch" => 4,
            _ => 5
        };
    }

    private static Color AssetDueColor(AssetViewModel asset)
    {
        return AssetDueStatus(asset) switch
        {
            "Cần sửa" or "Quá hạn" => ModernUi.Red,
            "7 ngày" or "30 ngày" => ModernUi.Orange,
            "Chưa có lịch" => ModernUi.Muted,
            _ => ModernUi.Green
        };
    }

    private static DateTime? ParseAssetDate(string value)
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

    private static bool TryParseMoney(string value, out decimal amount)
    {
        string text = (value ?? string.Empty).Trim().Replace(" ", "").Replace(",", "");
        if (text.Length == 0 || text == "-")
        {
            amount = 0;
            return true;
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) ||
               decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out amount);
    }

    private static void ResetAssetFilter(ComboBox combo, string[] options)
    {
        string previous = combo.Text;
        combo.Items.Clear();
        combo.Items.AddRange(options.Cast<object>().ToArray());
        int index = Array.IndexOf(options, previous);
        combo.SelectedIndex = index >= 0 ? index : 0;
    }

    private bool ShowMaintenanceScheduleDialog(
        AssetViewModel asset,
        out string category,
        out DateTime scheduledDate,
        out string status,
        out string assignedTo,
        out string note)
    {
        category = string.Empty;
        scheduledDate = DateTime.Today;
        status = string.Empty;
        assignedTo = string.Empty;
        note = string.Empty;
        string resultCategory = string.Empty;
        DateTime resultDate = DateTime.Today;
        string resultStatus = string.Empty;
        string resultAssignedTo = string.Empty;
        string resultNote = string.Empty;

        using var dialog = CreateAssetDialog("Lập lịch bảo trì", 430, 300);
        var title = ModernUi.Label(asset.AssetName, 10f, FontStyle.Bold, ModernUi.Navy);
        title.SetBounds(18, 18, dialog.ClientSize.Width - 36, 24);
        dialog.Controls.Add(title);

        var categoryInput = AddAssetInput(dialog, "Hạng mục", $"Bảo trì {asset.AssetType}".Trim(), 18, 52, 188);
        var dateInput = AddAssetInput(dialog, "Ngày thực hiện", DateText(asset.NextMaintenanceDate ?? DateTime.Today.AddDays(7)), 224, 52, 166);
        var statusInput = AddAssetValueCombo(dialog, "Trạng thái", MaintenanceStatusOptions(), "Đã lên lịch", 18, 104, 188);
        var assignedInput = AddAssetInput(dialog, "Người phụ trách", CurrentDisplayName(), 224, 104, 166);
        var noteInput = AddAssetInput(dialog, "Ghi chú", "", 18, 156, 372);

        var cancel = ModernUi.OutlineButton("Hủy", 96, 34);
        var save = ModernUi.Button("Lưu lịch", ModernUi.Blue, 112, 34);
        cancel.Location = new Point(dialog.ClientSize.Width - 226, 236);
        save.Location = new Point(dialog.ClientSize.Width - 122, 236);
        dialog.Controls.Add(cancel);
        dialog.Controls.Add(save);

        cancel.Click += (_, _) => dialog.DialogResult = DialogResult.Cancel;
        save.Click += (_, _) =>
        {
            DateTime? parsed = ParseAssetDate(dateInput.Text);
            if (string.IsNullOrWhiteSpace(categoryInput.Text) || !parsed.HasValue)
            {
                MessageBox.Show(dialog, "Vui lòng nhập hạng mục và ngày thực hiện hợp lệ.", "Lập lịch bảo trì", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            resultCategory = categoryInput.Text.Trim();
            resultDate = parsed.Value;
            resultStatus = ComboBoxHelper.GetSelectedValueString(statusInput);
            resultAssignedTo = assignedInput.Text.Trim();
            resultNote = noteInput.Text.Trim();
            dialog.DialogResult = DialogResult.OK;
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return false;
        }

        category = resultCategory;
        scheduledDate = resultDate;
        status = resultStatus;
        assignedTo = resultAssignedTo;
        note = resultNote;
        return true;
    }

    private bool ShowAssetRepairDialog(
        AssetViewModel asset,
        out string condition,
        out DateTime lastDate,
        out DateTime? nextDate,
        out decimal cost,
        out string note)
    {
        condition = asset.Condition;
        lastDate = DateTime.Today;
        nextDate = asset.NextMaintenanceDate;
        cost = asset.RepairCost;
        note = string.Empty;
        string resultCondition = asset.Condition;
        DateTime resultLastDate = DateTime.Today;
        DateTime? resultNextDate = asset.NextMaintenanceDate;
        decimal resultCost = asset.RepairCost;
        string resultNote = string.Empty;

        using var dialog = CreateAssetDialog("Ghi sửa chữa", 430, 300);
        var title = ModernUi.Label(asset.AssetName, 10f, FontStyle.Bold, ModernUi.Navy);
        title.SetBounds(18, 18, dialog.ClientSize.Width - 36, 24);
        dialog.Controls.Add(title);

        var conditionInput = AddAssetValueCombo(dialog, "Tình trạng sau sửa", AssetConditionOptions(), "Tốt", 18, 52, 188);
        var lastInput = AddAssetInput(dialog, "Ngày sửa", DateText(DateTime.Today), 224, 52, 166);
        var nextInput = AddAssetInput(dialog, "Bảo trì tiếp theo", DateText(DateTime.Today.AddMonths(1)), 18, 104, 188);
        var costInput = AddAssetInput(dialog, "Chi phí", asset.RepairCost.ToString("N0", CultureInfo.InvariantCulture), 224, 104, 166);
        var noteInput = AddAssetInput(dialog, "Ghi chú sửa chữa", asset.Note, 18, 156, 372);

        var cancel = ModernUi.OutlineButton("Hủy", 96, 34);
        var save = ModernUi.Button("Ghi nhận", ModernUi.Green, 112, 34);
        cancel.Location = new Point(dialog.ClientSize.Width - 226, 236);
        save.Location = new Point(dialog.ClientSize.Width - 122, 236);
        dialog.Controls.Add(cancel);
        dialog.Controls.Add(save);

        cancel.Click += (_, _) => dialog.DialogResult = DialogResult.Cancel;
        save.Click += (_, _) =>
        {
            DateTime? parsedLast = ParseAssetDate(lastInput.Text);
            DateTime? parsedNext = ParseAssetDate(nextInput.Text);
            if (!parsedLast.HasValue || !TryParseMoney(costInput.Text, out decimal parsedCost))
            {
                MessageBox.Show(dialog, "Vui lòng nhập ngày sửa và chi phí hợp lệ.", "Ghi sửa chữa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            resultCondition = ComboBoxHelper.GetSelectedValueString(conditionInput);
            resultLastDate = parsedLast.Value;
            resultNextDate = parsedNext;
            resultCost = parsedCost;
            resultNote = noteInput.Text.Trim();
            dialog.DialogResult = DialogResult.OK;
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return false;
        }

        condition = resultCondition;
        lastDate = resultLastDate;
        nextDate = resultNextDate;
        cost = resultCost;
        note = resultNote;
        return true;
    }

    private static IEnumerable<(string Text, string Value)> MaintenanceStatusOptions()
    {
        yield return ("Chờ xử lý", "Chờ xử lý");
        yield return ("Đã lên lịch", "Đã lên lịch");
        yield return ("Đang thực hiện", "Đang thực hiện");
        yield return ("Hoàn thành", "Hoàn thành");
        yield return ("Tạm hoãn", "Tạm hoãn");
    }

    private static Form CreateAssetDialog(string title, int width, int height)
    {
        return new Form
        {
            Text = title,
            Size = new Size(width, height),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.White,
            Font = ModernUi.Font(9.5f),
            ShowInTaskbar = false
        };
    }

    private static void ExportAssetCsv(IEnumerable<AssetViewModel> assets)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Xuất báo cáo tài sản",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"tai-san-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var lines = new List<string>
        {
            "Ma tai san,Ten tai san,Loai,Vi tri,Tinh trang,Bao tri gan nhat,Bao tri tiep theo,Canh bao,Chi phi,Ghi chu"
        };

        lines.AddRange(assets.Select(a => string.Join(",",
            Csv(a.AssetCode),
            Csv(a.AssetName),
            Csv(a.AssetType),
            Csv(a.Location),
            Csv(a.Condition),
            Csv(DateText(a.LastMaintenanceDate)),
            Csv(DateText(a.NextMaintenanceDate)),
            Csv(AssetDueStatus(a)),
            Csv(a.RepairCost.ToString("0", CultureInfo.InvariantCulture)),
            Csv(a.Note))));

        File.WriteAllLines(dialog.FileName, lines);
        MessageBox.Show("Đã xuất báo cáo tài sản.", "Tài sản", MessageBoxButtons.OK, MessageBoxIcon.Information);

        static string Csv(string value)
        {
            value ??= string.Empty;
            return value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }
    }

    private sealed class AssetViewModel
    {
        public int AssetID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime? PurchaseDate { get; set; }
        public string Condition { get; set; } = string.Empty;
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public decimal RepairCost { get; set; }
        public string Note { get; set; } = string.Empty;
    }
}
