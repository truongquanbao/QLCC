using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.DTO;
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
    private void RenderVisitors()
    {
        if (IsResident)
        {
            RenderResidentVisitors();
            return;
        }

        RenderStaffVisitors();
    }

    private void RenderStaffVisitors()
    {
        var page = BeginPage("Khách ra vào", "Vận hành / Khách ra vào");
        int w = PageWorkWidth();
        int y = 86;

        var visitors = LoadVisitorRows(false);
        var residents = ResidentDAL.GetAllResidents();
        var filteredVisitors = new List<VisitorViewModel>(visitors);
        VisitorViewModel selectedVisitor = visitors.FirstOrDefault();
        bool suppressGridSelection = false;
        bool editMode = false;

        var addButton = ModernUi.Button("+  Đăng ký khách", ModernUi.Blue, 148, 36);
        var approveButton = ModernUi.Button("Duyệt khách", ModernUi.Green, 122, 36);
        var checkoutActionButton = ModernUi.Button("Ghi nhận ra", ModernUi.Orange, 128, 36);
        var exportButton = ModernUi.Button("Xuất CSV", ModernUi.Teal, 104, 36);
        AddDashboardActionBar(page, y - 2, w, addButton, approveButton, checkoutActionButton, exportButton);
        y += 54;

        int statsW = (int)(w * 0.40);
        int typeChartW = (w - statsW - 24) / 2;
        var stats = ModernUi.Section("Thống kê khách hôm nay", statsW, 218);
        stats.Location = new Point(18, y);
        page.Controls.Add(stats);
        var typeChart = ModernUi.Section("Khách theo loại", typeChartW, 218);
        typeChart.Location = new Point(stats.Right + 12, y);
        page.Controls.Add(typeChart);
        var statusChart = ModernUi.Section("Khách theo trạng thái", w - statsW - typeChartW - 24, 218);
        statusChart.Location = new Point(typeChart.Right + 12, y);
        page.Controls.Add(statusChart);

        y += 232;

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 132);
        var dateFilter = AddVisitorFilter(filters, "Khoảng thời gian", new[] { "Hôm nay", "7 ngày", "30 ngày", "Tất cả" }, 16, 150);
        var typeFilter = AddVisitorFilter(filters, "Loại khách", new[] { "Tất cả", "Khách", "Giao hàng", "Dịch vụ", "Gia đình", "Khác" }, 186, 150);
        var statusFilter = AddVisitorFilter(filters, "Trạng thái", new[] { "Tất cả", "Chờ duyệt", "Đang trong tòa", "Từ chối", "Đã rời" }, 356, 166);
        var apartmentFilter = AddVisitorFilter(filters, "Căn hộ", BuildFilterOptions(visitors.Select(v => v.ApartmentCode)), 542, 150);
        var search = ModernUi.SearchBox("Tìm khách, căn hộ, số điện thoại...", 260, 34);
        filters.Controls.Add(search);
        var searchInput = search.Controls.OfType<TextBox>().First();
        var refreshButton = ModernUi.OutlineButton("Làm mới", 108, 34);
        filters.Controls.Add(refreshButton);
        int filterHeight = LayoutSearchWithRefresh(search, refreshButton, w, 84, 38, 712);
        filters.Height = filterHeight;
        page.Controls.Add(filters);

        y += filterHeight + 14;
        var frameWidths = DashboardListDetailWidths(w, 0.60f);
        int leftW = frameWidths.LeftWidth;
        int rightW = frameWidths.RightWidth;
        const int mainHeight = 506;

        var list = ModernUi.Section("Danh sách khách ra vào (0)", leftW, mainHeight);
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

        var detail = ModernUi.Section("Chi tiết khách ra vào", rightW, mainHeight);
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

        ComboBox AddVisitorFilter(Control parent, string label, string[] options, int x, int width)
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
            int targetId = selectedId > 0 ? selectedId : selectedVisitor?.VisitorID ?? 0;
            visitors = LoadVisitorRows(false);
            residents = ResidentDAL.GetAllResidents();
            selectedVisitor = targetId > 0
                ? visitors.FirstOrDefault(v => v.VisitorID == targetId)
                : visitors.FirstOrDefault();
            ApplyFilters();
        }

        void ApplyFilters()
        {
            string dateRange = dateFilter.Text;
            string visitorType = typeFilter.Text;
            string status = statusFilter.Text;
            string apartment = apartmentFilter.Text;
            string keyword = searchInput.Text.Trim();

            filteredVisitors = visitors
                .Where(v => VisitorInDateRange(v, dateRange))
                .Where(v => visitorType == "Tất cả" || v.VisitorType == visitorType)
                .Where(v => status == "Tất cả" || v.Status == status)
                .Where(v => apartment == "Tất cả" || v.ApartmentCode == apartment)
                .Where(v => keyword.Length == 0 ||
                    VisitorCode(v).Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    v.VisitorName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    v.Phone.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    v.IDNumber.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    v.ResidentName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    v.ApartmentCode.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                    v.Purpose.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                .OrderByDescending(v => v.ArrivalTime)
                .ToList();

            if (selectedVisitor == null || (selectedVisitor.VisitorID > 0 && !filteredVisitors.Any(v => v.VisitorID == selectedVisitor.VisitorID)))
            {
                selectedVisitor = filteredVisitors.FirstOrDefault();
            }

            RefreshGrid();
            RenderDetail();
        }

        void RefreshGrid()
        {
            var table = new DataTable();
            table.Columns.Add("Chọn", typeof(bool));
            table.Columns.Add("Mã phiếu");
            table.Columns.Add("Khách");
            table.Columns.Add("Liên hệ");
            table.Columns.Add("Căn hộ");
            table.Columns.Add("Cư dân");
            table.Columns.Add("Loại");
            table.Columns.Add("Giờ vào");
            table.Columns.Add("Giờ ra");
            table.Columns.Add("Trạng thái");

            for (int i = 0; i < filteredVisitors.Count; i++)
            {
                var visitor = filteredVisitors[i];
                table.Rows.Add(
                    selectedVisitor != null && visitor.VisitorID == selectedVisitor.VisitorID,
                    VisitorCode(visitor),
                    visitor.VisitorName,
                    Display(visitor.Phone),
                    Display(visitor.ApartmentCode),
                    Display(visitor.ResidentName),
                    visitor.VisitorType,
                    DateTimeText(visitor.ArrivalTime),
                    DateTimeText(visitor.DepartureTime),
                    visitor.Status);
            }

            suppressGridSelection = true;
            grid.DataSource = table;
            if (grid.Columns.Count > 0)
            {
                int[] widths = { 46, 108, 150, 105, 78, 130, 88, 100, 90, 110 };
                for (int i = 0; i < Math.Min(widths.Length, grid.Columns.Count); i++)
                {
                    grid.Columns[i].Width = widths[i];
                    grid.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                    grid.Columns[i].DefaultCellStyle.Alignment = i is 2 or 5
                        ? DataGridViewContentAlignment.MiddleLeft
                        : DataGridViewContentAlignment.MiddleCenter;
                }
            }

            int selectedIndex = selectedVisitor == null ? -1 : filteredVisitors.FindIndex(v => v.VisitorID == selectedVisitor.VisitorID);
            if (selectedIndex >= 0 && selectedIndex < grid.Rows.Count)
            {
                grid.ClearSelection();
                grid.Rows[selectedIndex].Selected = true;
                grid.CurrentCell = grid.Rows[selectedIndex].Cells[Math.Min(1, grid.Columns.Count - 1)];
            }
            suppressGridSelection = false;

            if (listTitle != null)
            {
                listTitle.Text = $"DANH SÁCH KHÁCH RA VÀO ({filteredVisitors.Count})";
            }
            paging.Text = filteredVisitors.Count == 0
                ? "Không có khách phù hợp bộ lọc"
                : $"Hiển thị 1 - {filteredVisitors.Count:N0} / {visitors.Count:N0} lượt khách";
            RefreshBottom();
        }

        void RenderDetail()
        {
            detailBody.Controls.Clear();
            if (selectedVisitor == null)
            {
                var empty = ModernUi.Label("Chọn một lượt khách để xem chi tiết, hoặc bấm Đăng ký khách để tạo phiếu mới.", 10f, FontStyle.Regular, ModernUi.Muted);
                empty.SetBounds(12, 20, detailBody.Width - 24, 42);
                detailBody.Controls.Add(empty);
                return;
            }

            bool isNew = selectedVisitor.VisitorID <= 0;

            if (!isNew && !editMode)
            {
                var codeLabel = ModernUi.Label(VisitorCode(selectedVisitor), 10.5f, FontStyle.Bold, ModernUi.Blue);
                codeLabel.SetBounds(18, 42, detailBody.Width - 190, 24);
                detailBody.Controls.Add(codeLabel);

                var statusBadge = ModernUi.Badge(selectedVisitor.Status, VisitorStatusColor(selectedVisitor.Status));
                statusBadge.SetBounds(Math.Max(18, detailBody.Width - 166), 40, 148, 28);
                detailBody.Controls.Add(statusBadge);

                int colW = Math.Max(132, (detailBody.Width - 54) / 2);
                int leftX = 18;
                int rightX = leftX + colW + 18;
                int infoY = 86;

                AddVisitorInfo(detailBody, "Khách", selectedVisitor.VisitorName, leftX, infoY, colW);
                AddVisitorInfo(detailBody, "Liên hệ", selectedVisitor.Phone, rightX, infoY, detailBody.Width - rightX - 18);
                infoY += 58;
                AddVisitorInfo(detailBody, "Căn hộ", selectedVisitor.ApartmentCode, leftX, infoY, colW);
                AddVisitorInfo(detailBody, "Cư dân", selectedVisitor.ResidentName, rightX, infoY, detailBody.Width - rightX - 18);
                infoY += 58;
                AddVisitorInfo(detailBody, "Loại khách", selectedVisitor.VisitorType, leftX, infoY, colW);
                AddVisitorInfo(detailBody, "CCCD / giấy tờ", selectedVisitor.IDNumber, rightX, infoY, detailBody.Width - rightX - 18);
                infoY += 58;
                AddVisitorInfo(detailBody, "Thời gian vào", DateTimeText(selectedVisitor.ArrivalTime), leftX, infoY, colW);
                AddVisitorInfo(detailBody, "Thời gian ra", DateTimeText(selectedVisitor.DepartureTime), rightX, infoY, detailBody.Width - rightX - 18);

                var purposeLabel = ModernUi.Label("Mục đích", 8.2f, FontStyle.Bold, ModernUi.Muted);
                purposeLabel.SetBounds(18, infoY + 58, detailBody.Width - 36, 16);
                detailBody.Controls.Add(purposeLabel);

                var purposeValue = ModernUi.Label(Display(selectedVisitor.Purpose), 9.2f, FontStyle.Regular, ModernUi.Text);
                purposeValue.SetBounds(18, infoY + 78, detailBody.Width - 36, 48);
                purposeValue.AutoEllipsis = true;
                detailBody.Controls.Add(purposeValue);

                int buttonW = Math.Max(104, (detailBody.Width - 44) / 2);
                int buttonY = detailBody.Height - 80;

                var update = ModernUi.Button("Cập nhật", ModernUi.Blue, buttonW, 32);
                update.Location = new Point(18, buttonY);
                detailBody.Controls.Add(update);

                var checkoutButton = ModernUi.Button("Ghi nhận ra", ModernUi.Green, buttonW, 32);
                checkoutButton.Location = new Point(update.Right + 8, buttonY);
                detailBody.Controls.Add(checkoutButton);

                var rejectButton = ModernUi.Button("Từ chối", ModernUi.Orange, buttonW, 32);
                rejectButton.Location = new Point(18, buttonY + 40);
                detailBody.Controls.Add(rejectButton);

                var deleteButton = ModernUi.Button("Xóa", ModernUi.Red, buttonW, 32);
                deleteButton.Location = new Point(rejectButton.Right + 8, buttonY + 40);
                detailBody.Controls.Add(deleteButton);

                update.Click += (_, _) =>
                {
                    editMode = true;
                    RenderDetail();
                };

                checkoutButton.Enabled = selectedVisitor.StatusValue == "Approved" && !selectedVisitor.DepartureTime.HasValue;
                rejectButton.Enabled = selectedVisitor.StatusValue != "Rejected" && !selectedVisitor.DepartureTime.HasValue;

                checkoutButton.Click += (_, _) => CheckoutSelectedVisitor();
                rejectButton.Click += (_, _) => RejectSelectedVisitor();
                deleteButton.Click += (_, _) => DeleteSelectedVisitor();

                return;
            }

            var code = ModernUi.Label(isNew ? "Phiếu mới" : VisitorCode(selectedVisitor), 13f, FontStyle.Bold, ModernUi.Navy);
            code.SetBounds(4, 2, Math.Max(130, detailBody.Width - 170), 30);
            detailBody.Controls.Add(code);

            var badge = ModernUi.Badge(selectedVisitor.Status, VisitorStatusColor(selectedVisitor.Status));
            badge.SetBounds(detailBody.Width - 148, 4, 144, 26);
            detailBody.Controls.Add(badge);

            int rowY = 40;
            var resident = AddVisitorResidentCombo(detailBody, "Cư dân / căn hộ", residents, selectedVisitor.ResidentID, 4, rowY, detailBody.Width - 8);
            bool canEdit = isNew || editMode;
            resident.Enabled = canEdit;
            rowY += 48;

            int halfW = (detailBody.Width - 20) / 2;
            var name = AddVisitorInput(detailBody, "Tên khách", selectedVisitor.VisitorName, 4, rowY, halfW);
            var phone = AddVisitorInput(detailBody, "Điện thoại", selectedVisitor.Phone, name.Right + 12, rowY, detailBody.Width - name.Right - 16);
            rowY += 48;
            var email = AddVisitorInput(detailBody, "Email", selectedVisitor.Email, 4, rowY, halfW);
            var idNumber = AddVisitorInput(detailBody, "CCCD / giấy tờ", selectedVisitor.IDNumber, email.Right + 12, rowY, detailBody.Width - email.Right - 16);
            rowY += 48;
            var type = AddVisitorValueCombo(detailBody, "Loại khách", VisitorTypeOptions(), selectedVisitor.VisitorTypeValue, 4, rowY, halfW);
            var arrival = AddVisitorInput(detailBody, "Thời gian vào", DateTimeText(selectedVisitor.ArrivalTime), type.Right + 12, rowY, detailBody.Width - type.Right - 16);
            rowY += 48;
            var purpose = AddVisitorInput(detailBody, "Mục đích", selectedVisitor.Purpose, 4, rowY, detailBody.Width - 8, true);
            rowY += 74;
            var note = AddVisitorInput(detailBody, "Ghi chú", selectedVisitor.Note, 4, rowY, detailBody.Width - 8);

            foreach (var input in new[] { name, phone, email, idNumber, arrival, purpose, note })
            {
                input.ReadOnly = !canEdit;
            }
            type.Enabled = canEdit;

            int formButtonY = detailBody.Height - 78;
            int formButtonW = Math.Max(88, (detailBody.Width - 24) / 3);

            var save = ModernUi.Button(isNew ? "Lưu phiếu" : "Cập nhật", ModernUi.Blue, formButtonW, 32);
            var approve = ModernUi.Button("Duyệt", ModernUi.Green, formButtonW, 32);
            var rejectFormButton = ModernUi.Button("Từ chối", ModernUi.Red, formButtonW, 32);

            save.Location = new Point(4, formButtonY);
            approve.Location = new Point(save.Right + 8, formButtonY);
            rejectFormButton.Location = new Point(approve.Right + 8, formButtonY);

            detailBody.Controls.Add(save);
            detailBody.Controls.Add(approve);
            detailBody.Controls.Add(rejectFormButton);

            var checkout = ModernUi.Button("Ghi nhận ra", ModernUi.Orange, formButtonW, 32);
            var delete = ModernUi.OutlineButton("Xóa", formButtonW, 32);

            checkout.Location = new Point(4, formButtonY + 40);
            delete.Location = new Point(checkout.Right + 8, formButtonY + 40);
            delete.ForeColor = ModernUi.Text;
            detailBody.Controls.Add(checkout);
            detailBody.Controls.Add(delete);

            save.Enabled = canEdit;
            approve.Enabled = !isNew && selectedVisitor.StatusValue == "Pending";
            rejectFormButton.Enabled = !isNew && selectedVisitor.StatusValue != "Rejected" && !selectedVisitor.DepartureTime.HasValue;
            checkout.Enabled = !isNew && selectedVisitor.StatusValue == "Approved" && !selectedVisitor.DepartureTime.HasValue;
            delete.Enabled = !isNew;

            save.Click += (_, _) =>
            {
                if (editMode && selectedVisitor != null && selectedVisitor.VisitorID > 0)
                {
                    int selectedResidentId = resident.GetSelectedValueInt();
                    var selectedResident = residents.FirstOrDefault(r => r.ResidentID == selectedResidentId);

                    selectedVisitor.ResidentID = selectedResidentId;

                    if (selectedResident != null)
                    {
                        selectedVisitor.ResidentName = Display(selectedResident.FullName);
                        selectedVisitor.ApartmentCode = Display(selectedResident.ApartmentCode);
                        selectedVisitor.ApartmentID = selectedResident.ApartmentID;
                        selectedVisitor.BuildingName = !string.IsNullOrWhiteSpace(selectedResident.ApartmentCode)
                            ? $"Tòa {selectedResident.ApartmentCode[0]}"
                            : selectedVisitor.BuildingName;
                    }

                    selectedVisitor.VisitorName = name.Text.Trim();
                    selectedVisitor.Phone = phone.Text.Trim();
                    selectedVisitor.Email = email.Text.Trim();
                    selectedVisitor.IDNumber = idNumber.Text.Trim();

                    string typeValue = ComboBoxHelper.GetSelectedValueString(type);
                    selectedVisitor.VisitorTypeValue = string.IsNullOrWhiteSpace(typeValue) ? "Guest" : typeValue;
                    selectedVisitor.VisitorType = VisitorTypeText(selectedVisitor.VisitorTypeValue);

                    DateTime? parsedArrival = ParseVisitorDateTime(arrival.Text);
                    if (parsedArrival.HasValue)
                    {
                        selectedVisitor.ArrivalTime = parsedArrival.Value;
                    }

                    selectedVisitor.Purpose = purpose.Text.Trim();
                    selectedVisitor.Note = note.Text.Trim();
                    selectedVisitor.UpdatedAt = DateTime.Now;

                    editMode = false;
                    ApplyFilters();
                    return;
                }

                int savedId = SaveVisitorFromInputs(
                    resident.GetSelectedValueInt(),
                    name.Text,
                    phone.Text,
                    email.Text,
                    idNumber.Text,
                    ComboBoxHelper.GetSelectedValueString(type),
                    arrival.Text,
                    purpose.Text,
                    note.Text);

                if (savedId > 0)
                {
                    ReloadData(savedId);
                }
            };

            approve.Click += (_, _) => ApproveSelectedVisitor();
            rejectFormButton.Click += (_, _) => RejectSelectedVisitor();
            checkout.Click += (_, _) => CheckoutSelectedVisitor();
            delete.Click += (_, _) => DeleteSelectedVisitor();
        }

        int SaveVisitorFromInputs(
            int residentId,
            string visitorName,
            string phone,
            string email,
            string idNumber,
            string visitorType,
            string arrivalText,
            string purpose,
            string note)
        {
            if (residentId <= 0)
            {
                MessageBox.Show("Vui lòng chọn cư dân / căn hộ.", "Khách ra vào", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }

            DateTime? parsedArrival = ParseVisitorDateTime(arrivalText);
            if (!parsedArrival.HasValue)
            {
                MessageBox.Show("Thời gian vào không hợp lệ. Vui lòng nhập dạng dd/MM/yyyy HH:mm.", "Khách ra vào", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }

            string safePurpose = string.IsNullOrWhiteSpace(purpose) ? "Khách ra vào" : purpose.Trim();
            var result = VisitorBLL.RegisterVisitor(
                residentId,
                visitorName.Trim(),
                phone.Trim(),
                email.Trim(),
                idNumber.Trim(),
                string.IsNullOrWhiteSpace(visitorType) ? "Guest" : visitorType,
                safePurpose,
                parsedArrival.Value,
                note.Trim());

            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Khách ra vào", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }

            MessageBox.Show("Đã tạo phiếu khách. Phiếu đang chờ duyệt.", "Khách ra vào", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return result.VisitorID;
        }

        void AddVisitor()
        {
            editMode = false;
            selectedVisitor = BuildVisitorDraft(residents.FirstOrDefault());
            RenderDetail();
        }

        void ApproveSelectedVisitor()
        {
            if (selectedVisitor == null || selectedVisitor.VisitorID <= 0)
            {
                MessageBox.Show("Vui lòng chọn lượt khách.", "Duyệt khách", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selectedVisitor.StatusValue = "Approved";
            selectedVisitor.Status = "Đang trong tòa";
            selectedVisitor.DepartureTime = null;
            selectedVisitor.UpdatedAt = DateTime.Now;

            ApplyFilters();
        }

        void RejectSelectedVisitor()
        {
            if (selectedVisitor == null || selectedVisitor.VisitorID <= 0)
            {
                MessageBox.Show("Vui lòng chọn lượt khách.", "Từ chối khách", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Từ chối lượt khách {selectedVisitor.VisitorName}?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            selectedVisitor.StatusValue = "Rejected";
            selectedVisitor.Status = "Từ chối";
            selectedVisitor.DepartureTime = null;
            selectedVisitor.UpdatedAt = DateTime.Now;

            ApplyFilters();
        }

        void CheckoutSelectedVisitor()
        {
            if (selectedVisitor == null || selectedVisitor.VisitorID <= 0)
            {
                MessageBox.Show("Vui lòng chọn lượt khách.", "Ghi nhận ra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selectedVisitor.StatusValue = "CheckedOut";
            selectedVisitor.Status = "Đã rời";
            selectedVisitor.DepartureTime = DateTime.Now;
            selectedVisitor.UpdatedAt = DateTime.Now;

            ApplyFilters();
        }

        void DeleteSelectedVisitor()
        {
            if (selectedVisitor == null || selectedVisitor.VisitorID <= 0)
            {
                MessageBox.Show("Vui lòng chọn lượt khách.", "Xóa khách", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Xóa phiếu khách {selectedVisitor.VisitorName}?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            visitors.Remove(selectedVisitor);
            selectedVisitor = visitors.FirstOrDefault();

            ApplyFilters();
        }

        void RefreshBottom()
        {
            foreach (var panel in new[] { stats, typeChart, statusChart })
            {
                foreach (Control child in panel.Controls.Cast<Control>().Skip(1).ToList())
                {
                    panel.Controls.Remove(child);
                    child.Dispose();
                }
            }

            int miniW = Math.Max(92, (stats.Width - 52) / 3);
            AddVisitorMiniStat(stats, "Tổng lượt", visitors.Count, ModernUi.Blue, 16, 46, miniW, 72);
            AddVisitorMiniStat(stats, "Hôm nay", visitors.Count(v => v.ArrivalTime.Date == DateTime.Today), ModernUi.Teal, 28 + miniW, 46, miniW, 72);
            AddVisitorMiniStat(stats, "Chờ duyệt", visitors.Count(v => v.StatusValue == "Pending"), ModernUi.Orange, 40 + miniW * 2, 46, miniW, 72);
            AddVisitorMiniStat(stats, "Trong tòa", visitors.Count(v => v.StatusValue == "Approved" && !v.DepartureTime.HasValue), ModernUi.Green, 16, 130, miniW, 72);
            AddVisitorMiniStat(stats, "Đã rời", visitors.Count(v => v.DepartureTime.HasValue || v.StatusValue == "CheckedOut"), ModernUi.Muted, 28 + miniW, 130, miniW, 72);

            var byType = new BarChartPanel
            {
                Location = new Point(12, 42),
                Size = new Size(typeChart.Width - 24, 160),
                BarColor = ModernUi.Teal,
                ShowValueLabels = true,
                GridSteps = 4
            };
            byType.Bars.AddRange(visitors.GroupBy(v => v.VisitorType).OrderBy(g => g.Key).Select(g => (g.Key, g.Count())));
            byType.AxisMax = Math.Max(1, byType.Bars.Count == 0 ? 1 : byType.Bars.Max(b => b.Value) + 1);
            typeChart.Controls.Add(byType);

            var byStatus = new BarChartPanel
            {
                Location = new Point(12, 42),
                Size = new Size(statusChart.Width - 24, 160),
                BarColor = ModernUi.Orange,
                ShowValueLabels = true,
                GridSteps = 4
            };
            byStatus.Bars.AddRange(visitors.GroupBy(v => v.Status).OrderBy(g => g.Key).Select(g => (g.Key, g.Count())));
            byStatus.AxisMax = Math.Max(1, byStatus.Bars.Count == 0 ? 1 : byStatus.Bars.Max(b => b.Value) + 1);
            statusChart.Controls.Add(byStatus);
        }

        dateFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        typeFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        statusFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        apartmentFilter.SelectedIndexChanged += (_, _) => ApplyFilters();
        searchInput.TextChanged += (_, _) => ApplyFilters();
        refreshButton.Click += (_, _) =>
        {
            dateFilter.SelectedIndex = 0;
            typeFilter.SelectedIndex = 0;
            statusFilter.SelectedIndex = 0;
            apartmentFilter.SelectedIndex = 0;
            searchInput.Clear();
            ReloadData();
        };
        grid.SelectionChanged += (_, _) =>
        {
            if (suppressGridSelection)
            {
                return;
            }

            if (grid.CurrentRow?.Index >= 0 && grid.CurrentRow.Index < filteredVisitors.Count)
            {
                editMode = false;
                selectedVisitor = filteredVisitors[grid.CurrentRow.Index];
                RefreshGrid();
                RenderDetail();
            }
        };

        addButton.Click += (_, _) => AddVisitor();
        approveButton.Click += (_, _) => ApproveSelectedVisitor();
        checkoutActionButton.Click += (_, _) => CheckoutSelectedVisitor();
        exportButton.Click += (_, _) => ExportVisitorCsv(visitors);

        ApplyFilters();
        if (ConsumeQuickAction("visitors", "add"))
        {
            AddVisitor();
        }
        page.AutoScroll = true;
        page.AutoScrollMinSize = new Size(0, y + 250);
    }

    private void RenderResidentVisitors()
    {
        var resident = GetCurrentResident();
        var page = BeginPage("Khách của tôi", resident == null ? "" : $"Căn hộ {Display(resident.ApartmentCode)}");
        int w = PageWorkWidth();
        int y = 86;

        if (resident == null)
        {
            var empty = ModernUi.Section("Không tìm thấy hồ sơ cư dân", w, 180);
            empty.Location = new Point(18, y);
            var text = ModernUi.Label("Tài khoản này chưa được liên kết với hồ sơ cư dân nên chưa thể đăng ký khách ra vào.", 10f, FontStyle.Regular, ModernUi.Text);
            text.SetBounds(18, 58, empty.Width - 36, 48);
            empty.Controls.Add(text);
            page.Controls.Add(empty);
            return;
        }

        var visitors = LoadVisitorRows(true);
        VisitorViewModel selectedVisitor = visitors.FirstOrDefault();
        bool suppressGridSelection = false;

        int formW = Math.Max(440, (int)(w * 0.43));
        int listW = w - formW - 12;
        var form = ModernUi.Section("Đăng ký khách", formW, 406);
        form.Location = new Point(18, y);
        page.Controls.Add(form);

        int halfW = (form.Width - 54) / 2;
        var type = AddVisitorValueCombo(form, "Loại khách", VisitorTypeOptions(), "Guest", 18, 52, halfW);
        var visitorName = AddVisitorInput(form, "Tên khách", "", type.Right + 18, 52, form.Width - type.Right - 36);
        var phone = AddVisitorInput(form, "Điện thoại", "", 18, 104, halfW);
        var email = AddVisitorInput(form, "Email", "", phone.Right + 18, 104, form.Width - phone.Right - 36);
        var idNumber = AddVisitorInput(form, "CCCD / giấy tờ", "", 18, 156, halfW);
        var arrival = AddVisitorInput(form, "Thời gian vào", DateTime.Now.AddHours(1).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture), idNumber.Right + 18, 156, form.Width - idNumber.Right - 36);
        var purpose = AddVisitorInput(form, "Mục đích", "", 18, 208, form.Width - 36, true);
        var note = AddVisitorInput(form, "Ghi chú", "", 18, 282, form.Width - 36);
        var submit = ModernUi.Button("Gửi đăng ký", ModernUi.Blue, 136, 34);
        submit.Location = new Point(18, 348);
        form.Controls.Add(submit);

        var list = ModernUi.Section($"Khách đã đăng ký ({visitors.Count})", listW, 406);
        list.Location = new Point(form.Right + 12, y);
        var listTitle = list.Controls.OfType<Label>().FirstOrDefault();
        var grid = ModernUi.Grid();
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 318);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ScrollBars = ScrollBars.Both;
        grid.RowTemplate.Height = 34;
        grid.CellFormatting += (_, e) => ApplyGridCellStyle(e);
        list.Controls.Add(grid);
        page.Controls.Add(list);

        y += 424;
        var detail = ModernUi.Section("Chi tiết lượt khách", w, 238);
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
            visitors = LoadVisitorRows(true);
            selectedVisitor = selectedId > 0
                ? visitors.FirstOrDefault(v => v.VisitorID == selectedId)
                : visitors.FirstOrDefault();
            RefreshResidentGrid();
            RenderResidentDetail();
        }

        void RefreshResidentGrid()
        {
            var table = new DataTable();
            table.Columns.Add("Mã phiếu");
            table.Columns.Add("Khách");
            table.Columns.Add("Loại");
            table.Columns.Add("Giờ vào");
            table.Columns.Add("Giờ ra");
            table.Columns.Add("Trạng thái");

            foreach (var visitor in visitors)
            {
                table.Rows.Add(
                    VisitorCode(visitor),
                    visitor.VisitorName,
                    visitor.VisitorType,
                    DateTimeText(visitor.ArrivalTime),
                    DateTimeText(visitor.DepartureTime),
                    visitor.Status);
            }

            suppressGridSelection = true;
            grid.DataSource = table;
            if (grid.Columns.Count > 0)
            {
                int[] widths = { 108, 154, 100, 132, 132, 120 };
                for (int i = 0; i < Math.Min(widths.Length, grid.Columns.Count); i++)
                {
                    grid.Columns[i].Width = widths[i];
                    grid.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                    grid.Columns[i].DefaultCellStyle.Alignment = i == 1
                        ? DataGridViewContentAlignment.MiddleLeft
                        : DataGridViewContentAlignment.MiddleCenter;
                }
            }

            int selectedIndex = selectedVisitor == null ? -1 : visitors.FindIndex(v => v.VisitorID == selectedVisitor.VisitorID);
            if (selectedIndex >= 0 && selectedIndex < grid.Rows.Count)
            {
                grid.ClearSelection();
                grid.Rows[selectedIndex].Selected = true;
                grid.CurrentCell = grid.Rows[selectedIndex].Cells[Math.Min(1, grid.Columns.Count - 1)];
            }

            suppressGridSelection = false;
            if (listTitle != null)
            {
                listTitle.Text = $"KHÁCH ĐÃ ĐĂNG KÝ ({visitors.Count})";
            }
        }

        void RenderResidentDetail()
        {
            detailBody.Controls.Clear();
            if (selectedVisitor == null)
            {
                var empty = ModernUi.Label("Bạn chưa có lượt khách nào được đăng ký.", 10f, FontStyle.Regular, ModernUi.Muted);
                empty.SetBounds(18, 18, detailBody.Width - 36, 26);
                detailBody.Controls.Add(empty);
                return;
            }

            var badge = ModernUi.Badge(selectedVisitor.Status, VisitorStatusColor(selectedVisitor.Status));
            badge.SetBounds(18, 14, 142, 28);
            detailBody.Controls.Add(badge);

            int x = 18;
            int yInfo = 58;
            int colW = Math.Max(160, (detailBody.Width - 54) / 3);
            AddVisitorInfo(detailBody, "Mã phiếu", VisitorCode(selectedVisitor), x, yInfo, colW);
            AddVisitorInfo(detailBody, "Tên khách", selectedVisitor.VisitorName, x + colW, yInfo, colW);
            AddVisitorInfo(detailBody, "Loại khách", selectedVisitor.VisitorType, x + colW * 2, yInfo, colW);
            AddVisitorInfo(detailBody, "Thời gian vào", DateTimeText(selectedVisitor.ArrivalTime), x, yInfo + 62, colW);
            AddVisitorInfo(detailBody, "Thời gian ra", DateTimeText(selectedVisitor.DepartureTime), x + colW, yInfo + 62, colW);
            AddVisitorInfo(detailBody, "Liên hệ", Display(selectedVisitor.Phone), x + colW * 2, yInfo + 62, colW);

            var purposeLabel = ModernUi.Label($"Mục đích: {Display(selectedVisitor.Purpose)}", 9.2f, FontStyle.Regular, ModernUi.Text);
            purposeLabel.SetBounds(18, yInfo + 124, detailBody.Width - 36, 26);
            purposeLabel.AutoEllipsis = true;
            detailBody.Controls.Add(purposeLabel);
        }

        submit.Click += (_, _) =>
        {
            DateTime? parsedArrival = ParseVisitorDateTime(arrival.Text);
            if (!parsedArrival.HasValue)
            {
                MessageBox.Show("Thời gian vào không hợp lệ. Vui lòng nhập dạng dd/MM/yyyy HH:mm.", "Đăng ký khách", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string safePurpose = string.IsNullOrWhiteSpace(purpose.Text) ? "Khách ra vào" : purpose.Text.Trim();
            var result = VisitorBLL.RegisterVisitor(
                resident.ResidentID,
                visitorName.Text.Trim(),
                phone.Text.Trim(),
                email.Text.Trim(),
                idNumber.Text.Trim(),
                ComboBoxHelper.GetSelectedValueString(type),
                safePurpose,
                parsedArrival.Value,
                note.Text.Trim());

            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Đăng ký khách", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show("Đã gửi đăng ký khách. Ban quản lý sẽ duyệt trước khi khách vào tòa.", "Đăng ký khách", MessageBoxButtons.OK, MessageBoxIcon.Information);
            visitorName.Clear();
            phone.Clear();
            email.Clear();
            idNumber.Clear();
            purpose.Clear();
            note.Clear();
            arrival.Text = DateTime.Now.AddHours(1).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            ReloadResidentData(result.VisitorID);
        };

        grid.SelectionChanged += (_, _) =>
        {
            if (suppressGridSelection)
            {
                return;
            }

            if (grid.CurrentRow?.Index >= 0 && grid.CurrentRow.Index < visitors.Count)
            {
                selectedVisitor = visitors[grid.CurrentRow.Index];
                RefreshResidentGrid();
                RenderResidentDetail();
            }
        };

        RefreshResidentGrid();
        RenderResidentDetail();
        page.AutoScroll = true;
        page.AutoScrollMinSize = new Size(0, y + 280);
    }

    private List<VisitorViewModel> LoadVisitorRows(bool currentResidentOnly)
    {
        ResidentDTO resident = null;
        if (currentResidentOnly)
        {
            resident = GetCurrentResident();
            if (resident == null)
            {
                return new List<VisitorViewModel>();
            }
        }

        var rows = currentResidentOnly
            ? VisitorDAL.GetVisitorsByResident(resident.ResidentID)
            : VisitorDAL.GetAllVisitors();

        var mapped = rows
            .Select(MapVisitorRow)
            .OrderByDescending(v => v.ArrivalTime)
            .ThenBy(v => v.ApartmentCode)
            .ThenBy(v => v.VisitorName)
            .ToList();

        if (!currentResidentOnly && mapped.Count == 0)
        {
            return BuildVisitorDemoRows();
        }

        return mapped;
    }

    private VisitorViewModel MapVisitorRow(dynamic row)
    {
        int visitorId = VisitorDynamicInt(row, "VisitorID");
        DateTime createdAt = VisitorDynamicDate(row, "CreatedAt") ?? DateTime.Today;
        DateTime arrivalTime = VisitorDynamicDate(row, "ArrivalTime", "CheckInTime") ?? createdAt;
        DateTime? departureTime = VisitorDynamicDate(row, "DepartureTime", "CheckOutTime");
        if (departureTime.HasValue && departureTime.Value <= DateTime.MinValue.AddDays(1))
        {
            departureTime = null;
        }

        string apartment = VisitorDynamicString(row, "ApartmentCode");
        string building = VisitorDynamicString(row, "BuildingName");
        if (string.IsNullOrWhiteSpace(building) && apartment.Length > 0)
        {
            building = $"Tòa {apartment[0]}";
        }

        string visitorTypeValue = VisitorTypeDbValue(VisitorDynamicString(row, "VisitorType"));
        string statusValue = VisitorStatusDbValue(VisitorDynamicString(row, "Status"), departureTime);

        return new VisitorViewModel
        {
            VisitorID = visitorId,
            ResidentID = VisitorDynamicInt(row, "ResidentID"),
            ResidentName = VisitorDynamicString(row, "ResidentName", "FullName"),
            ApartmentID = VisitorDynamicInt(row, "ApartmentID"),
            ApartmentCode = string.IsNullOrWhiteSpace(apartment) ? "-" : apartment,
            BuildingName = string.IsNullOrWhiteSpace(building) ? "Chưa rõ" : building,
            VisitorName = VisitorDynamicString(row, "VisitorName"),
            Phone = VisitorDynamicString(row, "Phone"),
            Email = VisitorDynamicString(row, "Email"),
            IDNumber = VisitorDynamicString(row, "IDNumber"),
            VisitorTypeValue = visitorTypeValue,
            VisitorType = VisitorTypeText(visitorTypeValue),
            Purpose = VisitorDynamicString(row, "Purpose"),
            ArrivalTime = arrivalTime,
            DepartureTime = departureTime,
            StatusValue = statusValue,
            Status = VisitorStatusText(statusValue, departureTime),
            ApprovedBy = VisitorDynamicString(row, "ApprovedBy"),
            Note = VisitorDynamicString(row, "Note"),
            CreatedAt = createdAt,
            UpdatedAt = VisitorDynamicDate(row, "UpdatedAt") ?? createdAt
        };
    }

    private VisitorViewModel BuildVisitorDraft(ResidentDTO resident)
    {
        return new VisitorViewModel
        {
            VisitorID = 0,
            ResidentID = resident?.ResidentID ?? 0,
            ResidentName = Display(resident?.FullName, ""),
            ApartmentID = resident?.ApartmentID ?? 0,
            ApartmentCode = Display(resident?.ApartmentCode, ""),
            BuildingName = !string.IsNullOrWhiteSpace(resident?.ApartmentCode) ? $"Tòa {resident.ApartmentCode[0]}" : "",
            VisitorName = string.Empty,
            Phone = string.Empty,
            Email = string.Empty,
            IDNumber = string.Empty,
            VisitorTypeValue = "Guest",
            VisitorType = "Khách",
            Purpose = "Khách ra vào",
            ArrivalTime = DateTime.Now,
            StatusValue = "Pending",
            Status = "Chờ duyệt",
            Note = string.Empty,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    private static TextBox AddVisitorInput(Control parent, string label, string value, int x, int y, int width, bool multiline = false)
    {
        var lbl = ModernUi.Label(label + ":", 8.6f, FontStyle.Regular, ModernUi.Muted);
        lbl.SetBounds(x, y, width, 18);
        parent.Controls.Add(lbl);

        var input = ModernUi.TextBox("", width);
        input.Text = value ?? string.Empty;
        input.Multiline = multiline;
        input.ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None;
        input.SetBounds(x, y + 18, width, multiline ? 56 : 28);
        parent.Controls.Add(input);
        return input;
    }

    private static ComboBox AddVisitorValueCombo(Control parent, string label, IEnumerable<(string Text, string Value)> options, string selectedValue, int x, int y, int width)
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

    private static ComboBox AddVisitorResidentCombo(Control parent, string label, IEnumerable<ResidentDTO> residents, int selectedResidentId, int x, int y, int width)
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

    private static void AddVisitorInfo(Control parent, string label, string value, int x, int y, int width)
    {
        var caption = ModernUi.Label(label, 8.2f, FontStyle.Bold, ModernUi.Muted);
        caption.SetBounds(x, y, width, 16);
        parent.Controls.Add(caption);

        var text = ModernUi.Label(Display(value), 9.5f, FontStyle.Bold, ModernUi.Text);
        text.SetBounds(x, y + 18, width, 24);
        text.AutoEllipsis = true;
        parent.Controls.Add(text);
    }

    private static void AddVisitorMiniStat(Control parent, string title, int value, Color color, int x, int y, int width, int height)
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

    private static IEnumerable<(string Text, string Value)> VisitorTypeOptions()
    {
        yield return ("Khách", "Guest");
        yield return ("Giao hàng", "Delivery");
        yield return ("Dịch vụ", "Service");
        yield return ("Gia đình", "Family");
        yield return ("Khác", "Other");
    }

    private static string VisitorTypeText(string value)
    {
        return (value ?? string.Empty).Trim() switch
        {
            "Guest" => "Khách",
            "Delivery" => "Giao hàng",
            "Service" => "Dịch vụ",
            "Family" => "Gia đình",
            "Other" => "Khác",
            "" => "Khách",
            "-" => "Khách",
            var other => other
        };
    }

    private static string VisitorTypeDbValue(string value)
    {
        return (value ?? string.Empty).Trim() switch
        {
            "Khách" => "Guest",
            "Giao hàng" => "Delivery",
            "Dịch vụ" => "Service",
            "Gia đình" => "Family",
            "Khác" => "Other",
            "" => "Guest",
            "-" => "Guest",
            var other => other
        };
    }

    private static string VisitorStatusText(string value, DateTime? departureTime)
    {
        if (departureTime.HasValue)
        {
            return "Đã rời";
        }

        return (value ?? string.Empty).Trim() switch
        {
            "Pending" => "Chờ duyệt",
            "Approved" => "Đang trong tòa",
            "Rejected" => "Từ chối",
            "CheckedOut" => "Đã rời",
            "CheckedIn" => "Đang trong tòa",
            "Chờ duyệt" => "Chờ duyệt",
            "Đang trong tòa" => "Đang trong tòa",
            "Từ chối" => "Từ chối",
            "Đã rời" => "Đã rời",
            "" => "Chờ duyệt",
            "-" => "Chờ duyệt",
            var other => ViStatus(other)
        };
    }

    private static string VisitorStatusDbValue(string value, DateTime? departureTime = null)
    {
        if (departureTime.HasValue)
        {
            return "CheckedOut";
        }

        return (value ?? string.Empty).Trim() switch
        {
            "Chờ duyệt" => "Pending",
            "Đang trong tòa" => "Approved",
            "Từ chối" => "Rejected",
            "Đã rời" => "CheckedOut",
            "CheckedIn" => "Approved",
            "" => "Pending",
            "-" => "Pending",
            var other => other
        };
    }

    private static Color VisitorStatusColor(string status)
    {
        return status switch
        {
            "Đang trong tòa" => ModernUi.Green,
            "Chờ duyệt" => ModernUi.Orange,
            "Từ chối" => ModernUi.Red,
            "Đã rời" => ModernUi.Muted,
            _ => ModernUi.Muted
        };
    }

    private static bool VisitorInDateRange(VisitorViewModel visitor, string filter)
    {
        DateTime today = DateTime.Today;
        return filter switch
        {
            "Hôm nay" => visitor.ArrivalTime.Date == today,
            "7 ngày" => visitor.ArrivalTime.Date >= today.AddDays(-6),
            "30 ngày" => visitor.ArrivalTime.Date >= today.AddDays(-29),
            _ => true
        };
    }

    private static DateTime? ParseVisitorDateTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim() == "-")
        {
            return null;
        }

        string[] formats =
        {
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy H:mm",
            "dd/MM/yyyy",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-ddTHH:mm",
            "MM/dd/yyyy HH:mm"
        };

        if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exact))
        {
            return exact;
        }

        return DateTime.TryParse(value, out DateTime parsed) ? parsed : null;
    }

    private static string VisitorCode(VisitorViewModel visitor)
    {
        DateTime createdAt = visitor.CreatedAt > DateTime.MinValue ? visitor.CreatedAt : DateTime.Today;
        return visitor.VisitorID <= 0 ? "Mới" : $"KRA{createdAt:yyMMdd}-{visitor.VisitorID:000}";
    }

    private static string VisitorDynamicString(dynamic obj, params string[] names)
        => VisitorDynamicString(obj, names, string.Empty);

    private static string VisitorDynamicString(dynamic obj, string[] names, string fallback)
    {
        if (obj == null)
        {
            return fallback;
        }

        object source = obj;
        var type = source.GetType();
        foreach (var name in names)
        {
            var prop = type.GetProperty(name);
            if (prop == null)
            {
                continue;
            }

            var value = prop.GetValue(source);
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                return value.ToString();
            }
        }

        return fallback;
    }

    private static int VisitorDynamicInt(dynamic obj, string name, int fallback = 0)
    {
        if (obj == null)
        {
            return fallback;
        }

        object source = obj;
        var prop = source.GetType().GetProperty(name);
        if (prop == null)
        {
            return fallback;
        }

        var value = prop.GetValue(source);
        if (value == null)
        {
            return fallback;
        }

        string text = value.ToString();
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : fallback;
    }

    private static DateTime? VisitorDynamicDate(dynamic obj, params string[] names)
    {
        if (obj == null)
        {
            return null;
        }

        object source = obj;
        var type = source.GetType();
        foreach (var name in names)
        {
            var prop = type.GetProperty(name);
            if (prop == null)
            {
                continue;
            }

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

    private static void ExportVisitorCsv(IEnumerable<VisitorViewModel> visitors)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Xuất báo cáo khách ra vào",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"khach-ra-vao-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var lines = new List<string>
        {
            "Ma phieu,Khach,Dien thoai,Can ho,Cu dan,Loai,Gio vao,Gio ra,Trang thai,Muc dich"
        };

        lines.AddRange(visitors.Select(v => string.Join(",",
            Csv(VisitorCode(v)),
            Csv(v.VisitorName),
            Csv(v.Phone),
            Csv(v.ApartmentCode),
            Csv(v.ResidentName),
            Csv(v.VisitorType),
            Csv(DateTimeText(v.ArrivalTime)),
            Csv(DateTimeText(v.DepartureTime)),
            Csv(v.Status),
            Csv(v.Purpose))));

        File.WriteAllLines(dialog.FileName, lines);
        MessageBox.Show("Đã xuất báo cáo khách ra vào.", "Khách ra vào", MessageBoxButtons.OK, MessageBoxIcon.Information);

        static string Csv(string value)
        {
            value ??= string.Empty;
            return value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }
    }

    private static List<VisitorViewModel> BuildVisitorDemoRows()
    {
        DateTime today = DateTime.Today;

        return new List<VisitorViewModel>
    {
        new VisitorViewModel
        {
            VisitorID = 1,
            ResidentID = 1,
            ResidentName = "Nguyễn Văn An",
            ApartmentCode = "A-1205",
            BuildingName = "Tòa A",
            VisitorName = "Nguyễn Minh Hà",
            Phone = "0901 234 567",
            Email = "minhha@example.com",
            IDNumber = "079199012345",
            VisitorTypeValue = "Family",
            VisitorType = "Gia đình",
            Purpose = "Thăm thân",
            ArrivalTime = today.AddHours(8).AddMinutes(15),
            DepartureTime = today.AddHours(9).AddMinutes(45),
            StatusValue = "CheckedOut",
            Status = "Đã rời",
            CreatedAt = today
        },
        new VisitorViewModel
        {
            VisitorID = 2,
            ResidentID = 2,
            ResidentName = "Trần Thị Bình",
            ApartmentCode = "B-0803",
            BuildingName = "Tòa B",
            VisitorName = "Trần Quốc Bảo",
            Phone = "0932 111 222",
            Email = "bao@example.com",
            IDNumber = "079199012345",
            VisitorTypeValue = "Delivery",
            VisitorType = "Giao hàng",
            Purpose = "Giao hàng Shopee",
            ArrivalTime = today.AddHours(9).AddMinutes(5),
            DepartureTime = null,
            StatusValue = "Approved",
            Status = "Đang trong tòa",
            CreatedAt = today
        },
        new VisitorViewModel
        {
            VisitorID = 3,
            ResidentID = 3,
            ResidentName = "Lê Hoàng Nam",
            ApartmentCode = "C-0510",
            BuildingName = "Tòa C",
            VisitorName = "Lê Hồng Phúc",
            Phone = "0987 654 321",
            VisitorTypeValue = "Service",
            VisitorType = "Dịch vụ",
            Purpose = "Bảo trì điều hòa",
            ArrivalTime = today.AddHours(9).AddMinutes(20),
            DepartureTime = null,
            StatusValue = "Approved",
            Status = "Đang trong tòa",
            CreatedAt = today
        },
        new VisitorViewModel
        {
            VisitorID = 4,
            ResidentID = 1,
            ResidentName = "Nguyễn Văn An",
            ApartmentCode = "A-1205",
            BuildingName = "Tòa A",
            VisitorName = "Phạm Thu Trang",
            Phone = "0912 333 444",
            VisitorTypeValue = "Family",
            VisitorType = "Gia đình",
            Purpose = "Thăm thân",
            ArrivalTime = today.AddHours(10),
            DepartureTime = today.AddHours(11).AddMinutes(30),
            StatusValue = "CheckedOut",
            Status = "Đã rời",
            CreatedAt = today
        },
        new VisitorViewModel
        {
            VisitorID = 5,
            ResidentName = "Đỗ Mạnh Hùng",
            ApartmentCode = "B-1604",
            BuildingName = "Tòa B",
            VisitorName = "Đỗ Văn Hùng",
            Phone = "0909 888 777",
            VisitorTypeValue = "Service",
            VisitorType = "Dịch vụ",
            Purpose = "Sửa điện",
            ArrivalTime = today.AddHours(10).AddMinutes(15),
            DepartureTime = null,
            StatusValue = "Pending",
            Status = "Chờ duyệt",
            CreatedAt = today
        },
        new VisitorViewModel
        {
            VisitorID = 6,
            ResidentName = "Vũ Thị Mai",
            ApartmentCode = "A-0908",
            BuildingName = "Tòa A",
            VisitorName = "Nguyễn Thị Mai",
            Phone = "0983 222 111",
            VisitorTypeValue = "Delivery",
            VisitorType = "Giao hàng",
            Purpose = "Giao hàng",
            ArrivalTime = today.AddHours(10).AddMinutes(30),
            DepartureTime = today.AddHours(10).AddMinutes(50),
            StatusValue = "CheckedOut",
            Status = "Đã rời",
            CreatedAt = today
        },
        new VisitorViewModel
        {
            VisitorID = 7,
            ResidentName = "Trần Thị Bình",
            ApartmentCode = "B-0803",
            BuildingName = "Tòa B",
            VisitorName = "Lê Minh Quân",
            Phone = "0944 555 666",
            VisitorTypeValue = "Family",
            VisitorType = "Gia đình",
            Purpose = "Thăm thân",
            ArrivalTime = today.AddHours(11),
            DepartureTime = null,
            StatusValue = "Approved",
            Status = "Đang trong tòa",
            CreatedAt = today
        },
        new VisitorViewModel
        {
            VisitorID = 8,
            ResidentName = "Phạm Thu Hà",
            ApartmentCode = "C-1210",
            BuildingName = "Tòa C",
            VisitorName = "Công ty An Phú",
            Phone = "028 1234 5678",
            VisitorTypeValue = "Service",
            VisitorType = "Dịch vụ",
            Purpose = "Bảo trì thiết bị",
            ArrivalTime = today.AddHours(11).AddMinutes(30),
            DepartureTime = null,
            StatusValue = "Approved",
            Status = "Đang trong tòa",
            CreatedAt = today
        },
        new VisitorViewModel
        {
            VisitorID = 9,
            ResidentName = "Nguyễn Văn An",
            ApartmentCode = "A-1205",
            BuildingName = "Tòa A",
            VisitorName = "Ship nhanh 24h",
            Phone = "1900 8888",
            VisitorTypeValue = "Delivery",
            VisitorType = "Giao hàng",
            Purpose = "Giao hàng",
            ArrivalTime = today.AddHours(12),
            DepartureTime = today.AddHours(12).AddMinutes(10),
            StatusValue = "CheckedOut",
            Status = "Đã rời",
            CreatedAt = today
        },
        new VisitorViewModel
        {
            VisitorID = 10,
            ResidentName = "Hoàng Văn Dũng",
            ApartmentCode = "B-1510",
            BuildingName = "Tòa B",
            VisitorName = "Hoàng Văn Tùng",
            Phone = "0977 666 333",
            VisitorTypeValue = "Service",
            VisitorType = "Dịch vụ",
            Purpose = "Kiểm tra nước",
            ArrivalTime = today.AddHours(13).AddMinutes(15),
            DepartureTime = null,
            StatusValue = "Approved",
            Status = "Đang trong tòa",
            CreatedAt = today
        }
    };
    }

    private sealed class VisitorViewModel
    {
        public int VisitorID { get; set; }
        public int ResidentID { get; set; }
        public string ResidentName { get; set; } = string.Empty;
        public int ApartmentID { get; set; }
        public string ApartmentCode { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string VisitorName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string IDNumber { get; set; } = string.Empty;
        public string VisitorTypeValue { get; set; } = "Guest";
        public string VisitorType { get; set; } = "Khách";
        public string Purpose { get; set; } = string.Empty;
        public DateTime ArrivalTime { get; set; }
        public DateTime? DepartureTime { get; set; }
        public string StatusValue { get; set; } = "Pending";
        public string Status { get; set; } = "Chờ duyệt";
        public string ApprovedBy { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
